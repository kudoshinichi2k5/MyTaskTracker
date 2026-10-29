import { Component, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  standalone: true,
  template: '<p class="p-6">Completing secure sign-in…</p>'
})
export class OAuthCallbackComponent {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private authService = inject(AuthService);

  constructor() {
    const query = this.route.snapshot.queryParamMap;
    const code = query.get('code');
    const state = query.get('state');
    if (!code || !state || query.get('error')) {
      void this.router.navigate(['/login']);
      return;
    }

    this.authService.completeAuthorization(code, state).subscribe({
      next: (returnUrl) => void this.router.navigateByUrl(returnUrl),
      error: () => void this.router.navigate(['/login'])
    });
  }
}
