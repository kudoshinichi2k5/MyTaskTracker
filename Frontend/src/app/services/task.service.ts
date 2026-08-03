import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface TaskItem {
  id: number;
  title: string;
  isCompleted: boolean;
}

@Injectable({ providedIn: 'root' })
export class TaskService {
  private http = inject(HttpClient);
  private apiUrl = environment.taskApi;

  getTasks() {
    return this.http.get<TaskItem[]>(`${this.apiUrl}/tasks`);
  }
}