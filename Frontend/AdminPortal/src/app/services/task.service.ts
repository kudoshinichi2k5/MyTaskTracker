import { Injectable, inject } from '@angular/core';

import {
  HttpClient,
  HttpErrorResponse
} from '@angular/common/http';

import {
  Observable,
  throwError
} from 'rxjs';

import {
  catchError
} from 'rxjs/operators';

import { environment } from '../../environments/environment';

export interface TaskItem {
  id: number;

  title: string;

  description?: string | null;

  completed: boolean;

  userId?: string | null;

  createdAt?: string;

  updatedAt?: string;
}

@Injectable({
  providedIn: 'root'
})
export class TaskService {
  private readonly http =
    inject(HttpClient);

  private readonly apiUrl =
    `${environment.taskApi}/tasks`;

  getTasks(): Observable<TaskItem[]> {
    return this.http
      .get<TaskItem[]>(
        this.apiUrl
      )
      .pipe(
        catchError(this.handleError)
      );
  }

  getTask(
    taskId: number
  ): Observable<TaskItem> {
    return this.http
      .get<TaskItem>(
        `${this.apiUrl}/${taskId}`
      )
      .pipe(
        catchError(this.handleError)
      );
  }

  private handleError(
    error: HttpErrorResponse
  ): Observable<never> {
    return throwError(
      () => error
    );
  }
}