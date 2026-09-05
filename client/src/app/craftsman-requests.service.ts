import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

export type CraftsmanRequestStatus = 'Pending' | 'Accepted' | 'Declined' | 'Withdrawn' | 'Completed';

export interface CraftsmanRequest {
  id: number;
  customerName: string;
  craftName: string;
  neededOn: string;
  price: number;
  description: string;
  status: CraftsmanRequestStatus;
  createdAt: string;
  respondedAt?: string | null;
}

interface ApiErrorResponse {
  message?: string;
}

@Injectable({ providedIn: 'root' })
export class CraftsmanRequestsService {
  private readonly apiUrl = 'http://localhost:5128';

  constructor(private readonly http: HttpClient) {}

  getRequests(): Observable<CraftsmanRequest[]> {
    return this.http.get<CraftsmanRequest[]>(`${this.apiUrl}/craftsman/requests`)
      .pipe(catchError(this.handleError));
  }

  accept(id: number): Observable<CraftsmanRequest> {
    return this.http.post<CraftsmanRequest>(`${this.apiUrl}/craftsman/requests/${id}/accept`, {})
      .pipe(catchError(this.handleError));
  }

  decline(id: number): Observable<CraftsmanRequest> {
    return this.http.post<CraftsmanRequest>(`${this.apiUrl}/craftsman/requests/${id}/decline`, {})
      .pipe(catchError(this.handleError));
  }

  private handleError = (error: HttpErrorResponse): Observable<never> => {
    const apiError = error.error as ApiErrorResponse | undefined;
    return throwError(() => new Error(apiError?.message ?? 'An unexpected error occurred.'));
  };
}