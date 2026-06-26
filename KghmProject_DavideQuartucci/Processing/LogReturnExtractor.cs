using System;
using System.Collections.Generic;
using KghmProject_DavideQuartucci.Models;

namespace KghmProject_DavideQuartucci.Processing
{
    public class LogReturnExtractor : FeatureExtractorBase
    {
        protected override void ExtractCore(List<KghmRecord> records)
        {
            // The first record has no predecessor: no valid log-return can be computed for it,
            // so it is excluded from the final dataset instead of receiving a placeholder value.
            records[0].LogReturnAdjClose = 0;
            records[0].LogReturnCopper = 0;
            records[0].LogReturnSilver = 0;
            records[0].IsValidForTraining = false;

            for (int i = 1; i < records.Count; i++)
            {
                KghmRecord current = records[i];
                KghmRecord previous = records[i - 1];

                // Formula: r_t = ln(P_t / P_{t-1}), uses only today's and yesterday's data (no look-ahead)
                if (previous.AdjClose > 0)
                    current.LogReturnAdjClose = Math.Log(current.AdjClose / previous.AdjClose);

                if (previous.CopperPrice > 0)
                    current.LogReturnCopper = Math.Log(current.CopperPrice / previous.CopperPrice);

                if (previous.SilverPrice > 0)
                    current.LogReturnSilver = Math.Log(current.SilverPrice / previous.SilverPrice);
            }
        }
    }
}
