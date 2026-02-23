namespace StressTest.Core.Models;

public class Rating
{
    public string CreditRating { get; set; } = string.Empty;
    /// <summary>Probability of Default as a percentage (e.g. 60 means 60%)</summary>
    public decimal ProbabilityOfDefault { get; set; }
}
