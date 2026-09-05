import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Craft } from './craft.service';

export interface AvailableCraftsman {
  id: number;
  name: string;
}

export interface CustomerRequestPayload {
  craftId: number;
  neededOn: string;
  price: number;
  description: string;
}

export interface CreatedCustomerRequest {
  id: number;
  status: string;
}

export interface CustomerRequest {
  id: number;
  craftsmanName: string;
  craftName: string;
  neededOn: string;
  price: number;
  description: string;
  status: string;
  createdAt: string;
  respondedAt?: string | null;
}

interface ApiErrorResponse {
  message?: string;
}

@Injectable({ providedIn: 'root' })
export class CustomerService {
  private readonly apiUrl = 'http://localhost:5128';

  constructor(private readonly http: HttpClient) {}

  getCrafts(): Observable<Craft[]> {
    return this.http.get<Craft[]>(`${this.apiUrl}/admin/crafts`).pipe(catchError(this.handleError));
  }

  getCraftsmen(craftId: number): Observable<AvailableCraftsman[]> {
    return this.http.get<AvailableCraftsman[]>(`${this.apiUrl}/customer/craftsmen?craftId=${craftId}`)
      .pipe(catchError(this.handleError));
  }

  createRequest(craftsmanId: number, payload: CustomerRequestPayload): Observable<CreatedCustomerRequest> {
    return this.http.post<CreatedCustomerRequest>(`${this.apiUrl}/customer/craftsmen/${craftsmanId}/request`, payload)
      .pipe(catchError(this.handleError));
  }

  getRequests(): Observable<CustomerRequest[]> {
    return this.http.get<CustomerRequest[]>(`${this.apiUrl}/customers/requests`).pipe(catchError(this.handleError));
  }

  withdraw(id: number): Observable<CustomerRequest> {
    return this.http.post<CustomerRequest>(`${this.apiUrl}/customers/requests/${id}/withdraw`, {}).pipe(catchError(this.handleError));
  }

  private handleError = (error: HttpErrorResponse): Observable<never> => {
    const apiError = error.error as ApiErrorResponse | undefined;
    return throwError(() => new Error(apiError?.message ?? 'An unexpected error occurred.'));
  };
}