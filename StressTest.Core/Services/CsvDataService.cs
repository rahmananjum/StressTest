using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using StressTest.Core.Models;

namespace StressTest.Core.Services;

public interface ICsvDataService
{
    IReadOnlyList<Portfolio> LoadPortfolios();
    IReadOnlyList<Loan> LoadLoans();
    IReadOnlyList<Rating> LoadRatings();
}

public class CsvDataService : ICsvDataService
{
    private readonly string _dataDirectory;

    public CsvDataService(string dataDirectory)
    {
        _dataDirectory = dataDirectory;
    }

    public IReadOnlyList<Portfolio> LoadPortfolios()
    {
        return Read<PortfolioCsvRecord, PortfolioMap>(Path.Combine(_dataDirectory, "portfolios.csv"))
            .Select(r => new Portfolio
            {
                PortId = r.Port_ID,
                PortName = r.Port_Name,
                PortCountry = r.Port_Country,
                PortCcy = r.Port_CCY
            })
            .ToList();
    }

    public IReadOnlyList<Loan> LoadLoans()
    {
        return Read<LoanCsvRecord, LoanMap>(Path.Combine(_dataDirectory, "loans.csv"))
            .Select(r => new Loan
            {
                LoanId = r.Loan_ID,
                PortId = r.Port_ID,
                OriginalLoanAmount = r.OriginalLoanAmount,
                OutstandingAmount = r.OutstandingAmount,
                CollateralValue = r.CollateralValue,
                CreditRating = r.CreditRating
            })
            .ToList();
    }

    public IReadOnlyList<Rating> LoadRatings()
    {
        return Read<RatingCsvRecord, RatingMap>(Path.Combine(_dataDirectory, "ratings.csv"))
            .Select(r => new Rating
            {
                CreditRating = r.Rating,
                ProbabilityOfDefault = r.ProbablilityOfDefault
            })
            .ToList();
    }

    private static List<TRecord> Read<TRecord, TMap>(string path)
        where TMap : ClassMap<TRecord>
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim
        };

        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<TMap>();
        return csv.GetRecords<TRecord>().ToList();
    }

    // ── CSV record types and maps ────────────────────────────────────────────

    private class PortfolioCsvRecord
    {
        public int Port_ID { get; set; }
        public string Port_Name { get; set; } = string.Empty;
        public string Port_Country { get; set; } = string.Empty;
        public string Port_CCY { get; set; } = string.Empty;
    }

    private sealed class PortfolioMap : ClassMap<PortfolioCsvRecord>
    {
        public PortfolioMap()
        {
            Map(m => m.Port_ID).Name("Port_ID");
            Map(m => m.Port_Name).Name("Port_Name");
            Map(m => m.Port_Country).Name("Port_Country");
            Map(m => m.Port_CCY).Name("Port_CCY");
        }
    }

    private class LoanCsvRecord
    {
        public int Loan_ID { get; set; }
        public int Port_ID { get; set; }
        public decimal OriginalLoanAmount { get; set; }
        public decimal OutstandingAmount { get; set; }
        public decimal CollateralValue { get; set; }
        public string CreditRating { get; set; } = string.Empty;
    }

    private sealed class LoanMap : ClassMap<LoanCsvRecord>
    {
        public LoanMap()
        {
            Map(m => m.Loan_ID).Name("Loan_ID");
            Map(m => m.Port_ID).Name("Port_ID");
            Map(m => m.OriginalLoanAmount).Name("OriginalLoanAmount");
            Map(m => m.OutstandingAmount).Name("OutstandingAmount");
            Map(m => m.CollateralValue).Name("CollateralValue");
            Map(m => m.CreditRating).Name("CreditRating");
        }
    }

    private class RatingCsvRecord
    {
        public string Rating { get; set; } = string.Empty;
        public decimal ProbablilityOfDefault { get; set; }
    }

    private sealed class RatingMap : ClassMap<RatingCsvRecord>
    {
        public RatingMap()
        {
            Map(m => m.Rating).Name("Rating");
            Map(m => m.ProbablilityOfDefault).Name("ProbablilityOfDefault");
        }
    }
}
