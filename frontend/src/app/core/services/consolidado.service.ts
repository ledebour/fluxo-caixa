import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import { ConsolidadoDiario, ConsolidadoPeriodo } from '@core/models/models';

@Injectable({ providedIn: 'root' })
export class ConsolidadoService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.consolidadoApiUrl;

  obterPorData(data: string): Observable<ConsolidadoDiario> {
    return this.http.get<ConsolidadoDiario>(`${this.baseUrl}/${data}`);
  }

  obterPorPeriodo(inicio: string, fim: string): Observable<ConsolidadoPeriodo> {
    const params = new HttpParams()
      .set('inicio', inicio)
      .set('fim', fim);
    return this.http.get<ConsolidadoPeriodo>(`${this.baseUrl}/periodo`, { params });
  }
}
