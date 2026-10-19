import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../core/services/auth.service';
import { UserProfile } from '../core/models/app.models';

interface NavItem {
  label: string;
  route: string;
  icon: string;
  roles: string[];
}

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './layout.component.html',
  styleUrls: ['./layout.component.scss']
})
export class LayoutComponent implements OnInit {
  currentUser: UserProfile | null = null;
  activeRole = '';
  assignedRoles: string[] = [];
  sidebarCollapsed = false;
  showProfileMenu = false;
  showNotifications = false;

  allNavItems: NavItem[] = [
    { label: 'Dashboard', route: '/dashboard', icon: 'dashboard', roles: [] },
    { label: 'Patients', route: '/patients', icon: 'users', roles: ['Admin', 'Doctor', 'Nurse', 'Receptionist'] },
    { label: 'Appointments', route: '/appointments', icon: 'calendar', roles: ['Admin', 'Doctor', 'Nurse', 'Receptionist', 'Patient'] },
    { label: 'Prescriptions', route: '/prescriptions', icon: 'pill', roles: ['Admin', 'Doctor', 'Nurse', 'Patient'] },
    { label: 'Laboratory', route: '/laboratory', icon: 'flask', roles: ['Admin', 'Doctor', 'Nurse', 'Patient'] },
    { label: 'Billing & Invoices', route: '/billing', icon: 'credit-card', roles: ['Admin', 'Receptionist', 'Patient'] },
    { label: 'User Management', route: '/users', icon: 'shield', roles: ['Admin'] },
    { label: 'Audit Logs', route: '/audit-logs', icon: 'file-text', roles: ['Admin'] },
    { label: 'Dev Mailbox', route: '/dev-mailbox', icon: 'mail', roles: [] }
  ];

  constructor(
    public authService: AuthService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
      this.assignedRoles = user?.roles || [];
      this.cdr.detectChanges();
    });

    this.authService.activeRole$.subscribe(role => {
      this.activeRole = role;
      this.cdr.detectChanges();
    });
  }

  get visibleNavItems(): NavItem[] {
    return this.allNavItems.filter(item => {
      if (item.roles.length === 0) return true;
      return item.roles.includes(this.activeRole);
    });
  }

  get initials(): string {
    if (!this.currentUser) return 'U';
    const first = this.currentUser.firstName?.charAt(0) || '';
    const last = this.currentUser.lastName?.charAt(0) || '';
    return `${first}${last}`.toUpperCase() || 'U';
  }

  onRoleChange(newRole: string): void {
    if (newRole === this.activeRole) return;

    this.authService.switchRole(newRole).subscribe({
      next: (res) => {
        // Navigates to dashboard to refresh widgets for the selected role
        this.router.navigate(['/dashboard']);
        this.cdr.detectChanges();
      },
      error: (err) => {
        alert(err.error?.message || 'Failed to switch role.');
        this.cdr.detectChanges();
      }
    });
  }

  toggleSidebar(): void {
    this.sidebarCollapsed = !this.sidebarCollapsed;
    this.cdr.detectChanges();
  }

  logout(): void {
    this.authService.logout();
  }
}
