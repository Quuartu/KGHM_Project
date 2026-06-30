using System;
using System.Collections.Generic;
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
        /// Grid search over the L2 penalty candidates using stratified k-fold cross-validation
        /// on the training set, selecting the value with the lowest average validation error.
        /// Folds are stratified: each fold receives the same class ratio as the full training set.
        /// </summary>
        /// <returns>The selected L2 penalty.</returns>
        /// <exception cref="InvalidOperationException">Thrown when called before <see cref="BaseClassifier.SplitDataset"/>.</exception>
        public double TuneRegularization()
        {
            if (_trainFeatures.Length == 0)
                throw new InvalidOperationException("SplitDataset must be called before tuning the hyperparameter.");

            int[] foldOf = BuildStratifiedFoldIndices(_trainTargets, _crossValidationFolds);

            double bestValidationError = double.MaxValue;
            double bestL2Penalty = _l2PenaltyCandidates[0];

            Console.WriteLine("\n\n=== Model Selection / Hyperparameter Tuning (5.2) ===\n");
            Console.WriteLine($"Stratified {_crossValidationFolds}-fold CV on training set.");
            Console.WriteLine($"{"L2 penalty",12} | {"CV error",10}");

            foreach (double l2Penalty in _l2PenaltyCandidates)
            {
                double totalError = 0;

                for (int fold = 0; fold < _crossValidationFolds; fold++)
                {
                    List<int> trainIdx = new List<int>(), valIdx = new List<int>();
                    for (int i = 0; i < _trainTargets.Length; i++)
                    {
                        if (foldOf[i] == fold) valIdx.Add(i);
                        else trainIdx.Add(i);
                    }

                    double[][] foldX = new double[trainIdx.Count][];
                    int[] foldY = new int[trainIdx.Count];
                    for (int i = 0; i < trainIdx.Count; i++) { foldX[i] = _trainFeatures[trainIdx[i]]; foldY[i] = _trainTargets[trainIdx[i]]; }

                    double[][] valX = new double[valIdx.Count][];
                    int[] valY = new int[valIdx.Count];
                    for (int i = 0; i < valIdx.Count; i++) { valX[i] = _trainFeatures[valIdx[i]]; valY[i] = _trainTargets[valIdx[i]]; }

                    LogisticRegression model = new IterativeReweightedLeastSquares<LogisticRegression>
                    {
                        MaxIterations = IrlsMaxIterations,
                        Regularization = l2Penalty
                    }.Learn(foldX, foldY, ComputeSampleWeights(foldY));

                    bool[] decisions = model.Decide(valX);
                    int[] predicted = new int[decisions.Length];
                    for (int i = 0; i < decisions.Length; i++) predicted[i] = decisions[i] ? 1 : 0;

                    totalError += ComputeMisclassificationRate(valY, predicted);
                }

                double validationError = totalError / _crossValidationFolds;
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

        /// <summary>
        /// Assigns a fold index to each sample using round-robin within each class,
        /// so every fold has the same class ratio as the full set.
        /// </summary>
        private static int[] BuildStratifiedFoldIndices(int[] targets, int k)
        {
            int[] foldOf = new int[targets.Length];
            int c0 = 0, c1 = 0;
            for (int i = 0; i < targets.Length; i++)
                foldOf[i] = targets[i] == 0 ? c0++ % k : c1++ % k;
            return foldOf;
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

            bool[] decisions = _model.Decide(features);
            int[] labels = new int[decisions.Length];
            for (int i = 0; i < decisions.Length; i++)
                labels[i] = decisions[i] ? 1 : 0;
            return labels;
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
