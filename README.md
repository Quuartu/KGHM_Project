# KGHM Downside Risk Prediction

This project is a C# academic implementation of a decision support system for financial risk management in the mining sector. It aims to predict the probability of a bearish (downside) movement for the KGHM stock one day in advance, using market data and commodity prices.

## Project Overview
The system processes raw historical market data, extracts relevant financial features, and performs a binary classification using a Logistic Regression model powered by the **Accord.NET** framework.

## Key Features
- **OOP Architecture**: Modular design following strict SOLID principles, featuring interfaces, inheritance, and encapsulation.
- **Data Preprocessing Pipeline**:
  - Raw CSV data ingestion using custom `IO` components.
  - Dynamic feature engineering (Log-returns, Moving Averages).
  - Feature standardization (Z-Score) to ensure model stability.
- **Machine Learning**: Binary classification using `Accord.Statistics.Models.Regression.LogisticRegression`.
- **Professional Standards**: No hardcoded values, XML-documented code, and fully configurable parameters.

## Architecture
The project is organized into logical layers:
- `Models`: Data representations (`KghmRecord`).
- `IO`: Data ingestion strategies (`CsvReader`, `CsvColumnMapping`).
- `Processing`: Mathematical feature extraction (`FeatureExtractorBase`, `LogReturnExtractor`, `MovingAverageExtractor`).
- `ML`: Training and evaluation logic using Accord.NET.

## Requirements
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- [Accord.NET Framework](http://accord-framework.net/)

## Usage
1. Place your `kghm.csv` file in the appropriate directory.
2. Configure column mappings in `Program.cs` (or via `CsvColumnMapping`).
3. Run the project using:
   ```bash
   dotnet run
