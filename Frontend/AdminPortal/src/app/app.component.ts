import {
  Component,
  OnInit,
  inject
} from '@angular/core';

import {
  CommonModule
} from '@angular/common';

import {
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet,
  NavigationEnd
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
export class AppComponent implements OnInit {
  private readonly authService =
    inject(AuthService);

  private readonly router =
    inject(Router);

  isAuthenticated = false;
  isReady = false;

  currentUrl = '';

  ngOnInit(): void {
    this.currentUrl =
      this.router.url;

    this.router.events
      .pipe(
        filter(
          event =>
            event instanceof NavigationEnd
        )
      )
      .subscribe(
        (event: NavigationEnd) => {
          this.currentUrl =
            event.urlAfterRedirects;
        }
      );

    void this.initialize();
  }

  private async initialize(): Promise<void> {
    await this.authService.initialLoadPromise;

    this.isAuthenticated =
      this.authService.hasValidToken;

    this.isReady = true;
  }

  get username(): string {
    return this.authService.username;
  }

  logout(): void {
    this.authService.logout();
  }

  isLoginPage(): boolean {
    return this.currentUrl.startsWith(
      '/login'
    );
  }
}