using FluxoCaixa.Lancamentos.API.Domain.Interfaces;
using FluxoCaixa.Lancamentos.API.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FluxoCaixa.Lancamentos.API.IntegrationTests;

/// <summary>
/// Factory que substitui PostgreSQL por InMemory e RabbitMQ por mock.
/// Permite testar controllers e middleware sem infraestrutura real.
/// </summary>
public class LancamentosApiFactory : WebApplicationFactory<Program>
{
    public IEventPublisher EventPublisher { get; } = Substitute.For<IEventPublisher>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove o DbContext real e substitui por InMemory
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<LancamentosDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<LancamentosDbContext>(options =>
                options.UseInMemoryDatabase("LancamentosTestDb"));

            // Remove o publisher real e substitui por mock
            var publisherDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IEventPublisher));
            if (publisherDescriptor != null)
                services.Remove(publisherDescriptor);

            services.AddSingleton(EventPublisher);
        });
    }
}
