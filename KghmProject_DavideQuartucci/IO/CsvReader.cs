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
        /// Creates a reader for the given file using the given column mapping.
        /// </summary>
        /// <param name="filePath">Path of the CSV file to read. </param>
        /// <param name="columnMapping">Mapping describing which column holds each required field.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is null, empty or whitespace.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="columnMapping"/> is null.</exception>
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
        /// Reads the file sequentially, parsing each line into a KghmRecord. Rows that are blank,
        /// malformed or contain missing data are skipped instead of stopping the read.
        /// </summary>
        /// <returns>The list of successfully parsed records, in file order.</returns>
        /// <exception cref="FileNotFoundException">Thrown when no file exists at the configured path.</exception>
        public List<KghmRecord> ReadData()
        {
            List<KghmRecord> records = new List<KghmRecord>();

            if (!File.Exists(_filePath))
            {
                throw new FileNotFoundException($"Cannot find the dataset at the specified path: {_filePath}");
            }

            // 'using' guarantees the file handle is closed and released.
            using (StreamReader reader = new StreamReader(_filePath))
            {
                // Skip the header row before parsing numeric data; its content is not needed.
                reader.ReadLine();

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