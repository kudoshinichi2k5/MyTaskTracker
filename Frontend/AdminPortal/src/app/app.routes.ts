import { Routes } from '@angular/router';
// import { LoginComponent } from './components/login/login.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { LoginComponent } from './features/login/login.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: 'login', component: LoginComponent },
  {
    path: 'forbidden',
    loadComponent: () =>
      import('./components/forbidden/forbidden.component').then((m) => m.ForbiddenComponent)
  },
  { path: 'dashboard', component: DashboardComponent, canActivate: [adminGuard] },
  { path: '**', redirectTo: 'dashboard' }
];