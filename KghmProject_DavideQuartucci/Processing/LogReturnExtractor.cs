using System;
using System.Collections.Generic;
using KghmProject_DavideQuartucci.Models;

namespace KghmProject_DavideQuartucci.Processing
{
    /// <summary>
    /// Computes the one-day log-return of the closing price, the copper price and the
    /// silver price for every record.
    /// </summary>
    public class LogReturnExtractor : FeatureExtractorBase
    {
        // A log-return is mathematically defined between two consecutive observations:
        // this is a structural constant of the formula, not a tunable business threshold.
        private const int MinimumRecordsRequiredForReturn = 2;

        /// <summary>
        /// Creates an extractor that needs at least two consecutive records to compute a
        /// one-day log-return.
        /// </summary>
        public LogReturnExtractor() : base(MinimumRecordsRequiredForReturn)
        {
        }

        /// <summary>
        /// Computes the log-returns for every record except the first, which has no
        /// predecessor and is therefore marked invalid for training.
        /// </summary>
        /// <param name="records">Chronologically ordered records with at least two elements.</param>
        protected override void ExtractCore(List<KghmRecord> records)
        {
            // The first record has no predecessor: no valid log-return can be computed for it,
            // so it is excluded from the final dataset instead of receiving a placeholder value.
            records[0].SetLogReturns(0, 0, 0);
            records[0].MarkInvalidForTraining();

            for (int i = 1; i < records.Count; i++)
            {
                KghmRecord current = records[i];
                KghmRecord previous = records[i - 1];

                // Formula: r_t = ln(P_t / P_{t-1}), uses only today's and yesterday's data (no look-ahead)
                double logReturnAdjClose;
                if (previous.AdjClose > 0)
                {
                    logReturnAdjClose = Math.Log(current.AdjClose / previous.AdjClose);
                }
                else
                {
                    logReturnAdjClose = 0;
                }

                double logReturnCopper;
                if (previous.CopperPrice > 0)
                {
                    logReturnCopper = Math.Log(current.CopperPrice / previous.CopperPrice);
                }
                else
                {
                    logReturnCopper = 0;
                }

                double logReturnSilver;
                if (previous.SilverPrice > 0)
                {
                    logReturnSilver = Math.Log(current.SilverPrice / previous.SilverPrice);
                }
                else
                {
                    logReturnSilver = 0;
                }

                current.SetLogReturns(logReturnAdjClose, logReturnCopper, logReturnSilver);
            }
        }
    }
}
