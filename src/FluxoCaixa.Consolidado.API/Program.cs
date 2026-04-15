using FluxoCaixa.Consolidado.API.API.Middleware;
using FluxoCaixa.Consolidado.API.Application.UseCases;
using FluxoCaixa.Consolidado.API.Domain.Interfaces;
using FluxoCaixa.Consolidado.API.Infrastructure.Cache;
using FluxoCaixa.Consolidado.API.Infrastructure.Data;
using FluxoCaixa.Consolidado.API.Infrastructure.Messaging;
using FluxoCaixa.Consolidado.API.Infrastructure.Repositories;
using FluxoCaixa.Shared.Messaging;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ─── Controllers ─────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// ─── Swagger ─────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new()
    {
        Title = "FluxoCaixa — Consolidado API",
        Version = "v1",
        Description = "Saldo diário consolidado. Cache Redis para 50 req/s. Consumer RabbitMQ."
    }));

// ─── CORS ────────────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200"];

builder.Services.AddCors(opts =>
    opts.AddPolicy("FrontendPolicy", p =>
        p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

// ─── PostgreSQL ───────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ConsolidadoDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL")));

// ─── Redis ───────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(
        builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379"));

// ─── RabbitMQ Settings ───────────────────────────────────────────────────────
builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection("RabbitMQ"));

// ─── DI ──────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IConsolidadoRepository, ConsolidadoRepository>();
builder.Services.AddSingleton<IConsolidadoCache, RedisConsolidadoCache>();
builder.Services.AddScoped<ConsultarConsolidadoUseCase>();
builder.Services.AddScoped<ProcessarLancamentoEventoUseCase>();

// Consumer RabbitMQ roda em background — independente da API
builder.Services.AddHostedService<RabbitMqConsumerService>();

// ─── Build ───────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Consolidado API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseCors("FrontendPolicy");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ConsolidadoDbContext>();
    if (db.Database.IsRelational())
    {
        db.Database.Migrate();
    }
}

app.Run();

namespace FluxoCaixa.Consolidado.API
{
    public partial class Program { }
}

