import {
  CommonModule
} from '@angular/common';

import {
  forkJoin
} from 'rxjs';

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

import {
  TaskItem,
  TaskService
} from '../../services/task.service';

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

  private readonly taskService =
    inject(TaskService);

  private readonly projectService =
    inject(ProjectService);

  private readonly commentService =
    inject(CommentService);

  projects: ProjectItem[] = [];

  tasks: TaskItem[] = [];

  comments: CommentItem[] = [];

  selectedProjectId = '';

  selectedTaskId: number | null = null;

  loadedTaskId: number | null = null;

  isLoading = false;

  isLoadingComments = false;

  error: string | null = null;

  commentsError: string | null = null;

  ngOnInit(): void {
    this.route.queryParamMap.subscribe(
      params => {
        const projectId =
          params.get('projectId');

        const taskId =
          params.get('taskId');

        this.selectedProjectId =
          projectId ?? '';

        this.selectedTaskId =
          taskId
            ? Number(taskId)
            : null;

        void this.loadData();
      }
    );
  }

  async loadData(): Promise<void> {
    this.isLoading = true;
    this.error = null;

    forkJoin({
      projects:
        this.projectService.getProjects(),

      tasks:
        this.taskService.getTasks()
    }).subscribe({
      next: ({ projects, tasks }) => {
        this.projects = projects;
        this.tasks = tasks;

        this.isLoading = false;

        if (
          this.selectedTaskId !== null
        ) {
          void this.loadComments();
        }
      },

      error: (error) => {
        console.error(
          'Failed to load comment context.',
          error
        );

        this.error =
          'Unable to load projects or tasks.';

        this.isLoading = false;
      }
    });
  }

  changeProject(
    projectId: string
  ): void {
    this.selectedProjectId =
      projectId;

    this.selectedTaskId = null;

    this.comments = [];
    this.loadedTaskId = null;
  }

  selectTask(
    taskId: number
  ): void {
    this.selectedTaskId =
      taskId;

    void this.loadComments();
  }

  async loadComments(): Promise<void> {
    const taskId =
      this.selectedTaskId;

    if (taskId === null) {
      this.comments = [];
      this.loadedTaskId = null;
      return;
    }

    this.isLoadingComments = true;
    this.commentsError = null;

    this.commentService
      .getComments(taskId)
      .subscribe({
        next: comments => {
          this.comments = comments;
          this.loadedTaskId = taskId;
          this.isLoadingComments = false;
        },

        error: () => {
          this.commentsError =
            'Unable to load comments.';
          this.comments = [];
          this.loadedTaskId = null;
          this.isLoadingComments = false;
        }
      });
  }

  get availableTasks(): TaskItem[] {
    if (!this.selectedProjectId) {
      return this.tasks;
    }

    const project =
      this.projects.find(
        item =>
          item.id ===
          this.selectedProjectId
      );

    if (!project) {
      return [];
    }

    const taskIds =
      new Set(project.taskIds);

    return this.tasks.filter(task =>
      taskIds.has(task.id)
    );
  }

  get selectedProject():
    ProjectItem | null {

    if (!this.selectedProjectId) {
      return null;
    }

    return (
      this.projects.find(
        project =>
          project.id ===
          this.selectedProjectId
      ) ?? null
    );
  }

  get selectedTask():
    TaskItem | null {

    if (this.selectedTaskId === null) {
      return null;
    }

    return (
      this.tasks.find(
        task =>
          task.id ===
          this.selectedTaskId
      ) ?? null
    );
  }
}