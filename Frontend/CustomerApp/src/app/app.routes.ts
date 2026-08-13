import { Routes } from '@angular/router';

import { LoginComponent } from './features/login/login.component';
import { authGuard } from './auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    component: LoginComponent
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./layout/app-shell.component').then(
        (m) => m.AppShellComponent
      ),
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'tasks'
      },
      {
        path: 'tasks',
        loadComponent: () =>
          import('./features/tasks/task-page.component').then(
            (m) => m.TaskPageComponent
          )
      },
      {
        path: 'projects',
        loadComponent: () =>
          import('./features/projects/project-page.component').then(
            (m) => m.ProjectPageComponent
          )
      },
      {
        path: 'comments',
        loadComponent: () =>
          import('./features/comments/comment-page.component').then(
            (m) => m.CommentPageComponent
          )
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'tasks'
  }
];