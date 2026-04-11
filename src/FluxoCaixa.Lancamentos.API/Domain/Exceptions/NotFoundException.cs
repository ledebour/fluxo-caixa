namespace FluxoCaixa.Lancamentos.API.Domain.Exceptions;

/// <summary>
/// Exceção lançada quando um recurso não é encontrado.
/// Mapeada para HTTP 404 no middleware global.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string entidade, Guid id)
        : base($"{entidade} com id '{id}' não encontrado.") { }
}
