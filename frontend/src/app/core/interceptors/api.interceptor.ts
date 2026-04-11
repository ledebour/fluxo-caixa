import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

export const apiInterceptor: HttpInterceptorFn = (req, next) => {
  const reqComHeaders = req.clone({
    setHeaders: { 'Content-Type': 'application/json' }
  });

  return next(reqComHeaders).pipe(
    catchError((error: HttpErrorResponse) => {
      const mensagem = error.error?.mensagem ?? error.message ?? 'Erro desconhecido';
      console.error(`[API Error] ${error.status} — ${mensagem}`);
      return throwError(() => ({ status: error.status, mensagem }));
    })
  );
};
