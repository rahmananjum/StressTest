namespace StressTest.Core.Models;

public class PortfolioResult
{
    public int PortId { get; set; }
    public string PortName { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal TotalOutstandingAmount { get; set; }
    public decimal TotalCollateralValue { get; set; }
    public decimal TotalScenarioCollateralValue { get; set; }
    public decimal TotalExpectedLoss { get; set; }
    public int LoanCount { get; set; }
}
