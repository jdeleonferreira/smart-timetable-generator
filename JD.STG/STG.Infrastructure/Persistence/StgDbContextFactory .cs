using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace STG.Infrastructure.Persistence;

public class StgDbContextFactory : IDesignTimeDbContextFactory<StgDbContext>
{
    public StgDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<StgDbContext>()
            .UseSqlite("Data Source=stg.db")
            .Options;

        return new StgDbContext(options);
    }
}
