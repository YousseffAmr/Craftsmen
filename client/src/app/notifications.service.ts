import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

export interface CraftsmanNotification {
  id: number;
  customerName: string;
  craftName: string;
  neededOn: string;
  price: number;
  description: string;
  status: string;
  createdAt: string;
}

export interface CustomerNotification {
  id: number;
  craftsmanName: string;
  craftName: string;
  neededOn: string;
  price: number;
  description: string;
  status: string;
  respondedAt: string;
}

interface ApiErrorResponse {
  message?: string;
}

@Injectable({ providedIn: 'root' })
export class NotificationsService {
  private readonly apiUrl = 'http://localhost:5128';

  constructor(private readonly http: HttpClient) {}

  getCraftsmanNotifications(): Observable<CraftsmanNotification[]> {
    return this.http.get<CraftsmanNotification[]>(`${this.apiUrl}/craftsman/notifications`)
      .pipe(catchError(this.handleError));
  }

  getCustomerNotifications(): Observable<CustomerNotification[]> {
    return this.http.get<CustomerNotification[]>(`${this.apiUrl}/customers/notifications`)
      .pipe(catchError(this.handleError));
  }

  private handleError = (error: HttpErrorResponse): Observable<never> => {
    const apiError = error.error as ApiErrorResponse | undefined;
    return throwError(() => new Error(apiError?.message ?? 'An unexpected error occurred.'));
  };
}
