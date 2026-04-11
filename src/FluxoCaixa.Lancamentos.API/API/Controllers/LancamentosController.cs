using FluxoCaixa.Lancamentos.API.Application.DTOs;
using FluxoCaixa.Lancamentos.API.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace FluxoCaixa.Lancamentos.API.API.Controllers;

/// <summary>
/// Gerencia os lançamentos financeiros (débitos e créditos).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class LancamentosController : ControllerBase
{
    private readonly CriarLancamentoUseCase _criar;
    private readonly RemoverLancamentoUseCase _remover;
    private readonly ConsultarLancamentosUseCase _consultar;

    public LancamentosController(
        CriarLancamentoUseCase criar,
        RemoverLancamentoUseCase remover,
        ConsultarLancamentosUseCase consultar)
    {
        _criar = criar;
        _remover = remover;
        _consultar = consultar;
    }

    /// <summary>Lista todos os lançamentos.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<LancamentoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterTodos(CancellationToken ct)
    {
        var resultado = await _consultar.ObterTodosAsync(ct);
        return Ok(resultado);
    }

    /// <summary>Busca um lançamento pelo ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LancamentoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken ct)
    {
        var resultado = await _consultar.ObterPorIdAsync(id, ct);
        return Ok(resultado);
    }

    /// <summary>Lista lançamentos de uma data específica (yyyy-MM-dd).</summary>
    [HttpGet("por-data/{data}")]
    [ProducesResponseType(typeof(IEnumerable<LancamentoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ObterPorData(DateTime data, CancellationToken ct)
    {
        var resultado = await _consultar.ObterPorDataAsync(data, ct);
        return Ok(resultado);
    }

    /// <summary>Cria um novo lançamento (débito ou crédito).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(LancamentoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Criar([FromBody] CriarLancamentoRequest request, CancellationToken ct)
    {
        var resultado = await _criar.ExecutarAsync(request, ct);
        return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Id }, resultado);
    }

    /// <summary>Remove um lançamento pelo ID.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remover(Guid id, CancellationToken ct)
    {
        await _remover.ExecutarAsync(id, ct);
        return NoContent();
    }
}
