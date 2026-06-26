using System;
using System.Collections.Generic;
using KghmProject_DavideQuartucci.Models;

namespace KghmProject_DavideQuartucci.Processing
{
    /// <summary>
    /// Abstract base class for time-series-based feature extractors.
    /// Centralizes the common guard clause (not enough history to compute a meaningful
    /// result) and exposes an abstract extension point: derived classes implement only
    /// the specific calculation logic (an example of inheritance and polymorphism across
    /// Feature Engineering strategies).
    /// </summary>
    public abstract class FeatureExtractorBase : IFeatureExtractor
    {
        private readonly int _minimumRecordsRequired;

        /// <summary>
        /// Creates the base extractor with the minimum amount of history it needs.
        /// </summary>
        /// <param name="minimumRecordsRequired">Minimum number of records needed before this
        /// extractor can produce a meaningful result (e.g. 2 for a one-day return, or the
        /// window size for a moving average). Must be at least 1.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="minimumRecordsRequired"/> is less than 1.</exception>
        protected FeatureExtractorBase(int minimumRecordsRequired)
        {
            if (minimumRecordsRequired < 1)
            {
                throw new ArgumentException("The minimum number of required records must be at least 1.", nameof(minimumRecordsRequired));
            }
            _minimumRecordsRequired = minimumRecordsRequired;
        }

        /// <summary>
        /// Runs the feature extraction strategy on the given records, skipping it entirely
        /// when there is not enough history to produce a meaningful result.
        /// </summary>
        /// <param name="records">Chronologically ordered records to enrich with derived features.</param>
        public void Extract(List<KghmRecord> records)
        {
            if (records == null || records.Count < _minimumRecordsRequired) return;

            ExtractCore(records);
        }

        /// <summary>
        /// Implemented by derived classes to compute and inject the specific derived
        /// features (e.g. log-returns, moving average).
        /// </summary>
        /// <param name="records">Chronologically ordered records, guaranteed to contain at
        /// least <c>minimumRecordsRequired</c> elements.</param>
        protected abstract void ExtractCore(List<KghmRecord> records);
    }
}
