import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TaskService, TaskItem } from '../../services/task.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-task-board',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './task-board.component.html',
  styleUrl: './task-board.component.css'
})
export class TaskBoardComponent implements OnInit {
  tasks: TaskItem[] = [];
  loadError = false;
  taskService = inject(TaskService);
  authService = inject(AuthService);

  get completedCount(): number {
    return this.tasks.filter(t => t.isCompleted).length;
  }

  get progressPercent(): number {
    if (this.tasks.length === 0) return 0;
    return Math.round((this.completedCount / this.tasks.length) * 100);
  }

  ngOnInit() {
    this.taskService.getTasks().subscribe({
      next: (data) => (this.tasks = data),
      error: () => (this.loadError = true)
    });
  }

  logout() {
    this.authService.logout();
  }
}