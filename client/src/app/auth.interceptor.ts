import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';
import { DemoPreviewService } from './demo-preview.service';

const tokenKey = 'craftconnect_token';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const authService = inject(AuthService);
  const demoPreviewService = inject(DemoPreviewService);
  const router = inject(Router);
  const token = localStorage.getItem(tokenKey);
  const isAuthRequest = request.url.includes('/auth/login') || request.url.includes('/auth/signup');
  const authenticatedRequest = token
    ? request.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`,
        },
      })
    : request;

  return next(authenticatedRequest).pipe(
    catchError((error: HttpErrorResponse) => {
      // TEMPORARY DEMO MODE: keep the UI on the preview route so existing loading/error states can render.
      if (error.status === 401 && !isAuthRequest && !demoPreviewService.isDemoModeEnabled()) {
        authService.clearToken();
        void router.navigate(['/login']);
      }

      return throwError(() => error);
    }),
  );
};