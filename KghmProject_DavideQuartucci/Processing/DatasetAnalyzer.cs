using System;
using System.Collections.Generic;
using System.Linq;
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
            foreach (var (name, select) in rawColumns)
            {
                double[] values = records.Select(select).ToArray();
                double mean = values.Average();
                double std = Math.Sqrt(values.Sum(v => (v - mean) * (v - mean)) / values.Length);
                Console.WriteLine($"{name,-14} | {mean,10:F2} | {std,10:F2} | {values.Min(),10:F2} | {values.Max(),10:F2}");
            }
        }
    }
}
