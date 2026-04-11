import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import { CriarLancamentoRequest, Lancamento } from '@core/models/models';

@Injectable({ providedIn: 'root' })
export class LancamentosService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.lancamentosApiUrl;

  listar(): Observable<Lancamento[]> {
    return this.http.get<Lancamento[]>(this.baseUrl);
  }

  obterPorId(id: string): Observable<Lancamento> {
    return this.http.get<Lancamento>(`${this.baseUrl}/${id}`);
  }

  obterPorData(data: string): Observable<Lancamento[]> {
    return this.http.get<Lancamento[]>(`${this.baseUrl}/por-data/${data}`);
  }

  criar(request: CriarLancamentoRequest): Observable<Lancamento> {
    return this.http.post<Lancamento>(this.baseUrl, request);
  }

  remover(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
