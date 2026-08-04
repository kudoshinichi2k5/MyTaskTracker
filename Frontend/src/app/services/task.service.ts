import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
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

  getTasks(): Observable<TaskItem[]> {
    return this.http.get<TaskItem[]>(`${this.apiUrl}/tasks`).pipe(
      catchError((err: HttpErrorResponse) => {
        console.error('[TaskService] Failed to load tasks:', err.status, err.message);
        return throwError(() => err);
      })
    );
  }
}