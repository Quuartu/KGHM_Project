using System.Collections.Generic;

namespace KghmProject_DavideQuartucci.IO
{
    /// <summary>
    /// Generic interface for reading data sources.
    /// </summary>
    /// <typeparam name="T">The record type returned by the read operation.</typeparam>
    public interface IDataReader<T>
    {
        /// <summary>
        /// Reads data from the source and returns a list of objects of type T.
        /// </summary>
        /// <returns>The list of records read from the source, in source order.</returns>
        List<T> ReadData();
    }
}