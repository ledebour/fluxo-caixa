using FluxoCaixa.Lancamentos.API.Application.DTOs;
using FluxoCaixa.Lancamentos.API.Domain.Exceptions;
using FluxoCaixa.Lancamentos.API.Domain.Interfaces;

namespace FluxoCaixa.Lancamentos.API.Application.UseCases;

public class ConsultarLancamentosUseCase
{
    private readonly ILancamentoRepository _repository;

    public ConsultarLancamentosUseCase(ILancamentoRepository repository)
    {
        _repository = repository;
    }

    public async Task<LancamentoResponse> ObterPorIdAsync(Guid id, CancellationToken ct = default)
    {
        var lancamento = await _repository.ObterPorIdAsync(id, ct)
            ?? throw new NotFoundException("Lancamento", id);

        return CriarLancamentoUseCase.MapearParaResponse(lancamento);
    }

    public async Task<IEnumerable<LancamentoResponse>> ObterTodosAsync(CancellationToken ct = default)
    {
        var lancamentos = await _repository.ObterTodosAsync(ct);
        return lancamentos.Select(CriarLancamentoUseCase.MapearParaResponse);
    }

    public async Task<IEnumerable<LancamentoResponse>> ObterPorDataAsync(DateTime data, CancellationToken ct = default)
    {
        var lancamentos = await _repository.ObterPorDataAsync(data, ct);
        return lancamentos.Select(CriarLancamentoUseCase.MapearParaResponse);
    }
}
