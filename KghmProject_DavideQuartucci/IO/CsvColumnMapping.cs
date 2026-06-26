namespace KghmProject_DavideQuartucci.IO
{
    /// <summary>
    /// Describes which column of the raw CSV holds each field required by KghmRecord.
    /// Centralizes all column indices in a single configuration object, avoiding
    /// magic numbers inside the parsing logic.
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
        /// Computed dynamically from the configured indices, not hardcoded.
        /// </summary>
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
