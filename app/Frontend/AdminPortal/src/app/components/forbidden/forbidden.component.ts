import { Component, inject } from '@angular/core';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-forbidden',
  standalone: true,
  templateUrl: './forbidden.component.html',
  styleUrl: './forbidden.component.css'
})
export class ForbiddenComponent {
  authService = inject(AuthService);

  logout() {
    this.authService.logout();
  }
}