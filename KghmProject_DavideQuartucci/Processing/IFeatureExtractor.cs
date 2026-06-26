using System.Collections.Generic;
using KghmProject_DavideQuartucci.Models;

namespace KghmProject_DavideQuartucci.Processing
{
    /// <summary>
    /// Defines the strategy for extracting or computing a specific set of features.
    /// </summary>
    public interface IFeatureExtractor
    {
        /// <summary>
        /// Processes the list of historical records to compute and inject derived features.
        /// </summary>
        void Extract(List<KghmRecord> records);
    }
}