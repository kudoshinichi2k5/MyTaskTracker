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

export interface ProjectItem {
  id: string;
  name: string;
  description: string | null;
  ownerUserId: string;

  // TaskService uses number IDs.
  taskIds: number[];

  createdAt: string;
  updatedAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class ProjectService {
  private readonly http =
    inject(HttpClient);

  private readonly apiUrl =
    `${environment.projectApi}/projects`;

  getProjects(): Observable<ProjectItem[]> {
    return this.http
      .get<ProjectItem[]>(this.apiUrl)
      .pipe(
        catchError(this.handleError)
      );
  }

  attachTask(
    projectId: string,
    taskId: number
  ): Observable<ProjectItem> {
    return this.http
      .post<ProjectItem>(
        `${this.apiUrl}/${projectId}/tasks/${taskId}`,
        {}
      )
      .pipe(
        catchError(this.handleError)
      );
  }

  detachTask(
    projectId: string,
    taskId: number
  ): Observable<ProjectItem> {
    return this.http
      .delete<ProjectItem>(
        `${this.apiUrl}/${projectId}/tasks/${taskId}`
      )
      .pipe(
        catchError(this.handleError)
      );
  }

  private handleError(
    error: HttpErrorResponse
  ) {
    return throwError(() => error);
  }
}