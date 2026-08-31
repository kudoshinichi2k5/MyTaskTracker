import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

  username = 'admin';
  password = '123456';
  errorMessage = '';
  isSubmitting = false;

  onLogin() {
    const username = this.username.trim();
    const password = this.password.trim();

    if (!username || !password) {
      this.errorMessage = 'Please enter username and password.';
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';

    this.authService.login(username, password).subscribe({
      next: () => this.router.navigateByUrl('/tasks'),
      error: () => {
        this.errorMessage = 'Invalid username or password.';
        this.isSubmitting = false;
      },
      complete: () => {
        this.isSubmitting = false;
      }
    });
  }
}