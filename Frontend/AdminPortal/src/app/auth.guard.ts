import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './services/auth.service';

export const authGuard: CanActivateFn = async () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  await authService.initialLoadPromise;

  if (authService.hasValidToken) {
    return true;
  }

  return router.parseUrl('/login');
};

// Stricter than authGuard: requires a valid token AND the "admin" realm
// role. A signed-in Customer App-only user (no admin role) who somehow
// lands here is sent to /forbidden instead of the dashboard.
export const adminGuard: CanActivateFn = async () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  await authService.initialLoadPromise;

  if (!authService.hasValidToken) {
    return router.parseUrl('/login');
  }

  if (!authService.isAdmin) {
    return router.parseUrl('/forbidden');
  }

  return true;
};