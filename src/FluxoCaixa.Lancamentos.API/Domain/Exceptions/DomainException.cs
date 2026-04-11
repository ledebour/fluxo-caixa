namespace FluxoCaixa.Lancamentos.API.Domain.Exceptions;

/// <summary>
/// Exceção lançada quando uma regra de negócio do domínio é violada.
/// Mapeada para HTTP 422 (Unprocessable Entity) no middleware global.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
