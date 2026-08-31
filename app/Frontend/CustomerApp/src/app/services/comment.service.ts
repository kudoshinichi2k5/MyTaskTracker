import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface CommentItem {
  id: string;
  taskId: string;
  authorUserId: string;
  body: string;
  createdAt: string;
  editedAt: string | null;
}

export interface CommentRequest {
  body: string;
}

@Injectable({ providedIn: 'root' })
export class CommentService {
  private http = inject(HttpClient);
  private readonly apiUrl = environment.commentApi;

  getComments(taskId: string): Observable<CommentItem[]> {
    return this.http.get<CommentItem[]>(`${this.apiUrl}/tasks/${taskId}/comments`).pipe(catchError(this.handleError));
  }

  createComment(taskId: string, request: CommentRequest): Observable<CommentItem> {
    return this.http.post<CommentItem>(`${this.apiUrl}/tasks/${taskId}/comments`, request).pipe(catchError(this.handleError));
  }

  updateComment(commentId: string, request: CommentRequest): Observable<CommentItem> {
    return this.http.put<CommentItem>(`${this.apiUrl}/comments/${commentId}`, request).pipe(catchError(this.handleError));
  }

  deleteComment(commentId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/comments/${commentId}`).pipe(catchError(this.handleError));
  }

  private handleError(error: HttpErrorResponse) {
    return throwError(() => error);
  }
}
