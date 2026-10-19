import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { Appointment, Doctor, Patient } from '../../core/models/app.models';

@Component({
  selector: 'app-appointment-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="appointments-page">
      <div class="page-header">
        <div>
          <h2>Appointment Management</h2>
          <p class="subtitle">Real-time clinical scheduling, conflict detection, and patient check-in queue.</p>
        </div>

        <button class="btn btn-primary" (click)="openBookingModal()">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="12" y1="5" x2="12" y2="19"></line>
            <line x1="5" y1="12" x2="19" y2="12"></line>
          </svg>
          Book Appointment
        </button>
      </div>

      <div *ngIf="conflictWarning" class="alert alert-warning">
        {{ conflictWarning }}
      </div>

      <div *ngIf="isLoading" class="loading-state">
        <div class="spinner"></div>
        <span>Loading scheduled appointments...</span>
      </div>

      <div *ngIf="!isLoading" class="table-container">
        <table class="data-table">
          <thead>
            <tr>
              <th>Patient</th>
              <th>Doctor &amp; Specialty</th>
              <th>Date &amp; Time</th>
              <th>Type / Duration</th>
              <th>Status</th>
              <th>Reason for Visit</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let a of appointments">
              <td>
                <div class="patient-cell">
                  <strong>{{ a.patientName }}</strong>
                  <span class="sub-mrn">{{ a.patientMrn }}</span>
                </div>
              </td>
              <td>
                <div class="doc-cell">
                  <span>{{ a.doctorName }}</span>
                  <span class="sub-spec">{{ a.specialty }}</span>
                </div>
              </td>
              <td>{{ a.scheduledDateTimeUtc | date:'short' }}</td>
              <td>{{ a.appointmentType }} ({{ a.durationMinutes }}m)</td>
              <td>
                <span class="badge" [ngClass]="{
                  'badge-primary': a.status === 'Scheduled',
                  'badge-success': a.status === 'Completed' || a.status === 'Confirmed',
                  'badge-warning': a.status === 'CheckedIn' || a.status === 'InConsultation',
                  'badge-danger': a.status === 'Cancelled'
                }">{{ a.status }}</span>
              </td>
              <td>{{ a.reasonForVisit }}</td>
              <td>
                <div class="action-buttons">
                  <button *ngIf="canCheckIn(a.status)" class="btn btn-secondary btn-sm" (click)="updateStatus(a.id, 'CheckedIn')">
                    Check-In
                  </button>
                  <button *ngIf="canComplete(a.status)" class="btn btn-primary btn-sm" (click)="updateStatus(a.id, 'Completed')">
                    Complete
                  </button>
                  <button *ngIf="a.status !== 'Cancelled' && a.status !== 'Completed'" class="btn btn-danger btn-sm" (click)="updateStatus(a.id, 'Cancelled')">
                    Cancel
                  </button>
                </div>
              </td>
            </tr>
            <tr *ngIf="appointments.length === 0">
              <td colspan="7" class="empty-cell">No appointments scheduled.</td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Book Appointment Modal -->
      <div *ngIf="showBookingModal" class="modal-overlay">
        <div class="modal-content">
          <div class="modal-header">
            <h3>Schedule Clinical Appointment</h3>
            <button class="close-btn" (click)="showBookingModal = false">&times;</button>
          </div>

          <div class="modal-body">
            <div class="form-group" *ngIf="activeRole !== 'Patient'">
              <label>Select Patient *</label>
              <select [(ngModel)]="bookingForm.patientId" class="modal-input">
                <option value="">-- Choose Patient --</option>
                <option *ngFor="let p of patients" [value]="p.id">{{ p.fullName }} ({{ p.medicalRecordNumber }})</option>
              </select>
            </div>

            <div class="form-group">
              <label>Select Attending Doctor *</label>
              <select [(ngModel)]="bookingForm.doctorId" class="modal-input">
                <option value="">-- Choose Doctor --</option>
                <option *ngFor="let doc of doctors" [value]="doc.id">{{ doc.fullName }} ({{ doc.specialtyName }})</option>
              </select>
            </div>

            <div class="form-row-2">
              <div class="form-group">
                <label>Date &amp; Time *</label>
                <input type="datetime-local" [(ngModel)]="bookingForm.scheduledDateTime" class="modal-input" />
              </div>
              <div class="form-group">
                <label>Duration (Minutes)</label>
                <select [(ngModel)]="bookingForm.durationMinutes" class="modal-input">
                  <option [ngValue]="15">15 Minutes</option>
                  <option [ngValue]="30">30 Minutes (Standard)</option>
                  <option [ngValue]="45">45 Minutes</option>
                  <option [ngValue]="60">60 Minutes</option>
                </select>
              </div>
            </div>

            <div class="form-row-2">
              <div class="form-group">
                <label>Appointment Type</label>
                <select [(ngModel)]="bookingForm.appointmentType" class="modal-input">
                  <option value="InPerson">In-Person Office Visit</option>
                  <option value="Telehealth">Telehealth Video Visit</option>
                  <option value="FollowUp">Follow-up Consultation</option>
                </select>
              </div>
            </div>

            <div class="form-group">
              <label>Reason for Visit *</label>
              <textarea [(ngModel)]="bookingForm.reasonForVisit" rows="2" class="modal-textarea" placeholder="Describe symptoms or purpose of appointment..."></textarea>
            </div>
          </div>

          <div class="modal-footer">
            <button class="btn btn-outline" (click)="showBookingModal = false">Cancel</button>
            <button class="btn btn-primary" (click)="saveAppointment()">Confirm Appointment</button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .appointments-page { display: flex; flex-direction: column; gap: 20px; }
    .page-header {
      display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 12px;
      h2 { font-size: 1.4rem; font-weight: 700; color: #0f172a; margin: 0; }
      .subtitle { color: #64748b; font-size: 0.85rem; margin-top: 2px; }
    }
    .alert-warning { background: #fef3c7; color: #92400e; padding: 12px; border-radius: 8px; font-size: 0.85rem; }
    .patient-cell, .doc-cell { display: flex; flex-direction: column; }
    .sub-mrn, .sub-spec { font-size: 0.75rem; color: #64748b; }
    .action-buttons { display: flex; gap: 6px; }
    .loading-state { display: flex; align-items: center; justify-content: center; gap: 12px; padding: 40px; color: #64748b; }
    .spinner { width: 22px; height: 22px; border: 2px solid #cbd5e1; border-top-color: #0284c7; border-radius: 50%; animation: spin 0.8s linear infinite; }
    @keyframes spin { to { transform: rotate(360deg); } }
    .empty-cell { text-align: center; padding: 40px; color: #94a3b8; }
    .modal-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; }
    .modal-body { display: flex; flex-direction: column; gap: 12px; }
    .form-row-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
    .form-group {
      display: flex; flex-direction: column; gap: 4px;
      label { font-size: 0.82rem; font-weight: 600; color: #334155; }
      .modal-input, .modal-textarea {
        border: 1px solid #cbd5e1; border-radius: 6px; padding: 8px 12px; font-size: 0.9rem;
        &:focus { outline: none; border-color: #0284c7; }
      }
    }
    .modal-footer { display: flex; justify-content: flex-end; gap: 10px; margin-top: 20px; }
  `]
})
export class AppointmentListComponent implements OnInit {
  appointments: Appointment[] = [];
  doctors: Doctor[] = [];
  patients: Patient[] = [];
  isLoading = true;
  showBookingModal = false;
  conflictWarning = '';
  activeRole = '';

  bookingForm: any = {
    patientId: '',
    doctorId: '',
    scheduledDateTime: '',
    durationMinutes: 30,
    appointmentType: 'InPerson',
    reasonForVisit: ''
  };

  constructor(
    private apiService: ApiService,
    public authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.authService.activeRole$.subscribe(r => {
      this.activeRole = r;
      this.cdr.detectChanges();
    });
    this.loadAppointments();

    this.apiService.getDoctors().subscribe({
      next: (res) => {
        this.doctors = (res as any)?.data ?? (res as any)?.Data ?? [];
        this.cdr.detectChanges();
      },
      error: () => {}
    });

    this.apiService.getPatients().subscribe({
      next: (res) => {
        this.patients = (res as any)?.data?.items ?? (res as any)?.Data?.Items ?? (res as any)?.data ?? [];
        this.cdr.detectChanges();
      },
      error: () => {}
    });
  }

  loadAppointments(): void {
    this.isLoading = true;
    this.cdr.detectChanges();
    this.apiService.getAppointments().subscribe({
      next: (res) => {
        this.isLoading = false;
        this.appointments = (res as any)?.data?.items ?? (res as any)?.Data?.Items ?? (res as any)?.data ?? (res as any)?.items ?? [];
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  canCheckIn(status: string): boolean {
    return status === 'Scheduled' || status === 'Confirmed';
  }

  canComplete(status: string): boolean {
    const role = this.authService.activeRoleValue;
    return (role === 'Doctor' || role === 'Admin') && (status === 'CheckedIn' || status === 'InConsultation');
  }

  updateStatus(id: string, status: string): void {
    this.apiService.updateAppointmentStatus(id, status).subscribe({
      next: () => {
        this.loadAppointments();
      },
      error: (err) => {
        alert(err.error?.message || 'Failed to update status.');
        this.cdr.detectChanges();
      }
    });
  }

  openBookingModal(): void {
    const d = new Date();
    d.setHours(d.getHours() + 2);
    d.setMinutes(0);
    this.bookingForm.scheduledDateTime = d.toISOString().slice(0, 16);
    this.showBookingModal = true;
    this.cdr.detectChanges();
  }

  saveAppointment(): void {
    if (!this.bookingForm.doctorId || !this.bookingForm.reasonForVisit || !this.bookingForm.scheduledDateTime) {
      alert('Doctor, appointment time, and reason for visit are required.');
      return;
    }

    const payload = {
      ...this.bookingForm,
      scheduledDateTimeUtc: new Date(this.bookingForm.scheduledDateTime).toISOString()
    };

    this.apiService.createAppointment(payload).subscribe({
      next: () => {
        alert('Appointment successfully booked!');
        this.showBookingModal = false;
        this.loadAppointments();
        this.cdr.detectChanges();
      },
      error: (err) => {
        alert(err.error?.message || 'Conflict detected: Doctor is not available at this time slot.');
        this.cdr.detectChanges();
      }
    });
  }
}
