import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

  username = '';
  email = '';
  password = '';
  confirmPassword = '';
  errorMessage = '';
  isSubmitting = false;
  submitAttempted = false;

  onRegister(form: NgForm): void {
    this.submitAttempted = true;

    const username = this.username.trim();
    const email = this.email.trim();
    const password = this.password;

    if (form.invalid) {
      this.errorMessage = 'Please fix the highlighted fields.';
      return;
    }

    if (password !== this.confirmPassword) {
      this.errorMessage = 'Passwords do not match.';
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';

    this.authService.register(username, email, password).subscribe({
      next: () => this.router.navigateByUrl('/tasks'),
      error: () => {
        this.errorMessage =
          this.authService.lastError ?? 'Could not create your account.';
        this.isSubmitting = false;
      },
      complete: () => {
        this.isSubmitting = false;
      }
    });
  }
}