using Microsoft.EntityFrameworkCore;
using StressTest.Core.Models;

namespace StressTest.Core.Data;

public class StressTestDbContext : DbContext
{
    public StressTestDbContext(DbContextOptions<StressTestDbContext> options) : base(options) { }

    public DbSet<StressTestRun> Runs => Set<StressTestRun>();
    public DbSet<StressTestRunResult> RunResults => Set<StressTestRunResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StressTestRun>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.CountryInputsJson).IsRequired();
            e.HasMany(x => x.Results)
             .WithOne(r => r.Run)
             .HasForeignKey(r => r.StressTestRunId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StressTestRunResult>(e =>
        {
            e.HasKey(x => x.Id);
        });
    }
}
