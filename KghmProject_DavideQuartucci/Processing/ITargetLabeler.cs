using System.Collections.Generic;
using KghmProject_DavideQuartucci.Models;

namespace KghmProject_DavideQuartucci.Processing
{
    /// <summary>
    /// Defines the strategy for dynamically labeling the Target Class.
    /// Separating this responsibility from IFeatureExtractor allows the risk business logic
    /// (e.g. threshold, time horizon) to change without touching the rest of the pipeline.
    /// </summary>
    public interface ITargetLabeler
    {
        /// <summary>
        /// Computes and assigns the TargetClass to each record in the list.
        /// </summary>
        /// <param name="records">Chronologically ordered records to label.</param>
        void AssignLabels(List<KghmRecord> records);
    }
}
