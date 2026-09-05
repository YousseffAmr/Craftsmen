import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

export interface HelperResponse { answer: string; link?: string; }
interface ApiError { message?: string; }
@Injectable({ providedIn: 'root' })
export class HelperService {
  private readonly apiUrl = 'http://localhost:5128';
  constructor(private readonly http: HttpClient) {}
  ask(question: string): Observable<HelperResponse> { return this.http.post<HelperResponse>(`${this.apiUrl}/helper/ask`, { question }).pipe(catchError(this.handleError)); }
  private handleError = (error: HttpErrorResponse): Observable<never> => throwError(() => new Error((error.error as ApiError | undefined)?.message ?? 'An unexpected error occurred.'));
}
