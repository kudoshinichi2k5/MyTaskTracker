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
      import(
        './components/forbidden/forbidden.component'
      ).then(
        m => m.ForbiddenComponent
      )
  },

  /*
   * =========================
   * Dashboard
   * =========================
   */

  {
    path: 'dashboard',
    component: DashboardComponent,
    canActivate: [authGuard]
  },

  /*
   * =========================
   * Workspace
   * =========================
   */

  {
    path: 'projects',
    canActivate: [authGuard],
    loadComponent: () =>
      import(
        './features/projects/project-list.component'
      ).then(
        m => m.ProjectListComponent
      )
  },

  {
  path: 'tasks',
    canActivate: [authGuard],
    loadComponent: () =>
      import(
        './features/tasks/task-list.component'
      ).then(
        m => m.TaskListComponent
      )
  },

  {
    path: 'comments',
    canActivate: [authGuard],
    loadComponent: () =>
      import(
        './features/comments/comment-list.component'
      ).then(
        m => m.CommentListComponent
      )
  },

  /*
   * =========================
   * Administration
   * =========================
   */

  {
    path: 'users',
    canActivate: [authGuard],
    loadComponent: () =>
      import(
        './features/users/user-management.component'
      ).then(
        m => m.UserManagementComponent
      )
  },

  {
    path: 'roles',
    canActivate: [authGuard],
    loadComponent: () =>
      import(
        './features/roles/role-management.component'
      ).then(
        m => m.RoleManagementComponent
      )
  },

  /*
   * =========================
   * Fallback
   * =========================
   */

  {
    path: '**',
    redirectTo: 'dashboard'
  }
];