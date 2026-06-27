namespace KghmProject_DavideQuartucci.ML
{
    /// <summary>Common lifecycle for supervised binary classifiers used in this pipeline.</summary>
    public interface IClassifier
    {
        /// <summary>Splits the dataset into training and test sets.</summary>
        void SplitDataset();

        /// <summary>Trains the final model on the whole training set.</summary>
        void TrainFinalModel();

        /// <summary>Predicts the class label for each row of the given feature matrix.</summary>
        /// <param name="features">Feature matrix, one row per observation.</param>
        /// <returns>Predicted label (0 or 1) for each row.</returns>
        int[] Predict(double[][] features);

        /// <summary>Evaluates the trained model on the test set and prints a report.</summary>
        void Evaluate();
    }
}
