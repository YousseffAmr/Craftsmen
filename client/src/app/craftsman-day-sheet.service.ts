import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CraftsmanRequest } from './craftsman-requests.service';

export interface DaySheetResponse {
  date: string;
  requests: CraftsmanRequest[];
}

interface ApiErrorResponse {
  message?: string;
}

@Injectable({ providedIn: 'root' })
export class CraftsmanDaySheetService {
  private readonly apiUrl = 'http://localhost:5128';

  constructor(private readonly http: HttpClient) {}

  getDaySheet(date: string): Observable<DaySheetResponse> {
    return this.http.get<DaySheetResponse>(`${this.apiUrl}/craftsman/day-sheet`, { params: { date } })
      .pipe(catchError(this.handleError));
  }

  complete(requestId: number): Observable<CraftsmanRequest> {
    return this.http.post<CraftsmanRequest>(`${this.apiUrl}/craftsman/day-sheet/${requestId}/complete`, {})
      .pipe(catchError(this.handleError));
  }

  private handleError = (error: HttpErrorResponse): Observable<never> => {
    const apiError = error.error as ApiErrorResponse | undefined;
    return throwError(() => new Error(apiError?.message ?? 'An unexpected error occurred.'));
  };
}
