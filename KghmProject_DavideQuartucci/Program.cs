using System;
using System.Collections.Generic;
using KghmProject_DavideQuartucci.IO;
using KghmProject_DavideQuartucci.Models;
using KghmProject_DavideQuartucci.Processing;

namespace KghmProject_DavideQuartucci
{
    /// <summary>
    /// Composition root and entry point: wires together the IO, Models and Processing
    /// classes with explicit configuration values, and runs the full preprocessing pipeline.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Application entry point. Reads the dataset, runs feature engineering and target
        /// labeling, builds the standardized dataset for Accord.NET, and prints a summary.
        /// </summary>
        /// <param name="args">Command-line arguments (not used).</param>
        static void Main(string[] args)
        {
            // =================================================================================
            // SINGLE CONFIGURATION POINT OF THE APPLICATION (composition root).
            // File path, column indices, MA period and risk threshold are never written inside
            // the business logic classes: they are defined here and passed to the constructors,
            // so the IO/Models/Processing classes remain reusable and testable.
            // =================================================================================
            string csvFilePath = @"../archive/kghm.csv";

            CsvColumnMapping columnMapping = new CsvColumnMapping(
                adjCloseIndex: 1,
                volumeIndex: 2,
                copperPriceIndex: 3,
                silverPriceIndex: 4,
                goldPriceIndex: 5,
                eurUsdIndex: 6,
                plnUsdIndex: 9,
                wig20Index: 23,
                dateIndex: 33);

            int movingAveragePeriod = 50;
            double downsideThreshold = -0.015; // Log-return threshold for next-day downside risk (-1.5%)

            Console.WriteLine("=== STARTING KGHM DATA PROCESSING PIPELINE ===");

            try
            {
                // --- 1. Reading and Cleaning -----------------------------------------------------
                IDataReader<KghmRecord> reader = new CsvReader(csvFilePath, columnMapping);

                Console.WriteLine("[INFO] Reading CSV file...");
                List<KghmRecord> records = reader.ReadData();
                Console.WriteLine($"[OK] Read complete. Records imported: {records.Count}");

                // --- 2. Feature Extraction, temporal consistency, Target Labeling and feature selection ---
                List<IFeatureExtractor> extractors = new List<IFeatureExtractor>
                {
                    new LogReturnExtractor(),
                    new MovingAverageExtractor(movingAveragePeriod)
                };
                ITargetLabeler targetLabeler = new DownsideRiskLabeler(downsideThreshold);

                // The features included in X are decided here, not inside DataPreprocessor:
                // the class stays generic and reusable with any subset of features.
                List<Func<KghmRecord, double>> featureSelectors = new List<Func<KghmRecord, double>>
                {
                    record => record.LogReturnAdjClose,
                    record => record.LogReturnCopper,
                    record => record.LogReturnSilver,
                    record => record.PriceToMa50Ratio,
                    record => record.Volume,
                    record => record.EurUsd,
                    record => record.PlnUsd,
                    record => record.Wig20,
                    record => record.GoldPrice
                };
                string[] featureNames = new string[]
                {
                    "LogRet_KGHM", "LogRet_Copper", "LogRet_Silver", "Ratio_MA50",
                    "Volume", "EUR/USD", "PLN/USD", "WIG20", "Gold"
                };

                DataPreprocessor preprocessor = new DataPreprocessor(extractors, targetLabeler, featureSelectors);

                Console.WriteLine("[INFO] Computing features (Log-Returns, MA50, Target Class)...");
                preprocessor.Process(records);
                Console.WriteLine("[OK] Data processing complete.");

                // --- 3. Standardization and final formatting for Accord.NET ----------------------
                double[][] features;
                int[] targets;
                preprocessor.BuildDataset(records, out features, out targets);
                Console.WriteLine($"[OK] Dataset ready for Accord.NET: X = [{features.Length} x {featureSelectors.Count}], y = [{targets.Length}]");

                // Print diagnostic summary report
                PrintExecutionSummary(records, features, targets, featureNames);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[CRITICAL ERROR] An error occurred during execution: {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine("=============================================");
        }

        /// <summary>
        /// Analyzes the processed dataset and prints a control summary report.
        /// </summary>
        private static void PrintExecutionSummary(List<KghmRecord> records, double[][] features, int[] targets, string[] featureNames)
        {
            int validCount = targets.Length;
            if (validCount == 0)
            {
                Console.WriteLine("\n[WARNING] No valid records available for training.");
                return;
            }

            int riskDays = 0;
            foreach (int targetValue in targets)
            {
                if (targetValue == 1) riskDays++;
            }

            Console.WriteLine("\n--- DATASET METRICS SUMMARY ---");
            Console.WriteLine($"Total historical observations read: {records.Count}");
            Console.WriteLine($"Observations valid for training (excluding MA50 warm-up and last day): {validCount}");
            Console.WriteLine($"Days with downside risk on the following day (Target = 1): {riskDays} ({(double)riskDays / validCount * 100:F2}%)");
            Console.WriteLine($"Stable/bullish days (Target = 0): {validCount - riskDays} ({(double)(validCount - riskDays) / validCount * 100:F2}%)");

            Console.WriteLine("\n--- SAMPLE CHECK (LAST 5 ROWS OF STANDARDIZED X, y) ---");
            Console.Write("Idx  | Target |");
            foreach (string name in featureNames)
            {
                Console.Write($" {name,12} |");
            }
            Console.WriteLine();

            int startIndex = Math.Max(0, validCount - 5);
            for (int i = startIndex; i < validCount; i++)
            {
                Console.Write($"{i:D4} | {targets[i],6} |");
                foreach (double value in features[i])
                {
                    Console.Write($" {value,12:F4} |");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
    }
}
