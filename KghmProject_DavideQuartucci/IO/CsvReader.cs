using System;
using System.Collections.Generic;
using System.IO;
using KghmProject_DavideQuartucci.Models;

namespace KghmProject_DavideQuartucci.IO
{
    /// <summary>
    /// Reads and parses a CSV file.
    /// </summary>
    public class CsvReader : IDataReader<KghmRecord>
    {
        private readonly string _filePath;
        private readonly CsvColumnMapping _columnMapping;

        /// <summary>
        /// Constructor accepting the file path and the column mapping.
        /// Avoids hardcoded strings and indices inside the methods.
        /// </summary>
        public CsvReader(string filePath, CsvColumnMapping columnMapping)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("The file path cannot be null or empty.", nameof(filePath));
            }
            _filePath = filePath;
            _columnMapping = columnMapping ?? throw new ArgumentNullException(nameof(columnMapping));
        }

        /// <summary>
        /// Reads the file sequentially and parses each line.
        /// </summary>
        public List<KghmRecord> ReadData()
        {
            var records = new List<KghmRecord>();

            if (!File.Exists(_filePath))
            {
                throw new FileNotFoundException($"Cannot find the dataset at the specified path: {_filePath}");
            }

            // 'using' guarantees the file handle is closed and released.
            using (var reader = new StreamReader(_filePath))
            {
                // Skip the header row before parsing numeric data.
                string? headerLine = reader.ReadLine();

                while (!reader.EndOfStream)
                {
                    string? line = reader.ReadLine();

                    // Blank line (e.g. trailing line at end of file): skip without parsing.
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string[] values = line.Split(',');

                    try
                    {
                        // Delegates parsing to the model's factory method, using the column indices
                        // supplied by the mapping injected in the constructor (no hardcoded index here).
                        KghmRecord record = KghmRecord.FromCsvLine(values, _columnMapping);
                        records.Add(record);
                    }
                    catch (Exception ex)
                    {
                        // Row discarded because it is malformed or has missing data: prevents a single
                        // dirty record from crashing the whole pipeline.
                        Console.WriteLine($"Error while parsing row: {ex.Message}");
                    }
                }
            }

            return records;
        }
    }
}