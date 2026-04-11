using FluxoCaixa.Consolidado.API.Application.DTOs;
using FluxoCaixa.Consolidado.API.Domain.Interfaces;

namespace FluxoCaixa.Consolidado.API.Application.UseCases;

/// <summary>
/// Consulta o consolidado diário usando o padrão Cache-Aside:
/// 1. Tenta buscar no Redis (rápido, < 1ms)
/// 2. Se não encontrar, busca no PostgreSQL
/// 3. Salva no Redis para próximas consultas
///
/// Suporta 50 req/s com &lt; 5% de perda graças ao cache Redis.
/// </summary>
public class ConsultarConsolidadoUseCase
{
    private readonly IConsolidadoRepository _repository;
    private readonly IConsolidadoCache _cache;
    private readonly ILogger<ConsultarConsolidadoUseCase> _logger;

    // TTL de 5 minutos — saldo pode ter pequeno delay (eventual consistency)
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public ConsultarConsolidadoUseCase(
        IConsolidadoRepository repository,
        IConsolidadoCache cache,
        ILogger<ConsultarConsolidadoUseCase> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ConsolidadoDiarioResponse?> ObterPorDataAsync(DateTime data, CancellationToken ct = default)
    {
        // 1. Tenta cache Redis
        var cached = await _cache.ObterAsync(data, ct);
        if (cached is not null)
        {
            _logger.LogDebug("Cache HIT para consolidado de {Data:yyyy-MM-dd}", data);
            return MapearParaResponse(cached, veioDoCache: true);
        }

        _logger.LogDebug("Cache MISS para consolidado de {Data:yyyy-MM-dd} — buscando no banco", data);

        // 2. Busca no banco
        var consolidado = await _repository.ObterPorDataAsync(data, ct);
        if (consolidado is null)
            return null;

        // 3. Popula cache para próximas consultas
        await _cache.SalvarAsync(consolidado, CacheTtl, ct);

        return MapearParaResponse(consolidado, veioDoCache: false);
    }

    public async Task<ConsolidadoPeriodoResponse> ObterPorPeriodoAsync(
        DateTime inicio, DateTime fim, CancellationToken ct = default)
    {
        if (inicio > fim)
            throw new ArgumentException("Data de início não pode ser maior que a data fim.");

        var consolidados = await _repository.ObterPorPeriodoAsync(inicio, fim, ct);
        var lista = consolidados.ToList();

        var dias = lista.Select(c => MapearParaResponse(c, false)).ToList();

        return new ConsolidadoPeriodoResponse
        {
            DataInicio = inicio.Date,
            DataFim = fim.Date,
            TotalCreditos = lista.Sum(c => c.TotalCreditos),
            TotalDebitos = lista.Sum(c => c.TotalDebitos),
            SaldoFinal = lista.Sum(c => c.SaldoFinal),
            TotalDias = lista.Count,
            Dias = dias
        };
    }

    private static ConsolidadoDiarioResponse MapearParaResponse(
        Domain.Entities.ConsolidadoDiario c, bool veioDoCache) => new()
    {
        Data = c.Data,
        TotalCreditos = c.TotalCreditos,
        TotalDebitos = c.TotalDebitos,
        SaldoFinal = c.SaldoFinal,
        QuantidadeLancamentos = c.QuantidadeLancamentos,
        AtualizadoEm = c.AtualizadoEm,
        VeioDoCache = veioDoCache
    };
}
