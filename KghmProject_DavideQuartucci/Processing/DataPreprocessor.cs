using System;
using System.Collections.Generic;
using KghmProject_DavideQuartucci.Models;

namespace KghmProject_DavideQuartucci.Processing
{
    /// <summary>
    /// Orchestrates the preprocessing pipeline: runs Feature Engineering, target labeling, and
    /// finally produces the data structures (X, y) required by Accord.NET's Logistic Regression.
    /// Extractors and labeler are injected by the caller (Dependency Injection): the class never
    /// instantiates a concrete strategy or a hardcoded threshold directly.
    /// </summary>
    public class DataPreprocessor
    {
        private readonly List<IFeatureExtractor> _extractors;
        private readonly ITargetLabeler _targetLabeler;

        public DataPreprocessor(List<IFeatureExtractor> extractors, ITargetLabeler targetLabeler)
        {
            _extractors = extractors ?? throw new ArgumentNullException(nameof(extractors));
            _targetLabeler = targetLabeler ?? throw new ArgumentNullException(nameof(targetLabeler));
        }

        /// <summary>
        /// Runs all Feature Engineering strategies in sequence, then the target labeling.
        /// </summary>
        public void Process(List<KghmRecord> records)
        {
            if (records == null || records.Count == 0) return;

            foreach (var extractor in _extractors)
            {
                extractor.Extract(records);
            }

            _targetLabeler.AssignLabels(records);
        }

        /// <summary>
        /// Builds the feature matrix X and the target vector y ready for Accord.NET.
        /// Invalid records (incomplete warm-up window, last day with no target) are excluded
        /// before the calculation, so they do not skew the mean and standard deviation.
        /// </summary>
        /// <param name="records">Records already processed by Process (features and target already computed).</param>
        /// <param name="featureSelectors">List of selectors indicating which record properties become
        /// columns of the X matrix. Supplied by the caller: no feature is hardcoded here.</param>
        public void BuildDataset(
            List<KghmRecord> records,
            List<Func<KghmRecord, double>> featureSelectors,
            out double[][] features,
            out int[] targets)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));
            if (featureSelectors == null || featureSelectors.Count == 0)
                throw new ArgumentException("At least one feature must be specified.", nameof(featureSelectors));

            List<KghmRecord> validRecords = new List<KghmRecord>();
            foreach (var record in records)
            {
                if (record.IsValidForTraining) validRecords.Add(record);
            }

            int rowCount = validRecords.Count;
            int columnCount = featureSelectors.Count;

            double[][] rawFeatures = new double[rowCount][];
            targets = new int[rowCount];

            for (int i = 0; i < rowCount; i++)
            {
                rawFeatures[i] = new double[columnCount];
                for (int j = 0; j < columnCount; j++)
                {
                    rawFeatures[i][j] = featureSelectors[j](validRecords[i]);
                }
                targets[i] = validRecords[i].TargetClass;
            }

            features = StandardizeColumns(rawFeatures);
        }

        /// <summary>
        /// Z-score standardization: for each column, subtracts the mean and divides by the sample
        /// standard deviation, so that every feature ends up with mean 0 and standard deviation 1.
        /// </summary>
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
                    standardized[row][col] = standardDeviations[col] > 0
                        ? (rawFeatures[row][col] - means[col]) / standardDeviations[col]
                        : 0.0;
                }
            }

            return standardized;
        }
    }
}
