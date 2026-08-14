import {
  Component,
  OnInit
} from '@angular/core';

import {
  Router,
  RouterOutlet
} from '@angular/router';

import { CommonModule } from '@angular/common';

import { AuthService } from './services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,

  imports: [
    CommonModule,
    RouterOutlet
  ],

  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {

  title = 'AdminPortal';

  isReady = false;

  isAuthenticated = false;

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  async ngOnInit(): Promise<void> {

    /*
     * Wait for persisted authentication to be restored.
     */
    await this.authService.initialLoadPromise;

    this.isAuthenticated =
      this.authService.hasValidToken;

    this.isReady = true;

    /*
     * Do not redirect here.
     *
     * authGuard is responsible for authorization.
     */
  }

  get username(): string {
    return this.authService.username;
  }

  logout(): void {
    this.authService.logout();
  }
}