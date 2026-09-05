import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

export interface BoardGroup {
  status: string;
  count: number;
  total: number;
}

export interface CraftsmanBoard {
  groups: BoardGroup[];
  totals: {
    requestCount: number;
    totalValue: number;
  };
}

interface ApiErrorResponse {
  message?: string;
}

@Injectable({ providedIn: 'root' })
export class CraftsmanBoardService {
  private readonly apiUrl = 'http://localhost:5128';

  constructor(private readonly http: HttpClient) {}

  getBoard(): Observable<CraftsmanBoard> {
    return this.http.get<CraftsmanBoard>(`${this.apiUrl}/craftsman/board`)
      .pipe(catchError(this.handleError));
  }

  private handleError = (error: HttpErrorResponse): Observable<never> => {
    const apiError = error.error as ApiErrorResponse | undefined;
    return throwError(() => new Error(apiError?.message ?? 'An unexpected error occurred.'));
  };
}
