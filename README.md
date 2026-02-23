# Portfolio Stress Test – C# .NET Solution

## Overview

A Blazor Server application that lets users run house-price stress tests across loan portfolios.

### Features
- **Section 1** – Enter per-country house price changes and view aggregated portfolio results (Outstanding Amount, Collateral, Scenario Collateral, Expected Loss)
- **Section 2** – Each run is automatically saved to an SQLite database (run timestamp, duration, country inputs, all portfolio results)
- **Section 3** – History page lists all past runs; click any run to view its full results

---

## Project Structure

```
StressTest.sln
├── StressTest.Core/           # Business logic & data layer
│   ├── Models/                # Domain models (Portfolio, Loan, Rating, run entities)
│   ├── Services/
│   │   ├── CsvDataService     # Reads the three CSV files
│   │   ├── StressTestCalculator  # Core formula calculations
│   │   └── StressTestRunService  # Orchestrates run + persistence
│   └── Data/
│       └── StressTestDbContext   # EF Core (SQLite)
│
├── StressTest.Tests/          # xUnit unit tests (FluentAssertions)
│   └── StressTestCalculatorTests.cs
│
└── StressTest.Web/            # Blazor Server UI
    ├── Pages/
    │   ├── Index.razor        # Section 1 & 2 – run the stress test
    │   └── History.razor      # Section 3 – view past runs
    └── Data/
        ├── loans.csv
        ├── portfolios.csv
        └── ratings.csv
```

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)

---

## Getting Started
### 1. Restore and Build

### 2. Run Unit Tests
dotnet test StressTest.Tests
Expected: **9 tests, all passing**.

### 3. Run the Web Application

set StressTest.Web as start up project
run --project StressTest.Web


on my machine it was https://localhost:57173/ (or the URL shown in the console).

The SQLite database (`stresstest.db`) is created automatically in `StressTest.Web/Data/` on first run.

---

## Business Logic

The stress test calculations per loan are:

| Term | Formula |
|---|---|
| Scenario Collateral Value | `CollateralValue × (1 + change% / 100)` |
| Recovery Rate (RR) | `Scenario Collateral Value / OutstandingAmount` |
| Loss Given Default (LGD) | `1 − RR` |
| Expected Loss (EL) | `PD × LGD × OutstandingAmount` |

Where **PD** (Probability of Default) is looked up from `ratings.csv` by the loan's credit rating and divided by 100 to convert from percentage to a decimal fraction.

Results are **aggregated (summed) by portfolio**.

---

## Database

SQLite – no server setup required. The file is auto-created at `StressTest.Web/Data/stresstest.db`.

Schema:
- **StressTestRuns** – one row per run (timestamp, duration, country inputs as JSON, summary stats)
- **StressTestRunResults** – one row per portfolio per run (all aggregated metrics)

To inspect the database directly:
```bash
sqlite3 StressTest.Web/Data/stresstest.db
.tables
SELECT * FROM StressTestRuns;
```
