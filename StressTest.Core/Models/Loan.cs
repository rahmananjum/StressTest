namespace StressTest.Core.Models;

public class Loan
{
    public int LoanId { get; set; }
    public int PortId { get; set; }
    public decimal OriginalLoanAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public decimal CollateralValue { get; set; }
    public string CreditRating { get; set; } = string.Empty;
}
