import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnInit,
  inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import {
  CommentItem,
  CommentService
} from '../../services/comment.service';

import {
  ProjectItem,
  ProjectService
} from '../../services/project.service';

import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-comment-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './comment-page.component.html',
  styleUrl: './comment-page.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CommentPageComponent implements OnInit {
  private readonly commentService = inject(CommentService);
  private readonly projectService = inject(ProjectService);
  private readonly authService = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly cdr = inject(ChangeDetectorRef);

  projects: ProjectItem[] = [];
  comments: CommentItem[] = [];

  selectedTaskId = '';

  isLoadingProjects = true;
  isLoadingComments = false;

  projectsError: string | null = null;
  commentsError: string | null = null;

  newCommentBody = '';

  editingCommentId: string | null = null;
  editingCommentBody = '';

  ngOnInit(): void {
    this.loadProjects();
  }

  get taskChoices(): string[] {
    return Array.from(
      new Set(
        this.projects.flatMap(
          project => project.taskIds
        )
      )
    );
  }

  loadProjects(): void {
    this.isLoadingProjects = true;
    this.projectsError = null;

    void this.authService.initialLoadPromise.then(() => {
      if (!this.authService.accessToken) {
        this.projectsError =
          'Please sign in again.';
        this.isLoadingProjects = false;
        this.cdr.markForCheck();
        return;
      }

      this.projectService.getProjects().subscribe({
        next: projects => {
          this.projects = projects;
          this.isLoadingProjects = false;

          if (
            !this.selectedTaskId &&
            this.taskChoices.length > 0
          ) {
            this.selectedTaskId =
              this.taskChoices[0];

            this.loadComments();
          }

          this.cdr.markForCheck();
        },
        error: () => {
          this.projectsError =
            "Couldn't load available tasks.";
          this.isLoadingProjects = false;
          this.cdr.markForCheck();
        }
      });
    });
  }

  selectTask(taskId: string): void {
    this.selectedTaskId = taskId;
    this.loadComments();
  }

  loadComments(): void {
    if (!this.selectedTaskId) {
      this.comments = [];
      return;
    }

    this.isLoadingComments = true;
    this.commentsError = null;

    this.commentService
      .getComments(this.selectedTaskId)
      .subscribe({
        next: comments => {
          this.comments = comments;
          this.isLoadingComments = false;
          this.cdr.markForCheck();
        },
        error: () => {
          this.commentsError =
            "Couldn't load comments for this task.";
          this.isLoadingComments = false;
          this.cdr.markForCheck();
        }
      });
  }

  addComment(): void {
    const body = this.newCommentBody.trim();

    if (!body || !this.selectedTaskId) {
      return;
    }

    this.commentService
      .createComment(
        this.selectedTaskId,
        { body }
      )
      .subscribe({
        next: comment => {
          this.comments = [
            comment,
            ...this.comments
          ];

          this.newCommentBody = '';
          this.cdr.markForCheck();
        },
        error: () => {
          this.toast.error(
            "Couldn't add the comment."
          );
        }
      });
  }

  startEdit(comment: CommentItem): void {
    this.editingCommentId = comment.id;
    this.editingCommentBody = comment.body;
  }

  cancelEdit(): void {
    this.editingCommentId = null;
    this.editingCommentBody = '';
  }

  saveComment(comment: CommentItem): void {
    const body = this.editingCommentBody.trim();

    if (!body) {
      return;
    }

    this.commentService
      .updateComment(
        comment.id,
        { body }
      )
      .subscribe({
        next: updated => {
          this.comments = this.comments.map(
            item =>
              item.id === updated.id
                ? updated
                : item
          );

          this.cancelEdit();
          this.cdr.markForCheck();
        },
        error: () => {
          this.toast.error(
            "Couldn't update the comment."
          );
        }
      });
  }

  deleteComment(comment: CommentItem): void {
    const confirmed = window.confirm(
      'Delete this comment?'
    );

    if (!confirmed) {
      return;
    }

    this.commentService
      .deleteComment(comment.id)
      .subscribe({
        next: () => {
          this.comments =
            this.comments.filter(
              item => item.id !== comment.id
            );

          this.cdr.markForCheck();
        },
        error: () => {
          this.toast.error(
            "Couldn't delete the comment."
          );
        }
      });
  }

  trackByCommentId(
    _: number,
    comment: CommentItem
  ): string {
    return comment.id;
  }
}