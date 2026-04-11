import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, DatePipe, CurrencyPipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { LancamentosService } from '@core/services/lancamentos.service';
import { Lancamento, TipoLancamento } from '@core/models/models';

@Component({
  selector: 'app-lancamentos-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DatePipe, CurrencyPipe],
  templateUrl: './lancamentos-page.component.html',
  styleUrl: './lancamentos-page.component.scss'
})
export class LancamentosPageComponent implements OnInit {
  private readonly service = inject(LancamentosService);
  private readonly fb = inject(FormBuilder);

  lancamentos = signal<Lancamento[]>([]);
  carregando = signal(false);
  salvando = signal(false);
  removendo = signal<string | null>(null);
  erro = signal<string | null>(null);
  sucesso = signal<string | null>(null);
  mostrarFormulario = signal(false);

  form = this.fb.group({
    data: [new Date().toISOString().split('T')[0], Validators.required],
    valor: [null as number | null, [Validators.required, Validators.min(0.01)]],
    tipo: ['Credito' as TipoLancamento, Validators.required],
    descricao: ['', [Validators.required, Validators.maxLength(200)]]
  });

  ngOnInit() { this.carregar(); }

  carregar() {
    this.carregando.set(true);
    this.erro.set(null);
    this.service.listar().subscribe({
      next: dados => { this.lancamentos.set(dados); this.carregando.set(false); },
      error: e => { this.erro.set(e.mensagem); this.carregando.set(false); }
    });
  }

  salvar() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.salvando.set(true);
    this.erro.set(null);

    const v = this.form.value;
    this.service.criar({
      data: v.data!,
      valor: v.valor!,
      tipo: v.tipo!,
      descricao: v.descricao!
    }).subscribe({
      next: () => {
        this.sucesso.set('Lançamento criado com sucesso!');
        this.form.reset({ data: new Date().toISOString().split('T')[0], tipo: 'Credito' });
        this.mostrarFormulario.set(false);
        this.salvando.set(false);
        this.carregar();
        setTimeout(() => this.sucesso.set(null), 3000);
      },
      error: e => { this.erro.set(e.mensagem); this.salvando.set(false); }
    });
  }

  remover(id: string) {
    if (!confirm('Confirmar remoção do lançamento?')) return;
    this.removendo.set(id);
    this.service.remover(id).subscribe({
      next: () => {
        this.lancamentos.update(l => l.filter(x => x.id !== id));
        this.removendo.set(null);
        this.sucesso.set('Lançamento removido.');
        setTimeout(() => this.sucesso.set(null), 3000);
      },
      error: e => { this.erro.set(e.mensagem); this.removendo.set(null); }
    });
  }

  totalCreditos(): number {
    return this.lancamentos().filter(l => l.tipo === 'Credito').reduce((s, l) => s + l.valor, 0);
  }

  totalDebitos(): number {
    return this.lancamentos().filter(l => l.tipo === 'Debito').reduce((s, l) => s + l.valor, 0);
  }

  toggleFormulario() {
    this.mostrarFormulario.update(v => !v);
    if (!this.mostrarFormulario()) this.form.reset({ data: new Date().toISOString().split('T')[0], tipo: 'Credito' });
  }

  fieldError(field: string): boolean {
    const ctrl = this.form.get(field);
    return !!(ctrl?.invalid && ctrl?.touched);
  }
}
