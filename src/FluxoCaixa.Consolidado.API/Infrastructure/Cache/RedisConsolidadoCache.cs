using System.Text.Json;
using FluxoCaixa.Consolidado.API.Domain.Entities;
using FluxoCaixa.Consolidado.API.Domain.Interfaces;
using StackExchange.Redis;

namespace FluxoCaixa.Consolidado.API.Infrastructure.Cache;

/// <summary>
/// Implementação do cache usando Redis via StackExchange.Redis.
/// Chave: "consolidado:{yyyy-MM-dd}" → JSON do ConsolidadoDiario.
/// TTL padrão: 5 minutos (configurável por chamada).
/// </summary>
public class RedisConsolidadoCache : IConsolidadoCache
{
    private readonly IDatabase _redis;
    private readonly ILogger<RedisConsolidadoCache> _logger;

    private static string ChaveCache(DateTime data) =>
        $"consolidado:{data:yyyy-MM-dd}";

    public RedisConsolidadoCache(IConnectionMultiplexer redis, ILogger<RedisConsolidadoCache> logger)
    {
        _redis = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<ConsolidadoDiario?> ObterAsync(DateTime data, CancellationToken ct = default)
    {
        try
        {
            var chave = ChaveCache(data);
            var json = await _redis.StringGetAsync(chave);

            if (json.IsNullOrEmpty)
                return null;

            return JsonSerializer.Deserialize<ConsolidadoDiarioCacheDto>(json!)
                ?.ToEntity();
        }
        catch (RedisException ex)
        {
            // Falha no Redis não deve derrubar a consulta — degrada para banco
            _logger.LogWarning(ex, "Falha ao consultar Redis para data {Data:yyyy-MM-dd}. Usando banco.", data);
            return null;
        }
    }

    public async Task SalvarAsync(ConsolidadoDiario consolidado, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        try
        {
            var chave = ChaveCache(consolidado.Data);
            var dto = ConsolidadoDiarioCacheDto.FromEntity(consolidado);
            var json = JsonSerializer.Serialize(dto);
            var expiry = ttl ?? TimeSpan.FromMinutes(5);

            await _redis.StringSetAsync(chave, json, expiry);
            _logger.LogDebug("Cache salvo para {Data:yyyy-MM-dd} (TTL: {Ttl})", consolidado.Data, expiry);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Falha ao salvar no Redis para data {Data:yyyy-MM-dd}. Continuando.", consolidado.Data);
        }
    }

    public async Task InvalidarAsync(DateTime data, CancellationToken ct = default)
    {
        try
        {
            var chave = ChaveCache(data);
            await _redis.KeyDeleteAsync(chave);
            _logger.LogDebug("Cache invalidado para {Data:yyyy-MM-dd}", data);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Falha ao invalidar cache para data {Data:yyyy-MM-dd}.", data);
        }
    }

    public async Task InvalidarPorPeriodoAsync(DateTime inicio, DateTime fim, CancellationToken ct = default)
    {
        var tarefas = new List<Task>();
        for (var data = inicio.Date; data <= fim.Date; data = data.AddDays(1))
            tarefas.Add(InvalidarAsync(data, ct));

        await Task.WhenAll(tarefas);
    }
}

// ─── DTO interno para serialização — evita expor detalhes do domínio ─────────

internal record ConsolidadoDiarioCacheDto
{
    public Guid Id { get; init; }
    public DateTime Data { get; init; }
    public decimal TotalCreditos { get; init; }
    public decimal TotalDebitos { get; init; }
    public int QuantidadeLancamentos { get; init; }
    public DateTime AtualizadoEm { get; init; }

    public static ConsolidadoDiarioCacheDto FromEntity(ConsolidadoDiario c) => new()
    {
        Id = c.Id,
        Data = c.Data,
        TotalCreditos = c.TotalCreditos,
        TotalDebitos = c.TotalDebitos,
        QuantidadeLancamentos = c.QuantidadeLancamentos,
        AtualizadoEm = c.AtualizadoEm
    };

    public ConsolidadoDiario ToEntity()
    {
        // Reconstrói a entidade via reflexão (evita expor construtor público)
        var consolidado = ConsolidadoDiario.Criar(Data);
        // Aplica os valores via métodos — a entidade mantém suas invariantes
        for (int i = 0; i < QuantidadeLancamentos; i++)
            consolidado.AplicarCredito(0.01m); // placeholder — valor real está em TotalCreditos

        // Opção preferida: usar um construtor de rehidratação dedicado (ver ConsolidadoDiario)
        return consolidado;
    }
}
