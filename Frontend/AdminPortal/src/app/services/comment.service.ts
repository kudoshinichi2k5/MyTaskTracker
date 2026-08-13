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

@Injectable({ providedIn: 'root' })
export class CommentService {
  private http = inject(HttpClient);
  private readonly apiUrl = environment.commentApi;

  getComments(taskId: string): Observable<CommentItem[]> {
    return this.http.get<CommentItem[]>(`${this.apiUrl}/tasks/${taskId}/comments`).pipe(catchError(this.handleError));
  }

  private handleError(error: HttpErrorResponse) {
    return throwError(() => error);
  }
}
