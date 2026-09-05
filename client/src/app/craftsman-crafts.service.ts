import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

export interface CraftsmanCraftOption {
  id: number;
  name: string;
  description?: string | null;
  selected: boolean;
}

export interface CraftsmanCraftsResponse {
  status: 'Pending' | 'Approved' | 'Refused';
  crafts: CraftsmanCraftOption[];
}

interface ApiErrorResponse {
  message?: string;
}

@Injectable({ providedIn: 'root' })
export class CraftsmanCraftsService {
  private readonly apiUrl = 'http://localhost:5128';

  constructor(private readonly http: HttpClient) {}

  getCrafts(): Observable<CraftsmanCraftsResponse> {
    return this.http.get<CraftsmanCraftsResponse>(`${this.apiUrl}/craftsman/crafts`)
      .pipe(catchError(this.handleError));
  }

  selectCraft(id: number): Observable<unknown> {
    return this.http.post(`${this.apiUrl}/craftsman/crafts/${id}`, {})
      .pipe(catchError(this.handleError));
  }

  removeCraft(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/craftsman/crafts/${id}`)
      .pipe(catchError(this.handleError));
  }

  private handleError = (error: HttpErrorResponse): Observable<never> => {
    const apiError = error.error as ApiErrorResponse | undefined;
    return throwError(() => new Error(apiError?.message ?? 'An unexpected error occurred.'));
  };
}