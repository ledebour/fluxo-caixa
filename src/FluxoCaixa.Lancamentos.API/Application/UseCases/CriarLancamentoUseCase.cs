using FluxoCaixa.Lancamentos.API.Application.DTOs;
using FluxoCaixa.Lancamentos.API.Domain.Entities;
using FluxoCaixa.Lancamentos.API.Domain.Interfaces;
using FluxoCaixa.Shared.Events;

namespace FluxoCaixa.Lancamentos.API.Application.UseCases;

/// <summary>
/// Orquestra a criação de um lançamento:
/// 1. Cria a entidade (regras de domínio validadas internamente)
/// 2. Persiste no repositório
/// 3. Publica evento no RabbitMQ para o serviço de Consolidado
/// </summary>
public class CriarLancamentoUseCase
{
    private readonly ILancamentoRepository _repository;
    private readonly IEventPublisher _eventPublisher;

    public CriarLancamentoUseCase(ILancamentoRepository repository, IEventPublisher eventPublisher)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
    }

    public async Task<LancamentoResponse> ExecutarAsync(CriarLancamentoRequest request, CancellationToken ct = default)
    {
        // Domínio valida as invariantes — lança DomainException se inválido
        var lancamento = Lancamento.Criar(request.Data, request.Valor, request.Tipo, request.Descricao);

        await _repository.AdicionarAsync(lancamento, ct);

        // Publica evento de forma assíncrona — Consolidado será notificado
        var evento = new LancamentoCriadoEvent
        {
            Id = lancamento.Id,
            Data = lancamento.Data,
            Valor = lancamento.Valor,
            Tipo = lancamento.Tipo,
            Descricao = lancamento.Descricao
        };

        await _eventPublisher.PublicarAsync(evento, ct);

        return MapearParaResponse(lancamento);
    }

    internal static LancamentoResponse MapearParaResponse(Lancamento l) => new()
    {
        Id = l.Id,
        Data = l.Data,
        Valor = l.Valor,
        Tipo = l.Tipo.ToString(),
        Descricao = l.Descricao,
        CriadoEm = l.CriadoEm
    };
}
