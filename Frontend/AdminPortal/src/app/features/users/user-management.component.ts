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
  AdminUser,
  UserAdminService
} from '../../services/user-admin.service';

import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-user-management',
  standalone: true,

  imports: [
    CommonModule,
    FormsModule
  ],

  templateUrl: './user-management.component.html',
  styleUrl: './user-management.component.css',

  changeDetection:
    ChangeDetectionStrategy.OnPush
})
export class UserManagementComponent
  implements OnInit {

  private readonly service =
    inject(UserAdminService);

  private readonly cdr =
    inject(ChangeDetectorRef);

  private readonly authService =
    inject(AuthService);

  readonly assignableRole = 'admin';

  users: AdminUser[] = [];

  searchTerm = '';

  statusFilter:
    'all' |
    'enabled' |
    'disabled' = 'all';

  accessFilter:
    'all' |
    'admin' |
    'standard' = 'all';

  isLoading = true;

  error: string | null = null;

  pendingUserIds =
    new Set<string>();

  async ngOnInit(): Promise<void> {

    await this.authService.initialLoadPromise;

    if (!this.authService.accessToken) {

      this.error =
        'Your session has expired. Please sign in again.';

      this.isLoading = false;

      this.cdr.markForCheck();

      return;
    }

    this.loadUsers();
  }

  loadUsers(): void {

    this.isLoading = true;

    this.error = null;

    this.service.getUsers().subscribe({

      next: (users) => {

        this.users = users;

        this.isLoading = false;

        this.cdr.markForCheck();
      },

      error: (error) => {

        console.error(
          '[UserManagement] Failed to load users',
          error
        );

        this.error =
          'Unable to load users. Please check the AuthService and try again.';

        this.isLoading = false;

        this.cdr.markForCheck();
      }
    });
  }

  get filteredUsers(): AdminUser[] {

    const search =
      this.searchTerm
        .trim()
        .toLowerCase();

    return this.users.filter(
      (user) => {

        const matchesSearch =
          !search ||
          user.username
            .toLowerCase()
            .includes(search) ||
          (user.email ?? '')
            .toLowerCase()
            .includes(search);

        const matchesStatus =
          this.statusFilter === 'all' ||
          (
            this.statusFilter === 'enabled' &&
            user.enabled
          ) ||
          (
            this.statusFilter === 'disabled' &&
            !user.enabled
          );

        const isAdmin =
          this.hasAdminRole(user);

        const matchesAccess =
          this.accessFilter === 'all' ||
          (
            this.accessFilter === 'admin' &&
            isAdmin
          ) ||
          (
            this.accessFilter === 'standard' &&
            !isAdmin
          );

        return (
          matchesSearch &&
          matchesStatus &&
          matchesAccess
        );
      }
    );
  }

  get totalUsers(): number {
    return this.users.length;
  }

  get enabledUsers(): number {
    return this.users.filter(
      user => user.enabled
    ).length;
  }

  get disabledUsers(): number {
    return this.users.filter(
      user => !user.enabled
    ).length;
  }

  get adminUsers(): number {
    return this.users.filter(
      user => this.hasAdminRole(user)
    ).length;
  }

  hasAdminRole(
    user: AdminUser
  ): boolean {

    return user.roles.includes(
      this.assignableRole
    );
  }

  toggleAdminRole(
    user: AdminUser
  ): void {

    if (
      this.pendingUserIds.has(user.id)
    ) {
      return;
    }

    const hadRole =
      this.hasAdminRole(user);

    /*
     * Never allow the UI to leave a
     * disabled account looking like it
     * has active admin access.
     *
     * The backend still remains authoritative.
     */
    if (!user.enabled && !hadRole) {
      this.error =
        `${user.username} is disabled. Enable the account before granting Admin access.`;

      this.cdr.markForCheck();

      return;
    }

    this.pendingUserIds.add(user.id);

    /*
     * Optimistic UI update.
     */
    if (hadRole) {

      user.roles =
        user.roles.filter(
          role =>
            role !== this.assignableRole
        );

    } else {

      user.roles = [
        ...user.roles,
        this.assignableRole
      ];
    }

    const request$ =
      hadRole
        ? this.service.removeRole(
            user.id,
            this.assignableRole
          )
        : this.service.assignRole(
            user.id,
            this.assignableRole
          );

    request$.subscribe({

      next: () => {

        this.pendingUserIds.delete(
          user.id
        );

        this.error = null;

        this.cdr.markForCheck();
      },

      error: () => {

        /*
         * Roll back optimistic update.
         */
        if (hadRole) {

          user.roles = [
            ...user.roles,
            this.assignableRole
          ];

        } else {

          user.roles =
            user.roles.filter(
              role =>
                role !==
                this.assignableRole
            );
        }

        this.pendingUserIds.delete(
          user.id
        );

        this.error =
          `Couldn't update the Admin role for ${user.username}.`;

        this.cdr.markForCheck();
      }
    });
  }

  clearFilters(): void {

    this.searchTerm = '';

    this.statusFilter = 'all';

    this.accessFilter = 'all';

    this.cdr.markForCheck();
  }
}