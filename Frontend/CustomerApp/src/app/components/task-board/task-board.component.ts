import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TaskService, TaskItem } from '../../services/task.service';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';
import { NotificationBellComponent } from '../notification-bell/notification-bell.component';
import { ProjectHubComponent } from '../project-hub/project-hub.component';

@Component({
  selector: 'app-task-board',
  standalone: true,
  imports: [CommonModule, FormsModule, NotificationBellComponent, ProjectHubComponent],
  templateUrl: './task-board.component.html',
  styleUrls: ['./task-board.component.css'], // ✅ corrected to plural
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TaskBoardComponent implements OnInit {
  tasks: TaskItem[] = [];
  loadError = false;
  isLoading = true;
  newTaskTitle = '';
  isAdding = false;
  editingTaskId: number | null = null;
  editingTitle = '';

  taskService = inject(TaskService);
  authService = inject(AuthService);
  private toast = inject(ToastService);
  private cdr = inject(ChangeDetectorRef);

  get completedCount(): number {
    return this.tasks.filter((t) => t.isCompleted).length;
  }

  get progressPercent(): number {
    if (this.tasks.length === 0) return 0;
    return Math.round((this.completedCount / this.tasks.length) * 100);
  }

  trackByTaskId(_: number, task: TaskItem) {
    return task.id;
  }

  ngOnInit() {
    this.taskService.getTasks().subscribe({
      next: (data) => {
        this.tasks = data;
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

  addTask() {
    const title = this.newTaskTitle.trim();
    if (!title || this.isAdding) return;

    this.isAdding = true;
    const newTask: TaskItem = { id: 0, title, isCompleted: false };
    this.taskService.addTask(newTask).subscribe({
      next: (task) => {
        this.tasks = [...this.tasks, task];
        this.newTaskTitle = '';
        this.isAdding = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.isAdding = false;
        this.toast.error("Couldn't add that task. Please try again.");
        this.cdr.markForCheck();
      }
    });
  }

  toggleTask(task: TaskItem) {
    const nextState = !task.isCompleted;
    task.isCompleted = nextState; // optimistic update
    this.taskService.updateTask({ ...task, isCompleted: nextState }).subscribe({
      error: () => {
        task.isCompleted = !nextState;
        this.toast.error("Couldn't update that task. Please try again.");
        this.cdr.markForCheck();
      }
    });
  }

  removeTask(task: TaskItem) {
    const previousTasks = this.tasks;
    this.tasks = this.tasks.filter((t) => t.id !== task.id);
    this.taskService.deleteTask(task.id).subscribe({
      error: () => {
        this.tasks = previousTasks;
        this.toast.error("Couldn't delete that task. Please try again.");
        this.cdr.markForCheck();
      }
    });
  }

  startEdit(task: TaskItem) {
    this.editingTaskId = task.id;
    this.editingTitle = task.title;
  }

  cancelEdit() {
    this.editingTaskId = null;
    this.editingTitle = '';
  }

  saveEdit(task: TaskItem) {
    const title = this.editingTitle.trim();

    // Nothing to send if it's unchanged or was cleared out - just close
    // the editor rather than round-tripping a no-op or an invalid request
    // (the backend rejects an empty title with a 400).
    if (!title || title === task.title) {
      this.cancelEdit();
      return;
    }

    const previousTitle = task.title;
    task.title = title; // optimistic update
    this.editingTaskId = null;

    this.taskService.updateTask({ ...task, title }).subscribe({
      error: () => {
        task.title = previousTitle;
        this.toast.error("Couldn't rename that task. Please try again.");
        this.cdr.markForCheck();
      }
    });
  }

  logout() {
    this.authService.logout();
  }
}
