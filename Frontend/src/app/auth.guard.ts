import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './services/auth.service';

// Thêm async vào đây
export const authGuard: CanActivateFn = async (route, state) => { 
  const authService = inject(AuthService);
  const router = inject(Router);

  // Ép Guard CHỜ quá trình khởi tạo và giải mã token của OAuth hoàn tất
  await authService.initialLoadPromise;

  if (authService.hasValidToken) {
    return true; // Có Token -> Cho phép vào
  }
  
  // Chưa có Token -> Đẩy về trang Login
  return router.parseUrl('/login');
};