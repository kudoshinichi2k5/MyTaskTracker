import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import {
  NavigationEnd,
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet
} from '@angular/router';

import { filter } from 'rxjs';

import { AuthService } from './services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive
  ],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  private readonly router = inject(Router);
  readonly authService = inject(AuthService);

  isAuthenticated = false;

  constructor() {
    void this.authService.initialLoadPromise.then(() => {
      this.isAuthenticated =
        !!this.authService.accessToken;
    });

    this.router.events
      .pipe(
        filter(
          (event) => event instanceof NavigationEnd
        )
      )
      .subscribe(() => {
        this.isAuthenticated =
          !!this.authService.accessToken;
      });
  }

  logout(): void {
    this.authService.logout();
  }
}