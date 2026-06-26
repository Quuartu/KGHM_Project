using System.Collections.Generic;
using KghmProject_DavideQuartucci.Models;

namespace KghmProject_DavideQuartucci.Processing
{
    /// <summary>
    /// Abstract base class for time-series-based feature extractors.
    /// Centralizes the common checks (null/empty list) and exposes an abstract extension
    /// point: derived classes implement only the specific calculation logic (an example
    /// of inheritance and polymorphism across Feature Engineering strategies).
    /// </summary>
    public abstract class FeatureExtractorBase : IFeatureExtractor
    {
        public void Extract(List<KghmRecord> records)
        {
            if (records == null || records.Count == 0) return;

            ExtractCore(records);
        }

        /// <summary>
        /// Implemented by derived classes to compute and inject the specific derived
        /// features (e.g. log-returns, moving average).
        /// </summary>
        protected abstract void ExtractCore(List<KghmRecord> records);
    }
}
