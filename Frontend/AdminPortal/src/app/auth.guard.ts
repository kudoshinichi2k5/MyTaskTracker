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
   * Wait for AuthService to restore and
   * validate adminportal_session.
   *
   * Without this, F5 can execute the guard
   * before the /verify request finishes.
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

  /*
   * Only administrators can access
   * the AdminPortal.
   */
  if (!authService.hasRole('admin')) {
    return router.parseUrl(
      '/forbidden'
    );
  }

  return true;
};