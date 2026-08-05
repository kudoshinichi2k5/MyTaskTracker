import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReportService, UserTaskSummary } from '../../services/report.service';
import { AuthService } from '../../services/auth.service';

type NavSection = 'reports' | 'users' | 'roles';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  activeSection: NavSection = 'reports';
  summary: UserTaskSummary[] = [];
  loadError = false;
  isLoading = true;

  reportService = inject(ReportService);
  authService = inject(AuthService);

  get totalTasks(): number {
    return this.summary.reduce((sum, row) => sum + row.totalTasks, 0);
  }

  get totalCompleted(): number {
    return this.summary.reduce((sum, row) => sum + row.completedTasks, 0);
  }

  ngOnInit() {
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

  setSection(section: NavSection) {
    this.activeSection = section;
  }

  logout() {
    this.authService.logout();
  }

  completionRate(row: UserTaskSummary): number {
    if (row.totalTasks === 0) return 0;
    return Math.round((row.completedTasks / row.totalTasks) * 100);
  }
}