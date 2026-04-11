// ─── Enums ───────────────────────────────────────────────────────────────────

export type TipoLancamento = 'Credito' | 'Debito';

// ─── Lançamentos ─────────────────────────────────────────────────────────────

export interface Lancamento {
  id: string;
  data: string;          // ISO date "yyyy-MM-dd"
  valor: number;
  tipo: TipoLancamento;
  descricao: string;
  criadoEm: string;
}

export interface CriarLancamentoRequest {
  data: string;
  valor: number;
  tipo: TipoLancamento;
  descricao: string;
}

// ─── Consolidado ─────────────────────────────────────────────────────────────

export interface ConsolidadoDiario {
  data: string;
  totalCreditos: number;
  totalDebitos: number;
  saldoFinal: number;
  quantidadeLancamentos: number;
  atualizadoEm: string;
  veioDoCache: boolean;
}

export interface ConsolidadoPeriodo {
  dataInicio: string;
  dataFim: string;
  totalCreditos: number;
  totalDebitos: number;
  saldoFinal: number;
  totalDias: number;
  dias: ConsolidadoDiario[];
}

// ─── Erro ────────────────────────────────────────────────────────────────────

export interface ApiError {
  mensagem: string;
  detalhe?: string;
  timestamp: string;
}
