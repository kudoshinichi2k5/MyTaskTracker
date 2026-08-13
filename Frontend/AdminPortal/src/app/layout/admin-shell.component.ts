import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import {
  RouterLink,
  RouterLinkActive,
  RouterOutlet
} from '@angular/router';

import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-admin-shell',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive
  ],
  templateUrl: './admin-shell.component.html',
  styleUrl: './admin-shell.component.css'
})
export class AdminShellComponent {
  private readonly authService = inject(AuthService);

  readonly navigation = [
    {
      label: 'Overview',
      path: '/overview',
      icon: '⌂'
    },
    {
      label: 'Users',
      path: '/users',
      icon: '●'
    },
    {
      label: 'Roles',
      path: '/roles',
      icon: '◆'
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
    const raw =
      localStorage.getItem('adminportal_session');

    if (!raw) {
      return 'Admin';
    }

    try {
      return JSON.parse(raw).username || 'Admin';
    } catch {
      return 'Admin';
    }
  }

  logout(): void {
    this.authService.logout();
  }
}