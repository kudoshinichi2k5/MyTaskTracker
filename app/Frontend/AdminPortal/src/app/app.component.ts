import { CommonModule } from '@angular/common';
import {
  Component,
  inject
} from '@angular/core';

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
  isReady = false;

  /*
   * Current browser URL.
   *
   * app.component.html uses this value to determine
   * which Admin module is currently active.
   */
  currentUrl = '';

  constructor() {

    /*
     * Restore the persisted authentication session first.
     */
    void this.authService.initialLoadPromise.then(() => {

      this.isAuthenticated =
        this.authService.hasValidToken;

      this.isReady = true;

      this.currentUrl =
        this.router.url;
    });

    /*
     * Keep currentUrl and authentication state synchronized
     * whenever Angular changes routes.
     */
    this.router.events
      .pipe(
        filter(
          (event) =>
            event instanceof NavigationEnd
        )
      )
      .subscribe((event) => {

        const navigation =
          event as NavigationEnd;

        this.currentUrl =
          navigation.urlAfterRedirects;

        this.isAuthenticated =
          this.authService.hasValidToken;
      });
  }

  logout(): void {
    this.authService.logout();
  }

  get username(): string {
    return this.authService.username;
  }
}
