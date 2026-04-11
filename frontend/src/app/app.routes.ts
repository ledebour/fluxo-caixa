import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'lancamentos',
    pathMatch: 'full'
  },
  {
    path: 'lancamentos',
    loadComponent: () =>
      import('./features/lancamentos/pages/lancamentos-page.component')
        .then(m => m.LancamentosPageComponent),
    title: 'Lançamentos — Fluxo de Caixa'
  },
  {
    path: 'consolidado',
    loadComponent: () =>
      import('./features/consolidado/pages/consolidado-page.component')
        .then(m => m.ConsolidadoPageComponent),
    title: 'Consolidado Diário — Fluxo de Caixa'
  },
  {
    path: '**',
    redirectTo: 'lancamentos'
  }
];
