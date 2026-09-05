import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

export interface PendingCraftsman {
  id: number;
  name: string;
  email: string;
  status: 'Pending';
}

interface ApiErrorResponse {
  message?: string;
}

@Injectable({ providedIn: 'root' })
export class CraftsmanAdminService {
  private readonly apiUrl = 'http://localhost:5128';

  constructor(private readonly http: HttpClient) {}

  getPending(): Observable<PendingCraftsman[]> {
    return this.http.get<PendingCraftsman[]>(`${this.apiUrl}/admin/craftsmen/pending`)
      .pipe(catchError(this.handleError));
  }

  approve(id: number): Observable<unknown> {
    return this.http.post(`${this.apiUrl}/admin/craftsmen/${id}/approve`, {})
      .pipe(catchError(this.handleError));
  }

  refuse(id: number): Observable<unknown> {
    return this.http.post(`${this.apiUrl}/admin/craftsmen/${id}/refuse`, {})
      .pipe(catchError(this.handleError));
  }

  private handleError = (error: HttpErrorResponse): Observable<never> => {
    const apiError = error.error as ApiErrorResponse | undefined;
    return throwError(() => new Error(apiError?.message ?? 'An unexpected error occurred.'));
  };
}