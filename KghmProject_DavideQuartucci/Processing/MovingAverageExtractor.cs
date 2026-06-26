using System;
using System.Collections.Generic;
using KghmProject_DavideQuartucci.Models;

namespace KghmProject_DavideQuartucci.Processing
{
    public class MovingAverageExtractor : FeatureExtractorBase
    {
        private readonly int _period;

        /// <summary>
        /// The moving average period is a required constructor parameter: no default value
        /// hidden inside the class, to avoid hardcoded periods/thresholds.
        /// </summary>
        public MovingAverageExtractor(int period)
        {
            if (period <= 0)
            {
                throw new ArgumentException("The moving average period must be positive.", nameof(period));
            }
            _period = period;
        }

        protected override void ExtractCore(List<KghmRecord> records)
        {
            for (int i = 0; i < records.Count; i++)
            {
                if (i < _period - 1)
                {
                    // Incomplete historical window: the MA50 would not be representative.
                    // The record is excluded from the final dataset instead of receiving a placeholder value.
                    records[i].IsValidForTraining = false;
                    continue;
                }

                double sum = 0;
                for (int j = i - _period + 1; j <= i; j++)
                {
                    sum += records[j].AdjClose;
                }

                double maValue = sum / _period;
                records[i].Ma50 = maValue;

                // Oscillator: percentage distance of today's price from the moving average
                records[i].PriceToMa50Ratio = maValue > 0 ? records[i].AdjClose / maValue : 1.0;
            }
        }
    }
}
