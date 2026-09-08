import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { DemoPreviewService } from './demo-preview.service';

export const adminGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const demoPreviewService = inject(DemoPreviewService);
  const router = inject(Router);

  // TEMPORARY DEMO MODE: allow the admin preview route without requiring real authentication.
  if (demoPreviewService.isDemoModeEnabled() && demoPreviewService.getRole() === 'Admin') {
    return demoPreviewService.isRoleAllowedForRoute(state.url, 'Admin') ? true : router.createUrlTree(['/admin/craftsmen']);
  }

  if (!authService.hasToken()) {
    return router.createUrlTree(['/login']);
  }

  return authService.getRole() === 'Admin'
    ? true
    : router.createUrlTree(['/home']);
};