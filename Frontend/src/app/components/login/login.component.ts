import { Component, inject } from '@angular/core';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  template: `
    <div style="padding: 20px; font-family: sans-serif; text-align: center;">
      <h2>Chào mừng đến với Task Tracker</h2>
      <p>Hệ thống hiện đã được bảo mật bằng OAuth2</p>
      <button (click)="onLogin()" style="padding: 10px 20px; font-size: 16px;">
        Đăng nhập qua Hệ thống Tập trung (SSO)
      </button>
    </div>
  `
})
export class LoginComponent {
  authService = inject(AuthService);

  onLogin() {
    this.authService.login();
  }
}