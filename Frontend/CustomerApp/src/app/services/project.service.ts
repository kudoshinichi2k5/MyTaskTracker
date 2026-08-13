import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface ProjectItem {
  id: string;
  name: string;
  description: string | null;
  ownerUserId: string;
  taskIds: string[];
  createdAt: string;
  updatedAt: string;
}

export interface ProjectRequest {
  name: string;
  description: string | null;
}

@Injectable({ providedIn: 'root' })
export class ProjectService {
  private http = inject(HttpClient);
  private readonly apiUrl = `${environment.projectApi}/projects`;

  getProjects(): Observable<ProjectItem[]> {
    return this.http.get<ProjectItem[]>(this.apiUrl).pipe(catchError(this.handleError));
  }

  createProject(request: ProjectRequest): Observable<ProjectItem> {
    return this.http.post<ProjectItem>(this.apiUrl, request).pipe(catchError(this.handleError));
  }

  updateProject(id: string, request: ProjectRequest): Observable<ProjectItem> {
    return this.http.put<ProjectItem>(`${this.apiUrl}/${id}`, request).pipe(catchError(this.handleError));
  }

  deleteProject(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`).pipe(catchError(this.handleError));
  }

  attachTask(projectId: string, taskId: string): Observable<ProjectItem> {
    return this.http.post<ProjectItem>(`${this.apiUrl}/${projectId}/tasks/${taskId}`, {}).pipe(catchError(this.handleError));
  }

  detachTask(projectId: string, taskId: string): Observable<ProjectItem> {
    return this.http.delete<ProjectItem>(`${this.apiUrl}/${projectId}/tasks/${taskId}`).pipe(catchError(this.handleError));
  }

  private handleError(error: HttpErrorResponse) {
    return throwError(() => error);
  }
}
