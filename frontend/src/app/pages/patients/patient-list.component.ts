import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { Patient, Doctor } from '../../core/models/app.models';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-patient-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  template: `
    <div class="patients-page">
      <div class="page-header">
        <div>
          <h2>Patient Directory</h2>
          <p class="subtitle">Search, view medical charts, and manage registered patients.</p>
        </div>

        <button *ngIf="canRegister" class="btn btn-primary" (click)="openRegisterModal()">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="12" y1="5" x2="12" y2="19"></line>
            <line x1="5" y1="12" x2="19" y2="12"></line>
          </svg>
          Register New Patient
        </button>
      </div>

      <div class="filter-card card">
        <div class="search-input-wrap">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="11" cy="11" r="8"></circle>
            <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
          </svg>
          <input
            type="text"
            [(ngModel)]="searchQuery"
            (ngModelChange)="onSearchChange()"
            placeholder="Search by patient name, MRN, email, or phone number..."
          />
        </div>
      </div>

      <div *ngIf="isLoading" class="loading-state">
        <div class="spinner"></div>
        <span>Retrieving patient records...</span>
      </div>

      <div *ngIf="!isLoading" class="table-container">
        <table class="data-table">
          <thead>
            <tr>
              <th>MRN</th>
              <th>Patient Name</th>
              <th>Contact Details</th>
              <th>DOB / Gender</th>
              <th>Blood Group</th>
              <th>Primary Doctor</th>
              <th>Action</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let p of patients">
              <td><span class="mrn-badge">{{ p.medicalRecordNumber }}</span></td>
              <td>
                <div class="patient-name-box">
                  <strong>{{ p.fullName }}</strong>
                  <span class="sub-email">{{ p.email }}</span>
                </div>
              </td>
              <td>{{ p.phoneNumber || 'N/A' }}</td>
              <td>{{ (p.dateOfBirth | date:'mediumDate') || 'N/A' }} ({{ p.gender || 'N/A' }})</td>
              <td><span class="blood-tag">{{ p.bloodGroup || 'N/A' }}</span></td>
              <td>{{ p.primaryCareDoctorName || 'Not Assigned' }}</td>
              <td>
                <button class="btn btn-outline btn-sm" (click)="viewChart(p.id)">
                  View Chart
                </button>
              </td>
            </tr>
            <tr *ngIf="patients.length === 0">
              <td colspan="7" class="empty-cell">No matching patient records found.</td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Register Patient Modal -->
      <div *ngIf="showModal" class="modal-overlay">
        <div class="modal-content">
          <div class="modal-header">
            <h3>Register New Patient</h3>
            <button class="close-btn" (click)="showModal = false">&times;</button>
          </div>

          <div class="modal-body">
            <div class="form-row-2">
              <div class="form-group">
                <label>First Name *</label>
                <input type="text" [(ngModel)]="newPatient.firstName" class="modal-input" placeholder="e.g. Michael" />
              </div>
              <div class="form-group">
                <label>Last Name *</label>
                <input type="text" [(ngModel)]="newPatient.lastName" class="modal-input" placeholder="e.g. Brown" />
              </div>
            </div>

            <div class="form-row-2">
              <div class="form-group">
                <label>Email Address *</label>
                <input type="email" [(ngModel)]="newPatient.email" class="modal-input" placeholder="michael@example.com" />
              </div>
              <div class="form-group">
                <label>Phone Number</label>
                <input type="tel" [(ngModel)]="newPatient.phoneNumber" class="modal-input" placeholder="+1 (555) 000-0000" />
              </div>
            </div>

            <div class="form-row-2">
              <div class="form-group">
                <label>Date of Birth</label>
                <input type="date" [(ngModel)]="newPatient.dateOfBirth" class="modal-input" />
              </div>
              <div class="form-group">
                <label>Gender</label>
                <select [(ngModel)]="newPatient.gender" class="modal-input">
                  <option value="Male">Male</option>
                  <option value="Female">Female</option>
                  <option value="Other">Other</option>
                </select>
              </div>
            </div>

            <div class="form-row-2">
              <div class="form-group">
                <label>Blood Group</label>
                <select [(ngModel)]="newPatient.bloodGroup" class="modal-input">
                  <option value="O+">O+</option>
                  <option value="O-">O-</option>
                  <option value="A+">A+</option>
                  <option value="A-">A-</option>
                  <option value="B+">B+</option>
                  <option value="B-">B-</option>
                  <option value="AB+">AB+</option>
                  <option value="AB-">AB-</option>
                </select>
              </div>
              <div class="form-group">
                <label>Primary Care Doctor</label>
                <select [(ngModel)]="newPatient.primaryCareDoctorId" class="modal-input">
                  <option [ngValue]="null">Select Primary Doctor</option>
                  <option *ngFor="let doc of doctors" [value]="doc.id">{{ doc.fullName }} ({{ doc.specialtyName }})</option>
                </select>
              </div>
            </div>

            <div class="form-group">
              <label>Address</label>
              <input type="text" [(ngModel)]="newPatient.address" class="modal-input" placeholder="Street Address" />
            </div>

            <div class="form-row-2">
              <div class="form-group">
                <label>Emergency Contact Name</label>
                <input type="text" [(ngModel)]="newPatient.emergencyContactName" class="modal-input" placeholder="Contact person" />
              </div>
              <div class="form-group">
                <label>Emergency Contact Phone</label>
                <input type="tel" [(ngModel)]="newPatient.emergencyContactPhone" class="modal-input" placeholder="+1 (555) 000-0000" />
              </div>
            </div>
          </div>

          <div class="modal-footer">
            <button class="btn btn-outline" (click)="showModal = false">Cancel</button>
            <button class="btn btn-primary" (click)="savePatient()">Create Patient Record</button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .patients-page { display: flex; flex-direction: column; gap: 20px; }
    .page-header {
      display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 12px;
      h2 { font-size: 1.4rem; font-weight: 700; color: #0f172a; margin: 0; }
      .subtitle { color: #64748b; font-size: 0.85rem; margin-top: 2px; }
    }
    .filter-card { padding: 12px 18px; }
    .search-input-wrap {
      display: flex; align-items: center; gap: 10px; color: #94a3b8;
      input { width: 100%; border: none; outline: none; font-size: 0.95rem; color: #1e293b; }
    }
    .mrn-badge {
      font-family: monospace; font-size: 0.75rem; font-weight: 700; background: #e0f2fe; color: #0369a1; padding: 3px 8px; border-radius: 6px;
    }
    .patient-name-box {
      display: flex; flex-direction: column;
      strong { color: #0f172a; }
      .sub-email { font-size: 0.75rem; color: #64748b; }
    }
    .blood-tag {
      font-weight: 700; color: #b91c1c; background: #fee2e2; padding: 2px 6px; border-radius: 4px; font-size: 0.75rem;
    }
    .loading-state {
      display: flex; align-items: center; justify-content: center; gap: 12px; padding: 40px; color: #64748b;
    }
    .spinner {
      width: 22px; height: 22px; border: 2px solid #cbd5e1; border-top-color: #0284c7; border-radius: 50%; animation: spin 0.8s linear infinite;
    }
    @keyframes spin { to { transform: rotate(360deg); } }
    .empty-cell { text-align: center; padding: 40px; color: #94a3b8; }
    .modal-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; }
    .modal-body { display: flex; flex-direction: column; gap: 12px; }
    .form-row-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
    .form-group {
      display: flex; flex-direction: column; gap: 4px;
      label { font-size: 0.82rem; font-weight: 600; color: #334155; }
      .modal-input {
        height: 38px; padding: 0 12px; border: 1px solid #cbd5e1; border-radius: 6px; font-size: 0.9rem;
        &:focus { outline: none; border-color: #0284c7; }
      }
    }
    .modal-footer { display: flex; justify-content: flex-end; gap: 10px; margin-top: 20px; }
  `]
})
export class PatientListComponent implements OnInit {
  patients: Patient[] = [];
  doctors: Doctor[] = [];
  searchQuery = '';
  isLoading = true;
  showModal = false;

  newPatient: any = {
    firstName: '',
    lastName: '',
    email: '',
    phoneNumber: '',
    dateOfBirth: '1990-01-01',
    gender: 'Male',
    bloodGroup: 'O+',
    address: '',
    primaryCareDoctorId: null,
    emergencyContactName: '',
    emergencyContactPhone: ''
  };

  constructor(
    private apiService: ApiService,
    private authService: AuthService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadPatients();
    this.apiService.getDoctors().subscribe({
      next: (res) => {
        this.doctors = (res as any)?.data ?? (res as any)?.Data ?? [];
        this.cdr.detectChanges();
      },
      error: () => {}
    });
  }

  get canRegister(): boolean {
    const role = this.authService.activeRoleValue;
    return ['SuperAdmin', 'Admin', 'Receptionist', 'HealthcareManager'].includes(role);
  }

  loadPatients(): void {
    this.isLoading = true;
    this.cdr.detectChanges();
    this.apiService.getPatients(this.searchQuery).subscribe({
      next: (res) => {
        this.isLoading = false;
        this.patients = (res as any)?.data?.items ?? (res as any)?.Data?.Items ?? (res as any)?.data ?? (res as any)?.items ?? [];
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  onSearchChange(): void {
    this.loadPatients();
  }

  openRegisterModal(): void {
    this.showModal = true;
    this.cdr.detectChanges();
  }

  savePatient(): void {
    if (!this.newPatient.firstName || !this.newPatient.lastName || !this.newPatient.email) {
      alert('First Name, Last Name, and Email are required.');
      return;
    }

    this.apiService.createPatient(this.newPatient).subscribe({
      next: (res) => {
        alert('Patient registered successfully!');
        this.showModal = false;
        this.loadPatients();
        this.cdr.detectChanges();
      },
      error: (err) => {
        alert(err.error?.message || 'Failed to create patient.');
        this.cdr.detectChanges();
      }
    });
  }

  viewChart(patientId: string): void {
    this.router.navigate(['/patients', patientId]);
  }
}
