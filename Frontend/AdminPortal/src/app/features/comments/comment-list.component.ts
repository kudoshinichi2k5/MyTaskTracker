import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
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

@Component({
  selector: 'app-comment-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './comment-list.component.html',
  styleUrl: './comment-list.component.css'
})
export class CommentListComponent implements OnInit {
  private readonly commentService = inject(CommentService);
  private readonly projectService = inject(ProjectService);
  private readonly authService = inject(AuthService);

  projects: ProjectItem[] = [];
  comments: CommentItem[] = [];

  selectedTaskId = '';
  loadedTaskId: string | null = null;

  isLoadingProjects = false;
  isLoadingComments = false;

  projectsError: string | null = null;
  commentsError: string | null = null;

  ngOnInit(): void {
    void this.loadProjects();
  }

  async loadProjects(): Promise<void> {
    await this.authService.initialLoadPromise;

    if (!this.authService.accessToken) {
      this.projectsError = 'Please sign in again.';
      return;
    }

    this.isLoadingProjects = true;
    this.projectsError = null;

    this.projectService.getProjects().subscribe({
      next: (projects: ProjectItem[]) => {
        this.projects = projects;
        this.isLoadingProjects = false;
      },

      error: () => {
        this.projectsError =
          'Unable to load projects for task selection.';
        this.isLoadingProjects = false;
      }
    });
  }

  selectTask(taskId: string): void {
    this.selectedTaskId = taskId;
    void this.loadComments();
  }

  async loadComments(): Promise<void> {
    await this.authService.initialLoadPromise;

    const taskId = this.selectedTaskId.trim();

    if (!taskId || !this.isValidGuid(taskId)) {
      this.commentsError = 'Please select a valid task.';
      this.comments = [];
      this.loadedTaskId = null;
      return;
    }

    this.isLoadingComments = true;
    this.commentsError = null;

    this.commentService.getComments(taskId).subscribe({
      next: (comments: CommentItem[]) => {
        this.comments = comments;
        this.loadedTaskId = taskId;
        this.isLoadingComments = false;
      },

      error: () => {
        this.commentsError =
          'Unable to load comments for this task.';
        this.comments = [];
        this.loadedTaskId = null;
        this.isLoadingComments = false;
      }
    });
  }

  get taskChoices(): string[] {
    const ids = this.projects.flatMap(
      (project: ProjectItem) => project.taskIds ?? []
    );

    return Array.from(new Set(ids));
  }

  private isValidGuid(value: string): boolean {
    return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(
      value
    );
  }
}