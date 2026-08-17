using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Oee.Persistence;

/// <summary>
/// Lets <c>dotnet ef</c> build a context without an application host.
/// </summary>
/// <remarks>
/// Design-time only. The running application configures its own context through DI; this
/// exists purely so <c>migrations add</c> and <c>database update</c> work from the command
/// line. The default connection string matches <c>docker-compose.yml</c>.
/// </remarks>
public sealed class OeeDbContextFactory : IDesignTimeDbContextFactory<OeeDbContext>
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=linecore;Username=linecore;Password=linecore";

    public OeeDbContext CreateDbContext(string[] args)
    {
        string connectionString =
            Environment.GetEnvironmentVariable("LINECORE_CONNECTION_STRING")
            ?? DefaultConnectionString;

        DbContextOptions<OeeDbContext> options =
            new DbContextOptionsBuilder<OeeDbContext>()
                .UseNpgsql(connectionString)
                .Options;

        return new OeeDbContext(options);
    }
}
