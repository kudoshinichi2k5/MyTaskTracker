import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Nếu có token hợp lệ (trong localStorage) -> Cho phép vào
  if (authService.hasValidToken) {
    // Tùy chọn: Bạn có thể check thêm Role ở đây nếu cần
    // const requiredRole = route.data['role'];
    return true;
  }

  // Nếu chưa đăng nhập -> Đá về trang Login cục bộ của app
  return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url }});
};