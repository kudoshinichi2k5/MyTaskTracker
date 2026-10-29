/* Roles Page Component - Modern UI with Consistent Design System */
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface Role {
  id: number;
  name: string;
  description: string;
  permissionCount: number;
  featureCount: number;
}

@Component({
  selector: 'app-role-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './role-page.component.html',
  styleUrls: ['./role-page.component.css']
})
export class RolePageComponent implements OnInit {
  roles: Role[] = [];

  ngOnInit(): void {
    // Fetch roles - same pattern as RoleService
    // this.roleService.getRoles().subscribe({
    //   next: (roles) => this.roles = roles,
    //   error: (err) => console.error('Failed to load roles', err)
    // });
  }
}