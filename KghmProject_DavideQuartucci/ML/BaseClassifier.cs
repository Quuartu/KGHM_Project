using System;
using Accord.Statistics.Analysis;
using KghmProject_DavideQuartucci.Processing;

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

        /// <param name="features">Raw (unstandardized) feature matrix, one row per observation.</param>
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
        /// shuffling), standardizes features by fitting only on the training split to prevent
        /// data leakage, then computes per-sample class weights on the training set.
        /// </summary>
        public void SplitDataset()
        {
            int testCount = (int)Math.Round(_features.Length * _testFraction);
            int trainCount = _features.Length - testCount;

            double[][] rawTrain = new double[trainCount][];
            _trainTargets = new int[trainCount];
            for (int i = 0; i < trainCount; i++)
            {
                rawTrain[i] = _features[i];
                _trainTargets[i] = _targets[i];
            }

            double[][] rawTest = new double[testCount][];
            _testTargets = new int[testCount];
            for (int i = 0; i < testCount; i++)
            {
                rawTest[i] = _features[trainCount + i];
                _testTargets[i] = _targets[trainCount + i];
            }

            Standardizer standardizer = new Standardizer();
            standardizer.Fit(rawTrain);
            _trainFeatures = standardizer.Transform(rawTrain);
            _testFeatures = standardizer.Transform(rawTest);

            _trainSampleWeights = ComputeSampleWeights(_trainTargets);

            int numPositives = 0;
            for (int i = 0; i < _trainTargets.Length; i++)
                if (_trainTargets[i] == 1) numPositives++;
            int testPositives = 0;
            for (int i = 0; i < _testTargets.Length; i++)
                if (_testTargets[i] == 1) testPositives++;

            Console.WriteLine("\n\n=== Train/Test Split & Class Balance (5.1) ===\n");
            Console.WriteLine($"Chronological split applied ({(1 - _testFraction) * 100:F0}% train, {_testFraction * 100:F0}% test).");
            Console.WriteLine($"Standardizer fitted on training set only (leakage-free).");
            Console.WriteLine($"Train set class 1 frequency: {(double)numPositives / _trainTargets.Length:P2}");
            Console.WriteLine($"Test set class 1 frequency: {(double)testPositives / _testTargets.Length:P2}");
            double weightPositive = _trainSampleWeights[Array.FindIndex(_trainTargets, t => t == 1)];
            double weightNegative = _trainSampleWeights[Array.FindIndex(_trainTargets, t => t == 0)];
            Console.WriteLine($"Class weights (computed from train set) -> stable: {weightNegative:F3}, risk: {weightPositive:F3}");
        }

        /// <summary>
        /// Computes inverse-frequency per-sample weights so both classes contribute equally
        /// to the loss. Both classes must appear in <paramref name="targets"/>.
        /// </summary>
        protected static double[] ComputeSampleWeights(int[] targets)
        {
            int numPositives = 0;
            for (int i = 0; i < targets.Length; i++)
                if (targets[i] == 1) numPositives++;
            int numNegatives = targets.Length - numPositives;
            double wPos = targets.Length / (2.0 * numPositives);
            double wNeg = targets.Length / (2.0 * numNegatives);
            double[] weights = new double[targets.Length];
            for (int i = 0; i < targets.Length; i++)
                weights[i] = targets[i] == 1 ? wPos : wNeg;
            return weights;
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

            Console.WriteLine("\n\n=== Evaluation on Test Set (6) ===\n");
            Console.WriteLine($"Accuracy : {confusionMatrix.Accuracy:P2}");
            Console.WriteLine($"Precision: {confusionMatrix.Precision:P2}");
            Console.WriteLine($"Recall   : {confusionMatrix.Recall:P2}");
            Console.WriteLine($"F1-Score : {confusionMatrix.FScore:F4}");
            Console.WriteLine($"MCC      : {confusionMatrix.MatthewsCorrelationCoefficient:F4}");

            Console.WriteLine("\nConfusion matrix:");
            Console.WriteLine($"{"",14} | {"Predicted 0",12} | {"Predicted 1",12}");
            Console.WriteLine($"{"Actual 0",14} | {confusionMatrix.TrueNegatives,12} | {confusionMatrix.FalsePositives,12}");
            Console.WriteLine($"{"Actual 1",14} | {confusionMatrix.FalseNegatives,12} | {confusionMatrix.TruePositives,12}");
        }
    }
}
