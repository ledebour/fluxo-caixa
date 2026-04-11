using FluxoCaixa.Consolidado.API.Domain.Entities;

namespace FluxoCaixa.Consolidado.Tests.Domain;

public class ConsolidadoDiarioTests
{
    [Fact]
    public void Criar_DeveIniciarComSaldoZerado()
    {
        var consolidado = ConsolidadoDiario.Criar(DateTime.Today);

        Assert.Equal(0, consolidado.TotalCreditos);
        Assert.Equal(0, consolidado.TotalDebitos);
        Assert.Equal(0, consolidado.SaldoFinal);
        Assert.Equal(0, consolidado.QuantidadeLancamentos);
    }

    [Fact]
    public void AplicarCredito_DeveAumentarTotalCreditosESaldo()
    {
        var consolidado = ConsolidadoDiario.Criar(DateTime.Today);

        consolidado.AplicarCredito(500m);

        Assert.Equal(500m, consolidado.TotalCreditos);
        Assert.Equal(500m, consolidado.SaldoFinal);
        Assert.Equal(1, consolidado.QuantidadeLancamentos);
    }

    [Fact]
    public void AplicarDebito_DeveAumentarTotalDebitosEReduzirSaldo()
    {
        var consolidado = ConsolidadoDiario.Criar(DateTime.Today);
        consolidado.AplicarCredito(1000m);
        consolidado.AplicarDebito(300m);

        Assert.Equal(1000m, consolidado.TotalCreditos);
        Assert.Equal(300m, consolidado.TotalDebitos);
        Assert.Equal(700m, consolidado.SaldoFinal);
        Assert.Equal(2, consolidado.QuantidadeLancamentos);
    }

    [Fact]
    public void SaldoFinal_DeveSerNegativoQuandoDebitosMaioresQueCreditos()
    {
        var consolidado = ConsolidadoDiario.Criar(DateTime.Today);
        consolidado.AplicarCredito(100m);
        consolidado.AplicarDebito(300m);

        Assert.Equal(-200m, consolidado.SaldoFinal);
    }

    [Fact]
    public void EstornarCredito_DeveReduzirTotalCreditos()
    {
        var consolidado = ConsolidadoDiario.Criar(DateTime.Today);
        consolidado.AplicarCredito(500m);
        consolidado.EstornarCredito(200m);

        Assert.Equal(300m, consolidado.TotalCreditos);
        Assert.Equal(0, consolidado.QuantidadeLancamentos);
    }

    [Fact]
    public void AplicarCredito_ComValorZero_DeveLancarArgumentException()
    {
        var consolidado = ConsolidadoDiario.Criar(DateTime.Today);
        Assert.Throws<ArgumentException>(() => consolidado.AplicarCredito(0));
    }

    [Fact]
    public void AplicarDebito_ComValorNegativo_DeveLancarArgumentException()
    {
        var consolidado = ConsolidadoDiario.Criar(DateTime.Today);
        Assert.Throws<ArgumentException>(() => consolidado.AplicarDebito(-50m));
    }

    [Fact]
    public void Rehidratar_DeveRestaurarEstadoCompleto()
    {
        var id = Guid.NewGuid();
        var data = new DateTime(2025, 1, 15);
        var atualizado = DateTime.UtcNow;

        var consolidado = ConsolidadoDiario.Rehidratar(id, data, 1000m, 400m, 5, atualizado);

        Assert.Equal(id, consolidado.Id);
        Assert.Equal(1000m, consolidado.TotalCreditos);
        Assert.Equal(400m, consolidado.TotalDebitos);
        Assert.Equal(600m, consolidado.SaldoFinal);
        Assert.Equal(5, consolidado.QuantidadeLancamentos);
    }
}
