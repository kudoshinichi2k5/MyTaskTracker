import {
  CommonModule
} from '@angular/common';

import {
  HttpErrorResponse
} from '@angular/common/http';

import { forkJoin } from 'rxjs';

import {
  Component,
  OnInit,
  inject
} from '@angular/core';

import {
  RouterLink
} from '@angular/router';

import {
  ProjectItem,
  ProjectService
} from '../../services/project.service';

import {
  AuthService
} from '../../services/auth.service';

@Component({
  selector: 'app-project-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink
  ],
  templateUrl:
    './project-list.component.html',
  styleUrl:
    './project-list.component.css'
})
export class ProjectListComponent
  implements OnInit {

  private readonly projectService =
    inject(ProjectService);

  private readonly authService =
    inject(AuthService);

  projects: ProjectItem[] = [];

  isLoading = false;

  error: string | null = null;

  ngOnInit(): void {
    void this.loadProjects();
  }

  async loadProjects(): Promise<void> {
    await this.authService.initialLoadPromise;

    if (!this.authService.accessToken) {
      this.error =
        'Please sign in again.';
      return;
    }

    this.isLoading = true;
    this.error = null;

    this.projectService
      .getProjects()
      .subscribe({
        next: (
          projects: ProjectItem[]
        ) => {
          this.projects = projects;
          this.isLoading = false;
        },

        error: (error: HttpErrorResponse) => {
          console.error(
            'Failed to load projects.',
            error
          );

          if (error.status === 401) {
            this.error =
              'Your session has expired. Please sign in again.';
          } else if (error.status === 403) {
            this.error =
              'You do not have permission to view projects.';
          } else if (error.status === 404) {
            this.error =
              'Project API endpoint was not found.';
          } else {
            this.error =
              'Unable to load projects.';
          }

          this.isLoading = false;
        }
      });
  }

  getTaskCount(
    project: ProjectItem
  ): number {
    return project.taskIds?.length ?? 0;
  }
}