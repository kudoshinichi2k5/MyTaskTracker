import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div style="padding: 20px; font-family: sans-serif;">
      <h2>Đăng nhập Hệ thống Tracking</h2>
      <input [(ngModel)]="username" placeholder="Username (admin)" style="margin-bottom: 10px;"/><br/>
      <input type="password" [(ngModel)]="password" placeholder="Password (123456)" style="margin-bottom: 10px;"/><br/>
      <button (click)="onLogin()">Đăng nhập</button>
    </div>
  `
})
export class LoginComponent {
  username = '';
  password = '';
  authService = inject(AuthService);
  router = inject(Router);

  onLogin() {
    this.authService.login(this.username, this.password).subscribe({
      next: () => {
        alert('Đăng nhập thành công!');
        this.router.navigate(['/tasks']);
      },
      error: () => alert('Sai tài khoản hoặc mật khẩu!')
    });
  }
}