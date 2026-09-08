import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { DemoPreviewService } from './demo-preview.service';

export const homeGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const demoPreviewService = inject(DemoPreviewService);
  const router = inject(Router);

  // TEMPORARY DEMO MODE: keep the normal authenticated route behavior while allowing preview users to land on the public home page.
  if (demoPreviewService.isDemoModeEnabled()) {
    return true;
  }

  if (!authService.hasToken()) {
    return router.createUrlTree(['/login']);
  }

  switch (authService.getRole()) {
    case 'Admin':
      return true;
    case 'Craftsman':
      return router.createUrlTree(['/craftsman/crafts']);
    case 'Customer':
      return router.createUrlTree(['/customer/craftsmen']);
    default:
      authService.clearToken();
      return router.createUrlTree(['/login']);
  }
};