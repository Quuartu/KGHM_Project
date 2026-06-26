using System;
using System.Collections.Generic;
using KghmProject_DavideQuartucci.Models;

namespace KghmProject_DavideQuartucci.Processing
{
    /// <summary>
    /// Computes the simple moving average of the closing price and the ratio between
    /// today's price and that average.
    /// </summary>
    public class MovingAverageExtractor : FeatureExtractorBase
    {
        private readonly int _period;

        /// <summary>
        /// Creates a moving-average extractor for the given window size. The window size is
        /// also the minimum number of records the base class requires before running.
        /// </summary>
        /// <param name="period">Number of trading days included in the moving average. Must be
        /// positive. No default value: the period must always be supplied explicitly by the caller.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="period"/> is not positive.</exception>
        public MovingAverageExtractor(int period) : base(ValidatePeriod(period))
        {
            _period = period;
        }

        /// <summary>
        /// Validates the period before it is forwarded to the base class constructor, so the
        /// caller gets a domain-specific error message instead of a generic one.
        /// </summary>
        /// <param name="period">Candidate moving average period.</param>
        /// <returns><paramref name="period"/> unchanged, when valid.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="period"/> is not positive.</exception>
        private static int ValidatePeriod(int period)
        {
            if (period <= 0)
            {
                throw new ArgumentException("The moving average period must be positive.", nameof(period));
            }
            return period;
        }

        /// <summary>
        /// Computes the moving average and the price-to-moving-average ratio for every
        /// record. Records inside the warm-up window (fewer than <c>period</c> preceding
        /// observations) are marked invalid instead of receiving a placeholder value.
        /// </summary>
        /// <param name="records">Chronologically ordered records with at least <c>period</c> elements.</param>
        protected override void ExtractCore(List<KghmRecord> records)
        {
            for (int i = 0; i < records.Count; i++)
            {
                if (i < _period - 1)
                {
                    // Incomplete historical window: the MA50 would not be representative.
                    // The record is excluded from the final dataset instead of receiving a placeholder value.
                    records[i].MarkInvalidForTraining();
                    continue;
                }

                double sum = 0;
                for (int j = i - _period + 1; j <= i; j++)
                {
                    sum += records[j].AdjClose;
                }

                double movingAverage = sum / _period;

                // Oscillator: percentage distance of today's price from the moving average
                double priceToMovingAverageRatio = movingAverage > 0 ? records[i].AdjClose / movingAverage : 1.0;

                records[i].SetMovingAverage(movingAverage, priceToMovingAverageRatio);
            }
        }
    }
}
