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
   * AuthService restores the persisted session
   * asynchronously when the application starts.
   *
   * Wait for it before checking the token.
   */
  await authService.initialLoadPromise;

  if (authService.hasValidToken) {
    return true;
  }

  return router.createUrlTree(
    ['/login'],
    {
      queryParams: {
        returnUrl: state.url
      }
    }
  );
};