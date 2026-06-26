using System.Globalization;
using KghmProject_DavideQuartucci.IO;

namespace KghmProject_DavideQuartucci.Models
{
    public class KghmRecord
    {
        // Raw data: encapsulated with a private setter, assignable only at construction time (immutable from outside).
        public double AdjClose { get; private set; }
        public double Volume { get; private set; }
        public double CopperPrice { get; private set; }
        public double SilverPrice { get; private set; }
        public double GoldPrice { get; private set; }
        public double EurUsd { get; private set; }
        public double PlnUsd { get; private set; }
        public double Wig20 { get; private set; }
        public string DateString { get; private set; }

        // Derived features: populated in a later stage of the pipeline (IFeatureExtractor, ITargetLabeler).
        public double LogReturnAdjClose { get; set; }
        public double LogReturnCopper { get; set; }
        public double LogReturnSilver { get; set; }
        public double Ma50 { get; set; }
        public double PriceToMa50Ratio { get; set; } // Oscillator: percentage distance from the MA50
        public int TargetClass { get; set; }          // 0 = Normal, 1 = Downside risk

        /// <summary>
        /// Indicates whether the record has all its features correctly computed (no incomplete
        /// warm-up window, no missing target). Records with this flag set to false are excluded
        /// from the final dataset passed to the Logistic Regression, so they do not skew the
        /// mean/standard deviation with placeholder values.
        /// </summary>
        public bool IsValidForTraining { get; set; } = true;

        public KghmRecord(
            double adjClose,
            double volume,
            double copperPrice,
            double silverPrice,
            double goldPrice,
            double eurUsd,
            double plnUsd,
            double wig20,
            string dateString)
        {
            AdjClose = adjClose;
            Volume = volume;
            CopperPrice = copperPrice;
            SilverPrice = silverPrice;
            GoldPrice = goldPrice;
            EurUsd = eurUsd;
            PlnUsd = plnUsd;
            Wig20 = wig20;
            DateString = dateString;
        }

        /// <summary>
        /// Factory method that builds a record from a CSV row already split into fields.
        /// Column indices are never written here: they come from the CsvColumnMapping parameter.
        /// </summary>
        public static KghmRecord FromCsvLine(string[] values, CsvColumnMapping columnMapping)
        {
            // Structural safety check: prevents IndexOutOfRange exceptions.
            // The minimum threshold is computed from the mapping itself, not hardcoded.
            if (values.Length < columnMapping.MinimumColumnCount())
            {
                throw new System.FormatException("The CSV row is corrupted or does not contain the required columns.");
            }

            // Defensive parsing: TryParse does not throw on empty/malformed values,
            // so the caller can discard the row without relying on exception cost.
            bool ok = true;
            ok &= TryGetDouble(values, columnMapping.AdjCloseIndex, out double adjClose);
            ok &= TryGetDouble(values, columnMapping.VolumeIndex, out double volume);
            ok &= TryGetDouble(values, columnMapping.CopperPriceIndex, out double copperPrice);
            ok &= TryGetDouble(values, columnMapping.SilverPriceIndex, out double silverPrice);
            ok &= TryGetDouble(values, columnMapping.GoldPriceIndex, out double goldPrice);
            ok &= TryGetDouble(values, columnMapping.EurUsdIndex, out double eurUsd);
            ok &= TryGetDouble(values, columnMapping.PlnUsdIndex, out double plnUsd);
            ok &= TryGetDouble(values, columnMapping.Wig20Index, out double wig20);

            string dateString = values[columnMapping.DateIndex].Trim();

            if (!ok || string.IsNullOrWhiteSpace(dateString))
            {
                throw new System.FormatException("The CSV row contains missing or non-numeric values.");
            }

            return new KghmRecord(adjClose, volume, copperPrice, silverPrice, goldPrice, eurUsd, plnUsd, wig20, dateString);
        }

        /// <summary>
        /// Wrapper around double.TryParse with invariant culture: isolates the handling of
        /// null/empty/malformed values in a single place, without relying on exceptions.
        /// </summary>
        private static bool TryGetDouble(string[] values, int index, out double result)
        {
            string raw = values[index].Trim();
            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }
    }
}
