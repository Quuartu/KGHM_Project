using System;

namespace KghmProject_DavideQuartucci.Processing
{
    /// <summary>
    /// Z-score standardizer that fits on a training split and transforms any split,
    /// preventing data leakage by keeping test statistics out of the training fit.
    /// </summary>
    public class Standardizer
    {
        private double[] _means = Array.Empty<double>();
        private double[] _stds = Array.Empty<double>();

        /// <summary>Computes per-column mean and population standard deviation from <paramref name="train"/>.</summary>
        /// <exception cref="ArgumentException">Thrown when <paramref name="train"/> is null or empty.</exception>
        public void Fit(double[][] train)
        {
            if (train == null || train.Length == 0)
                throw new ArgumentException("Training matrix must not be empty.", nameof(train));

            int cols = train[0].Length;
            _means = new double[cols];
            _stds = new double[cols];

            for (int col = 0; col < cols; col++)
            {
                double sum = 0;
                for (int row = 0; row < train.Length; row++)
                    sum += train[row][col];
                _means[col] = sum / train.Length;

                double sumSq = 0;
                for (int row = 0; row < train.Length; row++)
                {
                    double d = train[row][col] - _means[col];
                    sumSq += d * d;
                }
                _stds[col] = Math.Sqrt(sumSq / train.Length);
            }
        }

        /// <summary>
        /// Applies the fitted z-score transform to <paramref name="data"/>.
        /// Columns with zero standard deviation are left at 0.
        /// </summary>
        /// <returns>A new matrix with the same shape as <paramref name="data"/>, standardized column by column.</returns>
        /// <exception cref="InvalidOperationException">Thrown when called before <see cref="Fit"/>.</exception>
        public double[][] Transform(double[][] data)
        {
            if (_means.Length == 0)
                throw new InvalidOperationException("Fit must be called before Transform.");

            int cols = _means.Length;
            double[][] result = new double[data.Length][];
            for (int row = 0; row < data.Length; row++)
            {
                result[row] = new double[cols];
                for (int col = 0; col < cols; col++)
                    result[row][col] = _stds[col] > 0
                        ? (data[row][col] - _means[col]) / _stds[col]
                        : 0.0;
            }
            return result;
        }
    }
}
