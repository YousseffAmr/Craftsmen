import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { DemoPreviewService } from './demo-preview.service';

export const craftsmanGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const demoPreviewService = inject(DemoPreviewService);
  const router = inject(Router);

  // TEMPORARY DEMO MODE: preview craftsmen routes without a token or real account.
  if (demoPreviewService.isDemoModeEnabled() && demoPreviewService.getRole() === 'Craftsman') {
    return demoPreviewService.isRoleAllowedForRoute(state.url, 'Craftsman') ? true : router.createUrlTree(['/craftsman/crafts']);
  }

  if (!authService.hasToken()) {
    return router.createUrlTree(['/login']);
  }

  return authService.getRole() === 'Craftsman'
    ? true
    : router.createUrlTree(['/home']);
};