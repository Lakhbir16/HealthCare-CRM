import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { LabOrder, Patient } from '../../core/models/app.models';

@Component({
  selector: 'app-laboratory',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="lab-page">
      <div class="page-header">
        <div>
          <h2>Laboratory &amp; Diagnostics</h2>
          <p class="subtitle">Specimen collection tracking, clinical diagnostic panels, and lab results entry.</p>
        </div>

        <button *ngIf="canOrder" class="btn btn-primary" (click)="showOrderModal = true">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="12" y1="5" x2="12" y2="19"></line>
            <line x1="5" y1="12" x2="19" y2="12"></line>
          </svg>
          Order Lab Test
        </button>
      </div>

      <div *ngIf="isLoading" class="loading-state">
        <div class="spinner"></div>
        <span>Loading laboratory orders...</span>
      </div>

      <div *ngIf="!isLoading" class="table-container">
        <table class="data-table">
          <thead>
            <tr>
              <th>Order Number</th>
              <th>Patient</th>
              <th>Doctor</th>
              <th>Tests &amp; Results</th>
              <th>Priority</th>
              <th>Status</th>
              <th>Lab Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let order of labOrders">
              <td><span class="badge badge-neutral">{{ order.orderNumber }}</span></td>
              <td><strong>{{ order.patientName }}</strong></td>
              <td>{{ order.doctorName }}</td>
              <td>
                <div *ngFor="let item of order.items" class="test-item-row">
                  <span class="test-title"><strong>{{ item.testName }}</strong> ({{ item.specimenType }})</span>
                  <div *ngIf="item.results.length > 0" class="results-list">
                    <span *ngFor="let r of item.results" class="res-tag" [class.res-abnormal]="r.isAbnormal">
                      {{ r.parameterName }}: <strong>{{ r.value }} {{ r.unit }}</strong> (Ref: {{ r.referenceRange }})
                    </span>
                  </div>
                  <div *ngIf="item.results.length === 0" class="sub-pending">
                    Awaiting specimen processing
                  </div>
                </div>
              </td>
              <td>
                <span class="badge" [ngClass]="{
                  'badge-danger': order.priority === 'Stat',
                  'badge-warning': order.priority === 'Urgent',
                  'badge-primary': order.priority === 'Routine'
                }">{{ order.priority }}</span>
              </td>
              <td>
                <span class="badge" [ngClass]="{
                  'badge-success': order.status === 'Completed',
                  'badge-warning': order.status === 'InProcess' || order.status === 'Ordered'
                }">{{ order.status }}</span>
              </td>
              <td>
                <button
                  *ngIf="canRecordResult(order)"
                  class="btn btn-secondary btn-sm"
                  (click)="openResultModal(order)"
                >
                  Enter Result
                </button>
              </td>
            </tr>
            <tr *ngIf="labOrders.length === 0">
              <td colspan="7" class="empty-cell">No laboratory orders recorded.</td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Record Lab Result Modal -->
      <div *ngIf="showResultModal" class="modal-overlay">
        <div class="modal-content">
          <div class="modal-header">
            <h3>Enter Diagnostic Result</h3>
            <button class="close-btn" (click)="showResultModal = false">&times;</button>
          </div>

          <div class="modal-body">
            <div class="form-group">
              <label>Parameter Name *</label>
              <input type="text" [(ngModel)]="resultForm.parameterName" class="modal-input" placeholder="e.g. Total Cholesterol" />
            </div>

            <div class="form-row-2">
              <div class="form-group">
                <label>Measured Value *</label>
                <input type="text" [(ngModel)]="resultForm.value" class="modal-input" placeholder="e.g. 185" />
              </div>
              <div class="form-group">
                <label>Unit of Measure</label>
                <input type="text" [(ngModel)]="resultForm.unit" class="modal-input" placeholder="e.g. mg/dL" />
              </div>
            </div>

            <div class="form-row-2">
              <div class="form-group">
                <label>Reference Range</label>
                <input type="text" [(ngModel)]="resultForm.referenceRange" class="modal-input" placeholder="e.g. 125 - 200" />
              </div>
              <div class="form-group">
                <label>Abnormal Flag?</label>
                <select [(ngModel)]="resultForm.isAbnormal" class="modal-input">
                  <option [ngValue]="false">Normal</option>
                  <option [ngValue]="true">Abnormal / High / Low</option>
                </select>
              </div>
            </div>

            <div class="form-group">
              <label>Interpretation Notes</label>
              <textarea [(ngModel)]="resultForm.interpretationNotes" rows="2" class="modal-textarea" placeholder="Technician observations..."></textarea>
            </div>
          </div>

          <div class="modal-footer">
            <button class="btn btn-outline" (click)="showResultModal = false">Cancel</button>
            <button class="btn btn-primary" (click)="saveResult()">Submit Lab Result</button>
          </div>
        </div>
      </div>

      <!-- Order Lab Test Modal -->
      <div *ngIf="showOrderModal" class="modal-overlay">
        <div class="modal-content">
          <div class="modal-header">
            <h3>Order Laboratory Diagnostic Test</h3>
            <button class="close-btn" (click)="showOrderModal = false">&times;</button>
          </div>

          <div class="modal-body">
            <div class="form-group">
              <label>Select Patient *</label>
              <select [(ngModel)]="orderForm.patientId" class="modal-input">
                <option value="">-- Choose Patient --</option>
                <option *ngFor="let p of patients" [value]="p.id">{{ p.fullName }} ({{ p.medicalRecordNumber }})</option>
              </select>
            </div>

            <div class="form-row-2">
              <div class="form-group">
                <label>Test Panel Name *</label>
                <input type="text" [(ngModel)]="orderForm.testName" class="modal-input" placeholder="e.g. Lipid Panel" />
              </div>
              <div class="form-group">
                <label>Specimen Type</label>
                <select [(ngModel)]="orderForm.specimenType" class="modal-input">
                  <option value="Blood">Blood (Venipuncture)</option>
                  <option value="Urine">Urine Sample</option>
                  <option value="Swab">Nasopharyngeal Swab</option>
                </select>
              </div>
            </div>

            <div class="form-group">
              <label>Priority</label>
              <select [(ngModel)]="orderForm.priority" class="modal-input">
                <option value="Routine">Routine</option>
                <option value="Urgent">Urgent</option>
                <option value="Stat">Stat (Immediate)</option>
              </select>
            </div>

            <div class="form-group">
              <label>Clinical Indication</label>
              <textarea [(ngModel)]="orderForm.clinicalIndication" rows="2" class="modal-textarea" placeholder="Reasons for ordering..."></textarea>
            </div>
          </div>

          <div class="modal-footer">
            <button class="btn btn-outline" (click)="showOrderModal = false">Cancel</button>
            <button class="btn btn-primary" (click)="saveOrder()">Submit Order</button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .lab-page { display: flex; flex-direction: column; gap: 20px; }
    .page-header {
      display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 12px;
      h2 { font-size: 1.4rem; font-weight: 700; color: #0f172a; margin: 0; }
      .subtitle { color: #64748b; font-size: 0.85rem; margin-top: 2px; }
    }
    .test-item-row { margin-bottom: 6px; font-size: 0.85rem; }
    .results-list { display: flex; flex-direction: column; gap: 4px; margin-top: 4px; }
    .res-tag {
      font-size: 0.75rem; background: #f1f5f9; padding: 2px 6px; border-radius: 4px; color: #334155;
      &.res-abnormal { background: #fee2e2; color: #b91c1c; font-weight: 700; }
    }
    .sub-pending { font-size: 0.75rem; color: #94a3b8; font-style: italic; }
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
export class LaboratoryComponent implements OnInit {
  labOrders: LabOrder[] = [];
  patients: Patient[] = [];
  isLoading = true;

  showResultModal = false;
  showOrderModal = false;
  selectedOrderItemId = '';

  resultForm = {
    parameterName: '',
    value: '',
    unit: '',
    referenceRange: '',
    isAbnormal: false,
    interpretationNotes: ''
  };

  orderForm = {
    patientId: '',
    testName: '',
    specimenType: 'Blood',
    priority: 'Routine',
    clinicalIndication: ''
  };

  constructor(
    private apiService: ApiService,
    public authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadOrders();
    this.apiService.getPatients().subscribe({
      next: (r) => {
        this.patients = (r as any)?.data?.items ?? (r as any)?.Data?.Items ?? (r as any)?.data ?? [];
        this.cdr.detectChanges();
      },
      error: () => {}
    });
  }

  get canOrder(): boolean {
    const role = this.authService.activeRoleValue;
    return role === 'Doctor' || role === 'Admin';
  }

  canRecordResult(order: LabOrder): boolean {
    const role = this.authService.activeRoleValue;
    return (role === 'Doctor' || role === 'Nurse' || role === 'Admin') && order.status !== 'Completed';
  }

  loadOrders(): void {
    this.isLoading = true;
    this.cdr.detectChanges();
    this.apiService.getLabOrders().subscribe({
      next: (res) => {
        this.isLoading = false;
        this.labOrders = (res as any)?.data ?? (res as any)?.Data ?? [];
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  openResultModal(order: LabOrder): void {
    if (order.items && order.items.length > 0) {
      this.selectedOrderItemId = order.items[0].id;
      this.showResultModal = true;
      this.cdr.detectChanges();
    }
  }

  saveResult(): void {
    if (!this.resultForm.parameterName || !this.resultForm.value) {
      alert('Parameter Name and Value are required.');
      return;
    }

    this.apiService.recordLabResult(this.selectedOrderItemId, this.resultForm).subscribe({
      next: () => {
        alert('Lab result recorded!');
        this.showResultModal = false;
        this.loadOrders();
        this.cdr.detectChanges();
      },
      error: (err) => {
        alert(err.error?.message || 'Failed to record result.');
        this.cdr.detectChanges();
      }
    });
  }

  saveOrder(): void {
    if (!this.orderForm.patientId || !this.orderForm.testName) {
      alert('Patient and Test Name are required.');
      return;
    }

    const doctorId = this.authService.currentUserValue?.doctorId || this.authService.currentUserValue?.id || '00000000-0000-0000-0000-000000000000';

    const payload = {
      patientId: this.orderForm.patientId,
      priority: this.orderForm.priority,
      clinicalIndication: this.orderForm.clinicalIndication,
      items: [{
        testName: this.orderForm.testName,
        testCode: '80050',
        specimenType: this.orderForm.specimenType
      }]
    };

    this.apiService.createLabOrder(payload, doctorId).subscribe({
      next: () => {
        alert('Lab order placed successfully!');
        this.showOrderModal = false;
        this.loadOrders();
        this.cdr.detectChanges();
      },
      error: (err) => {
        alert(err.error?.message || 'Failed to place order.');
        this.cdr.detectChanges();
      }
    });
  }
}
