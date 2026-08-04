import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TaskService, TaskItem } from '../../services/task.service';
import { AuthService } from '../../services/auth.service';
import { NotificationBellComponent } from '../notification-bell/notification-bell.component';

@Component({
  selector: 'app-task-board',
  standalone: true,
  imports: [CommonModule, FormsModule, NotificationBellComponent],
  templateUrl: './task-board.component.html',
  styleUrls: ['./task-board.component.css'] // ✅ corrected to plural
})
export class TaskBoardComponent implements OnInit {
  tasks: TaskItem[] = [];
  loadError = false;
  newTaskTitle = '';
  isAdding = false;

  taskService = inject(TaskService);
  authService = inject(AuthService);

  get completedCount(): number {
    return this.tasks.filter((t) => t.isCompleted).length;
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
      },
      error: () => (this.isAdding = false)
    });
  }

  toggleTask(task: TaskItem) {
    const nextState = !task.isCompleted;
    task.isCompleted = nextState; // optimistic update
    this.taskService.updateTask({ ...task, isCompleted: nextState }).subscribe({
      error: () => (task.isCompleted = !nextState)
    });
  }

  removeTask(task: TaskItem) {
    const previousTasks = this.tasks;
    this.tasks = this.tasks.filter((t) => t.id !== task.id);
    this.taskService.deleteTask(task.id).subscribe({
      error: () => (this.tasks = previousTasks)
    });
  }

  logout() {
    this.authService.logout();
  }
}
