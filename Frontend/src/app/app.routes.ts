import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login.component';
import { TaskBoardComponent } from './components/task-board/task-board.component'; // Import thêm

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'tasks', component: TaskBoardComponent },
  { path: '', redirectTo: 'login', pathMatch: 'full' }
];