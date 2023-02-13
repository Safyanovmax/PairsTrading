using AppCore.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingWorker.Models;
using MathNet.Numerics.LinearRegression;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.Distributions;
using AppCore.Interfaces;
using AppCore.Models.Trading;

namespace TradingWorker.Services
{
    public class PairsTradingCalculationService : IPairsTradingCalculationService
    {
        class ParsedItem
        {
            public DateTime Date;
            public double ClosingPrice;
        }

        private readonly IConfiguration _configuration;

        public PairsTradingCalculationService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public PairsTradingCalculationData Calculate(TradePair pair)
        {
            var binanceCsvFile = GetFilePath(pair.CryptoCurrency, TradeExchangeType.Binance);
            var whitebitCsvFile = GetFilePath(pair.CryptoCurrency, TradeExchangeType.WhiteBIT);

            var binanceParsedItems = GetParsedItems(binanceCsvFile);
            var whitebitParsedItems = GetParsedItems(whitebitCsvFile);

            var binanceDates = binanceParsedItems.Select(r => r.Date).ToArray();
            var whitebitDates = whitebitParsedItems.Select(r => r.Date).ToArray();
            var datesInBoth = binanceDates.Intersect(whitebitDates).ToArray();

            var mergedData = new Dictionary<(DateTime, string), float>();
            foreach ((DateTime Date, float ClosingPrice) in
                binanceParsedItems
                .Where(r => datesInBoth.Contains(r.Date))
                .Select(r => (r.Date, (float)r.ClosingPrice)))
            {
                mergedData[(Date, "C1")] = ClosingPrice;
            }

            foreach ((DateTime Date, float ClosingPrice) in
                whitebitParsedItems
                .Where(r => datesInBoth.Contains(r.Date))
                .Select(r => (r.Date, (float)r.ClosingPrice)))
            {
                mergedData[(Date, "C2")] = ClosingPrice;
            }

            // Note the TimeSeriesTests class is defined below, and a key component of the project
            TimeSeriesTests tst = new TimeSeriesTests();

            // When performing tests for lagged timeseries, we want the lag to be at least 10, but for larger timeseries we allow it to be at a maximum of 1% of the observations -> timeseries with 8000 data points would yield regressions with 80 lags
            int lags = datesInBoth.Length / 100 > 10 ? datesInBoth.Length / 100 : 10;

            // Performing the ADF test, a Timeseries will be classified to be likely stationary if the according p-Value is below 1% -> 0.01, for values <= 0.01 the programm will assume non-stationarity
            float ADFp1 = tst.ADF(Column("C1", datesInBoth, mergedData), lags);
            float ADFp2 = tst.ADF(Column("C2", datesInBoth, mergedData), lags);

            bool stationary1 = ADFp1 < 0.01;
            bool stationary2 = ADFp2 < 0.01;

            string text1 = stationary1 ? "stationary" : "non-stationary";
            string text2 = stationary2 ? "stationary" : "non-stationary";
            Console.WriteLine($"\nThe Augmented Dickey-Fuller test yields a p-value of {ADFp1} for stock 1, concluding the Timeseries is {text1}.\nThe Augmented Dickey-Fuller test yields a p-value of {ADFp2} for stock 2, concluding the Timeseries is {text2}.");

            // Analysing for the cointegration
            if (!(stationary1 | stationary1))
            {
                // Both Timeseries are non-stationary, the next step is to Identify their order of Cointegration, this program will test up to the 3rd order of cointegration.
                // Higher degrees could be tested by adjusting the parameter below but would be difficult to interpret
                Console.WriteLine("\nPerforming the Engle-Granger Test to esitamte whether the stocks might be cointegrated... ");

                double[] series1 = Column("C1", datesInBoth, mergedData).Select(x => (double)x).ToArray();
                double[] series2 = Column("C2", datesInBoth, mergedData).Select(x => (double)x).ToArray();
                int DegreeOfCointegration = tst.EngelGrangerDegreeOfCointegration(
                    Column("C1", datesInBoth, mergedData)
                        .Select(x => (double)x).ToArray(),
                    Column("C2", datesInBoth, mergedData)
                        .Select(x => (double)x).ToArray(),
                    3,
                    lags);

                if (DegreeOfCointegration >= 1 & DegreeOfCointegration <= 3)
                {
                    Console.WriteLine($"The two Stocks are Cointegrated. Their degree of cointegration is {DegreeOfCointegration}!");
                    if (DegreeOfCointegration == 1)
                    {
                        Console.WriteLine("This indicates trading the Pairs Trading strategy could yield high returns for those two stocks!");
                    }
                    else if (DegreeOfCointegration == 2)
                    {
                        Console.WriteLine("This indicates trading the Pairs Trading strategy could yield high returns, but there might be more profitable pairs to trade!");
                    }
                    else
                    {
                        Console.WriteLine("This indicates trading the Pairs Trading strategy might be profitable, but the relationship of these two stocks is rather week!");
                    }
                }
                else
                {
                    Console.WriteLine("The two timeseries are not stationary, nor are they cointegrated of order 1, 2 or 3. Therfore, it is not advisable to try to apply the Pairs Trading strategy to this pair of Stocks!\nEnter Y to continue with the simulation anyway, otherwise the program will end:");
                    throw new Exception("Timeseries are not stationary");
                }
            }
            else
            {
                Console.WriteLine("At least one of the time series is already stationary!");
            }

            // Calculate the ratio of the stocks for each observation in the MergedData Dictionary
            // A high ratio means Stock1 is relativley overvalued, a low ratio means Stock 2 is relatively overvalued, a ratio close to the average of the ratios indicates a "good" relative valuation and thus the absence
            float[] ratioTS = Column("C1", datesInBoth, mergedData)
                .Zip(Column("C2", datesInBoth, mergedData),
                    (first, second) => (float)first / second)
                .ToArray();

            float avgRatio = ratioTS.Average();

            float stdRatio = (float)Math.Sqrt(ratioTS.Select(x => Math.Pow((x - avgRatio), 2)).Sum() / (ratioTS.Length - 1));

            // Now we defin the size of an actionable deviation
            // Under the assumption of a normal distibution, 68% of probability mass are within 1 std from the mean and 95% within 2 std from the mean.
            // we want to trade only a few and thus hopefully very profitable deviations, so our action level should be somewhere between 1 and 2, here I chose 1.8
            //float actionDeviation = (float)0.9; // or 1.2 If the ratio deviates more than 1.8 std from the mean, we will open a trade

            return new PairsTradingCalculationData
            {
                StandardRatio = Convert.ToDecimal(stdRatio),
                ActionDeviation = Convert.ToDecimal(pair.ActionDeviation),
                AverageRatio = Convert.ToDecimal(avgRatio)
            };
        }

        private float[] Column(string colname, DateTime[] datesInBoth, Dictionary<(DateTime, string), float> mergedData)
        {
            float[] values = new float[datesInBoth.Length];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = mergedData[(datesInBoth[i], colname)];
            }
            return values;
        }

        private List<ParsedItem> GetParsedItems(string filePath)
        {
            var lines = File.ReadAllLines(filePath);

            //bool warning = false;
            List<ParsedItem> data = new List<ParsedItem>();
            for (var index = 1; index < lines.Length; index++)
            {
                var row = lines[index];
                try
                {
                    string[] observations = row.Split(Convert.ToChar(','));
                    data.Add(new ParsedItem
                    {
                        Date = DateTimeOffset.FromUnixTimeSeconds(long.Parse(observations[0])).UtcDateTime,
                        ClosingPrice = Convert.ToDouble(observations[4])
                    });
                }
                catch
                {
                    //if (!warning)
                    //{
                    //    //Console.WriteLine($"Sometimes these files contain data that violates the expected format or that is missing. While reading in the file: '{path}' some errors occurred, the program will continue with the readable data!");
                    //    warning = true;
                    //}
                }
            }

            return data;
        }

        private string GetFilePath(TradeCurrencyType currencyType, TradeExchangeType exchangeType)
        {
            return _configuration.GetSection(exchangeType.ToString())[$"{currencyType}HistoricalDataFilePath"];
        }
    }

    class TimeSeriesTests
    {
        // Regress one timeseries on the other, test if the residuals of this regression satisfy requirement of stationarity by performing the Augmented Dickey Fuller Test on them and returning its p-Value
        public float EngelGrangerRegression(double[] values1, double[] values2, int lag)
        {
            if (values1.Length != values2.Length) { throw new ArgumentException($"EngelGranger test requires bot inputs to be of same lenght, the provided inputs are of length {values1.Length} and {values2.Length}"); }
            // The Engel-Granger Two Step Test for Cointegration, requires a method (like ADF) to test time series data for stationarity

            // simple regression of values1 = beta_0 + beta_1 * values2 + error
            var regModel = new MultipleRegressionModel();
            var y = DenseVector.OfArray(values1);
            var x = DenseMatrix.OfColumnArrays(values2);
            regModel.Fit(y, x);
            float[] u = regModel.errors.Select(i => (float)i).ToArray();
            // return ADF for the residuals
            return ADF(u, lag);
        }

        // Determine the Degree of Cointegration by taking the (lag) differences of two timeseries and performing the EngleGrangerRegression, until p-value is small enough or the maximum is reached
        // will test until ADF identifies stationarity, max should be left at 3, if no cointegration of 3rd degree the method returns -1 for no cointegration
        // Non-stationary inputs are assumed (this program  tested earlier for stationarity of inputs, no unit test)
        public int EngelGrangerDegreeOfCointegration(double[] series1, double[] series2, int maxDegree = 3, int lags = 10) // lag is only relevant for the ADF, should be used consistently in the programm
        {
            // performs the EG regression for the differenced inputs, returns the degree of differencing needed to reach stationary residuals in the regression and thus finds the order of cointegration
            for (int i = 1; i <= maxDegree; i++)
            {
                float pValue = EngelGrangerRegression(DifferenceOfDegree(series1, i), DifferenceOfDegree(series2, i), lags);
                if (pValue < 0.01)
                {
                    return i;
                }
            }
            return -1;
        }

        // Perform the Augmented Dickey Fuller Test on a timeseries with a given number of lags
        public float ADF(float[] values, int lag = 1)
        {
            // To keep the regression feasable in terms of the proportion of features and observations, the lag will be restricted to the squareroot of all observations
            // This relation was arbitrarly choosen and could be changed -> however a decreasing growth rate for the lag in the number of observations, is desirable for run time efficiency and computational precision
            if (lag > Math.Sqrt(values.Length)) { lag = Convert.ToInt32(Math.Sqrt(values.Length)); } // A warning could be added


            // The reasoning of the following code follows the ADF as described here: https://nwfsc-timeseries.github.io/atsa-labs/sec-boxjenkins-aug-dickey-fuller.html
            // The ADF performs a regresssion where the Dependent variable y_delta is regressed on the lag one of y_t and multiple lags of the differenced timeseries
            // First calculate the set of dependent variables
            double[] getYDifferences()
            {
                double[] results = new double[values.Length - 1];
                for (int i = 0; i < results.Length; i++)
                {
                    results[i] = (double)values[i + 1] - values[i];
                }
                return results;
            };
            double[] YDifferences = getYDifferences();
            // Because the ADF requires YDifferences to be regressed on its own lags (p), the number dependent variables for the observation are actually YDifferences.Lenght - lag
            int length = YDifferences.Length - lag;
            // alpha will be generated by allowing the regression to have an intercept, the other parameters are defined in the lines below

            // beta is defined as being the coefficient of t, thus the regression will require a count variable for the observatins
            int[] t_values = Enumerable.Range(0, length).ToArray();

            // The regression requires the last <length> observations of y_t with lag one, this is the parameter whose coefficient will be used to create the test statistic
            float[] y_1 = new float[length];
            Array.Copy(values, lag, y_1, 0, length); // lag does not need to be corrected by -1 as the YDifferences are calculated looking forward

            // define a function to get the differences in yDifferences with the desired shift -> shift 0 yields the dependent variable
            double[] YDifferencesWithLag(int shift)
            {
                double[] yDiff = new double[length];
                Array.Copy(YDifferences, lag - shift, yDiff, 0, length);
                return yDiff;
            }

            // Y_t - Y_{t-1}-> the dependent variable of the regression
            var y = DenseVector.OfArray(YDifferencesWithLag(0));

            // define a matrix with the lagged YDifferences, as features, a constant to facilitate an intercept in the regresion is added by the regressionmodel
            var x_values = new Vector<double>[lag + 2]; // plus 2 for  t_values and y_1
            x_values[0] = new DenseVector(t_values.Select(i => Convert.ToDouble(i)).ToArray());
            x_values[1] = new DenseVector(y_1.Select(i => (double)i).ToArray());

            // Fill the columns of the matrix with the lagged timeseries values (lag is increasing -> i)
            for (int i = 0; i < lag; i++)
            {
                float[] ydiff_t_i = new float[length];
                Array.Copy(values, lag - (i + 1), ydiff_t_i, 0, length);
                x_values[i + 2] = new DenseVector(Array.ConvertAll(ydiff_t_i, v => (double)v));
            }
            var x = DenseMatrix.OfColumns(x_values);
            // Perform a regression
            var rm = new MultipleRegressionModel();
            rm.Fit(y, x);
            // return the p-Value for the Intercept

            float testStatistic = (float)(((float)rm.betas.ToArray()[2]) / Math.Sqrt(rm.VarSEbyJ(2))); // cf. for ADF statistic with mutliple regressors (not shown in the link above)  https://en.wikipedia.org/wiki/Augmented_Dickey%E2%80%93Fuller_test
            return (float)(StudentT.CDF(0, 1, length - x.ColumnCount, testStatistic)); // The test is a onesided, testing for t-values beyond the critical threshold -> a sufficiently low CDF value here implies stationarity
        }

        public static double[] Difference(double[] values)
        {
            double[] result = new double[values.Length - 1];
            for (int i = 0; i < result.Length; i++) { result[i] = values[i + 1] - values[i]; }
            return result;
        }

        public double[] DifferenceOfDegree(double[] values, int degree)
        {
            for (int i = 1; i <= degree; i++) { values = Difference(values); }
            return values;
        }
    }

    class MultipleRegressionModel
    {
        public DenseMatrix x;
        public DenseVector y;
        public Vector<double> betas;
        public Vector<double> predictions;
        public Vector<double> errors;
        public double r_squared;
        public double r_squared_adjusted;
        private bool intercept;
        public int N;
        public int k;
        public void Fit(DenseVector y_train, DenseMatrix x_train, bool constant = true)
        {
            intercept = constant;
            y = y_train;
            if (intercept)
            {
                var features = new Vector<double>[x_train.ColumnCount + 1];
                features[0] = DenseVector.OfEnumerable(Enumerable.Repeat<double>(1, y.Count));
                for (int column = 0; column < x_train.ColumnCount; column++) { features[1 + column] = x_train.Column(column); }
                x = DenseMatrix.OfColumns(features);
            }
            else { x = x_train; }
            N = y.Count;
            k = x_train.ColumnCount;
            betas = MultipleRegression.QR(x, y);
            predictions = x.Transpose().LeftMultiply(betas);
            errors = y - predictions;
            r_squared = 1 - (Variance(errors) / Variance(y)); // R^2 = 1 - RSS/TSS
            r_squared_adjusted = 1 - ((1 - r_squared) * (N - 1) / (N - k - 1)); // R^2-Adjusted = 1 - (1-R^2)(N-1) / (N-k-1)
        }
        public static double Variance(Vector<double> values)
        {
            return values.Subtract(values.Average()).PointwisePower(2).Sum() / (values.Count - 1);
        }
        public Vector<double> BetaStandardDeviation()
        {
            double s2 = ErrorVariance();
            var s_squared = DenseVector.OfEnumerable(Enumerable.Repeat<double>(s2, x.ColumnCount));
            var ValueMatrix = x.TransposeThisAndMultiply(x).Inverse();
            var variancesOfEstimators = (s_squared * ValueMatrix).PointwiseSqrt();
            if (intercept)
            {
                // This code is to calculate the Variance of a multiple regression intercept estimator, cf. for mathematical deriviation https://math.stackexchange.com/questions/2916052/whats-the-variance-of-intercept-estimator-in-multiple-linear-regression
                var y_mat = x.RemoveColumn(0); // x without the intercept column
                var y_bar = y_mat.ColumnSums().ToColumnMatrix();
                var matrixCalculation = y_bar.Transpose() * (y_mat.Transpose() * y_mat - (1 / N) * (y_bar * y_bar.Transpose())).Inverse() * y_bar;
                var varianceBeta0 = (s2 / N + (s2 / (N * N)) * matrixCalculation).ToArray()[0, 0];
                variancesOfEstimators[0] = varianceBeta0;
            }
            // For larger matrices the algebraic method could not calculate all values and insted yielded some Nan values, the following code replaces the missin standard errors with estimates from a new regression cf, 
            for (int i = 0; i < variancesOfEstimators.Count; i++) { if (double.IsNaN(variancesOfEstimators[i])) { variancesOfEstimators[i] = VarSEbyJ(i); } }
            return variancesOfEstimators.PointwiseSqrt();
        }
        public Vector<double> TStatistics()
        {
            return betas / BetaStandardDeviation();
        }
        public Vector<double> PValues()
        {
            Vector<double> result = TStatistics();
            int df = intercept ? N - k - 1 : N - k;
            result.PointwiseAbs().Map(t => 2 * (1 - StudentT.CDF(0, 1, df, t)), result);
            return result;
        }
        public double VarSEbyJ(int j)
        {
            var new_y = DenseVector.OfVector(x.Column(j));
            var new_x = DenseMatrix.OfMatrix(x.RemoveColumn(j));
            var helpRegression = new MultipleRegressionModel();
            helpRegression.Fit(new_y, new_x, !intercept); // here x already has a constant column, so the regression model should not add another
            return Variance(errors) / (Variance(new_y) * (1 - helpRegression.r_squared));
        }
        public double ErrorVariance() { return Variance(errors); }
    }
}