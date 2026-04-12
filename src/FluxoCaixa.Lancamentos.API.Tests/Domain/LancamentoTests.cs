using FluxoCaixa.Lancamentos.API.Domain.Entities;
using FluxoCaixa.Lancamentos.API.Domain.Exceptions;
using FluxoCaixa.Shared.Enums;
using Xunit;
namespace FluxoCaixa.Lancamentos.Tests.Domain;

public class LancamentoTests
{
    // ─── Cenários de sucesso ──────────────────────────────────────────────────

    [Fact]
    public void Criar_ComDadosValidos_DeveRetornarLancamento()
    {
        var data = DateTime.Today;
        var valor = 150.00m;
        var tipo = TipoLancamento.Credito;
        var descricao = "Venda de produto";

        var lancamento = Lancamento.Criar(data, valor, tipo, descricao);

        Assert.NotEqual(Guid.Empty, lancamento.Id);
        Assert.Equal(data.Date, lancamento.Data);
        Assert.Equal(valor, lancamento.Valor);
        Assert.Equal(tipo, lancamento.Tipo);
        Assert.Equal(descricao, lancamento.Descricao);
    }

    [Theory]
    [InlineData(TipoLancamento.Credito)]
    [InlineData(TipoLancamento.Debito)]
    public void Criar_ComQualquerTipo_DeveSerPermitido(TipoLancamento tipo)
    {
        var lancamento = Lancamento.Criar(DateTime.Today, 100m, tipo, "Descrição teste");
        Assert.Equal(tipo, lancamento.Tipo);
    }

    [Fact]
    public void Criar_DeveNormalizarDataParaMeiaNite()
    {
        var dataComHora = new DateTime(2025, 1, 15, 14, 30, 0);
        var lancamento = Lancamento.Criar(dataComHora, 100m, TipoLancamento.Credito, "Teste");
        Assert.Equal(dataComHora.Date, lancamento.Data);
    }

    // ─── Validação de Data ────────────────────────────────────────────────────

    [Fact]
    public void Criar_ComDataFutura_DeveLancarDomainException()
    {
        var dataFutura = DateTime.Today.AddDays(1);

        var ex = Assert.Throws<DomainException>(() =>
            Lancamento.Criar(dataFutura, 100m, TipoLancamento.Credito, "Futuro"));

        Assert.Contains("data futura", ex.Message);
    }

    [Fact]
    public void Criar_ComDataDefault_DeveLancarDomainException()
    {
        Assert.Throws<DomainException>(() =>
            Lancamento.Criar(default, 100m, TipoLancamento.Credito, "Sem data"));
    }

    // ─── Validação de Valor ───────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void Criar_ComValorInvalido_DeveLancarDomainException(decimal valorInvalido)
    {
        var ex = Assert.Throws<DomainException>(() =>
            Lancamento.Criar(DateTime.Today, valorInvalido, TipoLancamento.Debito, "Valor inválido"));

        Assert.Contains("maior que zero", ex.Message);
    }

    [Fact]
    public void Criar_ComValorAcimaDoLimite_DeveLancarDomainException()
    {
        Assert.Throws<DomainException>(() =>
            Lancamento.Criar(DateTime.Today, 10_000_000m, TipoLancamento.Credito, "Acima do limite"));
    }

    // ─── Validação de Descrição ───────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Criar_ComDescricaoVazia_DeveLancarDomainException(string? descricao)
    {
        Assert.Throws<DomainException>(() =>
            Lancamento.Criar(DateTime.Today, 100m, TipoLancamento.Credito, descricao!));
    }

    [Fact]
    public void Criar_ComDescricaoMaior200Chars_DeveLancarDomainException()
    {
        var descricaoGrande = new string('x', 201);

        Assert.Throws<DomainException>(() =>
            Lancamento.Criar(DateTime.Today, 100m, TipoLancamento.Credito, descricaoGrande));
    }
}
