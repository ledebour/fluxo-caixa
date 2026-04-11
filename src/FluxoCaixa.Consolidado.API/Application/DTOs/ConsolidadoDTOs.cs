namespace FluxoCaixa.Consolidado.API.Application.DTOs;

/// <summary>Resposta com o saldo consolidado de um dia.</summary>
public record ConsolidadoDiarioResponse
{
    public DateTime Data { get; init; }
    public decimal TotalCreditos { get; init; }
    public decimal TotalDebitos { get; init; }
    public decimal SaldoFinal { get; init; }
    public int QuantidadeLancamentos { get; init; }
    public DateTime AtualizadoEm { get; init; }
    public bool VeioDoCache { get; init; }
}

/// <summary>Resposta de um período consolidado.</summary>
public record ConsolidadoPeriodoResponse
{
    public DateTime DataInicio { get; init; }
    public DateTime DataFim { get; init; }
    public decimal TotalCreditos { get; init; }
    public decimal TotalDebitos { get; init; }
    public decimal SaldoFinal { get; init; }
    public int TotalDias { get; init; }
    public IEnumerable<ConsolidadoDiarioResponse> Dias { get; init; } = [];
}

/// <summary>Resposta de erro padronizada.</summary>
public record ErrorResponse
{
    public string Mensagem { get; init; } = string.Empty;
    public string? Detalhe { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
