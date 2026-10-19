import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { Prescription, Patient, Doctor } from '../../core/models/app.models';

@Component({
  selector: 'app-prescription-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="rx-page">
      <div class="page-header">
        <div>
          <h2>Prescription &amp; Medication Management</h2>
          <p class="subtitle">E-Prescribing, pharmacy dispensing status, refills, and dosage instructions.</p>
        </div>

        <button *ngIf="canWrite" class="btn btn-primary" (click)="openWriteModal()">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="12" y1="5" x2="12" y2="19"></line>
            <line x1="5" y1="12" x2="19" y2="12"></line>
          </svg>
          Write New Prescription
        </button>
      </div>

      <div *ngIf="isLoading" class="loading-state">
        <div class="spinner"></div>
        <span>Retrieving prescriptions...</span>
      </div>

      <div *ngIf="!isLoading" class="table-container">
        <table class="data-table">
          <thead>
            <tr>
              <th>RX Number</th>
              <th>Patient</th>
              <th>Prescribed By</th>
              <th>Medications &amp; Instructions</th>
              <th>Date Prescribed</th>
              <th>Status</th>
              <th>Pharmacy Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let rx of prescriptions">
              <td><span class="badge badge-neutral">{{ rx.prescriptionNumber }}</span></td>
              <td><strong>{{ rx.patientName }}</strong></td>
              <td>{{ rx.doctorName }}</td>
              <td>
                <div *ngFor="let item of rx.items" class="med-item">
                  <strong>{{ item.medicationName }}</strong> ({{ item.dosage }}) &bull; {{ item.frequency }} ({{ item.durationDays }} days)
                  <p class="sub-inst" *ngIf="item.instructions"><em>Sig: {{ item.instructions }}</em></p>
                </div>
              </td>
              <td>{{ rx.prescribedAtUtc | date:'mediumDate' }}</td>
              <td>
                <span class="badge" [ngClass]="{
                  'badge-primary': rx.status === 'Active',
                  'badge-success': rx.status === 'Dispensed' || rx.status === 'Completed',
                  'badge-danger': rx.status === 'Discontinued' || rx.status === 'Expired'
                }">{{ rx.status }}</span>
              </td>
              <td>
                <button
                  *ngIf="canDispense(rx.status)"
                  class="btn btn-secondary btn-sm"
                  (click)="updateStatus(rx.id, 'Dispensed')"
                >
                  Mark Dispensed
                </button>
              </td>
            </tr>
            <tr *ngIf="prescriptions.length === 0">
              <td colspan="7" class="empty-cell">No prescriptions recorded.</td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Write Prescription Modal -->
      <div *ngIf="showModal" class="modal-overlay">
        <div class="modal-content">
          <div class="modal-header">
            <h3>Write Prescription</h3>
            <button class="close-btn" (click)="showModal = false">&times;</button>
          </div>

          <div class="modal-body">
            <div class="form-group">
              <label>Select Patient *</label>
              <select [(ngModel)]="newRx.patientId" class="modal-input">
                <option value="">-- Choose Patient --</option>
                <option *ngFor="let p of patients" [value]="p.id">{{ p.fullName }} ({{ p.medicalRecordNumber }})</option>
              </select>
            </div>

            <div class="form-row-2">
              <div class="form-group">
                <label>Medication Name *</label>
                <input type="text" [(ngModel)]="newRx.medicationName" class="modal-input" placeholder="e.g. Amoxicillin" />
              </div>
              <div class="form-group">
                <label>Dosage *</label>
                <input type="text" [(ngModel)]="newRx.dosage" class="modal-input" placeholder="e.g. 500mg" />
              </div>
            </div>

            <div class="form-row-2">
              <div class="form-group">
                <label>Frequency *</label>
                <input type="text" [(ngModel)]="newRx.frequency" class="modal-input" placeholder="e.g. 3 times daily with meals" />
              </div>
              <div class="form-group">
                <label>Duration (Days)</label>
                <input type="number" [(ngModel)]="newRx.durationDays" class="modal-input" />
              </div>
            </div>

            <div class="form-group">
              <label>Quantity &amp; Sig (Instructions)</label>
              <textarea [(ngModel)]="newRx.instructions" rows="2" class="modal-textarea" placeholder="Take 1 capsule by mouth every 8 hours..."></textarea>
            </div>
          </div>

          <div class="modal-footer">
            <button class="btn btn-outline" (click)="showModal = false">Cancel</button>
            <button class="btn btn-primary" (click)="savePrescription()">Sign &amp; Prescribe</button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .rx-page { display: flex; flex-direction: column; gap: 20px; }
    .page-header {
      display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 12px;
      h2 { font-size: 1.4rem; font-weight: 700; color: #0f172a; margin: 0; }
      .subtitle { color: #64748b; font-size: 0.85rem; margin-top: 2px; }
    }
    .med-item { margin-bottom: 6px; font-size: 0.88rem; }
    .sub-inst { font-size: 0.78rem; color: #64748b; margin: 2px 0 0; }
    .loading-state { display: flex; align-items: center; justify-content: center; gap: 12px; padding: 40px; color: #64748b; }
    .spinner { width: 22px; height: 22px; border: 2px solid #cbd5e1; border-top-color: #0284c7; border-radius: 50%; animation: spin 0.8s linear infinite; }
    @keyframes spin { to { transform: rotate(360deg); } }
    .empty-cell { text-align: center; padding: 40px; color: #94a3b8; }
    .modal-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
    .modal-body { display: flex; flex-direction: column; gap: 12px; }
    .form-row-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
    .form-group {
      display: flex; flex-direction: column; gap: 4px;
      label { font-size: 0.8rem; font-weight: 600; color: #334155; }
      .modal-input, .modal-textarea {
        border: 1px solid #cbd5e1; border-radius: 6px; padding: 8px 12px; font-size: 0.9rem;
        &:focus { outline: none; border-color: #0284c7; }
      }
    }
    .modal-footer { display: flex; justify-content: flex-end; gap: 10px; margin-top: 20px; }
  `]
})
export class PrescriptionListComponent implements OnInit {
  prescriptions: Prescription[] = [];
  patients: Patient[] = [];
  isLoading = true;
  showModal = false;

  newRx = {
    patientId: '',
    medicationName: '',
    dosage: '',
    frequency: 'Once daily',
    durationDays: 30,
    instructions: ''
  };

  constructor(
    private apiService: ApiService,
    public authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadPrescriptions();
    this.apiService.getPatients().subscribe({
      next: (r) => {
        this.patients = (r as any)?.data?.items ?? (r as any)?.Data?.Items ?? (r as any)?.data ?? [];
        this.cdr.detectChanges();
      },
      error: () => {}
    });
  }

  get canWrite(): boolean {
    const role = this.authService.activeRoleValue;
    return role === 'Doctor' || role === 'Admin';
  }

  canDispense(status: string): boolean {
    const role = this.authService.activeRoleValue;
    return (role === 'Doctor' || role === 'Nurse' || role === 'Admin') && status === 'Active';
  }

  loadPrescriptions(): void {
    this.isLoading = true;
    this.cdr.detectChanges();
    this.apiService.getPrescriptions().subscribe({
      next: (res) => {
        this.isLoading = false;
        this.prescriptions = (res as any)?.data ?? (res as any)?.Data ?? [];
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  openWriteModal(): void {
    this.showModal = true;
    this.cdr.detectChanges();
  }

  savePrescription(): void {
    if (!this.newRx.patientId || !this.newRx.medicationName || !this.newRx.dosage) {
      alert('Patient, Medication Name, and Dosage are required.');
      return;
    }

    const doctorId = this.authService.currentUserValue?.doctorId || this.authService.currentUserValue?.id || '00000000-0000-0000-0000-000000000000';

    const payload = {
      patientId: this.newRx.patientId,
      doctorId: doctorId,
      items: [{
        medicationName: this.newRx.medicationName,
        dosage: this.newRx.dosage,
        frequency: this.newRx.frequency,
        durationDays: this.newRx.durationDays,
        quantity: this.newRx.durationDays,
        refillsAllowed: 1,
        instructions: this.newRx.instructions
      }]
    };

    this.apiService.createPrescription(payload, doctorId).subscribe({
      next: () => {
        alert('Prescription created successfully!');
        this.showModal = false;
        this.loadPrescriptions();
        this.cdr.detectChanges();
      },
      error: (err) => {
        alert(err.error?.message || 'Failed to create prescription.');
        this.cdr.detectChanges();
      }
    });
  }

  updateStatus(id: string, status: string): void {
    this.apiService.updatePrescriptionStatus(id, status).subscribe({
      next: () => {
        alert('Prescription status updated!');
        this.loadPrescriptions();
        this.cdr.detectChanges();
      },
      error: (err) => {
        alert(err.error?.message || 'Failed to update prescription status.');
        this.cdr.detectChanges();
      }
    });
  }
}
