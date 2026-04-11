import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="app-shell">
      <aside class="sidebar">
        <div class="sidebar__brand">
          <span class="sidebar__icon">₢</span>
          <span class="sidebar__title">FluxoCaixa</span>
        </div>

        <nav class="sidebar__nav">
          <a routerLink="/lancamentos"
             routerLinkActive="sidebar__link--active"
             class="sidebar__link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M12 2v20M2 12h20"/>
            </svg>
            Lançamentos
          </a>
          <a routerLink="/consolidado"
             routerLinkActive="sidebar__link--active"
             class="sidebar__link">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <rect x="3" y="3" width="7" height="7"/><rect x="14" y="3" width="7" height="7"/>
              <rect x="3" y="14" width="7" height="7"/><rect x="14" y="14" width="7" height="7"/>
            </svg>
            Consolidado
          </a>
        </nav>

        <div class="sidebar__footer">
          <span class="sidebar__version">v1.0.0</span>
        </div>
      </aside>

      <main class="main-content">
        <router-outlet />
      </main>
    </div>
  `,
  styleUrl: './app.component.scss'
})
export class AppComponent {}
