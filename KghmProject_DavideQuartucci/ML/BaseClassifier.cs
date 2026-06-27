using System;
using System.Linq;
using Accord.Statistics.Analysis;

namespace KghmProject_DavideQuartucci.ML
{
    /// <summary>
    /// Base class for binary classifiers trained on a chronologically ordered dataset.
    /// Handles the train/test split, class weighting and test set evaluation; subclasses
    /// only implement the algorithm-specific training and prediction logic.
    /// </summary>
    public abstract class BaseClassifier : IClassifier
    {
        private readonly double[][] _features;
        private readonly int[] _targets;
        private readonly double _testFraction;

        protected double[][] _trainFeatures = Array.Empty<double[]>();
        protected double[][] _testFeatures = Array.Empty<double[]>();
        protected int[] _trainTargets = Array.Empty<int>();
        protected int[] _testTargets = Array.Empty<int>();

        /// <summary>Per-sample weight for each row in <see cref="_trainTargets"/>, used to mitigate class imbalance.</summary>
        protected double[] _trainSampleWeights = Array.Empty<double>();

        /// <param name="features">Standardized feature matrix, one row per observation.</param>
        /// <param name="targets">Binary target vector (0 = stable, 1 = downside risk).</param>
        /// <param name="testFraction">Fraction of observations reserved for the test set.</param>
        /// <exception cref="ArgumentException">Thrown when the inputs are invalid.</exception>
        protected BaseClassifier(double[][] features, int[] targets, double testFraction)
        {
            if (features == null || features.Length == 0)
                throw new ArgumentException("Feature matrix must contain at least one row.", nameof(features));
            if (targets == null || targets.Length != features.Length)
                throw new ArgumentException("Target vector must have the same length as the feature matrix.", nameof(targets));
            if (testFraction <= 0.0 || testFraction >= 1.0)
                throw new ArgumentException("Test fraction must be between 0 and 1.", nameof(testFraction));

            _features = features;
            _targets = targets;
            _testFraction = testFraction;
        }

        /// <summary>
        /// Splits the dataset into train and test sets preserving chronological order (no
        /// shuffling), then computes per-sample class weights on the training set.
        /// </summary>
        public void SplitDataset()
        {
            int testCount = (int)Math.Round(_features.Length * _testFraction);
            int trainCount = _features.Length - testCount;

            _trainFeatures = _features.Take(trainCount).ToArray();
            _trainTargets = _targets.Take(trainCount).ToArray();
            _testFeatures = _features.Skip(trainCount).ToArray();
            _testTargets = _targets.Skip(trainCount).ToArray();

            // ponytail: assumes both classes appear in the training set; numPositives == 0
            // would surface as a 0.00% or 100.00% line below instead of failing silently.
            int numPositives = _trainTargets.Count(t => t == 1);
            int numNegatives = _trainTargets.Length - numPositives;
            double weightPositive = _trainTargets.Length / (2.0 * numPositives);
            double weightNegative = _trainTargets.Length / (2.0 * numNegatives);
            _trainSampleWeights = _trainTargets.Select(t => t == 1 ? weightPositive : weightNegative).ToArray();

            double trainPositiveRate = (double)numPositives / _trainTargets.Length;
            double testPositiveRate = (double)_testTargets.Count(t => t == 1) / _testTargets.Length;

            Console.WriteLine("\nPhase 2: Train/Test Split & Class Balance");
            Console.WriteLine($"Chronological split applied ({(1 - _testFraction) * 100:F0}% train, {_testFraction * 100:F0}% test).");
            Console.WriteLine($"Train set class 1 frequency: {trainPositiveRate:P2}");
            Console.WriteLine($"Test set class 1 frequency: {testPositiveRate:P2}");
            Console.WriteLine($"Class weights -> stable: {weightNegative:F3}, risk: {weightPositive:F3}");
        }

        /// <summary>Trains the final model on the entire training set.</summary>
        public abstract void TrainFinalModel();

        /// <summary>Predicts the class label for each row of the given feature matrix.</summary>
        /// <param name="features">Standardized feature matrix, one row per observation.</param>
        /// <returns>Predicted label (0 or 1) for each row.</returns>
        public abstract int[] Predict(double[][] features);

        /// <summary>Predicts the class label for a single observation.</summary>
        public int Predict(double[] features) => Predict(new[] { features })[0];

        /// <summary>Evaluates the trained model on the test set and prints accuracy, precision, recall, F1-score and the confusion matrix.</summary>
        /// <exception cref="InvalidOperationException">Thrown when called before <see cref="TrainFinalModel"/>.</exception>
        public virtual void Evaluate()
        {
            int[] predicted = Predict(_testFeatures);
            ConfusionMatrix confusionMatrix = new ConfusionMatrix(predicted, _testTargets);

            Console.WriteLine("\nPhase 4: Evaluation on Test Set");
            Console.WriteLine($"Accuracy : {confusionMatrix.Accuracy:P2}");
            Console.WriteLine($"Precision: {confusionMatrix.Precision:P2}");
            Console.WriteLine($"Recall   : {confusionMatrix.Recall:P2}");
            Console.WriteLine($"F1-Score : {confusionMatrix.FScore:F4}");

            Console.WriteLine("\nConfusion matrix:");
            Console.WriteLine($"{"",14} | {"Predicted 0",12} | {"Predicted 1",12}");
            Console.WriteLine($"{"Actual 0",14} | {confusionMatrix.TrueNegatives,12} | {confusionMatrix.FalsePositives,12}");
            Console.WriteLine($"{"Actual 1",14} | {confusionMatrix.FalseNegatives,12} | {confusionMatrix.TruePositives,12}");
        }
    }
}
