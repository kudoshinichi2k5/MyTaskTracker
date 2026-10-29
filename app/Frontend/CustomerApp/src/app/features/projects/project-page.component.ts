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
  ProjectItem,
  ProjectService
} from '../../services/project.service';

import {
  TaskItem,
  TaskService
} from '../../services/task.service';

import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-project-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './project-page.component.html',
  styleUrl: './project-page.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectPageComponent implements OnInit {
  private readonly projectService = inject(ProjectService);
  private readonly taskService = inject(TaskService);
  private readonly authService = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly cdr = inject(ChangeDetectorRef);

  projects: ProjectItem[] = [];
  tasks: TaskItem[] = [];

  isLoading = true;
  loadError: string | null = null;
  taskLoadError: string | null = null;

  newProjectName = '';
  newProjectDescription = '';
  isCreating = false;

  editingProjectId: string | null = null;
  editingProjectName = '';
  editingProjectDescription = '';

  managingTasksForProjectId: string | null = null;
  selectedTaskToLinkId: number | null = null;
  isUpdatingTaskLinks = false;

  ngOnInit(): void {
    this.loadProjects();
  }

  get totalProjects(): number {
    return this.projects.length;
  }

  get totalLinkedTasks(): number {
    return this.projects.reduce(
      (total, project) => total + project.taskIds.length,
      0
    );
  }

  loadProjects(): void {
    this.isLoading = true;
    this.loadError = null;

    void this.authService.initialLoadPromise.then(() => {
      if (!this.authService.accessToken) {
        this.isLoading = false;
        this.loadError = 'Please sign in again.';
        this.cdr.markForCheck();
        return;
      }

      this.projectService.getProjects().subscribe({
        next: projects => {
          this.projects = projects;
          this.isLoading = false;
          this.loadTasks();
          this.cdr.markForCheck();
        },
        error: () => {
          this.isLoading = false;
          this.loadError =
            "Couldn't load projects. Please try again.";
          this.cdr.markForCheck();
        }
      });
    });
  }

  loadTasks(): void {
    this.taskLoadError = null;

    this.taskService.getTasks().subscribe({
      next: tasks => {
        this.tasks = tasks;
        this.cdr.markForCheck();
      },
      error: () => {
        this.tasks = [];
        this.taskLoadError = "Couldn't load tasks for project linking.";
        this.cdr.markForCheck();
      }
    });
  }

  createProject(): void {
    const name = this.newProjectName.trim();

    if (!name || this.isCreating) {
      return;
    }

    this.isCreating = true;

    this.projectService.createProject({
      name,
      description:
        this.newProjectDescription.trim() || null
    }).subscribe({
      next: project => {
        this.projects = [
          project,
          ...this.projects
        ];

        this.newProjectName = '';
        this.newProjectDescription = '';
        this.isCreating = false;

        this.cdr.markForCheck();
      },
      error: () => {
        this.isCreating = false;

        this.toast.error(
          "Couldn't create that project."
        );

        this.cdr.markForCheck();
      }
    });
  }

  startEdit(project: ProjectItem): void {
    this.editingProjectId = project.id;
    this.editingProjectName = project.name;
    this.editingProjectDescription =
      project.description ?? '';
  }

  cancelEdit(): void {
    this.editingProjectId = null;
    this.editingProjectName = '';
    this.editingProjectDescription = '';
  }

  saveProject(project: ProjectItem): void {
    const name = this.editingProjectName.trim();

    if (!name) {
      return;
    }

    this.projectService.updateProject(
      project.id,
      {
        name,
        description:
          this.editingProjectDescription.trim() || null
      }
    ).subscribe({
      next: updated => {
        this.projects = this.projects.map(item =>
          item.id === updated.id
            ? updated
            : item
        );

        this.cancelEdit();
        this.cdr.markForCheck();
      },
      error: () => {
        this.toast.error(
          "Couldn't update that project."
        );
      }
    });
  }

  deleteProject(project: ProjectItem): void {
    const confirmed = window.confirm(
      `Delete "${project.name}"?`
    );

    if (!confirmed) {
      return;
    }

    this.projectService
      .deleteProject(project.id)
      .subscribe({
        next: () => {
          this.projects = this.projects.filter(
            item => item.id !== project.id
          );

          this.cdr.markForCheck();
        },
        error: () => {
          this.toast.error(
            "Couldn't delete that project."
          );
        }
      });
  }

  toggleTaskManager(project: ProjectItem): void {
    if (this.managingTasksForProjectId === project.id) {
      this.managingTasksForProjectId = null;
      this.selectedTaskToLinkId = null;
      return;
    }

    this.managingTasksForProjectId = project.id;
    this.selectedTaskToLinkId = null;
  }

  getLinkedTasks(project: ProjectItem): TaskItem[] {
    const linkedTaskIds = new Set(project.taskIds);
    return this.tasks.filter(task => linkedTaskIds.has(task.id));
  }

  getAvailableTasks(project: ProjectItem): TaskItem[] {
    const linkedTaskIds = new Set(project.taskIds);
    return this.tasks.filter(task => !linkedTaskIds.has(task.id));
  }

  linkTask(project: ProjectItem): void {
    const taskId = this.selectedTaskToLinkId;

    if (taskId === null || this.isUpdatingTaskLinks) {
      return;
    }

    this.isUpdatingTaskLinks = true;

    this.projectService.attachTask(project.id, taskId).subscribe({
      next: updated => {
        this.replaceProject(updated);
        this.selectedTaskToLinkId = null;
        this.isUpdatingTaskLinks = false;
        this.toast.success('Task linked to project.');
        this.cdr.markForCheck();
      },
      error: () => {
        this.isUpdatingTaskLinks = false;
        this.toast.error("Couldn't link that task. Please try again.");
        this.cdr.markForCheck();
      }
    });
  }

  unlinkTask(project: ProjectItem, task: TaskItem): void {
    if (this.isUpdatingTaskLinks) {
      return;
    }

    this.isUpdatingTaskLinks = true;

    this.projectService.detachTask(project.id, task.id).subscribe({
      next: updated => {
        this.replaceProject(updated);
        this.isUpdatingTaskLinks = false;
        this.toast.success('Task removed from project.');
        this.cdr.markForCheck();
      },
      error: () => {
        this.isUpdatingTaskLinks = false;
        this.toast.error("Couldn't remove that task. Please try again.");
        this.cdr.markForCheck();
      }
    });
  }

  private replaceProject(updated: ProjectItem): void {
    this.projects = this.projects.map(project =>
      project.id === updated.id ? updated : project
    );
  }

  trackByProjectId(
    _: number,
    project: ProjectItem
  ): string {
    return project.id;
  }
}
