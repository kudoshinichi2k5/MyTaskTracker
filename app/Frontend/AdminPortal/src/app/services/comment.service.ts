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

export interface CommentItem {
  id: string;

  // TaskService uses number IDs.
  taskId: number;

  authorUserId: string;

  body: string;

  createdAt: string;

  editedAt: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class CommentService {
  private readonly http =
    inject(HttpClient);

  private readonly apiUrl =
    environment.commentApi;

  /**
   * GET:
   * /api/v1/tasks/{taskId}/comments
   */
  getComments(
    taskId: number
  ): Observable<CommentItem[]> {
    return this.http
      .get<CommentItem[]>(
        `${this.apiUrl}/tasks/${taskId}/comments`
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