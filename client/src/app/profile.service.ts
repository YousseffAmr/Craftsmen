import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

export interface CraftsmanProfile { id: number; name: string; contactInfo?: string | null; dailyRate: number; status: string; }
export interface CustomerProfile { id: number; name: string; contactInfo?: string | null; }
interface ApiError { message?: string; }

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly apiUrl = 'http://localhost:5128';
  constructor(private readonly http: HttpClient) {}
  getCraftsman(): Observable<CraftsmanProfile> { return this.http.get<CraftsmanProfile>(`${this.apiUrl}/craftsman/profile`).pipe(catchError(this.handleError)); }
  updateCraftsman(profile: Pick<CraftsmanProfile, 'name'|'contactInfo'|'dailyRate'>): Observable<CraftsmanProfile> { return this.http.put<CraftsmanProfile>(`${this.apiUrl}/craftsman/profile`, profile).pipe(catchError(this.handleError)); }
  getCustomer(): Observable<CustomerProfile> { return this.http.get<CustomerProfile>(`${this.apiUrl}/customers/profile`).pipe(catchError(this.handleError)); }
  updateCustomer(profile: Pick<CustomerProfile, 'name'|'contactInfo'>): Observable<CustomerProfile> { return this.http.put<CustomerProfile>(`${this.apiUrl}/customers/profile`, profile).pipe(catchError(this.handleError)); }
  private handleError = (error: HttpErrorResponse): Observable<never> => throwError(() => new Error((error.error as ApiError | undefined)?.message ?? 'An unexpected error occurred.'));
}
