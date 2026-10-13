using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.API.Data;

namespace RaceDay.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Generated ONCE per factory instance (not inside the AddDbContext delegate,
    // which EF Core re-runs once per request) so every request in a test shares
    // the same in-memory database.
    private readonly string _dbName = "RaceDayTestDb_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<RaceDayContext>));

            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<RaceDayContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });
        });
    }
}
