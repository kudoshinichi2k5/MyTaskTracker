import {
  CommonModule
} from '@angular/common';

import {
  Component,
  OnInit,
  inject
} from '@angular/core';

import {
  Router
} from '@angular/router';

import {
  TaskItem,
  TaskService
} from '../../services/task.service';

@Component({
  selector: 'app-task-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './task-list.component.html',
  styleUrl: './task-list.component.css'
})
export class TaskListComponent implements OnInit {
  private readonly taskService =
    inject(TaskService);

  private readonly router =
    inject(Router);

  tasks: TaskItem[] = [];

  isLoading = false;
  error: string | null = null;

  ngOnInit(): void {
    void this.loadTasks();
  }

  async loadTasks(): Promise<void> {
    this.isLoading = true;
    this.error = null;

    this.taskService.getTasks().subscribe({
      next: (tasks: TaskItem[]) => {
        this.tasks = tasks;
        this.isLoading = false;
      },

      error: () => {
        this.error =
          'Unable to load tasks.';
        this.isLoading = false;
      }
    });
  }

  openComments(task: TaskItem): void {
    void this.router.navigate(
      ['/comments'],
      {
        queryParams: {
          taskId: task.id
        }
      }
    );
  }

  get completedCount(): number {
    return this.tasks.filter(
      task => task.completed
    ).length;
  }

  get pendingCount(): number {
    return this.tasks.filter(
      task => !task.completed
    ).length;
  }
}