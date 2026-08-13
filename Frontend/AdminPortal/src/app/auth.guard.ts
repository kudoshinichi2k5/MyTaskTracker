import { inject } from '@angular/core';
import {
  CanActivateFn,
  Router
} from '@angular/router';

import { AuthService } from './services/auth.service';

export const authGuard: CanActivateFn = async (
  route,
  state
) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  /*
   * Important:
   * AuthService restores and verifies the persisted
   * session asynchronously.
   *
   * Wait for that process before checking the token.
   * Otherwise a browser reload can temporarily see
   * hasValidToken === false and redirect to /login.
   */
  await authService.initialLoadPromise;

  if (!authService.hasValidToken) {
    return router.createUrlTree(
      ['/login'],
      {
        queryParams: {
          returnUrl: state.url
        }
      }
    );
  }

  if (!authService.hasRole('admin')) {
    return router.parseUrl('/forbidden');
  }

  return true;
};