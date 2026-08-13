import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnInit,
  inject
} from '@angular/core';
import { CommonModule } from '@angular/common';

import { AuthService } from '../../services/auth.service';

import { UserAdminService } from '../../services/user-admin.service';

@Component({
  selector: 'app-role-management',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './role-management.component.html',
  styleUrl: './role-management.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RoleManagementComponent implements OnInit {
  private readonly service = inject(UserAdminService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly authService = inject(AuthService);
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
      next: roles => {
        this.roles = roles;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.error =
          "Couldn't load roles. Please try again.";
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }
}