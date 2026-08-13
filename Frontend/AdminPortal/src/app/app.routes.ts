import { Routes } from '@angular/router';

import { LoginComponent } from './features/login/login.component';
import { authGuard } from './auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    component: LoginComponent
  },

  {
    path: 'forbidden',
    loadComponent: () =>
      import('./components/forbidden/forbidden.component')
        .then(m => m.ForbiddenComponent)
  },

  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./layout/admin-shell.component')
        .then(m => m.AdminShellComponent),

    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'overview'
      },

      {
        path: 'overview',
        loadComponent: () =>
          import('./features/overview/overview-page.component')
            .then(m => m.OverviewPageComponent)
      },

      {
        path: 'users',
        loadComponent: () =>
          import('./features/users/user-management.component')
            .then(m => m.UserManagementComponent)
      },

      {
        path: 'roles',
        loadComponent: () =>
          import('./features/roles/role-management.component')
            .then(m => m.RoleManagementComponent)
      },

      {
        path: 'projects',
        loadComponent: () =>
          import('./features/projects/project-management.component')
            .then(m => m.ProjectManagementComponent)
      },

      {
        path: 'comments',
        loadComponent: () =>
          import('./features/comments/comment-management.component')
            .then(m => m.CommentManagementComponent)
      }
    ]
  },

  {
    path: '**',
    redirectTo: 'overview'
  }
];