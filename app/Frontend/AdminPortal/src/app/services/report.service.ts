import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface UserTaskSummary {
  userId: string;
  totalTasks: number;
  completedTasks: number;
}

@Injectable({ providedIn: 'root' })
export class ReportService {
  private http = inject(HttpClient);
  private apiUrl = environment.taskApi;

  getTaskSummary(): Observable<UserTaskSummary[]> {
    return this.http.get<UserTaskSummary[]>(`${this.apiUrl}/tasks/admin/summary`).pipe(
      catchError((err: HttpErrorResponse) => {
        console.error('[ReportService] Failed to load task summary:', err.status, err.message);
        return throwError(() => err);
      })
    );
  }
}