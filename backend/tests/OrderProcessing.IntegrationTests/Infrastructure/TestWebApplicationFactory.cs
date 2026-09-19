using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OrderProcessing.IntegrationTests.Infrastructure;

/// <summary>
/// Each instance gets its own real SQLite file (not :memory:, per the assessment's requirement for a real
/// relational engine) so tests observe genuine multi-connection concurrency/locking behavior.
/// </summary>
public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public string DbPath { get; } = Path.Combine(Path.GetTempPath(), $"order-processing-tests-{Guid.NewGuid():N}.db");

    public Dictionary<string, string?> ExtraConfiguration { get; } = new();

    public Action<IServiceCollection>? ConfigureTestServices { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var settings = new Dictionary<string, string?>(ExtraConfiguration)
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={DbPath};Default Timeout=5"
            };
            config.AddInMemoryCollection(settings);
        });

        builder.ConfigureServices(services => ConfigureTestServices?.Invoke(services));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (File.Exists(DbPath))
        {
            try
            {
                File.Delete(DbPath);
            }
            catch (IOException)
            {
                // Best-effort cleanup; the OS temp folder will reclaim it eventually.
            }
        }
    }
}
