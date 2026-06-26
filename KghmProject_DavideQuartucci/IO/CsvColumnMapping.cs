namespace KghmProject_DavideQuartucci.IO
{
    /// <summary>
    /// Describes which column of the raw CSV holds each field required by KghmRecord.
    /// Using a single configuration object, avoiding numbers inside the parsing logic.
    /// </summary>
    public class CsvColumnMapping
    {
        public int AdjCloseIndex { get; }
        public int VolumeIndex { get; }
        public int CopperPriceIndex { get; }
        public int SilverPriceIndex { get; }
        public int GoldPriceIndex { get; }
        public int EurUsdIndex { get; }
        public int PlnUsdIndex { get; }
        public int Wig20Index { get; }
        public int DateIndex { get; }

        /// <summary>
        /// Creates a column mapping from explicit indices. None of the indices has a default
        /// value: every position must be supplied by the caller.
        /// </summary>
        /// <param name="adjCloseIndex">Zero-based index of the adjusted closing price column.</param>
        /// <param name="volumeIndex">Zero-based index of the traded volume column.</param>
        /// <param name="copperPriceIndex">Zero-based index of the copper price column.</param>
        /// <param name="silverPriceIndex">Zero-based index of the silver price column.</param>
        /// <param name="goldPriceIndex">Zero-based index of the gold price column.</param>
        /// <param name="eurUsdIndex">Zero-based index of the EUR/USD exchange rate column.</param>
        /// <param name="plnUsdIndex">Zero-based index of the PLN/USD exchange rate column.</param>
        /// <param name="wig20Index">Zero-based index of the WIG20 index column.</param>
        /// <param name="dateIndex">Zero-based index of the trading date column.</param>
        public CsvColumnMapping(
            int adjCloseIndex,
            int volumeIndex,
            int copperPriceIndex,
            int silverPriceIndex,
            int goldPriceIndex,
            int eurUsdIndex,
            int plnUsdIndex,
            int wig20Index,
            int dateIndex)
        {
            AdjCloseIndex = adjCloseIndex;
            VolumeIndex = volumeIndex;
            CopperPriceIndex = copperPriceIndex;
            SilverPriceIndex = silverPriceIndex;
            GoldPriceIndex = goldPriceIndex;
            EurUsdIndex = eurUsdIndex;
            PlnUsdIndex = plnUsdIndex;
            Wig20Index = wig20Index;
            DateIndex = dateIndex;
        }

        /// <summary>
        /// Minimum number of columns a CSV row must have to be interpreted with this mapping.
        /// Computed dynamically from the configured indices.
        /// </summary>
        /// <returns>The highest configured column index, plus one.</returns>
        public int MinimumColumnCount()
        {
            int maxIndex = AdjCloseIndex;
            if (VolumeIndex > maxIndex) maxIndex = VolumeIndex;
            if (CopperPriceIndex > maxIndex) maxIndex = CopperPriceIndex;
            if (SilverPriceIndex > maxIndex) maxIndex = SilverPriceIndex;
            if (GoldPriceIndex > maxIndex) maxIndex = GoldPriceIndex;
            if (EurUsdIndex > maxIndex) maxIndex = EurUsdIndex;
            if (PlnUsdIndex > maxIndex) maxIndex = PlnUsdIndex;
            if (Wig20Index > maxIndex) maxIndex = Wig20Index;
            if (DateIndex > maxIndex) maxIndex = DateIndex;
            return maxIndex + 1;
        }
    }
}
