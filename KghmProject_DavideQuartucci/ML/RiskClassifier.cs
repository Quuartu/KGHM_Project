using System;
using System.Linq;
using Accord.MachineLearning;
using Accord.MachineLearning.Performance;
using Accord.Statistics.Models.Regression;
using Accord.Statistics.Models.Regression.Fitting;

namespace KghmProject_DavideQuartucci.ML
{
    /// <summary>
    /// Binary downside-risk classifier based on Accord.NET logistic regression. Adds
    /// k-fold cross-validation for L2 penalty selection and IRLS training on top of the
    /// shared lifecycle defined in <see cref="BaseClassifier"/>.
    /// </summary>
    public class RiskClassifier : BaseClassifier
    {
        private const int IrlsMaxIterations = 100;

        private readonly int _crossValidationFolds;
        private readonly double[] _l2PenaltyCandidates;

        private double? _selectedRegularization;
        private LogisticRegression? _model;

        /// <summary>L2 penalty selected by <see cref="TuneRegularization"/>.</summary>
        /// <exception cref="InvalidOperationException">Thrown when accessed before tuning has run.</exception>
        public double SelectedRegularization =>
            _selectedRegularization ?? throw new InvalidOperationException(
                "TuneRegularization must be called before the selected hyperparameter is available.");

        /// <param name="features">Standardized feature matrix, one row per observation.</param>
        /// <param name="targets">Binary target vector (0 = stable, 1 = downside risk).</param>
        /// <param name="testFraction">Fraction of observations reserved for the test set.</param>
        /// <param name="crossValidationFolds">Number of folds used by <see cref="TuneRegularization"/>.</param>
        /// <param name="l2PenaltyCandidates">L2 penalty values evaluated during tuning. Defaults to a log-spaced range.</param>
        /// <exception cref="ArgumentException">Thrown when the inputs are invalid.</exception>
        public RiskClassifier(
            double[][] features,
            int[] targets,
            double testFraction = 0.2,
            int crossValidationFolds = 5,
            double[]? l2PenaltyCandidates = null)
            : base(features, targets, testFraction)
        {
            if (crossValidationFolds < 2)
                throw new ArgumentException("At least 2 folds are required for cross-validation.", nameof(crossValidationFolds));

            _crossValidationFolds = crossValidationFolds;
            _l2PenaltyCandidates = l2PenaltyCandidates ?? new[] { 1e-4, 1e-3, 1e-2, 1e-1, 1.0, 10.0 };
        }

        /// <summary>
        /// Grid search over the L2 penalty candidates using k-fold cross-validation on the
        /// training set, selecting the value with the lowest average validation error.
        /// </summary>
        /// <returns>The selected L2 penalty.</returns>
        /// <exception cref="InvalidOperationException">Thrown when called before <see cref="BaseClassifier.SplitDataset"/>.</exception>
        public double TuneRegularization()
        {
            if (_trainFeatures.Length == 0)
                throw new InvalidOperationException("SplitDataset must be called before tuning the hyperparameter.");

            double bestValidationError = double.MaxValue;
            double bestL2Penalty = _l2PenaltyCandidates[0];

            Console.WriteLine("\n\n=== Model Selection / Hyperparameter Tuning (5.2) ===\n");
            Console.WriteLine($"{"L2 penalty",12} | {"CV error",10}");

            foreach (double l2Penalty in _l2PenaltyCandidates)
            {
                CrossValidation<LogisticRegression, IterativeReweightedLeastSquares<LogisticRegression>, double[], int> crossValidation =
                    CrossValidation.Create(
                        k: _crossValidationFolds,
                        learner: (_) => new IterativeReweightedLeastSquares<LogisticRegression>
                        {
                            MaxIterations = IrlsMaxIterations,
                            Regularization = l2Penalty
                        },
                        fit: (teacher, x, y, weights) => teacher.Learn(x, y, weights),
                        loss: (expected, actual, _) => ComputeMisclassificationRate(expected, actual),
                        x: _trainFeatures,
                        y: _trainTargets);

                CrossValidationResult<LogisticRegression, double[], int> result =
                    crossValidation.Learn(_trainFeatures, _trainTargets, _trainSampleWeights);
                double validationError = result.Validation.Mean;

                Console.WriteLine($"{l2Penalty,12:G4} | {validationError,10:P2}");

                if (validationError < bestValidationError)
                {
                    bestValidationError = validationError;
                    bestL2Penalty = l2Penalty;
                }
            }

            _selectedRegularization = bestL2Penalty;
            Console.WriteLine($"Selected L2 penalty: {bestL2Penalty:G4} (CV error {bestValidationError:P2})");

            return bestL2Penalty;
        }

        /// <summary>Trains the final logistic regression model on the whole training set.</summary>
        /// <exception cref="InvalidOperationException">Thrown when called before <see cref="TuneRegularization"/>.</exception>
        public override void TrainFinalModel()
        {
            IterativeReweightedLeastSquares<LogisticRegression> learner = new IterativeReweightedLeastSquares<LogisticRegression>
            {
                MaxIterations = IrlsMaxIterations,
                Regularization = SelectedRegularization
            };

            _model = learner.Learn(_trainFeatures, _trainTargets, _trainSampleWeights);
        }

        /// <summary>Predicts the class label for each row using the trained model.</summary>
        /// <param name="features">Standardized feature matrix, one row per observation.</param>
        /// <returns>Predicted label (0 or 1) for each row.</returns>
        /// <exception cref="InvalidOperationException">Thrown when called before <see cref="TrainFinalModel"/>.</exception>
        public override int[] Predict(double[][] features)
        {
            if (_model == null)
                throw new InvalidOperationException("TrainFinalModel must be called before prediction.");

            return _model.Decide(features).Select(isRisk => isRisk ? 1 : 0).ToArray();
        }

        /// <summary>Misclassification rate between expected and predicted labels.</summary>
        private static double ComputeMisclassificationRate(int[] expected, int[] actual)
        {
            int errors = 0;
            for (int i = 0; i < expected.Length; i++)
            {
                if (expected[i] != actual[i]) errors++;
            }

            return (double)errors / expected.Length;
        }
    }
}
