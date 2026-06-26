using System;
using System.Globalization;
using KghmProject_DavideQuartucci.IO;

namespace KghmProject_DavideQuartucci.Models
{
    /// <summary>
    /// Represents a single trading day for the KGHM stock: the raw market data read from the
    /// CSV source, plus the derived features and target label computed later by the
    /// Processing pipeline (IFeatureExtractor, ITargetLabeler).
    /// </summary>
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

        // Derived features: read-only from the outside. They can only change through the
        // dedicated Set*/Mark* methods below, called by IFeatureExtractor/ITargetLabeler
        // implementations, never through a public property setter.
        public double LogReturnAdjClose { get; private set; }
        public double LogReturnCopper { get; private set; }
        public double LogReturnSilver { get; private set; }
        public double Ma50 { get; private set; }
        public double PriceToMa50Ratio { get; private set; } // Oscillator: percentage distance from the MA50
        public int TargetClass { get; private set; }          // 0 = Normal, 1 = Downside risk

        /// <summary>
        /// Indicates whether the record has all its features correctly computed (no incomplete
        /// warm-up window, no missing target). Records with this flag set to false are excluded
        /// from the final dataset passed to the Logistic Regression, so they do not skew the
        /// mean/standard deviation with placeholder values.
        /// </summary>
        public bool IsValidForTraining { get; private set; } = true;

        /// <summary>
        /// Creates a record from already-parsed raw market data. Derived features are not set
        /// here: they are populated afterwards by the Processing pipeline through the
        /// dedicated Set*/Mark* methods, never through this constructor.
        /// </summary>
        /// <param name="adjClose">Adjusted closing price of the stock.</param>
        /// <param name="volume">Traded volume.</param>
        /// <param name="copperPrice">Copper price (KGHM's main commodity).</param>
        /// <param name="silverPrice">Silver price.</param>
        /// <param name="goldPrice">Gold price.</param>
        /// <param name="eurUsd">EUR/USD exchange rate.</param>
        /// <param name="plnUsd">PLN/USD exchange rate.</param>
        /// <param name="wig20">WIG20 index level (Warsaw Stock Exchange).</param>
        /// <param name="dateString">Trading date, as read from the source file.</param>
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
        /// Sets the one-day log-returns for the closing price, copper price and silver price.
        /// Intended to be called exactly once per record by an IFeatureExtractor implementation.
        /// </summary>
        /// <param name="logReturnAdjClose">Log-return of the closing price versus the previous day.</param>
        /// <param name="logReturnCopper">Log-return of the copper price versus the previous day.</param>
        /// <param name="logReturnSilver">Log-return of the silver price versus the previous day.</param>
        public void SetLogReturns(double logReturnAdjClose, double logReturnCopper, double logReturnSilver)
        {
            LogReturnAdjClose = logReturnAdjClose;
            LogReturnCopper = logReturnCopper;
            LogReturnSilver = logReturnSilver;
        }

        /// <summary>
        /// Sets the moving average and the price-to-moving-average ratio.
        /// Intended to be called exactly once per record by an IFeatureExtractor implementation.
        /// </summary>
        /// <param name="ma50">Value of the moving average.</param>
        /// <param name="priceToMa50Ratio">Ratio between the closing price and the moving average.</param>
        public void SetMovingAverage(double ma50, double priceToMa50Ratio)
        {
            Ma50 = ma50;
            PriceToMa50Ratio = priceToMa50Ratio;
        }

        /// <summary>
        /// Assigns the dynamically computed target class.
        /// Intended to be called by an ITargetLabeler implementation.
        /// </summary>
        /// <param name="targetClass">0 for a normal day, 1 for downside risk.</param>
        public void SetTargetClass(int targetClass)
        {
            TargetClass = targetClass;
        }

        /// <summary>
        /// Marks the record as unusable for training (incomplete feature window or missing
        /// target), so DataPreprocessor excludes it from the final dataset.
        /// </summary>
        public void MarkInvalidForTraining()
        {
            IsValidForTraining = false;
        }

        /// <summary>
        /// Builds a record from a single CSV row already split into fields. Column indices are
        /// never written here: they come from the CsvColumnMapping parameter.
        /// </summary>
        /// <param name="values">Fields of a single CSV row, already split on the delimiter.</param>
        /// <param name="columnMapping">Mapping describing which index holds each required field.</param>
        /// <returns>A new <see cref="KghmRecord"/> populated with the raw fields from the row.</returns>
        /// <exception cref="FormatException">Thrown when the row has fewer columns than required,
        /// or when any required field is missing or is not a valid number.</exception>
        public static KghmRecord FromCsvLine(string[] values, CsvColumnMapping columnMapping)
        {
            // Structural safety check: prevents IndexOutOfRange exceptions.
            // The minimum threshold is computed from the mapping itself, not hardcoded.
            if (values.Length < columnMapping.MinimumColumnCount())
            {
                throw new FormatException("The CSV row is corrupted or does not contain the required columns.");
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
                throw new FormatException("The CSV row contains missing or non-numeric values.");
            }

            return new KghmRecord(adjClose, volume, copperPrice, silverPrice, goldPrice, eurUsd, plnUsd, wig20, dateString);
        }

        /// <summary>
        /// Wrapper around double.TryParse with invariant culture: isolates the handling of
        /// null/empty/malformed values in a single place, without relying on exceptions, and
        /// avoids crashes caused by systems that use a different decimal separator.
        /// </summary>
        /// <param name="values">Fields of the CSV row.</param>
        /// <param name="index">Index of the field to parse.</param>
        /// <param name="result">The parsed value, or 0 when parsing fails.</param>
        /// <returns>True if the field was successfully parsed as a number; otherwise false.</returns>
        private static bool TryGetDouble(string[] values, int index, out double result)
        {
            string raw = values[index].Trim();
            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }
    }
}
