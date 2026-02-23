using StressTest.Core.Models;

namespace StressTest.Core.Services;

public interface IStressTestCalculator
{
    /// <summary>
    /// Runs the stress test for the given country percentage changes and
    /// returns aggregated results grouped by portfolio.
    /// </summary>
    /// <param name="countryChanges">
    /// Dictionary of country code -> percentage change (e.g. "GB" -> -5.12).
    /// This is a raw percentage, so -5.12 means collateral is multiplied by (1 + (-5.12/100)).
    /// </param>
    IReadOnlyList<PortfolioResult> Calculate(
        IReadOnlyDictionary<string, decimal> countryChanges,
        IReadOnlyList<Portfolio> portfolios,
        IReadOnlyList<Loan> loans,
        IReadOnlyList<Rating> ratings);
}

public class StressTestCalculator : IStressTestCalculator
{
    public IReadOnlyList<PortfolioResult> Calculate(
        IReadOnlyDictionary<string, decimal> countryChanges,
        IReadOnlyList<Portfolio> portfolios,
        IReadOnlyList<Loan> loans,
        IReadOnlyList<Rating> ratings)
    {
        // Build fast lookup dictionaries
        var portfolioById = portfolios.ToDictionary(p => p.PortId);
        var pdByRating = ratings.ToDictionary(
            r => r.CreditRating,
            r => r.ProbabilityOfDefault / 100m,   // convert percentage to decimal fraction
            StringComparer.OrdinalIgnoreCase);

        // Group loans by portfolio
        var loansByPortfolio = loans.GroupBy(l => l.PortId);

        var results = new List<PortfolioResult>();

        foreach (var group in loansByPortfolio)
        {
            if (!portfolioById.TryGetValue(group.Key, out var portfolio))
                continue;

            // Get the country-specific percentage change (default 0 if not supplied)
            countryChanges.TryGetValue(portfolio.PortCountry, out var pctChange);
            var multiplier = 1m + (pctChange / 100m);

            decimal totalOutstanding = 0m;
            decimal totalCollateral = 0m;
            decimal totalScenarioCollateral = 0m;
            decimal totalExpectedLoss = 0m;
            int loanCount = 0;

            foreach (var loan in group)
            {
                totalOutstanding += loan.OutstandingAmount;
                totalCollateral += loan.CollateralValue;

                var scenarioCollateral = loan.CollateralValue * multiplier;
                totalScenarioCollateral += scenarioCollateral;

                // RR = Scenario Collateral Value / Loan Amount (Outstanding)
                // Guard against division by zero
                decimal rr = loan.OutstandingAmount != 0
                    ? scenarioCollateral / loan.OutstandingAmount
                    : 0m;

                // LGD = 1 - RR
                decimal lgd = 1m - rr;

                // PD lookup
                pdByRating.TryGetValue(loan.CreditRating, out var pd);

                // EL = PD * LGD * OutstandingAmount
                totalExpectedLoss += pd * lgd * loan.OutstandingAmount;

                loanCount++;
            }

            results.Add(new PortfolioResult
            {
                PortId = portfolio.PortId,
                PortName = portfolio.PortName,
                Country = portfolio.PortCountry,
                Currency = portfolio.PortCcy,
                TotalOutstandingAmount = totalOutstanding,
                TotalCollateralValue = totalCollateral,
                TotalScenarioCollateralValue = totalScenarioCollateral,
                TotalExpectedLoss = totalExpectedLoss,
                LoanCount = loanCount
            });
        }

        return results.OrderBy(r => r.PortId).ToList();
    }
}
