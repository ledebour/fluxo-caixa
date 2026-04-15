using FluxoCaixa.Consolidado.API.Domain.Interfaces;
using FluxoCaixa.Consolidado.API.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using StackExchange.Redis;

namespace FluxoCaixa.Consolidado.API.IntegrationTests;

/// <summary>
/// Factory para testes de integração do Consolidado API.
/// Substitui PostgreSQL por InMemory, Redis por mock e desabilita RabbitMQ consumer.
/// </summary>
public class ConsolidadoApiFactory : WebApplicationFactory<Program>
{
    public IConsolidadoCache CacheSubstitute { get; } = Substitute.For<IConsolidadoCache>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove DbContext real → InMemory
            var dbDesc = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ConsolidadoDbContext>));
            if (dbDesc != null) services.Remove(dbDesc);

            services.AddDbContext<ConsolidadoDbContext>(options =>
                options.UseInMemoryDatabase($"ConsolidadoTest_{Guid.NewGuid()}"));

            // Remove Redis real → mock
            var redisDesc = services.SingleOrDefault(
                d => d.ServiceType == typeof(IConnectionMultiplexer));
            if (redisDesc != null) services.Remove(redisDesc);

            var cacheDesc = services.SingleOrDefault(
                d => d.ServiceType == typeof(IConsolidadoCache));
            if (cacheDesc != null) services.Remove(cacheDesc);

            services.AddSingleton(CacheSubstitute);

            // Remove RabbitMQ hosted service para não tentar conectar
            var hostedServices = services
                .Where(d => d.ImplementationType?.Name == "RabbitMqConsumerService")
                .ToList();
            foreach (var svc in hostedServices)
                services.Remove(svc);
        });
    }
}
