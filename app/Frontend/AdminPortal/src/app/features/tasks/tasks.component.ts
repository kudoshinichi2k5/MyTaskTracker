import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TaskService } from '../../services/task.service';
import { TaskItem } from '../../services/task.service';

@Component({
  selector: 'app-tasks',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './tasks.component.html',
  styleUrls: ['./task-list.component.css'],
})
export class TaskListComponent {
  tasks: TaskItem[] = [];
  completedCount = 0;
  pendingCount = 0;
  isLoading = false;
  error: string | null = null;

  constructor(private taskService: TaskService) {}

  loadTasks(): void {
    this.isLoading = true;
    this.error = null;
    this.taskService.getTasks().subscribe({
      next: (data) => {
        this.tasks = data;
        this.completedCount = this.tasks.filter((t) => t.isCompleted).length;
        this.pendingCount = this.tasks.filter((t) => !t.isCompleted).length;
        this.isLoading = false;
      },
      error: (err) => {
        this.error = err instanceof Error ? err.message : 'Failed to load tasks';
        this.isLoading = false;
      },
    });
  }

  openComments(task: TaskItem): void {
    console.warn('Open comments for task', task.id);
  }
}
