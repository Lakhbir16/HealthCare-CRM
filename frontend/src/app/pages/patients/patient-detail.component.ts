import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import {
  Patient,
  VitalSign,
  MedicalHistory,
  Allergy,
  Diagnosis,
  ClinicalNote,
  Prescription
} from '../../core/models/app.models';

@Component({
  selector: 'app-patient-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  template: `
    <div *ngIf="isLoading" class="loading-state">
      <div class="spinner"></div>
      <span>Loading patient medical chart...</span>
    </div>

    <div class="patient-chart-page" *ngIf="!isLoading && patient">
      <!-- Chart Header -->
      <div class="chart-header card">
        <div class="header-left">
          <div class="avatar-circle">
            {{ patient.firstName.charAt(0) }}{{ patient.lastName.charAt(0) }}
          </div>
          <div class="header-info">
            <div class="name-mrn">
              <h2>{{ patient.fullName }}</h2>
              <span class="mrn-tag">{{ patient.medicalRecordNumber }}</span>
              <span class="blood-tag">{{ patient.bloodGroup || 'Blood: N/A' }}</span>
            </div>
            <p class="demographics">
              {{ patient.gender }} &bull; DOB: {{ (patient.dateOfBirth | date:'mediumDate') || 'N/A' }} &bull; Phone: {{ patient.phoneNumber || 'N/A' }} &bull; Email: {{ patient.email }}
            </p>
          </div>
        </div>

        <div class="header-right">
          <a routerLink="/patients" class="btn btn-outline btn-sm">&larr; Patient Directory</a>
          <button *ngIf="canRecordVitals" class="btn btn-primary btn-sm" (click)="showVitalsModal = true">
            + Record Vitals
          </button>
        </div>
      </div>

      <!-- Tab Navigation -->
      <div class="tabs-bar">
        <button class="tab-btn" [class.active]="activeTab === 'vitals'" (click)="activeTab = 'vitals'">Vital Signs</button>
        <button class="tab-btn" [class.active]="activeTab === 'diagnoses'" (click)="activeTab = 'diagnoses'">Diagnoses</button>
        <button class="tab-btn" [class.active]="activeTab === 'allergies'" (click)="activeTab = 'allergies'">Allergies</button>
        <button class="tab-btn" [class.active]="activeTab === 'history'" (click)="activeTab = 'history'">Medical History</button>
        <button class="tab-btn" [class.active]="activeTab === 'prescriptions'" (click)="activeTab = 'prescriptions'">Prescriptions</button>
        <button *ngIf="canViewNotes" class="tab-btn" [class.active]="activeTab === 'notes'" (click)="activeTab = 'notes'">Clinical Notes</button>
      </div>

      <!-- Tab Content: Vitals -->
      <div *ngIf="activeTab === 'vitals'" class="tab-pane card">
        <div class="pane-header">
          <h3>Vital Signs History</h3>
        </div>
        <div class="table-container">
          <table class="data-table">
            <thead>
              <tr>
                <th>Date / Time</th>
                <th>BP (mmHg)</th>
                <th>Heart Rate</th>
                <th>Temp (°F)</th>
                <th>SpO2</th>
                <th>BMI</th>
                <th>Recorded By</th>
                <th>Notes</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let v of vitals">
                <td>{{ v.recordedAtUtc | date:'short' }}</td>
                <td><strong>{{ v.bloodPressureSystolic }}/{{ v.bloodPressureDiastolic }}</strong></td>
                <td>{{ v.heartRateBpm }} bpm</td>
                <td>{{ v.temperatureFahrenheit }} °F</td>
                <td>{{ v.oxygenSaturationPercent }}%</td>
                <td>{{ v.bmi || 'N/A' }}</td>
                <td>{{ v.recordedByUserName || 'Clinical Staff' }}</td>
                <td>{{ v.notes || '-' }}</td>
              </tr>
              <tr *ngIf="vitals.length === 0">
                <td colspan="8" class="empty-cell">No vital sign records found.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Tab Content: Diagnoses -->
      <div *ngIf="activeTab === 'diagnoses'" class="tab-pane card">
        <div class="pane-header">
          <h3>Diagnoses (ICD-10)</h3>
        </div>
        <div class="table-container">
          <table class="data-table">
            <thead>
              <tr>
                <th>ICD-10 Code</th>
                <th>Condition Description</th>
                <th>Diagnosed Date</th>
                <th>Doctor</th>
                <th>Status</th>
                <th>Notes</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let d of diagnoses">
                <td><span class="badge badge-primary">{{ d.icd10Code }}</span></td>
                <td><strong>{{ d.description }}</strong></td>
                <td>{{ d.diagnosedAtUtc | date:'mediumDate' }}</td>
                <td>{{ d.doctorName || 'Attending Physician' }}</td>
                <td><span class="badge badge-success">{{ d.status }}</span></td>
                <td>{{ d.notes || '-' }}</td>
              </tr>
              <tr *ngIf="diagnoses.length === 0">
                <td colspan="6" class="empty-cell">No active diagnoses recorded.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Tab Content: Allergies -->
      <div *ngIf="activeTab === 'allergies'" class="tab-pane card">
        <div class="pane-header">
          <h3>Allergies &amp; Adverse Reactions</h3>
        </div>
        <div class="table-container">
          <table class="data-table">
            <thead>
              <tr>
                <th>Allergen</th>
                <th>Severity</th>
                <th>Reaction</th>
                <th>Diagnosed Date</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let a of allergies">
                <td><strong class="text-danger">{{ a.allergen }}</strong></td>
                <td>
                  <span class="badge" [ngClass]="{'badge-danger': a.severity === 'Severe', 'badge-warning': a.severity !== 'Severe'}">
                    {{ a.severity }}
                  </span>
                </td>
                <td>{{ a.reaction || 'Unspecified' }}</td>
                <td>{{ (a.diagnosedDate | date:'mediumDate') || 'N/A' }}</td>
              </tr>
              <tr *ngIf="allergies.length === 0">
                <td colspan="4" class="empty-cell">No known drug or environmental allergies recorded.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Tab Content: Medical History -->
      <div *ngIf="activeTab === 'history'" class="tab-pane card">
        <div class="pane-header">
          <h3>Past Medical History</h3>
        </div>
        <div class="table-container">
          <table class="data-table">
            <thead>
              <tr>
                <th>Condition</th>
                <th>Status</th>
                <th>Diagnosed Date</th>
                <th>Clinical Notes</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let h of medicalHistories">
                <td><strong>{{ h.condition }}</strong></td>
                <td><span class="badge badge-neutral">{{ h.status }}</span></td>
                <td>{{ (h.diagnosedDate | date:'mediumDate') || 'N/A' }}</td>
                <td>{{ h.notes || '-' }}</td>
              </tr>
              <tr *ngIf="medicalHistories.length === 0">
                <td colspan="4" class="empty-cell">No past conditions on record.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Tab Content: Prescriptions -->
      <div *ngIf="activeTab === 'prescriptions'" class="tab-pane card">
        <div class="pane-header">
          <h3>Prescriptions &amp; Medications</h3>
        </div>
        <div class="table-container">
          <table class="data-table">
            <thead>
              <tr>
                <th>Prescription #</th>
                <th>Medication &amp; Dosage</th>
                <th>Frequency</th>
                <th>Doctor</th>
                <th>Status</th>
                <th>Date</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let rx of prescriptions">
                <td><span class="badge badge-neutral">{{ rx.prescriptionNumber }}</span></td>
                <td>
                  <div *ngFor="let item of rx.items">
                    <strong>{{ item.medicationName }}</strong> ({{ item.dosage }})
                  </div>
                </td>
                <td>
                  <div *ngFor="let item of rx.items">
                    {{ item.frequency }}
                  </div>
                </td>
                <td>{{ rx.doctorName }}</td>
                <td><span class="badge badge-success">{{ rx.status }}</span></td>
                <td>{{ rx.prescribedAtUtc | date:'mediumDate' }}</td>
              </tr>
              <tr *ngIf="prescriptions.length === 0">
                <td colspan="6" class="empty-cell">No prescriptions recorded.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Tab Content: Clinical Notes -->
      <div *ngIf="activeTab === 'notes' && canViewNotes" class="tab-pane card">
        <div class="pane-header">
          <h3>Physician Clinical Notes (SOAP)</h3>
        </div>
        <div class="notes-stack">
          <div *ngFor="let n of clinicalNotes" class="note-card card">
            <div class="note-meta">
              <strong>{{ n.doctorName }}</strong> &bull; Note Type: {{ n.noteType }} &bull; {{ n.createdAtUtc | date:'medium' }}
            </div>
            <div class="soap-grid">
              <div class="soap-block">
                <span class="soap-tag">S (Subjective)</span>
                <p>{{ n.subjective || 'None' }}</p>
              </div>
              <div class="soap-block">
                <span class="soap-tag">O (Objective)</span>
                <p>{{ n.objective || 'None' }}</p>
              </div>
              <div class="soap-block">
                <span class="soap-tag">A (Assessment)</span>
                <p>{{ n.assessment || 'None' }}</p>
              </div>
              <div class="soap-block">
                <span class="soap-tag">P (Plan)</span>
                <p>{{ n.plan || 'None' }}</p>
              </div>
            </div>
          </div>
          <div *ngIf="clinicalNotes.length === 0" class="empty-cell">
            No clinical SOAP notes recorded for this patient.
          </div>
        </div>
      </div>

      <!-- Record Vitals Modal -->
      <div *ngIf="showVitalsModal" class="modal-overlay">
        <div class="modal-content">
          <div class="modal-header">
            <h3>Record Vitals for {{ patient.fullName }}</h3>
            <button class="close-btn" (click)="showVitalsModal = false">&times;</button>
          </div>
          <div class="modal-body">
            <div class="form-row-2">
              <div class="form-group">
                <label>BP Systolic (mmHg)</label>
                <input type="number" [(ngModel)]="newVitals.bloodPressureSystolic" class="modal-input" />
              </div>
              <div class="form-group">
                <label>BP Diastolic (mmHg)</label>
                <input type="number" [(ngModel)]="newVitals.bloodPressureDiastolic" class="modal-input" />
              </div>
            </div>
            <div class="form-row-2">
              <div class="form-group">
                <label>Heart Rate (BPM)</label>
                <input type="number" [(ngModel)]="newVitals.heartRateBpm" class="modal-input" />
              </div>
              <div class="form-group">
                <label>Temp (°F)</label>
                <input type="number" step="0.1" [(ngModel)]="newVitals.temperatureFahrenheit" class="modal-input" />
              </div>
            </div>
            <div class="form-row-2">
              <div class="form-group">
                <label>Oxygen Saturation (% SpO2)</label>
                <input type="number" [(ngModel)]="newVitals.oxygenSaturationPercent" class="modal-input" />
              </div>
              <div class="form-group">
                <label>Respiratory Rate</label>
                <input type="number" [(ngModel)]="newVitals.respiratoryRateBpm" class="modal-input" />
              </div>
            </div>
            <div class="form-group">
              <label>Notes</label>
              <textarea [(ngModel)]="newVitals.notes" rows="2" class="modal-textarea"></textarea>
            </div>
          </div>
          <div class="modal-footer">
            <button class="btn btn-outline" (click)="showVitalsModal = false">Cancel</button>
            <button class="btn btn-primary" (click)="saveVitals()">Save Vitals</button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .patient-chart-page { display: flex; flex-direction: column; gap: 20px; }
    .chart-header {
      display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 16px;
      .header-left {
        display: flex; align-items: center; gap: 16px;
        .avatar-circle {
          width: 56px; height: 56px; border-radius: 50%; background: #0284c7; color: #fff;
          font-weight: 700; font-size: 1.3rem; display: flex; align-items: center; justify-content: center;
        }
        .name-mrn {
          display: flex; align-items: center; gap: 10px; flex-wrap: wrap;
          h2 { font-size: 1.4rem; font-weight: 700; color: #0f172a; margin: 0; }
          .mrn-tag { font-family: monospace; font-size: 0.8rem; font-weight: 700; background: #e0f2fe; color: #0369a1; padding: 3px 8px; border-radius: 6px; }
          .blood-tag { font-size: 0.75rem; font-weight: 700; background: #fee2e2; color: #991b1b; padding: 2px 6px; border-radius: 4px; }
        }
        .demographics { font-size: 0.85rem; color: #64748b; margin-top: 4px; }
      }
      .header-right { display: flex; gap: 10px; }
    }
    .tabs-bar {
      display: flex; gap: 4px; border-bottom: 2px solid #e2e8f0;
      .tab-btn {
        padding: 10px 18px; font-size: 0.88rem; font-weight: 600; color: #64748b; background: none; border: none; cursor: pointer;
        border-bottom: 2px solid transparent; margin-bottom: -2px; transition: all 0.2s;
        &:hover { color: #0284c7; }
        &.active { color: #0284c7; border-bottom-color: #0284c7; }
      }
    }
    .tab-pane { padding: 20px; }
    .pane-header { margin-bottom: 16px; h3 { font-size: 1.1rem; color: #1e293b; margin: 0; } }
    .empty-cell { text-align: center; padding: 36px; color: #94a3b8; }
    .notes-stack { display: flex; flex-direction: column; gap: 14px; }
    .note-card {
      padding: 16px; background: #f8fafc; border: 1px solid #e2e8f0;
      .note-meta { font-size: 0.82rem; color: #475569; margin-bottom: 12px; }
      .soap-grid {
        display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 12px;
        .soap-block {
          background: #ffffff; border: 1px solid #e2e8f0; border-radius: 6px; padding: 10px;
          .soap-tag { font-weight: 700; font-size: 0.75rem; color: #0284c7; display: block; margin-bottom: 4px; }
          p { font-size: 0.85rem; color: #334155; margin: 0; }
        }
      }
    }
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
    .loading-state { display: flex; align-items: center; justify-content: center; gap: 12px; padding: 40px; color: #64748b; }
    .spinner { width: 22px; height: 22px; border: 2px solid #cbd5e1; border-top-color: #0284c7; border-radius: 50%; animation: spin 0.8s linear infinite; }
    @keyframes spin { to { transform: rotate(360deg); } }
    .modal-footer { display: flex; justify-content: flex-end; gap: 10px; margin-top: 20px; }
  `]
})
export class PatientDetailComponent implements OnInit {
  patientId = '';
  patient: Patient | null = null;
  activeTab = 'vitals';
  isLoading = true;

  vitals: VitalSign[] = [];
  diagnoses: Diagnosis[] = [];
  allergies: Allergy[] = [];
  medicalHistories: MedicalHistory[] = [];
  prescriptions: Prescription[] = [];
  clinicalNotes: ClinicalNote[] = [];

  showVitalsModal = false;
  newVitals = {
    bloodPressureSystolic: 120,
    bloodPressureDiastolic: 80,
    heartRateBpm: 72,
    temperatureFahrenheit: 98.6,
    oxygenSaturationPercent: 98,
    respiratoryRateBpm: 16,
    notes: ''
  };

  constructor(
    private route: ActivatedRoute,
    private apiService: ApiService,
    public authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.patientId = params['id'];
      if (this.patientId) {
        this.loadPatientData();
      }
    });
  }

  get canRecordVitals(): boolean {
    const role = this.authService.activeRoleValue;
    return ['Doctor', 'Nurse'].includes(role);
  }

  get canViewNotes(): boolean {
    const role = this.authService.activeRoleValue;
    return ['Doctor', 'Nurse', 'Admin'].includes(role);
  }

  loadPatientData(): void {
    this.isLoading = true;
    this.cdr.detectChanges();
    this.apiService.getPatientById(this.patientId).subscribe({
      next: (res) => {
        this.isLoading = false;
        this.patient = (res as any)?.data ?? (res as any)?.Data ?? (res as any);
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });

    this.apiService.getPatientVitals(this.patientId).subscribe({
      next: (res) => {
        this.vitals = (res as any)?.data ?? (res as any)?.Data ?? [];
        this.cdr.detectChanges();
      },
      error: () => {}
    });

    this.apiService.getPatientDiagnoses(this.patientId).subscribe({
      next: (res) => {
        this.diagnoses = (res as any)?.data ?? (res as any)?.Data ?? [];
        this.cdr.detectChanges();
      },
      error: () => {}
    });

    this.apiService.getPatientAllergies(this.patientId).subscribe({
      next: (res) => {
        this.allergies = (res as any)?.data ?? (res as any)?.Data ?? [];
        this.cdr.detectChanges();
      },
      error: () => {}
    });

    this.apiService.getPatientHistory(this.patientId).subscribe({
      next: (res) => {
        this.medicalHistories = (res as any)?.data ?? (res as any)?.Data ?? [];
        this.cdr.detectChanges();
      },
      error: () => {}
    });

    this.apiService.getPrescriptions(this.patientId).subscribe({
      next: (res) => {
        this.prescriptions = (res as any)?.data ?? (res as any)?.Data ?? [];
        this.cdr.detectChanges();
      },
      error: () => {}
    });

    if (this.canViewNotes) {
      this.apiService.getPatientClinicalNotes(this.patientId).subscribe({
        next: (res) => {
          this.clinicalNotes = (res as any)?.data ?? (res as any)?.Data ?? [];
          this.cdr.detectChanges();
        },
        error: () => {}
      });
    }
  }

  saveVitals(): void {
    this.apiService.recordVitals(this.patientId, this.newVitals as any).subscribe({
      next: () => {
        alert('Vitals saved successfully!');
        this.showVitalsModal = false;
        this.apiService.getPatientVitals(this.patientId).subscribe(r => {
          this.vitals = (r as any)?.data ?? (r as any)?.Data ?? [];
          this.cdr.detectChanges();
        });
        this.cdr.detectChanges();
      },
      error: (err) => {
        alert(err.error?.message || 'Failed to record vitals.');
        this.cdr.detectChanges();
      }
    });
  }
}
