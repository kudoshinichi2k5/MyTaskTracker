import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnInit,
  inject
} from '@angular/core';
import { CommonModule } from '@angular/common';

import {
  ReportService,
  UserTaskSummary
} from '../../services/report.service';

@Component({
  selector: 'app-overview-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './overview-page.component.html',
  styleUrl: './overview-page.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OverviewPageComponent implements OnInit {
  private readonly reportService = inject(ReportService);
  private readonly cdr = inject(ChangeDetectorRef);

  summary: UserTaskSummary[] = [];

  isLoading = true;
  loadError = false;

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
    if (!this.totalTasks) {
      return 0;
    }

    return Math.round(
      (this.totalCompleted / this.totalTasks) * 100
    );
  }

  ngOnInit(): void {
    this.loadReport();
  }

  loadReport(): void {
    this.isLoading = true;
    this.loadError = false;

    this.reportService.getTaskSummary().subscribe({
      next: data => {
        this.summary = data;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.isLoading = false;
        this.loadError = true;
        this.cdr.markForCheck();
      }
    });
  }

  rowCompletion(row: UserTaskSummary): number {
    if (!row.totalTasks) {
      return 0;
    }

    return Math.round(
      (row.completedTasks / row.totalTasks) * 100
    );
  }
}