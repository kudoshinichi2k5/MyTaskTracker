import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnInit,
  inject
} from '@angular/core';

import { CommonModule } from '@angular/common';

import { AuthService } from '../../services/auth.service';

import {
  UserAdminService
} from '../../services/user-admin.service';

interface RoleDefinition {
  name: string;
  description: string;
  accessLevel: string;
  capabilities: string[];
}

@Component({
  selector: 'app-role-management',
  standalone: true,

  imports: [
    CommonModule
  ],

  templateUrl:
    './role-management.component.html',

  styleUrl:
    './role-management.component.css',

  changeDetection:
    ChangeDetectionStrategy.OnPush
})
export class RoleManagementComponent
  implements OnInit {

  private readonly service =
    inject(UserAdminService);

  private readonly cdr =
    inject(ChangeDetectorRef);

  private readonly authService =
    inject(AuthService);

  roles: string[] = [];

  isLoading = true;

  error: string | null = null;

  async ngOnInit(): Promise<void> {

    await this.authService.initialLoadPromise;

    if (!this.authService.accessToken) {

      this.error =
        'Your session has expired. Please sign in again.';

      this.isLoading = false;

      this.cdr.markForCheck();

      return;
    }

    this.loadRoles();
  }

  loadRoles(): void {

    this.isLoading = true;

    this.error = null;

    this.service.getRoles().subscribe({

      next: (roles) => {

        this.roles = roles;

        this.isLoading = false;

        this.cdr.markForCheck();
      },

      error: (error) => {

        console.error(
          '[RoleManagement] Failed to load roles',
          error
        );

        this.error =
          'Unable to load roles. Please try again.';

        this.isLoading = false;

        this.cdr.markForCheck();
      }
    });
  }

  get roleDefinitions(): RoleDefinition[] {

    return this.roles.map(
      role => this.getRoleDefinition(role)
    );
  }

  getRoleDefinition(
    role: string
  ): RoleDefinition {

    if (
      role.toLowerCase() === 'admin'
    ) {

      return {
        name: 'admin',

        description:
          'System administrator with access to the Admin Portal.',

        accessLevel:
          'Full administrative access',

        capabilities: [
          'Access Admin Portal',
          'View team task summaries',
          'View projects',
          'Review task comments',
          'Manage user Admin access',
          'Assign the admin role',
          'Remove the admin role'
        ]
      };
    }

    return {
      name: role,

      description:
        'Application role managed by the authentication service.',

      accessLevel:
        'Application-defined access',

      capabilities: [
        'Permissions are defined by the backend'
      ]
    };
  }

  get totalRoles(): number {
    return this.roles.length;
  }

  isAdminRole(
    role: string
  ): boolean {

    return role.toLowerCase() === 'admin';
  }
}