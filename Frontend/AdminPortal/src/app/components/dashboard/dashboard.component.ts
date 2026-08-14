import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';

import {
  ReportService,
  UserTaskSummary
} from '../../services/report.service';

import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  private readonly reportService = inject(ReportService);
  private readonly authService = inject(AuthService);

  summary: UserTaskSummary[] = [];

  isLoading = true;
  loadError = false;

  ngOnInit(): void {
    void this.loadReport();
  }

  async loadReport(): Promise<void> {
    await this.authService.initialLoadPromise;

    if (!this.authService.accessToken) {
      this.loadError = true;
      this.isLoading = false;
      return;
    }

    this.reportService.getTaskSummary().subscribe({
      next: (data) => {
        this.summary = data;
        this.isLoading = false;
      },
      error: () => {
        this.loadError = true;
        this.isLoading = false;
      }
    });
  }

  get totalTasks(): number {
    return this.summary.reduce(
      (sum, row) => sum + row.totalTasks,
      0
    );
  }

  get totalCompleted(): number {
    return this.summary.reduce(
      (sum, row) => sum + row.completedTasks,
      0
    );
  }

  get totalPending(): number {
    return this.totalTasks - this.totalCompleted;
  }

  get completionRate(): number {
    if (this.totalTasks === 0) {
      return 0;
    }

    return Math.round(
      (this.totalCompleted / this.totalTasks) * 100
    );
  }

  get memberCount(): number {
    return this.summary.length;
  }

  get averageTasksPerMember(): number {
    if (this.memberCount === 0) {
      return 0;
    }

    return Math.round(
      this.totalTasks / this.memberCount
    );
  }

  get highestWorkload(): UserTaskSummary | null {
    if (this.summary.length === 0) {
      return null;
    }

    return this.summary.reduce(
      (highest, current) =>
        current.totalTasks > highest.totalTasks
          ? current
          : highest
    );
  }

  get highestCompletion(): UserTaskSummary | null {
    const membersWithTasks =
      this.summary.filter(
        row => row.totalTasks > 0
      );

    if (membersWithTasks.length === 0) {
      return null;
    }

    return membersWithTasks.reduce(
      (best, current) => {

        const bestRate =
          best.completedTasks /
          best.totalTasks;

        const currentRate =
          current.completedTasks /
          current.totalTasks;

        return currentRate > bestRate
          ? current
          : best;
      }
    );
  }
}