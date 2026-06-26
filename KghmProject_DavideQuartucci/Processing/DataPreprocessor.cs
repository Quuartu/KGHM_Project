using System;
using System.Collections.Generic;
using KghmProject_DavideQuartucci.Models;

namespace KghmProject_DavideQuartucci.Processing
{
    /// <summary>
    /// Orchestrates the preprocessing pipeline: runs Feature Engineering, target labeling, and
    /// finally produces the data structures (X, y) required by Accord.NET's Logistic Regression.
    /// Extractors, labeler and feature selection are all injected by the caller (Dependency
    /// Injection): the class never instantiates a concrete strategy or a hardcoded threshold,
    /// and every method relies on this injected state rather than on its own parameters.
    /// </summary>
    public class DataPreprocessor
    {
        private readonly List<IFeatureExtractor> _extractors;
        private readonly ITargetLabeler _targetLabeler;
        private readonly List<Func<KghmRecord, double>> _featureSelectors;

        /// <summary>
        /// Creates a preprocessor configured with the feature extraction strategies, the
        /// labeling strategy and the feature selection to use for the whole pipeline.
        /// </summary>
        /// <param name="extractors">Feature engineering strategies to run, in order.</param>
        /// <param name="targetLabeler">Strategy used to assign the target class.</param>
        /// <param name="featureSelectors">Selectors indicating which record properties become
        /// columns of the feature matrix X. Supplied by the caller: no feature is hardcoded here.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="extractors"/> or
        /// <paramref name="targetLabeler"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="featureSelectors"/> is
        /// null or empty.</exception>
        public DataPreprocessor(
            List<IFeatureExtractor> extractors,
            ITargetLabeler targetLabeler,
            List<Func<KghmRecord, double>> featureSelectors)
        {
            _extractors = extractors ?? throw new ArgumentNullException(nameof(extractors));
            _targetLabeler = targetLabeler ?? throw new ArgumentNullException(nameof(targetLabeler));
            if (featureSelectors == null || featureSelectors.Count == 0)
            {
                throw new ArgumentException("At least one feature selector must be specified.", nameof(featureSelectors));
            }
            _featureSelectors = featureSelectors;
        }

        /// <summary>
        /// Runs all Feature Engineering strategies in sequence, then the target labeling.
        /// </summary>
        /// <param name="records">Chronologically ordered records to enrich with features and labels.</param>
        public void Process(List<KghmRecord> records)
        {
            if (records == null || records.Count == 0) return;

            foreach (IFeatureExtractor extractor in _extractors)
            {
                extractor.Extract(records);
            }

            _targetLabeler.AssignLabels(records);
        }

        /// <summary>
        /// Builds the feature matrix X and the target vector y ready for Accord.NET, using the
        /// feature selectors supplied to the constructor. Invalid records (incomplete warm-up
        /// window, last day with no target) are excluded before the calculation, so they do not
        /// skew the mean and standard deviation.
        /// </summary>
        /// <param name="records">Records already processed by Process (features and target already computed).</param>
        /// <param name="features">Output: standardized feature matrix, one row per valid record.</param>
        /// <param name="targets">Output: target class vector, one entry per valid record.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="records"/> is null.</exception>
        public void BuildDataset(
            List<KghmRecord> records,
            out double[][] features,
            out int[] targets)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));

            List<KghmRecord> validRecords = new List<KghmRecord>();
            foreach (KghmRecord record in records)
            {
                if (record.IsValidForTraining) validRecords.Add(record);
            }

            int rowCount = validRecords.Count;
            int columnCount = _featureSelectors.Count;

            double[][] rawFeatures = new double[rowCount][];
            targets = new int[rowCount];

            for (int i = 0; i < rowCount; i++)
            {
                rawFeatures[i] = new double[columnCount];
                for (int j = 0; j < columnCount; j++)
                {
                    rawFeatures[i][j] = _featureSelectors[j](validRecords[i]);
                }
                targets[i] = validRecords[i].TargetClass;
            }

            features = StandardizeColumns(rawFeatures);
        }

        /// <summary>
        /// Z-score standardization: for each column, subtracts the mean and divides by the sample
        /// standard deviation, so that every feature ends up with mean 0 and standard deviation 1.
        /// Declared static because it is a pure transformation that does not depend on the
        /// preprocessor's configuration (extractors, labeler, feature selectors).
        /// </summary>
        /// <param name="rawFeatures">Feature matrix before standardization.</param>
        /// <returns>A new matrix with the same shape as <paramref name="rawFeatures"/>, standardized column by column.</returns>
        private static double[][] StandardizeColumns(double[][] rawFeatures)
        {
            int rowCount = rawFeatures.Length;
            if (rowCount == 0) return rawFeatures;

            int columnCount = rawFeatures[0].Length;
            double[] means = new double[columnCount];
            double[] standardDeviations = new double[columnCount];

            for (int col = 0; col < columnCount; col++)
            {
                double sum = 0;
                for (int row = 0; row < rowCount; row++)
                {
                    sum += rawFeatures[row][col];
                }
                means[col] = sum / rowCount;
            }

            for (int col = 0; col < columnCount; col++)
            {
                double sumSquaredDeviations = 0;
                for (int row = 0; row < rowCount; row++)
                {
                    double deviation = rawFeatures[row][col] - means[col];
                    sumSquaredDeviations += deviation * deviation;
                }
                standardDeviations[col] = Math.Sqrt(sumSquaredDeviations / rowCount);
            }

            double[][] standardized = new double[rowCount][];
            for (int row = 0; row < rowCount; row++)
            {
                standardized[row] = new double[columnCount];
                for (int col = 0; col < columnCount; col++)
                {
                    // Constant column (zero standard deviation): avoid division by zero by
                    // leaving the feature centered at 0 instead of producing NaN/Infinity.
                    if (standardDeviations[col] > 0)
                    {
                        standardized[row][col] = (rawFeatures[row][col] - means[col]) / standardDeviations[col];
                    }
                    else
                    {
                        standardized[row][col] = 0.0;
                    }
                }
            }

            return standardized;
        }
    }
}
