import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TaskService, TaskItem } from '../../services/task.service';

@Component({
  selector: 'app-task-board',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div style="padding: 20px; font-family: sans-serif;">
      <h2>📋 Danh sách Công việc của tôi</h2>
      <ul style="list-style-type: none; padding-left: 0;">
        <li *ngFor="let task of tasks" style="margin-bottom: 10px;">
          <label>
            <input type="checkbox" [checked]="task.isCompleted" disabled />
            <span [style.text-decoration]="task.isCompleted ? 'line-through' : 'none'">
              {{ task.title }}
            </span>
          </label>
        </li>
      </ul>
      <button (click)="logout()">Đăng xuất</button>
    </div>
  `
})
export class TaskBoardComponent implements OnInit {
  tasks: TaskItem[] = [];
  taskService = inject(TaskService);

  ngOnInit() {
    this.taskService.getTasks().subscribe({
      next: (data) => this.tasks = data,
      error: (err) => alert('Lỗi lấy dữ liệu: Không có quyền truy cập (Missing Token)')
    });
  }

  logout() {
    localStorage.removeItem('jwt_token');
    window.location.href = '/login'; // Reset lại toàn bộ app
  }
}