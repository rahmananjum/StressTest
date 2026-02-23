namespace StressTest.Core.Models;

/// <summary>Represents a saved stress test run in the database.</summary>
public class StressTestRun
{
    public int Id { get; set; }
    public DateTime RunAt { get; set; }
    public long DurationMs { get; set; }
    public string CountryInputsJson { get; set; } = string.Empty;
    public int TotalPortfolios { get; set; }
    public int TotalLoans { get; set; }
    public decimal TotalExpectedLoss { get; set; }

    public ICollection<StressTestRunResult> Results { get; set; } = new List<StressTestRunResult>();
}
