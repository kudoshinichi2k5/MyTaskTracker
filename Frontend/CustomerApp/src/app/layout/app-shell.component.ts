import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthService } from '../services/auth.service';
import { NotificationBellComponent } from '../components/notification-bell/notification-bell.component';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    NotificationBellComponent
  ],
  templateUrl: './app-shell.component.html',
  styleUrl: './app-shell.component.css'
})
export class AppShellComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly navigation = [
    {
      label: 'My Tasks',
      path: '/tasks',
      icon: '✓'
    },
    {
      label: 'Projects',
      path: '/projects',
      icon: '▦'
    },
    {
      label: 'Comments',
      path: '/comments',
      icon: '◌'
    }
  ];

  get username(): string {
    const session = localStorage.getItem('customerapp_session');

    if (!session) {
      return 'User';
    }

    try {
      const parsed = JSON.parse(session);
      return parsed.username || 'User';
    } catch {
      return 'User';
    }
  }

  logout(): void {
    this.authService.logout();
  }

  navigateTo(path: string): void {
    void this.router.navigate([path]);
  }
}