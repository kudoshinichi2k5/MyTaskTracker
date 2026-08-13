import { Routes } from '@angular/router';
import { LoginComponent } from './features/login/login.component';
import { TaskBoardComponent } from './components/task-board/task-board.component';
import { authGuard } from './auth.guard'; // Import Guard

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { 
    path: 'tasks', 
    component: TaskBoardComponent,
    canActivate: [authGuard] // Gắn "chốt chặn" vào đây
  }, 
  { path: '', redirectTo: 'login', pathMatch: 'full' }
];