import {
  CommonModule
} from '@angular/common';

import {
  Component,
  OnInit,
  inject
} from '@angular/core';

import {
  ActivatedRoute,
  Router
} from '@angular/router';

import {
  TaskItem,
  TaskService
} from '../../services/task.service';

import {
  ProjectItem,
  ProjectService
} from '../../services/project.service';

@Component({
  selector: 'app-task-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './task-list.component.html',
  styleUrl: './task-list.component.css'
})
export class TaskListComponent
  implements OnInit {

  private readonly taskService =
    inject(TaskService);

  private readonly projectService =
    inject(ProjectService);

  private readonly router =
    inject(Router);

  private readonly route =
    inject(ActivatedRoute);

  tasks: TaskItem[] = [];

  projects: ProjectItem[] = [];

  selectedProjectId: string | null = null;

  isLoading = false;

  isLoadingProjects = false;

  error: string | null = null;

  ngOnInit(): void {
    this.route.queryParamMap.subscribe(
      params => {
        this.selectedProjectId =
          params.get('projectId');

        void this.loadTasks();
      }
    );

    void this.loadProjects();
  }

  async loadProjects(): Promise<void> {
    this.isLoadingProjects = true;

    this.projectService
      .getProjects()
      .subscribe({
        next: projects => {
          this.projects = projects;
          this.isLoadingProjects = false;
        },

        error: () => {
          this.projects = [];
          this.isLoadingProjects = false;
        }
      });
  }

  async loadTasks(): Promise<void> {
    this.isLoading = true;
    this.error = null;

    this.taskService
      .getTasks()
      .subscribe({
        next: tasks => {
          this.tasks =
            this.filterTasks(tasks);

          this.isLoading = false;
        },

        error: () => {
          this.error =
            'Unable to load tasks.';
          this.isLoading = false;
        }
      });
  }

  private filterTasks(
    tasks: TaskItem[]
  ): TaskItem[] {
    if (!this.selectedProjectId) {
      return tasks;
    }

    const project =
      this.projects.find(
        item =>
          item.id ===
          this.selectedProjectId
      );

    if (!project) {
      return tasks;
    }

    const taskIds =
      new Set(project.taskIds);

    return tasks.filter(task =>
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

  get completedCount(): number {
    return this.tasks.filter(
      task => task.completed
    ).length;
  }

  get pendingCount(): number {
    return this.tasks.filter(
      task => !task.completed
    ).length;
  }

  openComments(task: TaskItem): void {
    void this.router.navigate(
      ['/comments'],
      {
        queryParams: {
          projectId:
            this.selectedProjectId,
          taskId: task.id
        }
      }
    );
  }

  changeProject(
    projectId: string
  ): void {
    void this.router.navigate(
      ['/tasks'],
      {
        queryParams: projectId
          ? { projectId }
          : {}
      }
    );
  }

  viewProject(project: ProjectItem): void {
    void this.router.navigate(
      ['/tasks'],
      {
        queryParams: {
          projectId: project.id
        }
      }
    );
  }
}