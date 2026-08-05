import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReportService, UserTaskSummary } from '../../services/report.service';
import { UserAdminService, AdminUser } from '../../services/user-admin.service';
import { AuthService } from '../../services/auth.service';

type NavSection = 'reports' | 'users' | 'roles';

// The only role this screen lets an admin toggle from the Users table.
const ASSIGNABLE_ROLE = 'admin';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  activeSection: NavSection = 'reports';
  readonly assignableRole = ASSIGNABLE_ROLE;

  // Reports
  summary: UserTaskSummary[] = [];
  loadError = false;
  isLoading = true;

  // Users
  users: AdminUser[] = [];
  usersLoading = false;
  usersLoaded = false;
  usersError: string | null = null;
  pendingUserIds = new Set<string>();

  // Roles
  roles: string[] = [];
  rolesLoading = false;
  rolesLoaded = false;
  rolesError: string | null = null;

  reportService = inject(ReportService);
  userAdminService = inject(UserAdminService);
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

    if (section === 'users' && !this.usersLoaded) {
      this.loadUsers();
    }
    if (section === 'roles' && !this.rolesLoaded) {
      this.loadRoles();
    }
  }

  private loadUsers() {
    this.usersLoading = true;
    this.usersError = null;
    this.userAdminService.getUsers().subscribe({
      next: (data) => {
        this.users = data;
        this.usersLoaded = true;
        this.usersLoading = false;
      },
      error: () => {
        this.usersError =
          "Couldn't load users. This usually means the backend's Keycloak service-account credentials " +
          "(Keycloak:AdminClientId / Keycloak:AdminClientSecret) aren't configured yet.";
        this.usersLoading = false;
      }
    });
  }

  private loadRoles() {
    this.rolesLoading = true;
    this.rolesError = null;
    this.userAdminService.getRoles().subscribe({
      next: (data) => {
        this.roles = data;
        this.rolesLoaded = true;
        this.rolesLoading = false;
      },
      error: () => {
        this.rolesError = "Couldn't load realm roles.";
        this.rolesLoading = false;
      }
    });
  }

  hasAdminRole(user: AdminUser): boolean {
    return user.roles.includes(this.assignableRole);
  }

  toggleAdminRole(user: AdminUser) {
    if (this.pendingUserIds.has(user.id)) return;

    const hadRole = this.hasAdminRole(user);
    this.pendingUserIds.add(user.id);

    // Optimistic update, rolled back on failure.
    user.roles = hadRole
      ? user.roles.filter((r) => r !== this.assignableRole)
      : [...user.roles, this.assignableRole];

    const request$ = hadRole
      ? this.userAdminService.removeRole(user.id, this.assignableRole)
      : this.userAdminService.assignRole(user.id, this.assignableRole);

    request$.subscribe({
      next: () => this.pendingUserIds.delete(user.id),
      error: () => {
        user.roles = hadRole
          ? [...user.roles, this.assignableRole]
          : user.roles.filter((r) => r !== this.assignableRole);
        this.pendingUserIds.delete(user.id);
        this.usersError = `Couldn't update the role for ${user.username}. Please try again.`;
      }
    });
  }

  logout() {
    this.authService.logout();
  }

  completionRate(row: UserTaskSummary): number {
    if (row.totalTasks === 0) return 0;
    return Math.round((row.completedTasks / row.totalTasks) * 100);
  }
}