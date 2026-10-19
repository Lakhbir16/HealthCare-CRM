import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { UserManagement } from '../../core/models/app.models';

@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="users-page">
      <div class="page-header">
        <div>
          <h2>User &amp; Access Control Management</h2>
          <p class="subtitle">Multi-role assignments, account lockouts, credential administration, and role-based access.</p>
        </div>

        <button class="btn btn-primary" (click)="showCreateModal = true">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="12" y1="5" x2="12" y2="19"></line>
            <line x1="5" y1="12" x2="19" y2="12"></line>
          </svg>
          Add New User
        </button>
      </div>

      <div *ngIf="isLoading" class="loading-state">
        <div class="spinner"></div>
        <span>Loading system user accounts...</span>
      </div>

      <div *ngIf="!isLoading" class="table-container">
        <table class="data-table">
          <thead>
            <tr>
              <th>User Name</th>
              <th>Email</th>
              <th>Assigned Roles (Multi-Role)</th>
              <th>Status</th>
              <th>Lockout</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let u of users">
              <td><strong>{{ u.firstName }} {{ u.lastName }}</strong></td>
              <td>{{ u.email }}</td>
              <td>
                <div class="roles-tags-wrap">
                  <span *ngFor="let r of u.roles" class="badge badge-primary">{{ r }}</span>
                </div>
              </td>
              <td>
                <span class="badge" [ngClass]="u.isActive ? 'badge-success' : 'badge-danger'">
                  {{ u.isActive ? 'Active' : 'Deactivated' }}
                </span>
              </td>
              <td>
                <span class="badge" [ngClass]="u.isLockedOut ? 'badge-danger' : 'badge-neutral'">
                  {{ u.isLockedOut ? 'LOCKED' : 'Unlocked' }}
                </span>
              </td>
              <td>
                <div class="action-buttons">
                  <button class="btn btn-outline btn-sm" (click)="openRolesModal(u)">
                    Edit Roles
                  </button>
                  <button
                    class="btn btn-sm"
                    [ngClass]="u.isLockedOut ? 'btn-secondary' : 'btn-danger'"
                    (click)="toggleLock(u)"
                  >
                    {{ u.isLockedOut ? 'Unlock' : 'Lock' }}
                  </button>
                </div>
              </td>
            </tr>
            <tr *ngIf="users.length === 0">
              <td colspan="6" class="empty-cell">No users registered.</td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Manage Roles Modal -->
      <div *ngIf="showRolesModal" class="modal-overlay">
        <div class="modal-content">
          <div class="modal-header">
            <h3>Assign Roles for {{ selectedUser?.firstName }} {{ selectedUser?.lastName }}</h3>
            <button class="close-btn" (click)="showRolesModal = false">&times;</button>
          </div>

          <div class="modal-body">
            <p class="role-modal-hint">A user can be assigned multiple roles simultaneously (e.g. Doctor + HealthcareManager):</p>
            <div class="roles-checklist">
              <label *ngFor="let r of allAvailableRoles" class="role-check-item">
                <input type="checkbox" [checked]="isRoleSelected(r)" (change)="toggleRole(r)" />
                <span class="role-name">{{ r }}</span>
              </label>
            </div>
          </div>

          <div class="modal-footer">
            <button class="btn btn-outline" (click)="showRolesModal = false">Cancel</button>
            <button class="btn btn-primary" (click)="saveUserRoles()">Update Roles</button>
          </div>
        </div>
      </div>

      <!-- Create User Modal -->
      <div *ngIf="showCreateModal" class="modal-overlay">
        <div class="modal-content">
          <div class="modal-header">
            <h3>Create System User Account</h3>
            <button class="close-btn" (click)="showCreateModal = false">&times;</button>
          </div>

          <div class="modal-body">
            <div class="form-row-2">
              <div class="form-group">
                <label>First Name *</label>
                <input type="text" [(ngModel)]="newUser.firstName" class="modal-input" placeholder="e.g. Rachel" />
              </div>
              <div class="form-group">
                <label>Last Name *</label>
                <input type="text" [(ngModel)]="newUser.lastName" class="modal-input" placeholder="e.g. Adams" />
              </div>
            </div>

            <div class="form-group">
              <label>Email Address *</label>
              <input type="email" [(ngModel)]="newUser.email" class="modal-input" placeholder="rachel@healthcare.com" />
            </div>

            <div class="form-group">
              <label>Temporary Password *</label>
              <input type="password" [(ngModel)]="newUser.password" class="modal-input" placeholder="TempPass@123" />
            </div>

            <div class="form-group">
              <label>Initial Assigned Roles</label>
              <div class="roles-checklist-mini">
                <label *ngFor="let r of allAvailableRoles" class="role-check-mini">
                  <input type="checkbox" (change)="toggleNewUserRole(r)" />
                  <span>{{ r }}</span>
                </label>
              </div>
            </div>
          </div>

          <div class="modal-footer">
            <button class="btn btn-outline" (click)="showCreateModal = false">Cancel</button>
            <button class="btn btn-primary" (click)="createUser()">Create Account</button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .users-page { display: flex; flex-direction: column; gap: 20px; }
    .page-header {
      display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 12px;
      h2 { font-size: 1.4rem; font-weight: 700; color: #0f172a; margin: 0; }
      .subtitle { color: #64748b; font-size: 0.85rem; margin-top: 2px; }
    }
    .roles-tags-wrap { display: flex; flex-wrap: wrap; gap: 4px; }
    .action-buttons { display: flex; gap: 6px; }
    .loading-state { display: flex; align-items: center; justify-content: center; gap: 12px; padding: 40px; color: #64748b; }
    .spinner { width: 22px; height: 22px; border: 2px solid #cbd5e1; border-top-color: #0284c7; border-radius: 50%; animation: spin 0.8s linear infinite; }
    @keyframes spin { to { transform: rotate(360deg); } }
    .empty-cell { text-align: center; padding: 40px; color: #94a3b8; }
    .role-modal-hint { font-size: 0.85rem; color: #64748b; margin-bottom: 12px; }
    .roles-checklist {
      display: grid; grid-template-columns: 1fr 1fr; gap: 10px; max-height: 280px; overflow-y: auto; padding: 4px;
      .role-check-item {
        display: flex; align-items: center; gap: 8px; font-size: 0.88rem; color: #1e293b; cursor: pointer;
        background: #f8fafc; padding: 8px 12px; border-radius: 6px; border: 1px solid #e2e8f0;
      }
    }
    .roles-checklist-mini {
      display: grid; grid-template-columns: 1fr 1fr; gap: 6px; max-height: 180px; overflow-y: auto;
      .role-check-mini { display: flex; align-items: center; gap: 6px; font-size: 0.8rem; cursor: pointer; }
    }
    .modal-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
    .modal-body { display: flex; flex-direction: column; gap: 12px; }
    .form-row-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
    .form-group {
      display: flex; flex-direction: column; gap: 4px;
      label { font-size: 0.8rem; font-weight: 600; color: #334155; }
      .modal-input {
        height: 38px; border: 1px solid #cbd5e1; border-radius: 6px; padding: 0 12px; font-size: 0.9rem;
        &:focus { outline: none; border-color: #0284c7; }
      }
    }
    .modal-footer { display: flex; justify-content: flex-end; gap: 10px; margin-top: 20px; }
  `]
})
export class UserManagementComponent implements OnInit {
  users: UserManagement[] = [];
  isLoading = true;

  showRolesModal = false;
  showCreateModal = false;
  selectedUser: UserManagement | null = null;
  selectedRoles: string[] = [];

  allAvailableRoles = [
    'Admin', 'Doctor', 'Nurse', 'Receptionist', 'Patient'
  ];

  newUser = {
    firstName: '',
    lastName: '',
    email: '',
    password: '',
    roles: [] as string[]
  };

  constructor(
    private apiService: ApiService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.isLoading = true;
    this.cdr.detectChanges();
    this.apiService.getUsers().subscribe({
      next: (res) => {
        this.isLoading = false;
        this.users = (res as any)?.data ?? (res as any)?.Data ?? [];
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  openRolesModal(user: UserManagement): void {
    this.selectedUser = user;
    this.selectedRoles = [...user.roles];
    this.showRolesModal = true;
    this.cdr.detectChanges();
  }

  isRoleSelected(role: string): boolean {
    return this.selectedRoles.includes(role);
  }

  toggleRole(role: string): void {
    const idx = this.selectedRoles.indexOf(role);
    if (idx > -1) {
      this.selectedRoles.splice(idx, 1);
    } else {
      this.selectedRoles.push(role);
    }
    this.cdr.detectChanges();
  }

  toggleNewUserRole(role: string): void {
    const idx = this.newUser.roles.indexOf(role);
    if (idx > -1) {
      this.newUser.roles.splice(idx, 1);
    } else {
      this.newUser.roles.push(role);
    }
    this.cdr.detectChanges();
  }

  saveUserRoles(): void {
    if (!this.selectedUser) return;

    this.apiService.updateUserRoles(this.selectedUser.id, this.selectedRoles).subscribe({
      next: () => {
        alert('User roles updated successfully!');
        this.showRolesModal = false;
        this.loadUsers();
        this.cdr.detectChanges();
      },
      error: (err) => {
        alert(err.error?.message || 'Failed to update roles.');
        this.cdr.detectChanges();
      }
    });
  }

  toggleLock(user: UserManagement): void {
    const newLockState = !user.isLockedOut;
    this.apiService.setUserStatus(user.id, user.isActive, newLockState).subscribe({
      next: () => {
        user.isLockedOut = newLockState;
        this.cdr.detectChanges();
      },
      error: (err) => {
        alert(err.error?.message || 'Failed to update user status.');
        this.cdr.detectChanges();
      }
    });
  }

  createUser(): void {
    if (!this.newUser.email || !this.newUser.firstName || !this.newUser.password) {
      alert('Email, First Name, and Password are required.');
      return;
    }

    this.apiService.createUser(this.newUser).subscribe({
      next: () => {
        alert('User created successfully!');
        this.showCreateModal = false;
        this.loadUsers();
        this.cdr.detectChanges();
      },
      error: (err) => {
        alert(err.error?.message || 'Failed to create user.');
        this.cdr.detectChanges();
      }
    });
  }
}
