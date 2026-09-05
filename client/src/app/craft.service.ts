import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

export interface Craft {
  id: number;
  name: string;
  description?: string | null;
}

export interface CraftPayload {
  name: string;
  description?: string | null;
}

interface ApiErrorResponse {
  status: number;
  message: string;
  errors?: Record<string, string>;
}

@Injectable({ providedIn: 'root' })
export class CraftService {
  private readonly apiUrl = 'http://localhost:5128';

  constructor(private readonly http: HttpClient) {}

  getCrafts(): Observable<Craft[]> {
    return this.http.get<Craft[]>(`${this.apiUrl}/admin/crafts`).pipe(catchError(this.handleError));
  }

  createCraft(payload: CraftPayload): Observable<Craft> {
    return this.http.post<Craft>(`${this.apiUrl}/admin/crafts`, payload).pipe(catchError(this.handleError));
  }

  updateCraft(id: number, payload: CraftPayload): Observable<Craft> {
    return this.http.put<Craft>(`${this.apiUrl}/admin/crafts/${id}`, payload).pipe(catchError(this.handleError));
  }

  deleteCraft(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/admin/crafts/${id}`).pipe(catchError(this.handleError));
  }

  private handleError = (error: HttpErrorResponse): Observable<never> => {
    const apiError = error.error as ApiErrorResponse | undefined;
    const message = apiError?.message ?? 'An unexpected error occurred.';
    return throwError(() => new Error(message));
  };
}
