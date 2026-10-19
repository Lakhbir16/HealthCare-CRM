import { Routes } from '@angular/router';
import { LoginComponent } from './pages/login/login.component';
import { ForgotPasswordComponent } from './pages/forgot-password/forgot-password.component';
import { VerifyOtpComponent } from './pages/verify-otp/verify-otp.component';
import { ResetPasswordComponent } from './pages/reset-password/reset-password.component';
import { DevMailboxComponent } from './pages/dev-mailbox/dev-mailbox.component';
import { LayoutComponent } from './layout/layout.component';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { PatientListComponent } from './pages/patients/patient-list.component';
import { PatientDetailComponent } from './pages/patients/patient-detail.component';
import { AppointmentListComponent } from './pages/appointments/appointment-list.component';
import { PrescriptionListComponent } from './pages/prescriptions/prescription-list.component';
import { LaboratoryComponent } from './pages/laboratory/laboratory.component';
import { BillingComponent } from './pages/billing/billing.component';
import { UserManagementComponent } from './pages/users/user-management.component';
import { AuditLogsComponent } from './pages/audit-logs/audit-logs.component';
import { AccessDeniedComponent, NotFoundComponent } from './pages/status/status-pages.component';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'forgot-password', component: ForgotPasswordComponent },
  { path: 'verify-otp', component: VerifyOtpComponent },
  { path: 'reset-password', component: ResetPasswordComponent },
  { path: 'dev-mailbox', component: DevMailboxComponent },

  // Authenticated Portal Pages within Common Multi-Role Layout
  {
    path: '',
    component: LayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', component: DashboardComponent },
      {
        path: 'patients',
        component: PatientListComponent,
        data: { roles: ['Admin', 'Doctor', 'Nurse', 'Receptionist'] }
      },
      {
        path: 'patients/:id',
        component: PatientDetailComponent,
        data: { roles: ['Admin', 'Doctor', 'Nurse', 'Receptionist', 'Patient'] }
      },
      {
        path: 'appointments',
        component: AppointmentListComponent,
        data: { roles: ['Admin', 'Doctor', 'Nurse', 'Receptionist', 'Patient'] }
      },
      {
        path: 'prescriptions',
        component: PrescriptionListComponent,
        data: { roles: ['Admin', 'Doctor', 'Nurse', 'Patient'] }
      },
      {
        path: 'laboratory',
        component: LaboratoryComponent,
        data: { roles: ['Admin', 'Doctor', 'Nurse', 'Patient'] }
      },
      {
        path: 'billing',
        component: BillingComponent,
        data: { roles: ['Admin', 'Receptionist', 'Patient'] }
      },
      {
        path: 'users',
        component: UserManagementComponent,
        data: { roles: ['Admin'] }
      },
      {
        path: 'audit-logs',
        component: AuditLogsComponent,
        data: { roles: ['Admin'] }
      }
    ]
  },

  { path: 'access-denied', component: AccessDeniedComponent },
  { path: '**', component: NotFoundComponent }
];
