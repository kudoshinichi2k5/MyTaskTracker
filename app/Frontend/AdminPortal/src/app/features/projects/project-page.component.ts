/* Project Page Component - Modern UI with Consistent Design System */
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface Project {
  id: number;
  name: string;
  description: string;
  owner: string;
  members: number;
  status: 'Active' | 'On Hold' | 'Archived';
  initials: string;
}

@Component({
  selector: 'app-project-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './project-page.component.html',
  styleUrls: ['./project-page.component.css']
})
export class ProjectPageComponent implements OnInit {
  projects: Project[] = [];

  ngOnInit(): void {
    // Fetch projects - same pattern as ProjectService
    // this.projectService.getProjects().subscribe({
    //   next: (projects) => this.projects = projects,
    //   error: (err) => console.error('Failed to load projects', err)
    // });
  }
}