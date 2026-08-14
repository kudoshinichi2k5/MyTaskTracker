import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';

import {
  ProjectItem,
  ProjectService
} from '../../services/project.service';

import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-project-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './project-list.component.html',
  styleUrl: './project-list.component.css'
})
export class ProjectListComponent implements OnInit {
  private readonly projectService = inject(ProjectService);
  private readonly authService = inject(AuthService);

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
        'Please sign in again to load projects.';
      return;
    }

    this.isLoading = true;
    this.error = null;

    this.projectService.getProjects().subscribe({
      next: (projects: ProjectItem[]) => {
        this.projects = projects;
        this.isLoading = false;
      },

      error: () => {
        this.error = 'Unable to load projects.';
        this.isLoading = false;
      }
    });
  }

  getTaskCount(project: ProjectItem): number {
    return project.taskIds?.length ?? 0;
  }
}