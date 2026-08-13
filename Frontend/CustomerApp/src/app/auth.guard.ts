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
  const authService =
    inject(AuthService);

  const router =
    inject(Router);

  /*
   * On browser reload, AuthService must first:
   *
   * 1. Read customerapp_session
   * 2. Verify the access token
   * 3. Refresh it if necessary
   *
   * Do not check hasValidToken before
   * that process finishes.
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