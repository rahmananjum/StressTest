using FluentAssertions;
using StressTest.Core.Models;
using StressTest.Core.Services;
using Xunit;

namespace StressTest.Tests;

public class StressTestCalculatorTests
{
    private readonly StressTestCalculator _calculator = new();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Portfolio MakePortfolio(int id, string country = "GB", string ccy = "GBP") =>
        new() { PortId = id, PortName = $"PORT{id:D2}", PortCountry = country, PortCcy = ccy };

    private static Loan MakeLoan(int id, int portId, decimal outstanding, decimal collateral, string rating = "BB") =>
        new() { LoanId = id, PortId = portId, OriginalLoanAmount = outstanding, OutstandingAmount = outstanding, CollateralValue = collateral, CreditRating = rating };

    private static Rating MakeRating(string rating, decimal pdPct) =>
        new() { CreditRating = rating, ProbabilityOfDefault = pdPct };

    private static readonly IReadOnlyList<Rating> DefaultRatings = new[]
    {
        MakeRating("AAA", 1m),
        MakeRating("AA",  10m),
        MakeRating("A",   25m),
        MakeRating("BBB", 40m),
        MakeRating("BB",  60m),
        MakeRating("B",   75m),
        MakeRating("CCC", 95m)
    };

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Calculate_SingleLoan_NoChange_ExpectedLossIsCorrect()
    {
        // Arrange
        // PD(BB) = 60% = 0.60
        // ScenarioCollateral = 100 * (1 + 0/100) = 100
        // RR = 100 / 80 = 1.25
        // LGD = 1 - 1.25 = -0.25
        // EL = 0.60 * (-0.25) * 80 = -12
        var portfolios = new[] { MakePortfolio(1, "GB") };
        var loans = new[] { MakeLoan(1, 1, outstanding: 80m, collateral: 100m, rating: "BB") };
        var changes = new Dictionary<string, decimal> { ["GB"] = 0m };

        // Act
        var results = _calculator.Calculate(changes, portfolios, loans, DefaultRatings);

        // Assert
        results.Should().HaveCount(1);
        var r = results[0];
        r.TotalOutstandingAmount.Should().Be(80m);
        r.TotalCollateralValue.Should().Be(100m);
        r.TotalScenarioCollateralValue.Should().Be(100m);
        r.TotalExpectedLoss.Should().BeApproximately(-12m, 0.0001m);
    }

    [Fact]
    public void Calculate_SingleLoan_NegativeHousePriceChange_ReducesScenarioCollateral()
    {
        // Arrange
        // Change = -5.12% => multiplier = 0.9488
        // ScenarioCollateral = 66000 * 0.9488 = 62620.8
        var portfolios = new[] { MakePortfolio(1, "GB") };
        var loans = new[] { MakeLoan(1, 1, outstanding: 54202m, collateral: 66000m, rating: "BB") };
        var changes = new Dictionary<string, decimal> { ["GB"] = -5.12m };

        // Act
        var results = _calculator.Calculate(changes, portfolios, loans, DefaultRatings);

        // Assert
        var r = results[0];
        r.TotalScenarioCollateralValue.Should().BeApproximately(66000m * (1m - 0.0512m), 0.01m);
    }

    [Fact]
    public void Calculate_ExpectedLoss_Formula_IsCorrect()
    {
        // Arrange - hand-calculated values
        // PD(BB) = 60% = 0.60
        // Change = -10% => multiplier = 0.90
        // ScenarioCollateral = 100 * 0.90 = 90
        // RR = 90 / 100 = 0.90
        // LGD = 1 - 0.90 = 0.10
        // EL = 0.60 * 0.10 * 100 = 6
        var portfolios = new[] { MakePortfolio(1, "GB") };
        var loans = new[] { MakeLoan(1, 1, outstanding: 100m, collateral: 100m, rating: "BB") };
        var changes = new Dictionary<string, decimal> { ["GB"] = -10m };

        // Act
        var results = _calculator.Calculate(changes, portfolios, loans, DefaultRatings);

        // Assert
        results[0].TotalExpectedLoss.Should().BeApproximately(6m, 0.0001m);
    }

    [Fact]
    public void Calculate_MultipleLoansInPortfolio_AggregatesCorrectly()
    {
        // Arrange - 2 loans, both in same portfolio
        var portfolios = new[] { MakePortfolio(1, "US") };
        var loans = new[]
        {
            MakeLoan(1, 1, outstanding: 100m, collateral: 100m, rating: "BB"),  // EL = 6 (as above)
            MakeLoan(2, 1, outstanding: 200m, collateral: 200m, rating: "BB")   // EL = 12 (scaled)
        };
        var changes = new Dictionary<string, decimal> { ["US"] = -10m };

        // Act
        var results = _calculator.Calculate(changes, portfolios, loans, DefaultRatings);

        // Assert
        results.Should().HaveCount(1);
        results[0].TotalOutstandingAmount.Should().Be(300m);
        results[0].TotalCollateralValue.Should().Be(300m);
        results[0].LoanCount.Should().Be(2);
        results[0].TotalExpectedLoss.Should().BeApproximately(18m, 0.0001m);
    }

    [Fact]
    public void Calculate_MultiplePortfolios_GroupedCorrectly()
    {
        // Arrange
        var portfolios = new[] { MakePortfolio(1, "GB"), MakePortfolio(2, "US") };
        var loans = new[]
        {
            MakeLoan(1, 1, outstanding: 100m, collateral: 100m, rating: "BB"),
            MakeLoan(2, 2, outstanding: 200m, collateral: 200m, rating: "BB")
        };
        var changes = new Dictionary<string, decimal> { ["GB"] = -10m, ["US"] = -5m };

        // Act
        var results = _calculator.Calculate(changes, portfolios, loans, DefaultRatings);

        // Assert
        results.Should().HaveCount(2);
        results.Select(r => r.PortId).Should().BeEquivalentTo(new[] { 1, 2 });
    }

    [Fact]
    public void Calculate_MissingCountryChange_DefaultsToZeroChange()
    {
        // Arrange - country "DE" not in changes dict
        var portfolios = new[] { MakePortfolio(1, "DE") };
        var loans = new[] { MakeLoan(1, 1, outstanding: 100m, collateral: 100m, rating: "BB") };
        var changes = new Dictionary<string, decimal>(); // empty

        // Act
        var results = _calculator.Calculate(changes, portfolios, loans, DefaultRatings);

        // Assert - ScenarioCollateral = CollateralValue * 1 = CollateralValue
        results[0].TotalScenarioCollateralValue.Should().Be(100m);
    }

    [Fact]
    public void Calculate_ZeroOutstandingAmount_DoesNotThrow()
    {
        // Arrange
        var portfolios = new[] { MakePortfolio(1, "GB") };
        var loans = new[] { MakeLoan(1, 1, outstanding: 0m, collateral: 100m, rating: "BB") };
        var changes = new Dictionary<string, decimal> { ["GB"] = -5m };

        // Act
        var act = () => _calculator.Calculate(changes, portfolios, loans, DefaultRatings);

        // Assert
        act.Should().NotThrow();
        var results = act();
        results[0].TotalExpectedLoss.Should().Be(0m);
    }

    [Fact]
    public void Calculate_AllCreditRatings_ProduceDifferentExpectedLosses()
    {
        // Arrange - each rating with identical loan amounts; higher risk should yield higher EL
        var ratings = new[] { "AAA", "AA", "A", "BBB", "BB", "B", "CCC" };
        var portfolios = ratings.Select((r, i) => MakePortfolio(i + 1, "GB")).ToArray();
        var loans = ratings.Select((r, i) => MakeLoan(i + 1, i + 1, 100m, 100m, r)).ToArray();
        var changes = new Dictionary<string, decimal> { ["GB"] = -10m };

        // Act
        var results = _calculator.Calculate(changes, portfolios, loans, DefaultRatings)
            .OrderBy(r => r.PortId)
            .ToList();

        // Assert - expected losses should increase as rating degrades
        var els = results.Select(r => r.TotalExpectedLoss).ToList();
        for (int i = 1; i < els.Count; i++)
            els[i].Should().BeGreaterThan(els[i - 1],
                $"rating {ratings[i]} should have higher EL than {ratings[i - 1]}");
    }

    [Fact]
    public void Calculate_PositiveHousePriceChange_ReducesExpectedLoss()
    {
        // A house price increase => higher RR => lower (or negative) LGD => lower EL
        var portfolios = new[] { MakePortfolio(1, "SG") };
        var loans = new[] { MakeLoan(1, 1, outstanding: 100m, collateral: 80m, rating: "BB") };
        var changesNeg = new Dictionary<string, decimal> { ["SG"] = -10m };
        var changesPos = new Dictionary<string, decimal> { ["SG"] = +10m };

        var elNeg = _calculator.Calculate(changesNeg, portfolios, loans, DefaultRatings)[0].TotalExpectedLoss;
        var elPos = _calculator.Calculate(changesPos, portfolios, loans, DefaultRatings)[0].TotalExpectedLoss;

        elPos.Should().BeLessThan(elNeg);
    }

    [Fact]
    public void Calculate_UnknownRating_TreatedAsZeroPd()
    {
        // Arrange - rating "D" not in ratings list
        var portfolios = new[] { MakePortfolio(1, "GB") };
        var loans = new[] { MakeLoan(1, 1, outstanding: 100m, collateral: 100m, rating: "D") };
        var changes = new Dictionary<string, decimal> { ["GB"] = -10m };

        // Act
        var results = _calculator.Calculate(changes, portfolios, loans, DefaultRatings);

        // Assert - PD defaults to 0 => EL = 0
        results[0].TotalExpectedLoss.Should().Be(0m);
    }
}
