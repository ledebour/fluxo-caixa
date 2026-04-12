using FluxoCaixa.Lancamentos.API.Domain.Exceptions;
using Xunit;
namespace FluxoCaixa.Lancamentos.API.Tests.Domain;

public class ExcecoesDominioTests
{
    // ─── DomainException ─────────────────────────────────────────────────────

    [Fact]
    public void DomainException_DeveTerMensagemCorreta()
    {
        var mensagem = "Regra de negócio violada.";

        var ex = new DomainException(mensagem);

        Assert.Equal(mensagem, ex.Message);
    }

    [Fact]
    public void DomainException_DeveSerException()
    {
        var ex = new DomainException("teste");

        Assert.IsAssignableFrom<Exception>(ex);
    }

    // ─── NotFoundException ────────────────────────────────────────────────────

    [Fact]
    public void NotFoundException_DeveTerMensagemComEntidadeEId()
    {
        var id = Guid.NewGuid();

        var ex = new NotFoundException("Lancamento", id);

        Assert.Contains("Lancamento", ex.Message);
        Assert.Contains(id.ToString(), ex.Message);
    }

    [Fact]
    public void NotFoundException_DeveSerException()
    {
        var ex = new NotFoundException("Entidade", Guid.NewGuid());

        Assert.IsAssignableFrom<Exception>(ex);
    }
}
