import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const homeGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

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