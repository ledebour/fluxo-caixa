using FluxoCaixa.Shared.Enums;

namespace FluxoCaixa.Lancamentos.API.Application.DTOs;

// ─── Request ─────────────────────────────────────────────────────────────────

/// <summary>Request para criação de um novo lançamento.</summary>
public record CriarLancamentoRequest
{
    public DateTime Data { get; init; }
    public decimal Valor { get; init; }
    public TipoLancamento Tipo { get; init; }
    public string Descricao { get; init; } = string.Empty;
}

// ─── Response ────────────────────────────────────────────────────────────────

/// <summary>Resposta com os dados de um lançamento.</summary>
public record LancamentoResponse
{
    public Guid Id { get; init; }
    public DateTime Data { get; init; }
    public decimal Valor { get; init; }
    public string Tipo { get; init; } = string.Empty;
    public string Descricao { get; init; } = string.Empty;
    public DateTime CriadoEm { get; init; }
}

/// <summary>Resposta de erro padronizada para a API.</summary>
public record ErrorResponse
{
    public string Mensagem { get; init; } = string.Empty;
    public string? Detalhe { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
