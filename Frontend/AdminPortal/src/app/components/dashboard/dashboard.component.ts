import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReportService, UserTaskSummary } from '../../services/report.service';
import { UserAdminService, AdminUser } from '../../services/user-admin.service';
import { ProjectService, ProjectItem } from '../../services/project.service';
import { CommentService, CommentItem } from '../../services/comment.service';
import { AuthService } from '../../services/auth.service';

type NavSection = 'reports' | 'users' | 'roles' | 'projects' | 'comments';

const ASSIGNABLE_ROLE = 'admin';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  activeSection: NavSection = 'reports';
  readonly assignableRole = ASSIGNABLE_ROLE;

  summary: UserTaskSummary[] = [];
  loadError = false;
  isLoading = true;

  users: AdminUser[] = [];
  usersLoading = false;
  usersLoaded = false;
  usersError: string | null = null;
  pendingUserIds = new Set<string>();

  roles: string[] = [];
  rolesLoading = false;
  rolesLoaded = false;
  rolesError: string | null = null;

  projects: ProjectItem[] = [];
  projectsLoading = false;
  projectsLoaded = false;
  projectsError: string | null = null;

  commentTaskId = '';
  comments: CommentItem[] = [];
  commentsLoading = false;
  commentsLoaded = false;
  commentsError: string | null = null;
  loadedCommentTaskId: string | null = null;

  reportService = inject(ReportService);
  userAdminService = inject(UserAdminService);
  projectService = inject(ProjectService);
  commentService = inject(CommentService);
  authService = inject(AuthService);

  get totalTasks(): number {
    return this.summary.reduce((sum, row) => sum + row.totalTasks, 0);
  }

  get totalCompleted(): number {
    return this.summary.reduce((sum, row) => sum + row.completedTasks, 0);
  }

  get projectTaskLinks(): number {
    return this.projects.reduce((sum, project) => sum + project.taskIds.length, 0);
  }

  ngOnInit() {
    void this.authService.initialLoadPromise.finally(() => {
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
    if (section === 'projects' && !this.projectsLoaded) {
      this.loadProjects();
    }
    if (section === 'comments' && !this.commentsLoaded && this.loadedCommentTaskId) {
      this.loadComments();
    }
  }

  private async loadUsers() {
    await this.authService.initialLoadPromise;
    if (!this.authService.accessToken) {
      this.usersError = 'Please sign in again to load users.';
      this.usersLoading = false;
      return;
    }
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
          "Couldn't load users. Please check the AuthService admin endpoints and try again.";
        this.usersLoading = false;
      }
    });
  }

  private async loadRoles() {
    await this.authService.initialLoadPromise;
    if (!this.authService.accessToken) {
      this.rolesError = 'Please sign in again to load roles.';
      this.rolesLoading = false;
      return;
    }
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

  async loadProjects() {
    await this.authService.initialLoadPromise;
    if (!this.authService.accessToken) {
      this.projectsError = 'Please sign in again to load projects.';
      this.projectsLoading = false;
      return;
    }
    this.projectsLoading = true;
    this.projectsError = null;
    this.projectService.getProjects().subscribe({
      next: (data) => {
        this.projects = data;
        this.projectsLoaded = true;
        this.projectsLoading = false;
      },
      error: () => {
        this.projectsError = "Couldn't load projects.";
        this.projectsLoading = false;
      }
    });
  }

  async loadComments() {
    await this.authService.initialLoadPromise;
    if (!this.authService.accessToken) {
      this.commentsError = 'Please sign in again to load comments.';
      this.commentsLoading = false;
      return;
    }
    const taskId = this.commentTaskId.trim();
    if (!taskId || !this.isValidGuid(taskId)) {
      this.commentsError = 'Enter a valid task GUID first.';
      this.comments = [];
      this.loadedCommentTaskId = null;
      this.commentsLoaded = false;
      this.commentsLoading = false;
      return;
    }

    this.commentsLoading = true;
    this.commentsError = null;
    this.loadedCommentTaskId = taskId;
    this.commentService.getComments(taskId).subscribe({
      next: (data) => {
        this.comments = data;
        this.commentsLoaded = true;
        this.commentsLoading = false;
      },
      error: () => {
        this.commentsError = "Couldn't load comments for that task.";
        this.commentsLoading = false;
      }
    });
  }

  get taskChoices(): string[] {
    const taskIds = this.projects.flatMap((project) => project.taskIds);
    return Array.from(new Set(taskIds));
  }

  selectTask(taskId: string) {
    this.commentTaskId = taskId;
    this.loadComments();
  }

  hasAdminRole(user: AdminUser): boolean {
    return user.roles.includes(this.assignableRole);
  }

  toggleAdminRole(user: AdminUser) {
    if (this.pendingUserIds.has(user.id)) return;

    const hadRole = this.hasAdminRole(user);
    this.pendingUserIds.add(user.id);

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

  private isValidGuid(value: string): boolean {
    return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value);
  }
}
