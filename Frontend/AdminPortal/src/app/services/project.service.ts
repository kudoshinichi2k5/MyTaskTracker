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

  // TaskService uses numeric task IDs.
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

  /**
   * environment.projectApi:
   * http://localhost:5004/api/v1
   *
   * ProjectService endpoints:
   * /api/v1/projects
   */
  private readonly apiUrl =
    `${environment.projectApi}/projects`;

  getProjects(): Observable<ProjectItem[]> {
    return this.http
      .get<ProjectItem[]>(
        this.apiUrl
      )
      .pipe(
        catchError(this.handleError)
      );
  }

  getProject(
    projectId: string
  ): Observable<ProjectItem> {
    return this.http
      .get<ProjectItem>(
        `${this.apiUrl}/${projectId}`
      )
      .pipe(
        catchError(this.handleError)
      );
  }

  createProject(
    name: string,
    description?: string | null
  ): Observable<ProjectItem> {
    return this.http
      .post<ProjectItem>(
        this.apiUrl,
        {
          name,
          description:
            description ?? null
        }
      )
      .pipe(
        catchError(this.handleError)
      );
  }

  updateProject(
    projectId: string,
    name: string,
    description?: string | null
  ): Observable<ProjectItem> {
    return this.http
      .put<ProjectItem>(
        `${this.apiUrl}/${projectId}`,
        {
          name,
          description:
            description ?? null
        }
      )
      .pipe(
        catchError(this.handleError)
      );
  }

  deleteProject(
    projectId: string
  ): Observable<void> {
    return this.http
      .delete<void>(
        `${this.apiUrl}/${projectId}`
      )
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
  ): Observable<never> {
    return throwError(
      () => error
    );
  }
}