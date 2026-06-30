using System;
using System.Collections.Generic;
using KghmProject_DavideQuartucci.Models;

namespace KghmProject_DavideQuartucci.Processing
{
    /// <summary>
    /// Stateless utility class for reporting descriptive statistics on the dataset.
    /// Declared static because it holds no configuration and is never instantiated:
    /// it only exposes pure read-only operations over the records it is given.
    /// </summary>
    public static class DatasetAnalyzer
    {
        /// <summary>Prints mean, standard deviation, min and max of the raw (pre-standardization) features.</summary>
        /// <param name="records">Records as read from the source, before any feature engineering.</param>
        public static void PrintRawStatistics(List<KghmRecord> records)
        {
            (string Name, Func<KghmRecord, double> Select)[] rawColumns = new[]
            {
                ("KGHM Price", (Func<KghmRecord, double>)(r => r.AdjClose)),
                ("Copper Price", (Func<KghmRecord, double>)(r => r.CopperPrice)),
                ("Silver Price", (Func<KghmRecord, double>)(r => r.SilverPrice)),
                ("WIG20 Index", (Func<KghmRecord, double>)(r => r.Wig20))
            };

            Console.WriteLine("\nDescriptive statistics of the raw features:");
            Console.WriteLine($"{"Feature",-14} | {"Mean",10} | {"Std.Dev.",10} | {"Min",10} | {"Max",10}");
            foreach ((string name, Func<KghmRecord, double> select) in rawColumns)
            {
                double[] values = new double[records.Count];
                for (int i = 0; i < records.Count; i++)
                    values[i] = select(records[i]);

                double sum = 0.0;
                for (int i = 0; i < values.Length; i++)
                    sum += values[i];
                double mean = sum / values.Length;

                double variance = 0.0;
                double min = values[0];
                double max = values[0];
                for (int i = 0; i < values.Length; i++)
                {
                    variance += (values[i] - mean) * (values[i] - mean);
                    if (values[i] < min) min = values[i];
                    if (values[i] > max) max = values[i];
                }
                double std = Math.Sqrt(variance / values.Length);
                Console.WriteLine($"{name,-14} | {mean,10:F2} | {std,10:F2} | {min,10:F2} | {max,10:F2}");
            }
        }
    }
}
