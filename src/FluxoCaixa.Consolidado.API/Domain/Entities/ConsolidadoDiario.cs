namespace FluxoCaixa.Consolidado.API.Domain.Entities;

public class ConsolidadoDiario
{
    public Guid Id { get; private set; }
    public DateTime Data { get; private set; }
    public decimal TotalCreditos { get; private set; }
    public decimal TotalDebitos { get; private set; }
    public decimal SaldoFinal => TotalCreditos - TotalDebitos;
    public int QuantidadeLancamentos { get; private set; }
    public DateTime AtualizadoEm { get; private set; }

    private ConsolidadoDiario() { }

    private ConsolidadoDiario(DateTime data)
    {
        Id = Guid.NewGuid();
        Data = data.Date;
        TotalCreditos = 0;
        TotalDebitos = 0;
        QuantidadeLancamentos = 0;
        AtualizadoEm = DateTime.UtcNow;
    }

    public static ConsolidadoDiario Criar(DateTime data) => new(data);

    public static ConsolidadoDiario Rehidratar(
        Guid id, DateTime data, decimal totalCreditos, decimal totalDebitos,
        int quantidadeLancamentos, DateTime atualizadoEm)
    {
        return new ConsolidadoDiario
        {
            Id = id,
            Data = data.Date,
            TotalCreditos = totalCreditos,
            TotalDebitos = totalDebitos,
            QuantidadeLancamentos = quantidadeLancamentos,
            AtualizadoEm = atualizadoEm
        };
    }

    public void AplicarCredito(decimal valor)
    {
        if (valor <= 0) throw new ArgumentException("Valor de crédito deve ser positivo.", nameof(valor));
        TotalCreditos += valor;
        QuantidadeLancamentos++;
        AtualizadoEm = DateTime.UtcNow;
    }

    public void AplicarDebito(decimal valor)
    {
        if (valor <= 0) throw new ArgumentException("Valor de débito deve ser positivo.", nameof(valor));
        TotalDebitos += valor;
        QuantidadeLancamentos++;
        AtualizadoEm = DateTime.UtcNow;
    }

    public void EstornarCredito(decimal valor)
    {
        TotalCreditos = Math.Max(0, TotalCreditos - valor);
        QuantidadeLancamentos = Math.Max(0, QuantidadeLancamentos - 1);
        AtualizadoEm = DateTime.UtcNow;
    }

    public void EstornarDebito(decimal valor)
    {
        TotalDebitos = Math.Max(0, TotalDebitos - valor);
        QuantidadeLancamentos = Math.Max(0, QuantidadeLancamentos - 1);
        AtualizadoEm = DateTime.UtcNow;
    }
}
