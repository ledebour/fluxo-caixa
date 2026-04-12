using FluxoCaixa.Lancamentos.API.Domain.Entities;
using FluxoCaixa.Lancamentos.API.Domain.Exceptions;
using FluxoCaixa.Shared.Enums;
using Xunit;
namespace FluxoCaixa.Lancamentos.Tests.Domain;

/// <summary>
/// Testes de fronteira (boundary) e edge cases do domínio Lancamento.
/// Complementa LancamentoTests.cs com cobertura de valores limítrofes.
/// </summary>
public class LancamentoBoundaryTests
{
    // ─── Valor mínimo válido ──────────────────────────────────────────────────

    [Fact]
    public void Criar_ComValorMinimo_DeveSerPermitido()
    {
        var lancamento = Lancamento.Criar(DateTime.Today, 0.01m, TipoLancamento.Credito, "Valor mínimo");
        Assert.Equal(0.01m, lancamento.Valor);
    }

    [Fact]
    public void Criar_ComValorMaximo_DeveSerPermitido()
    {
        var lancamento = Lancamento.Criar(DateTime.Today, 9_999_999.99m, TipoLancamento.Credito, "Valor máximo");
        Assert.Equal(9_999_999.99m, lancamento.Valor);
    }

    // ─── Fronteira de data ────────────────────────────────────────────────────

    [Fact]
    public void Criar_ComDataHoje_DeveSerPermitido()
    {
        var lancamento = Lancamento.Criar(DateTime.Today, 100m, TipoLancamento.Debito, "Hoje");
        Assert.Equal(DateTime.Today, lancamento.Data);
    }

    [Fact]
    public void Criar_ComDataOntem_DeveSerPermitido()
    {
        var ontem = DateTime.Today.AddDays(-1);
        var lancamento = Lancamento.Criar(ontem, 100m, TipoLancamento.Credito, "Ontem");
        Assert.Equal(ontem.Date, lancamento.Data);
    }

    [Fact]
    public void Criar_ComDataMuitoAntiga_DeveSerPermitido()
    {
        var dataAntiga = new DateTime(2000, 1, 1);
        var lancamento = Lancamento.Criar(dataAntiga, 100m, TipoLancamento.Credito, "Data antiga");
        Assert.Equal(dataAntiga, lancamento.Data);
    }

    // ─── Fronteira de descrição ───────────────────────────────────────────────

    [Fact]
    public void Criar_ComDescricaoExatamente1Char_DeveSerPermitido()
    {
        var lancamento = Lancamento.Criar(DateTime.Today, 100m, TipoLancamento.Credito, "X");
        Assert.Equal("X", lancamento.Descricao);
    }

    [Fact]
    public void Criar_ComDescricaoExatamente200Chars_DeveSerPermitido()
    {
        var descricao200 = new string('A', 200);
        var lancamento = Lancamento.Criar(DateTime.Today, 100m, TipoLancamento.Credito, descricao200);
        Assert.Equal(200, lancamento.Descricao.Length);
    }

    [Fact]
    public void Criar_ComDescricao201Chars_DeveLancarDomainException()
    {
        var descricao201 = new string('A', 201);
        Assert.Throws<DomainException>(() =>
            Lancamento.Criar(DateTime.Today, 100m, TipoLancamento.Credito, descricao201));
    }

    // ─── ID gerado automaticamente ────────────────────────────────────────────

    [Fact]
    public void Criar_DoisLancamentos_DevemTerIdsDistintos()
    {
        var l1 = Lancamento.Criar(DateTime.Today, 100m, TipoLancamento.Credito, "Primeiro");
        var l2 = Lancamento.Criar(DateTime.Today, 100m, TipoLancamento.Credito, "Segundo");

        Assert.NotEqual(l1.Id, l2.Id);
    }

    // ─── Normalização de data com hora ────────────────────────────────────────

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(12, 0, 0)]
    [InlineData(23, 59, 59)]
    public void Criar_ComDiferentesHorarios_SempreNormalizaParaMeiaNite(int h, int m, int s)
    {
        var dataComHora = new DateTime(2025, 6, 15, h, m, s);
        var lancamento = Lancamento.Criar(dataComHora, 100m, TipoLancamento.Credito, "Hora teste");
        Assert.Equal(new DateTime(2025, 6, 15), lancamento.Data);
    }
}
