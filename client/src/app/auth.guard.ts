import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { DemoPreviewService } from './demo-preview.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const demoPreviewService = inject(DemoPreviewService);
  const router = inject(Router);

  // TEMPORARY DEMO MODE: bypass auth-only checks for explicit preview routes without creating fake tokens.
  if (demoPreviewService.isDemoModeEnabled() && state.url.startsWith('/admin/') && demoPreviewService.getRole() === 'Admin') {
    return true;
  }

  return authService.hasToken() ? true : router.createUrlTree(['/login']);
};