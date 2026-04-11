import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule, DatePipe, CurrencyPipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ConsolidadoService } from '@core/services/consolidado.service';
import { ConsolidadoDiario, ConsolidadoPeriodo } from '@core/models/models';

@Component({
  selector: 'app-consolidado-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DatePipe, CurrencyPipe],
  templateUrl: './consolidado-page.component.html',
  styleUrl: './consolidado-page.component.scss'
})
export class ConsolidadoPageComponent implements OnInit {
  private readonly service = inject(ConsolidadoService);
  private readonly fb = inject(FormBuilder);

  // ─── State ─────────────────────────────────────────────────────────────────
  consolidadoHoje = signal<ConsolidadoDiario | null>(null);
  periodo = signal<ConsolidadoPeriodo | null>(null);
  carregandoHoje = signal(false);
  carregandoPeriodo = signal(false);
  erroHoje = signal<string | null>(null);
  erroPeriodo = signal<string | null>(null);

  // Período padrão: últimos 7 dias
  readonly hoje = new Date().toISOString().split('T')[0];
  private readonly seteDiasAtras = new Date(Date.now() - 6 * 86400000).toISOString().split('T')[0];

  formPeriodo = this.fb.group({
    inicio: [this.seteDiasAtras, Validators.required],
    fim: [this.hoje, Validators.required]
  });

  // ─── Computed ───────────────────────────────────────────────────────────────
  saldoClass = computed(() => {
    const saldo = this.consolidadoHoje()?.saldoFinal ?? 0;
    if (saldo > 0) return 'valor-positivo';
    if (saldo < 0) return 'valor-negativo';
    return 'valor-neutro';
  });

  barMaximo = computed(() => {
    const dias = this.periodo()?.dias ?? [];
    return Math.max(...dias.map(d => Math.max(d.totalCreditos, d.totalDebitos)), 1);
  });

  ngOnInit() {
    this.carregarHoje();
    this.carregarPeriodo();
  }

  carregarHoje() {
    this.carregandoHoje.set(true);
    this.erroHoje.set(null);
    this.service.obterPorData(this.hoje).subscribe({
      next: d => { this.consolidadoHoje.set(d); this.carregandoHoje.set(false); },
      error: e => {
        if (e.status === 404) {
          this.consolidadoHoje.set(null);
          this.erroHoje.set('Nenhum lançamento encontrado para hoje.');
        } else {
          this.erroHoje.set(e.mensagem);
        }
        this.carregandoHoje.set(false);
      }
    });
  }

  carregarPeriodo() {
    if (this.formPeriodo.invalid) { this.formPeriodo.markAllAsTouched(); return; }
    const { inicio, fim } = this.formPeriodo.value;
    this.carregandoPeriodo.set(true);
    this.erroPeriodo.set(null);
    this.service.obterPorPeriodo(inicio!, fim!).subscribe({
      next: p => { this.periodo.set(p); this.carregandoPeriodo.set(false); },
      error: e => { this.erroPeriodo.set(e.mensagem); this.carregandoPeriodo.set(false); }
    });
  }

  barWidth(valor: number): number {
    return Math.round((valor / this.barMaximo()) * 100);
  }

  formatarData(iso: string): string {
    const [, m, d] = iso.split('-');
    return `${d}/${m}`;
  }

  trackByData(_: number, item: ConsolidadoDiario) { return item.data; }
}
