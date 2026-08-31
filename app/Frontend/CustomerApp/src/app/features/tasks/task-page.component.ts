import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnInit,
  inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import {
  TaskItem,
  TaskService
} from '../../services/task.service';

import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-task-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './task-page.component.html',
  styleUrl: './task-page.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TaskPageComponent implements OnInit {
  private readonly taskService = inject(TaskService);
  private readonly toast = inject(ToastService);
  private readonly cdr = inject(ChangeDetectorRef);

  tasks: TaskItem[] = [];

  isLoading = true;
  loadError = false;
  isAdding = false;

  newTaskTitle = '';

  editingTaskId: number | null = null;
  editingTitle = '';

  get completedCount(): number {
    return this.tasks.filter(task => task.isCompleted).length;
  }

  get pendingCount(): number {
    return this.tasks.length - this.completedCount;
  }

  get progressPercent(): number {
    if (!this.tasks.length) {
      return 0;
    }

    return Math.round(
      (this.completedCount / this.tasks.length) * 100
    );
  }

  ngOnInit(): void {
    this.loadTasks();
  }

  loadTasks(): void {
    this.isLoading = true;
    this.loadError = false;

    this.taskService.getTasks().subscribe({
      next: tasks => {
        this.tasks = tasks;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.loadError = true;
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  addTask(): void {
    const title = this.newTaskTitle.trim();

    if (!title || this.isAdding) {
      return;
    }

    this.isAdding = true;

    const request: TaskItem = {
      id: 0,
      title,
      isCompleted: false
    };

    this.taskService.addTask(request).subscribe({
      next: task => {
        this.tasks = [...this.tasks, task];
        this.newTaskTitle = '';
        this.isAdding = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.isAdding = false;
        this.toast.error(
          "Couldn't add that task. Please try again."
        );
        this.cdr.markForCheck();
      }
    });
  }

  toggleTask(task: TaskItem): void {
    const previousState = task.isCompleted;
    const nextState = !previousState;

    task.isCompleted = nextState;
    this.cdr.markForCheck();

    this.taskService
      .updateTask({
        ...task,
        isCompleted: nextState
      })
      .subscribe({
        error: () => {
          task.isCompleted = previousState;

          this.toast.error(
            "Couldn't update that task. Please try again."
          );

          this.cdr.markForCheck();
        }
      });
  }

  removeTask(task: TaskItem): void {
    const previousTasks = this.tasks;

    this.tasks = this.tasks.filter(
      item => item.id !== task.id
    );

    this.cdr.markForCheck();

    this.taskService.deleteTask(task.id).subscribe({
      error: () => {
        this.tasks = previousTasks;

        this.toast.error(
          "Couldn't delete that task. Please try again."
        );

        this.cdr.markForCheck();
      }
    });
  }

  startEdit(task: TaskItem): void {
    this.editingTaskId = task.id;
    this.editingTitle = task.title;
  }

  cancelEdit(): void {
    this.editingTaskId = null;
    this.editingTitle = '';
  }

  saveEdit(task: TaskItem): void {
    const title = this.editingTitle.trim();

    if (!title || title === task.title) {
      this.cancelEdit();
      return;
    }

    const previousTitle = task.title;

    task.title = title;
    this.editingTaskId = null;

    this.taskService.updateTask({
      ...task,
      title
    }).subscribe({
      error: () => {
        task.title = previousTitle;

        this.toast.error(
          "Couldn't rename that task. Please try again."
        );

        this.cdr.markForCheck();
      }
    });
  }

  trackByTaskId(_: number, task: TaskItem): number {
    return task.id;
  }
}