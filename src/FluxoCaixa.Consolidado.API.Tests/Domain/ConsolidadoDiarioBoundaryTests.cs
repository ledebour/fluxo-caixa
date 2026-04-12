using FluxoCaixa.Consolidado.API.Domain.Entities;
using Xunit;
namespace FluxoCaixa.Consolidado.Tests.Domain;

public class ConsolidadoDiarioBoundaryTests
{
    // ─── Múltiplos lançamentos ────────────────────────────────────────────────

    [Fact]
    public void AplicarCredito_Multiplos_DeveAcumularCorretamente()
    {
        var c = ConsolidadoDiario.Criar(DateTime.Today);
        c.AplicarCredito(100m);
        c.AplicarCredito(200m);
        c.AplicarCredito(300m);

        Assert.Equal(600m, c.TotalCreditos);
        Assert.Equal(3, c.QuantidadeLancamentos);
    }

    [Fact]
    public void AplicarDebito_Multiplos_DeveAcumularCorretamente()
    {
        var c = ConsolidadoDiario.Criar(DateTime.Today);
        c.AplicarDebito(50m);
        c.AplicarDebito(75m);

        Assert.Equal(125m, c.TotalDebitos);
        Assert.Equal(2, c.QuantidadeLancamentos);
    }

    [Fact]
    public void MisturaCredidosEDebitos_SaldoDeveEstarCorreto()
    {
        var c = ConsolidadoDiario.Criar(DateTime.Today);
        c.AplicarCredito(1000m);
        c.AplicarCredito(500m);
        c.AplicarDebito(300m);
        c.AplicarDebito(200m);

        Assert.Equal(1500m, c.TotalCreditos);
        Assert.Equal(500m, c.TotalDebitos);
        Assert.Equal(1000m, c.SaldoFinal);
        Assert.Equal(4, c.QuantidadeLancamentos);
    }

    // ─── Estorno ─────────────────────────────────────────────────────────────

    [Fact]
    public void EstornarDebito_DeveReduzirTotalDebitos()
    {
        var c = ConsolidadoDiario.Criar(DateTime.Today);
        c.AplicarDebito(400m);
        c.EstornarDebito(150m);

        Assert.Equal(250m, c.TotalDebitos);
        Assert.Equal(0, c.QuantidadeLancamentos);
    }

    [Fact]
    public void EstornarCredito_ValorMaiorQueTotal_NaoDeveResultarEmNegativo()
    {
        var c = ConsolidadoDiario.Criar(DateTime.Today);
        c.AplicarCredito(100m);
        c.EstornarCredito(500m); // estorna mais do que tem

        Assert.Equal(0m, c.TotalCreditos);
    }

    [Fact]
    public void EstornarDebito_ValorMaiorQueTotal_NaoDeveResultarEmNegativo()
    {
        var c = ConsolidadoDiario.Criar(DateTime.Today);
        c.AplicarDebito(100m);
        c.EstornarDebito(999m);

        Assert.Equal(0m, c.TotalDebitos);
    }

    [Fact]
    public void EstornarQuantidade_NuncaDeveSerNegativa()
    {
        var c = ConsolidadoDiario.Criar(DateTime.Today);
        c.AplicarCredito(100m);
        c.EstornarCredito(100m);
        c.EstornarCredito(100m); // segundo estorno sem lançamento

        Assert.Equal(0, c.QuantidadeLancamentos);
    }

    // ─── Rehidratar ───────────────────────────────────────────────────────────

    [Fact]
    public void Rehidratar_SaldoFinalDeveSerCalculadoCorretamente()
    {
        var c = ConsolidadoDiario.Rehidratar(
            Guid.NewGuid(), DateTime.Today,
            totalCreditos: 2000m, totalDebitos: 800m,
            quantidadeLancamentos: 10, atualizadoEm: DateTime.UtcNow);

        Assert.Equal(1200m, c.SaldoFinal);
    }

    [Fact]
    public void Rehidratar_DevePermitirAplicarNovosLancamentosApos()
    {
        var c = ConsolidadoDiario.Rehidratar(
            Guid.NewGuid(), DateTime.Today,
            totalCreditos: 500m, totalDebitos: 200m,
            quantidadeLancamentos: 3, atualizadoEm: DateTime.UtcNow);

        c.AplicarCredito(100m);

        Assert.Equal(600m, c.TotalCreditos);
        Assert.Equal(4, c.QuantidadeLancamentos);
    }

    [Fact]
    public void Rehidratar_DataDeveSerNormalizadaParaDate()
    {
        var dataComHora = new DateTime(2025, 6, 15, 14, 30, 0);
        var c = ConsolidadoDiario.Rehidratar(
            Guid.NewGuid(), dataComHora, 100m, 0m, 1, DateTime.UtcNow);

        Assert.Equal(new DateTime(2025, 6, 15), c.Data);
    }

    // ─── AtualizadoEm ─────────────────────────────────────────────────────────

    [Fact]
    public void AplicarCredito_DeveAtualizarAtualizadoEm()
    {
        var antes = DateTime.UtcNow.AddSeconds(-1);
        var c = ConsolidadoDiario.Criar(DateTime.Today);
        c.AplicarCredito(100m);

        Assert.True(c.AtualizadoEm >= antes);
    }

    [Fact]
    public void AplicarDebito_DeveAtualizarAtualizadoEm()
    {
        var antes = DateTime.UtcNow.AddSeconds(-1);
        var c = ConsolidadoDiario.Criar(DateTime.Today);
        c.AplicarDebito(50m);

        Assert.True(c.AtualizadoEm >= antes);
    }

    // ─── SaldoFinal computado ─────────────────────────────────────────────────

    [Fact]
    public void SaldoFinal_SemLancamentos_DeveSerZero()
    {
        var c = ConsolidadoDiario.Criar(DateTime.Today);
        Assert.Equal(0m, c.SaldoFinal);
    }

    [Fact]
    public void SaldoFinal_DebitosMaioresQueCreditos_DeveSerNegativo()
    {
        var c = ConsolidadoDiario.Criar(DateTime.Today);
        c.AplicarCredito(100m);
        c.AplicarDebito(350m);

        Assert.Equal(-250m, c.SaldoFinal);
    }
}
