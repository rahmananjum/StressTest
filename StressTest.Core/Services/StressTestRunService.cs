using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StressTest.Core.Data;
using StressTest.Core.Models;

namespace StressTest.Core.Services;

public interface IStressTestRunService
{
    /// <summary>Executes a stress test, persists it to the DB and returns the full run record.</summary>
    Task<StressTestRun> RunAsync(IReadOnlyDictionary<string, decimal> countryChanges, CancellationToken ct = default);

    /// <summary>Returns all past runs (without results).</summary>
    Task<IReadOnlyList<StressTestRun>> GetRunsAsync(CancellationToken ct = default);

    /// <summary>Returns a specific run with its per-portfolio results.</summary>
    Task<StressTestRun?> GetRunWithResultsAsync(int id, CancellationToken ct = default);
}

public class StressTestRunService : IStressTestRunService
{
    private readonly ICsvDataService _csvData;
    private readonly IStressTestCalculator _calculator;
    private readonly StressTestDbContext _db;

    public StressTestRunService(
        ICsvDataService csvData,
        IStressTestCalculator calculator,
        StressTestDbContext db)
    {
        _csvData = csvData;
        _calculator = calculator;
        _db = db;
    }

    public async Task<StressTestRun> RunAsync(
        IReadOnlyDictionary<string, decimal> countryChanges,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        var portfolios = _csvData.LoadPortfolios();
        var loans = _csvData.LoadLoans();
        var ratings = _csvData.LoadRatings();

        var portfolioResults = _calculator.Calculate(countryChanges, portfolios, loans, ratings);

        sw.Stop();

        var run = new StressTestRun
        {
            RunAt = DateTime.UtcNow,
            DurationMs = sw.ElapsedMilliseconds,
            CountryInputsJson = JsonSerializer.Serialize(countryChanges),
            TotalPortfolios = portfolioResults.Count,
            TotalLoans = portfolioResults.Sum(r => r.LoanCount),
            TotalExpectedLoss = portfolioResults.Sum(r => r.TotalExpectedLoss),
            Results = portfolioResults.Select(pr => new StressTestRunResult
            {
                PortId = pr.PortId,
                PortName = pr.PortName,
                Country = pr.Country,
                Currency = pr.Currency,
                TotalOutstandingAmount = pr.TotalOutstandingAmount,
                TotalCollateralValue = pr.TotalCollateralValue,
                TotalScenarioCollateralValue = pr.TotalScenarioCollateralValue,
                TotalExpectedLoss = pr.TotalExpectedLoss,
                LoanCount = pr.LoanCount
            }).ToList()
        };

        _db.Runs.Add(run);
        await _db.SaveChangesAsync(ct);

        return run;
    }

    public async Task<IReadOnlyList<StressTestRun>> GetRunsAsync(CancellationToken ct = default)
    {
        return await _db.Runs
            .OrderByDescending(r => r.RunAt)
            .ToListAsync(ct);
    }

    public async Task<StressTestRun?> GetRunWithResultsAsync(int id, CancellationToken ct = default)
    {
        return await _db.Runs
            .Include(r => r.Results)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }
}
