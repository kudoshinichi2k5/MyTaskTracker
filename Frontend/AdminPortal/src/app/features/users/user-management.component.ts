import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnInit,
  inject
} from '@angular/core';
import { CommonModule } from '@angular/common';

import {
  AdminUser,
  UserAdminService
} from '../../services/user-admin.service';

@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './user-management.component.html',
  styleUrl: './user-management.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserManagementComponent implements OnInit {
  private readonly service = inject(UserAdminService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly assignableRole = 'admin';

  users: AdminUser[] = [];

  isLoading = true;
  error: string | null = null;

  pendingUserIds = new Set<string>();

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.isLoading = true;
    this.error = null;

    this.service.getUsers().subscribe({
      next: users => {
        this.users = users;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.error =
          "Couldn't load users. Please try again.";
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  hasAdminRole(user: AdminUser): boolean {
    return user.roles.includes(
      this.assignableRole
    );
  }

  toggleAdminRole(user: AdminUser): void {
    if (this.pendingUserIds.has(user.id)) {
      return;
    }

    const hadRole =
      this.hasAdminRole(user);

    this.pendingUserIds.add(user.id);

    if (hadRole) {
      user.roles = user.roles.filter(
        role => role !== this.assignableRole
      );
    } else {
      user.roles = [
        ...user.roles,
        this.assignableRole
      ];
    }

    const request$ = hadRole
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
        this.pendingUserIds.delete(user.id);
        this.cdr.markForCheck();
      },
      error: () => {
        if (hadRole) {
          user.roles = [
            ...user.roles,
            this.assignableRole
          ];
        } else {
          user.roles = user.roles.filter(
            role => role !== this.assignableRole
          );
        }

        this.pendingUserIds.delete(user.id);

        this.error =
          `Couldn't update the role for ${user.username}.`;

        this.cdr.markForCheck();
      }
    });
  }
}