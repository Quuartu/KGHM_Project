using System;
using System.Collections.Generic;
using KghmProject_DavideQuartucci.Models;

namespace KghmProject_DavideQuartucci.Processing
{
    /// <summary>
    /// Labels each record with the downside risk of the FOLLOWING day: TargetClass[t] = 1 if the
    /// return realized going from day t to day t+1 is at or below the threshold.
    /// This way, day t's features (including its own log-return) remain legitimate predictors:
    /// they do not directly determine the label, which depends on the future closing price.
    /// The last record has no observable following day and is therefore excluded from the
    /// training dataset (IsValidForTraining = false).
    /// </summary>
    public class DownsideRiskLabeler : ITargetLabeler
    {
        private readonly double _downsideThreshold;

        /// <summary>
        /// Creates a labeler that flags the following day as downside risk when its
        /// log-return falls at or below the given threshold.
        /// </summary>
        /// <param name="downsideThreshold">Log-return threshold (e.g. -0.015 for -1.5%) below which
        /// the following day is considered "downside risk". No default value: the threshold must
        /// always be supplied explicitly by the caller.</param>
        public DownsideRiskLabeler(double downsideThreshold)
        {
            _downsideThreshold = downsideThreshold;
        }

        /// <summary>
        /// Assigns the target class to every record based on the log-return realized on the
        /// following day, and marks records with no observable following day as invalid.
        /// </summary>
        /// <param name="records">Chronologically ordered records to label.</param>
        public void AssignLabels(List<KghmRecord> records)
        {
            if (records == null || records.Count == 0) return;

            for (int i = 0; i < records.Count - 1; i++)
            {
                KghmRecord current = records[i];
                KghmRecord next = records[i + 1];

                if (current.AdjClose <= 0)
                {
                    current.MarkInvalidForTraining();
                    continue;
                }

                double nextDayLogReturn = Math.Log(next.AdjClose / current.AdjClose);
                int targetClass = nextDayLogReturn <= _downsideThreshold ? 1 : 0;
                current.SetTargetClass(targetClass);
            }

            // The last record has no following day on which to compute the label.
            records[records.Count - 1].MarkInvalidForTraining();
        }
    }
}
