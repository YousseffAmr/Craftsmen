import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

export type UserRole = 'Customer' | 'Craftsman';
export type AuthenticatedRole = UserRole | 'Admin';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface SignupRequest {
  name: string;
  email: string;
  password: string;
  role?: UserRole;
}

export interface SignupResponse {
  id: number;
  name: string;
  email: string;
  role: UserRole;
}

export interface LoginResponse {
  token: string;
  user: {
    id: number;
    name: string;
    email: string;
    role: AuthenticatedRole;
  };
}

interface ApiErrorResponse {
  status: number;
  message: string;
  errors?: Record<string, string>;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = 'http://localhost:5128';
  private readonly tokenKey = 'craftconnect_token';

  constructor(private readonly http: HttpClient) {}

  signup(payload: SignupRequest): Observable<SignupResponse> {
    return this.http.post<SignupResponse>(`${this.apiUrl}/auth/signup`, payload).pipe(catchError(this.handleError));
  }

  login(payload: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/auth/login`, payload).pipe(catchError(this.handleError));
  }

  storeToken(token: string): void {
    localStorage.setItem(this.tokenKey, token);
  }

  hasToken(): boolean {
    return Boolean(localStorage.getItem(this.tokenKey));
  }

  clearToken(): void {
    localStorage.removeItem(this.tokenKey);
  }

  getRole(): AuthenticatedRole | null {
    const token = localStorage.getItem(this.tokenKey);
    if (!token) {
      return null;
    }

    try {
      const encodedPayload = token.split('.')[1]
        .replace(/-/g, '+')
        .replace(/_/g, '/');
      const paddedPayload = encodedPayload.padEnd(encodedPayload.length + ((4 - encodedPayload.length % 4) % 4), '=');
      const payload = JSON.parse(atob(paddedPayload)) as {
        role?: AuthenticatedRole | AuthenticatedRole[];
        [claim: string]: unknown;
      };
      const role = payload.role ?? payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      return Array.isArray(role) ? role[0] ?? null : role as AuthenticatedRole | null;
    } catch {
      return null;
    }
  }

  private handleError = (error: HttpErrorResponse): Observable<never> => {
    const apiError = error.error as ApiErrorResponse | undefined;
    const message = apiError?.message ?? 'An unexpected error occurred.';
    return throwError(() => new Error(message));
  };
}
