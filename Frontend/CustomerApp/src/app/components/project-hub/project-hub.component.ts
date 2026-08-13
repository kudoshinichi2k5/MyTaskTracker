import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CommentItem, CommentService } from '../../services/comment.service';
import { ProjectItem, ProjectService } from '../../services/project.service';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-project-hub',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './project-hub.component.html',
  styleUrl: './project-hub.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectHubComponent implements OnInit {
  projects: ProjectItem[] = [];
  projectsLoading = true;
  projectsError: string | null = null;
  newProjectName = '';
  newProjectDescription = '';
  editingProjectId: string | null = null;
  editingProjectName = '';
  editingProjectDescription = '';

  commentTaskId = '';
  comments: CommentItem[] = [];
  commentsLoading = false;
  commentsError: string | null = null;
  newCommentBody = '';
  editingCommentId: string | null = null;
  editingCommentBody = '';
  loadedCommentTaskId: string | null = null;

  private readonly projectService = inject(ProjectService);
  private readonly commentService = inject(CommentService);
  private readonly toast = inject(ToastService);
  private readonly cdr = inject(ChangeDetectorRef);

  get projectCount(): number {
    return this.projects.length;
  }

  get attachedTaskCount(): number {
    return this.projects.reduce((sum, project) => sum + project.taskIds.length, 0);
  }

  get visibleCommentCount(): number {
    return this.loadedCommentTaskId ? this.comments.length : 0;
  }

  ngOnInit(): void {
    this.loadProjects();
  }

  trackByProjectId(_: number, project: ProjectItem) {
    return project.id;
  }

  trackByCommentId(_: number, comment: CommentItem) {
    return comment.id;
  }

  private isValidGuid(value: string): boolean {
    return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value);
  }

  loadProjects() {
    this.projectsLoading = true;
    this.projectsError = null;
    this.projectService.getProjects().subscribe({
      next: (projects) => {
        this.projects = projects;
        this.projectsLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.projectsError = "Couldn't load projects. Please try again.";
        this.projectsLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  createProject() {
    const name = this.newProjectName.trim();
    const description = this.newProjectDescription.trim() || null;
    if (!name) return;

    this.projectService.createProject({ name, description }).subscribe({
      next: (project) => {
        this.projects = [project, ...this.projects];
        this.newProjectName = '';
        this.newProjectDescription = '';
        this.cdr.markForCheck();
      },
      error: () => this.toast.error("Couldn't create that project.")
    });
  }

  beginEditProject(project: ProjectItem) {
    this.editingProjectId = project.id;
    this.editingProjectName = project.name;
    this.editingProjectDescription = project.description ?? '';
  }

  cancelEditProject() {
    this.editingProjectId = null;
    this.editingProjectName = '';
    this.editingProjectDescription = '';
  }

  saveProject(project: ProjectItem) {
    const name = this.editingProjectName.trim();
    if (!name) return;

    const request = { name, description: this.editingProjectDescription.trim() || null };
    this.projectService.updateProject(project.id, request).subscribe({
      next: (updated) => {
        this.projects = this.projects.map((item) => (item.id === updated.id ? updated : item));
        this.cancelEditProject();
        this.cdr.markForCheck();
      },
      error: () => this.toast.error("Couldn't update that project.")
    });
  }

  deleteProject(project: ProjectItem) {
    this.projectService.deleteProject(project.id).subscribe({
      next: () => {
        this.projects = this.projects.filter((item) => item.id !== project.id);
        this.cdr.markForCheck();
      },
      error: () => this.toast.error("Couldn't delete that project.")
    });
  }

  loadComments() {
    const taskId = this.commentTaskId.trim();
    if (!taskId || !this.isValidGuid(taskId)) {
      this.commentsError = 'Enter a valid task GUID first.';
      this.comments = [];
      this.loadedCommentTaskId = null;
      this.commentsLoading = false;
      this.cdr.markForCheck();
      return;
    }

    this.commentsLoading = true;
    this.commentsError = null;
    this.loadedCommentTaskId = taskId;
    this.commentService.getComments(taskId).subscribe({
      next: (comments) => {
        this.comments = comments;
        this.commentsLoading = false;
        this.newCommentBody = '';
        this.cdr.markForCheck();
      },
      error: () => {
        this.commentsError = "Couldn't load comments for that task.";
        this.commentsLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  addComment() {
    const taskId = this.loadedCommentTaskId;
    const body = this.newCommentBody.trim();
    if (!taskId || !body) return;

    this.commentService.createComment(taskId, { body }).subscribe({
      next: (comment) => {
        this.comments = [comment, ...this.comments];
        this.newCommentBody = '';
        this.cdr.markForCheck();
      },
      error: () => this.toast.error("Couldn't add that comment.")
    });
  }

  beginEditComment(comment: CommentItem) {
    this.editingCommentId = comment.id;
    this.editingCommentBody = comment.body;
  }

  cancelEditComment() {
    this.editingCommentId = null;
    this.editingCommentBody = '';
  }

  saveComment(comment: CommentItem) {
    const body = this.editingCommentBody.trim();
    if (!body) return;

    this.commentService.updateComment(comment.id, { body }).subscribe({
      next: (updated) => {
        this.comments = this.comments.map((item) => (item.id === updated.id ? updated : item));
        this.cancelEditComment();
        this.cdr.markForCheck();
      },
      error: () => this.toast.error("Couldn't update that comment.")
    });
  }

  deleteComment(comment: CommentItem) {
    this.commentService.deleteComment(comment.id).subscribe({
      next: () => {
        this.comments = this.comments.filter((item) => item.id !== comment.id);
        this.cdr.markForCheck();
      },
      error: () => this.toast.error("Couldn't delete that comment.")
    });
  }
}
