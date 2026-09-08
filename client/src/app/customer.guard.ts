import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { DemoPreviewService } from './demo-preview.service';

export const customerGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const demoPreviewService = inject(DemoPreviewService);
  const router = inject(Router);

  // TEMPORARY DEMO MODE: preview customer routes without a token or real account.
  if (demoPreviewService.isDemoModeEnabled() && demoPreviewService.getRole() === 'Customer') {
    return demoPreviewService.isRoleAllowedForRoute(state.url, 'Customer') ? true : router.createUrlTree(['/customer/craftsmen']);
  }

  if (!authService.hasToken()) {
    return router.createUrlTree(['/login']);
  }

  return authService.getRole() === 'Customer'
    ? true
    : router.createUrlTree(['/home']);
};