using FluxoCaixa.Shared.Messaging;
using FluxoCaixa.Lancamentos.API.API.Middleware;
using FluxoCaixa.Lancamentos.API.Application.UseCases;
using FluxoCaixa.Lancamentos.API.Domain.Interfaces;
using FluxoCaixa.Lancamentos.API.Infrastructure.Data;
using FluxoCaixa.Lancamentos.API.Infrastructure.Messaging;
using FluxoCaixa.Lancamentos.API.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ─── Controllers ─────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        // Serializa enums como string (ex: "Credito" em vez de 1)
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// ─── Swagger / OpenAPI ───────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "FluxoCaixa — Lançamentos API",
        Version = "v1",
        Description = "Microserviço responsável pelo controle de lançamentos financeiros (débitos e créditos)."
    });

    // Inclui comentários XML nos endpoints
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

// ─── CORS ────────────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200"];

builder.Services.AddCors(opts =>
{
    opts.AddPolicy("FrontendPolicy", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ─── Banco de Dados (PostgreSQL + EF Core) ───────────────────────────────────
builder.Services.AddDbContext<LancamentosDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL")));

// ─── Repositórios ────────────────────────────────────────────────────────────
builder.Services.AddScoped<ILancamentoRepository, LancamentoRepository>();

// ─── Mensageria (RabbitMQ) ───────────────────────────────────────────────────
// ─── RabbitMQ Settings
builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection("RabbitMQ"));

builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

// ─── Use Cases (Application) ─────────────────────────────────────────────────
builder.Services.AddScoped<CriarLancamentoUseCase>();
builder.Services.AddScoped<RemoverLancamentoUseCase>();
builder.Services.AddScoped<ConsultarLancamentosUseCase>();

// ─── Build ───────────────────────────────────────────────────────────────────
var app = builder.Build();

// ─── Middleware Pipeline ──────────────────────────────────────────────────────
app.UseMiddleware<ExceptionHandlingMiddleware>(); // deve ser o primeiro

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Lançamentos API v1");
        c.RoutePrefix = string.Empty; // Swagger na raiz: http://localhost:5001
    });
}

app.UseCors("FrontendPolicy");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// ─── Migrations automáticas em desenvolvimento ───────────────────────────────
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<LancamentosDbContext>();
    if (db.Database.IsRelational())
    {
        db.Database.Migrate();
    }
}

app.Run();
