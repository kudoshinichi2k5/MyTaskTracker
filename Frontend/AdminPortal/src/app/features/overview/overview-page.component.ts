import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnInit,
  inject
} from '@angular/core';

import {
  CommonModule
} from '@angular/common';

import {
  RouterLink
} from '@angular/router';

import {
  ReportService,
  UserTaskSummary
} from '../../services/report.service';

import {
  ProjectItem,
  ProjectService
} from '../../services/project.service';

@Component({
  selector: 'app-overview-page',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink
  ],
  templateUrl:
    './overview-page.component.html',
  styleUrl:
    './overview-page.component.css',
  changeDetection:
    ChangeDetectionStrategy.OnPush
})
export class OverviewPageComponent
  implements OnInit {

  private readonly reportService =
    inject(ReportService);

  private readonly projectService =
    inject(ProjectService);

  private readonly cdr =
    inject(ChangeDetectorRef);

  summary: UserTaskSummary[] = [];

  projects: ProjectItem[] = [];

  isLoading = true;
  loadError = false;

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.isLoading = true;
    this.loadError = false;

    this.reportService
      .getTaskSummary()
      .subscribe({
        next: summary => {
          this.summary = summary;

          this.projectService
            .getProjects()
            .subscribe({
              next: projects => {
                this.projects = projects;
                this.isLoading = false;
                this.cdr.markForCheck();
              },

              error: () => {
                this.projects = [];
                this.isLoading = false;
                this.cdr.markForCheck();
              }
            });
        },

        error: () => {
          this.isLoading = false;
          this.loadError = true;
          this.cdr.markForCheck();
        }
      });
  }

  get totalTasks(): number {
    return this.summary.reduce(
      (sum, row) =>
        sum + row.totalTasks,
      0
    );
  }

  get totalCompleted(): number {
    return this.summary.reduce(
      (sum, row) =>
        sum + row.completedTasks,
      0
    );
  }

  get totalPending(): number {
    return (
      this.totalTasks -
      this.totalCompleted
    );
  }

  get completionRate(): number {
    if (!this.totalTasks) {
      return 0;
    }

    return Math.round(
      (this.totalCompleted /
        this.totalTasks) *
        100
    );
  }

  get totalProjects(): number {
    return this.projects.length;
  }

  get totalProjectTasks(): number {
    return this.projects.reduce(
      (sum, project) =>
        sum +
        (project.taskIds?.length ?? 0),
      0
    );
  }
}