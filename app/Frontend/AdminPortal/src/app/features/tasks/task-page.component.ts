/* Task Page Component - Modern UI with Consistent Design System */
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface Task {
  id: number;
  title: string;
  description: string;
  priority: 'High' | 'Medium' | 'Low';
  status: 'To Do' | 'In Progress' | 'Completed';
  assignee: string;
  createdAt: Date;
}

@Component({
  selector: 'app-task-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './task-page.component.html',
  styleUrls: ['./task-page.component.css']
})
export class TaskPageComponent implements OnInit {
  tasks: Task[] = [];

  ngOnInit(): void {
    // Fetch tasks - same pattern as TaskService
    // this.taskService.getTasks().subscribe({
    //   next: (tasks) => this.tasks = tasks,
    //   error: (err) => console.error('Failed to load tasks', err)
    // });
  }
}