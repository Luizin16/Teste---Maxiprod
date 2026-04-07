using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MinhasFinancas.Infrastructure.Data;

namespace MinhasFinancas.Integration.Tests.Setup;

/// <summary>
/// WebApplicationFactory customizada que substitui o SQLite por banco InMemory.
/// Garante isolamento total entre os testes de integração.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove o DbContext real (SQLite)
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<MinhasFinancasDbContext>));

            if (descriptor != null)
                services.Remove(descriptor);

            // Adiciona DbContext em memória com nome único por instância
            services.AddDbContext<MinhasFinancasDbContext>(options =>
                options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
        });
    }

    /// <summary>
    /// Cria um HttpClient configurado para testes de integração.
    /// </summary>
    public HttpClient CreateApiClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }
}
