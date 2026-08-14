import {
  CommonModule
} from '@angular/common';

import {
  Component,
  OnInit,
  inject
} from '@angular/core';

import {
  ActivatedRoute
} from '@angular/router';

import {
  FormsModule
} from '@angular/forms';

import {
  CommentItem,
  CommentService
} from '../../services/comment.service';

import {
  ProjectItem,
  ProjectService
} from '../../services/project.service';

@Component({
  selector: 'app-comment-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl:
    './comment-list.component.html',
  styleUrl:
    './comment-list.component.css'
})
export class CommentListComponent
  implements OnInit {

  private readonly route =
    inject(ActivatedRoute);

  private readonly projectService =
    inject(ProjectService);

  private readonly commentService =
    inject(CommentService);

  projects: ProjectItem[] = [];

  comments: CommentItem[] = [];

  selectedProjectId = '';
  selectedTaskId = '';

  isLoadingProjects = false;
  isLoadingComments = false;

  projectsError: string | null = null;
  commentsError: string | null = null;

  ngOnInit(): void {
    void this.loadProjects();

    this.route.queryParams.subscribe(params => {
      const taskId = params['taskId'];

      if (taskId) {
        this.selectedTaskId = taskId;
        void this.loadComments();
      }
    });
  }

  async loadProjects(): Promise<void> {
    this.isLoadingProjects = true;
    this.projectsError = null;

    this.projectService.getProjects().subscribe({
      next: projects => {
        this.projects = projects;
        this.isLoadingProjects = false;

        this.selectProjectFromTask();
      },

      error: () => {
        this.projectsError =
          'Unable to load projects.';
        this.isLoadingProjects = false;
      }
    });
  }

  selectProject(projectId: string): void {
    this.selectedProjectId = projectId;
    this.selectedTaskId = '';
    this.comments = [];
    this.commentsError = null;
  }

  selectTask(taskId: string): void {
    this.selectedTaskId = taskId;

    if (taskId) {
      void this.loadComments();
    }
  }

  async loadComments(): Promise<void> {
    const taskId =
      this.selectedTaskId.trim();

    if (!taskId) {
      this.comments = [];
      return;
    }

    this.isLoadingComments = true;
    this.commentsError = null;

    this.commentService
      .getComments(taskId)
      .subscribe({
        next: comments => {
          this.comments = comments;
          this.isLoadingComments = false;
        },

        error: () => {
          this.commentsError =
            'Unable to load comments.';
          this.comments = [];
          this.isLoadingComments = false;
        }
      });
  }

  get selectedProject(): ProjectItem | null {
    return (
      this.projects.find(
        project =>
          project.id ===
          this.selectedProjectId
      ) ?? null
    );
  }

  get selectedTaskIds(): string[] {
    return (
      this.selectedProject?.taskIds ?? []
    );
  }

  private selectProjectFromTask(): void {
    if (!this.selectedTaskId) {
      return;
    }

    const project =
      this.projects.find(
        item =>
          item.taskIds.includes(
            this.selectedTaskId
          )
      );

    if (project) {
      this.selectedProjectId =
        project.id;
    }
  }
}