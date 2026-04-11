using FluxoCaixa.Consolidado.API.Application.DTOs;
using FluxoCaixa.Consolidado.API.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace FluxoCaixa.Consolidado.API.API.Controllers;

/// <summary>
/// Expõe o saldo consolidado diário e por período.
/// Suporta até 50 req/s com cache Redis (máx 5% de perda).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ConsolidadoController : ControllerBase
{
    private readonly ConsultarConsolidadoUseCase _consultar;

    public ConsolidadoController(ConsultarConsolidadoUseCase consultar)
    {
        _consultar = consultar;
    }

    /// <summary>Retorna o saldo consolidado de uma data específica (yyyy-MM-dd).</summary>
    [HttpGet("{data:datetime}")]
    [ProducesResponseType(typeof(ConsolidadoDiarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorData(DateTime data, CancellationToken ct)
    {
        var resultado = await _consultar.ObterPorDataAsync(data, ct);
        return resultado is null
            ? NotFound(new ErrorResponse { Mensagem = $"Nenhum lançamento encontrado para {data:yyyy-MM-dd}." })
            : Ok(resultado);
    }

    /// <summary>Retorna o consolidado de um período (inicio e fim em query string).</summary>
    [HttpGet("periodo")]
    [ProducesResponseType(typeof(ConsolidadoPeriodoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ObterPorPeriodo(
        [FromQuery] DateTime inicio,
        [FromQuery] DateTime fim,
        CancellationToken ct)
    {
        var resultado = await _consultar.ObterPorPeriodoAsync(inicio, fim, ct);
        return Ok(resultado);
    }
}
