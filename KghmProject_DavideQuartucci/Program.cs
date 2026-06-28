using System;
using System.Collections.Generic;
using KghmProject_DavideQuartucci.IO;
using KghmProject_DavideQuartucci.ML;
using KghmProject_DavideQuartucci.Models;
using KghmProject_DavideQuartucci.Processing;

namespace KghmProject_DavideQuartucci
{
    /// <summary>
    /// Entry point: wires the IO, Models and Processing components together and runs the
    /// preprocessing and classification pipeline on the KGHM dataset.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            // Configuration is centralized here so the IO/Models/Processing classes stay
            // generic and reusable with different inputs.
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
            double downsideThreshold = -0.015; // next-day log-return threshold for downside risk

            try
            {
                Console.WriteLine("Phase 1: Data Loading & Preprocessing");

                IDataReader<KghmRecord> reader = new CsvReader(csvFilePath, columnMapping);
                List<KghmRecord> records = reader.ReadData();

                DatasetAnalyzer.PrintRawStatistics(records);

                List<IFeatureExtractor> extractors = new List<IFeatureExtractor>
                {
                    new LogReturnExtractor(),
                    new MovingAverageExtractor(movingAveragePeriod)
                };
                ITargetLabeler targetLabeler = new DownsideRiskLabeler(downsideThreshold);

                // The features included in X are decided here, not inside DataPreprocessor,
                // which stays generic and reusable with any subset of features.
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
                preprocessor.Process(records);

                double[][] features;
                int[] targets;
                preprocessor.BuildDataset(records, out features, out targets);

                Console.WriteLine($"Loaded {records.Count} records, {targets.Length} valid after feature extraction.");
                Console.WriteLine($"Feature matrix shape: {features.Length}x{featureSelectors.Count}.");

                PrintDatasetSummary(targets, features, featureNames);

                RiskClassifier classifier = new RiskClassifier(features, targets);
                classifier.SplitDataset();
                classifier.TuneRegularization();
                classifier.TrainFinalModel();
                classifier.Evaluate();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Execution failed: {ex.Message}");
                Console.ResetColor();
            }
        }

        /// <summary>Prints the class distribution and the last few standardized rows of the dataset.</summary>
        private static void PrintDatasetSummary(int[] targets, double[][] features, string[] featureNames)
        {
            int total = targets.Length;
            if (total == 0)
            {
                Console.WriteLine("No valid records available for training.");
                return;
            }

            int numPositives = 0;
            foreach (int target in targets)
            {
                if (target == 1) numPositives++;
            }

            Console.WriteLine($"Class 1 (downside risk): {numPositives} ({(double)numPositives / total * 100:F2}%)");
            Console.WriteLine($"Class 0 (stable)       : {total - numPositives} ({(double)(total - numPositives) / total * 100:F2}%)");

            Console.WriteLine("\nLast 5 rows of the standardized dataset:");
            Console.Write("Idx  | Target |");
            foreach (string name in featureNames)
            {
                Console.Write($" {name,12} |");
            }
            Console.WriteLine();

            int startIndex = Math.Max(0, total - 5);
            for (int i = startIndex; i < total; i++)
            {
                Console.Write($"{i:D4} | {targets[i],6} |");
                foreach (double value in features[i])
                {
                    Console.Write($" {value,12:F4} |");
                }
                Console.WriteLine();
            }
        }
    }
}
