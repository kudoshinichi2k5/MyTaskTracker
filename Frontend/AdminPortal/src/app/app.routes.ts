import { Routes } from '@angular/router';

import { DashboardComponent } from './components/dashboard/dashboard.component';
import { LoginComponent } from './features/login/login.component';

import { authGuard } from './auth.guard';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'dashboard'
  },

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
    path: 'dashboard',
    component: DashboardComponent,
    canActivate: [authGuard]
  },

  {
    path: 'projects',
    canActivate: [authGuard],
    loadComponent: () =>
      import(
        './features/projects/project-list/project-list.component'
      ).then(m => m.ProjectListComponent)
  },

  {
    path: 'comments',
    canActivate: [authGuard],
    loadComponent: () =>
      import(
        './features/comments/comment-list/comment-list.component'
      ).then(m => m.CommentListComponent)
  },

  {
    path: 'users',
    canActivate: [authGuard],
    loadComponent: () =>
      import(
        './features/users/user-management.component'
      ).then(m => m.UserManagementComponent)
  },

  {
    path: 'roles',
    canActivate: [authGuard],
    loadComponent: () =>
      import(
        './features/roles/role-management.component'
      ).then(m => m.RoleManagementComponent)
  },

  {
    path: '**',
    redirectTo: 'dashboard'
  }
];