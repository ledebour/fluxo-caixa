using FluxoCaixa.Lancamentos.API.Domain.Exceptions;
using FluxoCaixa.Shared.Enums;

namespace FluxoCaixa.Lancamentos.API.Domain.Entities;

/// <summary>
/// Entidade de domínio que representa um lançamento financeiro.
/// Encapsula as regras de negócio e invariantes do domínio.
/// </summary>
public class Lancamento
{
    public Guid Id { get; private set; }
    public DateTime Data { get; private set; }
    public decimal Valor { get; private set; }
    public TipoLancamento Tipo { get; private set; }
    public string Descricao { get; private set; }
    public DateTime CriadoEm { get; private set; }

    // EF Core — construtor privado sem parâmetros
    private Lancamento() { }

    private Lancamento(DateTime data, decimal valor, TipoLancamento tipo, string descricao)
    {
        Id = Guid.NewGuid();
        Data = data.Date; // normaliza para meia-noite — dia contábil
        Valor = valor;
        Tipo = tipo;
        Descricao = descricao;
        CriadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Factory method — única forma pública de criar um Lancamento.
    /// Garante que as invariantes de domínio sejam sempre validadas.
    /// </summary>
    public static Lancamento Criar(DateTime data, decimal valor, TipoLancamento tipo, string descricao)
    {
        ValidarData(data);
        ValidarValor(valor);
        ValidarDescricao(descricao);

        return new Lancamento(data, valor, tipo, descricao);
    }

    // ─── Regras de domínio ────────────────────────────────────────

    private static void ValidarData(DateTime data)
    {
        if (data == default)
            throw new DomainException("A data do lançamento é obrigatória.");

        if (data.Date > DateTime.Now.Date)
            throw new DomainException("Não é permitido criar lançamentos com data futura.");
    }

    private static void ValidarValor(decimal valor)
    {
        if (valor <= 0)
            throw new DomainException("O valor do lançamento deve ser maior que zero.");

        if (valor > 9_999_999.99m)
            throw new DomainException("O valor do lançamento excede o limite permitido.");
    }

    private static void ValidarDescricao(string descricao)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            throw new DomainException("A descrição do lançamento é obrigatória.");

        if (descricao.Length > 200)
            throw new DomainException("A descrição não pode ultrapassar 200 caracteres.");
    }
}
