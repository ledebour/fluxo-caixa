using FluxoCaixa.Lancamentos.API.Domain.Entities;
using FluxoCaixa.Lancamentos.API.Domain.Exceptions;
using FluxoCaixa.Lancamentos.API.Domain.Interfaces;
using FluxoCaixa.Shared.Events;

namespace FluxoCaixa.Lancamentos.API.Application.UseCases;

/// <summary>
/// Orquestra a remoção de um lançamento:
/// 1. Busca e valida existência
/// 2. Remove do repositório
/// 3. Publica evento para o Consolidado recalcular o saldo
/// </summary>
public class RemoverLancamentoUseCase
{
    private readonly ILancamentoRepository _repository;
    private readonly IEventPublisher _eventPublisher;

    public RemoverLancamentoUseCase(ILancamentoRepository repository, IEventPublisher eventPublisher)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
    }

    public async Task ExecutarAsync(Guid id, CancellationToken ct = default)
    {
        var lancamento = await _repository.ObterPorIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Lancamento), id);

        await _repository.RemoverAsync(lancamento, ct);

        var evento = new LancamentoRemovidoEvent
        {
            Id = lancamento.Id,
            Data = lancamento.Data,
            Valor = lancamento.Valor,
            Tipo = lancamento.Tipo
        };

        await _eventPublisher.PublicarAsync(evento, ct);
    }
}
