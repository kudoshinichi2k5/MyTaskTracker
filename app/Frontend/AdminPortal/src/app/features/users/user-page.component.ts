/* Users Page Component - Modern UI with Consistent Design System */
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface User {
  id: number;
  username: string;
  fullName: string;
  email: string;
  role: 'Admin' | 'Editor' | 'Viewer';
  status: 'Active' | 'Invited' | 'Suspended';
  initials: string;
}

@Component({
  selector: 'app-user-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './user-page.component.html',
  styleUrls: ['./user-page.component.css']
})
export class UserPageComponent implements OnInit {
  users: User[] = [];

  ngOnInit(): void {
    // Fetch users - same pattern as UserService
    // this.userService.getUsers().subscribe({
    //   next: (users) => this.users = users,
    //   error: (err) => console.error('Failed to load users', err)
    // });
  }
}