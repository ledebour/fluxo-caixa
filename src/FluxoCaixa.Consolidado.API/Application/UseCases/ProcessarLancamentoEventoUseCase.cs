using FluxoCaixa.Consolidado.API.Domain.Entities;
using FluxoCaixa.Consolidado.API.Domain.Interfaces;
using FluxoCaixa.Shared.Enums;
using FluxoCaixa.Shared.Events;

namespace FluxoCaixa.Consolidado.API.Application.UseCases;

/// <summary>
/// Processa eventos de lançamento recebidos via RabbitMQ:
/// 1. Busca ou cria o ConsolidadoDiario da data do evento
/// 2. Aplica o crédito ou débito
/// 3. Persiste no banco
/// 4. Invalida o cache Redis (forçando recálculo na próxima consulta)
/// </summary>
public class ProcessarLancamentoEventoUseCase
{
    private readonly IConsolidadoRepository _repository;
    private readonly IConsolidadoCache _cache;
    private readonly ILogger<ProcessarLancamentoEventoUseCase> _logger;

    public ProcessarLancamentoEventoUseCase(
        IConsolidadoRepository repository,
        IConsolidadoCache cache,
        ILogger<ProcessarLancamentoEventoUseCase> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task ProcessarCriadoAsync(LancamentoCriadoEvent evento, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Processando LancamentoCriado {Id} | Data: {Data:yyyy-MM-dd} | Tipo: {Tipo} | Valor: {Valor:C}",
            evento.Id, evento.Data, evento.Tipo, evento.Valor);

        var consolidado = await ObterOuCriarConsolidadoAsync(evento.Data, ct);

        if (evento.Tipo == TipoLancamento.Credito)
            consolidado.AplicarCredito(evento.Valor);
        else
            consolidado.AplicarDebito(evento.Valor);

        await _repository.SalvarAsync(consolidado, ct);
        await _cache.InvalidarAsync(evento.Data, ct);

        _logger.LogInformation(
            "Consolidado de {Data:yyyy-MM-dd} atualizado. SaldoFinal: {Saldo:C}",
            evento.Data, consolidado.SaldoFinal);
    }

    public async Task ProcessarRemovidoAsync(LancamentoRemovidoEvent evento, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Processando LancamentoRemovido {Id} | Data: {Data:yyyy-MM-dd} | Tipo: {Tipo} | Valor: {Valor:C}",
            evento.Id, evento.Data, evento.Tipo, evento.Valor);

        var consolidado = await ObterOuCriarConsolidadoAsync(evento.Data, ct);

        if (evento.Tipo == TipoLancamento.Credito)
            consolidado.EstornarCredito(evento.Valor);
        else
            consolidado.EstornarDebito(evento.Valor);

        await _repository.SalvarAsync(consolidado, ct);
        await _cache.InvalidarAsync(evento.Data, ct);
    }

    private async Task<ConsolidadoDiario> ObterOuCriarConsolidadoAsync(DateTime data, CancellationToken ct)
    {
        var consolidado = await _repository.ObterPorDataAsync(data, ct);
        if (consolidado is null)
        {
            consolidado = ConsolidadoDiario.Criar(data);
            _logger.LogInformation("Novo consolidado criado para {Data:yyyy-MM-dd}", data);
        }
        return consolidado;
    }
}
