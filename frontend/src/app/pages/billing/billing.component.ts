import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { Invoice, Patient } from '../../core/models/app.models';

@Component({
  selector: 'app-billing',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="billing-page">
      <div class="page-header">
        <div>
          <h2>Billing, Insurance &amp; Invoices</h2>
          <p class="subtitle">Itemized clinical statements, insurance adjustments, patient copays, and payments.</p>
        </div>

        <div class="header-actions">
          <button *ngIf="canCreateInvoice" class="btn btn-primary" (click)="showInvoiceModal = true">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="12" y1="5" x2="12" y2="19"></line>
              <line x1="5" y1="12" x2="19" y2="12"></line>
            </svg>
            Create Invoice
          </button>
        </div>
      </div>

      <div *ngIf="isLoading" class="loading-state">
        <div class="spinner"></div>
        <span>Loading invoices and billing statements...</span>
      </div>

      <div *ngIf="!isLoading" class="table-container">
        <table class="data-table">
          <thead>
            <tr>
              <th>Invoice #</th>
              <th>Patient</th>
              <th>Date Issued</th>
              <th>Total Amount</th>
              <th>Insurance Covered</th>
              <th>Patient Balance Due</th>
              <th>Status</th>
              <th>Action</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let inv of invoices">
              <td><span class="badge badge-neutral">{{ inv.invoiceNumber }}</span></td>
              <td><strong>{{ inv.patientName }}</strong></td>
              <td>{{ inv.issueDateUtc | date:'mediumDate' }}</td>
              <td>{{ inv.totalAmount | currency:'USD' }}</td>
              <td><span class="text-green">-{{ inv.insuranceCoveredAmount | currency:'USD' }}</span></td>
              <td>
                <strong [class.text-danger]="inv.status !== 'Paid'">
                  {{ (inv.patientAmountDue - inv.paidAmount) | currency:'USD' }}
                </strong>
              </td>
              <td>
                <span class="badge" [ngClass]="{
                  'badge-success': inv.status === 'Paid',
                  'badge-warning': inv.status === 'PartiallyPaid' || inv.status === 'Pending',
                  'badge-danger': inv.status === 'Overdue'
                }">{{ inv.status }}</span>
              </td>
              <td>
                <button
                  *ngIf="inv.status !== 'Paid'"
                  class="btn btn-outline btn-sm"
                  (click)="openPaymentModal(inv)"
                >
                  Pay / Record Payment
                </button>
              </td>
            </tr>
            <tr *ngIf="invoices.length === 0">
              <td colspan="8" class="empty-cell">No billing invoices found.</td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Record Payment Modal -->
      <div *ngIf="showPaymentModal" class="modal-overlay">
        <div class="modal-content">
          <div class="modal-header">
            <h3>Record Payment for {{ selectedInvoice?.invoiceNumber }}</h3>
            <button class="close-btn" (click)="showPaymentModal = false">&times;</button>
          </div>

          <div class="modal-body">
            <div class="balance-banner">
              <span>Remaining Balance:</span>
              <strong>{{ (selectedInvoice?.patientAmountDue - selectedInvoice?.paidAmount) | currency:'USD' }}</strong>
            </div>

            <div class="form-group">
              <label>Payment Amount ($) *</label>
              <input type="number" [(ngModel)]="paymentForm.amount" class="modal-input" />
            </div>

            <div class="form-group">
              <label>Payment Method</label>
              <select [(ngModel)]="paymentForm.paymentMethod" class="modal-input">
                <option value="CreditCard">Credit / Debit Card</option>
                <option value="Online">Online Healthcare Portal</option>
                <option value="Check">Personal Check</option>
                <option value="Cash">Cash (Front Desk)</option>
              </select>
            </div>

            <div class="form-group">
              <label>Transaction / Reference #</label>
              <input type="text" [(ngModel)]="paymentForm.transactionReference" class="modal-input" placeholder="e.g. TXN-998822" />
            </div>
          </div>

          <div class="modal-footer">
            <button class="btn btn-outline" (click)="showPaymentModal = false">Cancel</button>
            <button class="btn btn-primary" (click)="submitPayment()">Confirm Payment</button>
          </div>
        </div>
      </div>

      <!-- Create Invoice Modal -->
      <div *ngIf="showInvoiceModal" class="modal-overlay">
        <div class="modal-content">
          <div class="modal-header">
            <h3>Generate Billing Invoice</h3>
            <button class="close-btn" (click)="showInvoiceModal = false">&times;</button>
          </div>

          <div class="modal-body">
            <div class="form-group">
              <label>Select Patient *</label>
              <select [(ngModel)]="newInvoice.patientId" class="modal-input">
                <option value="">-- Choose Patient --</option>
                <option *ngFor="let p of patients" [value]="p.id">{{ p.fullName }} ({{ p.medicalRecordNumber }})</option>
              </select>
            </div>

            <div class="form-group">
              <label>Service Description *</label>
              <input type="text" [(ngModel)]="newInvoice.serviceDescription" class="modal-input" placeholder="e.g. Office Consultation - Established (CPT 99214)" />
            </div>

            <div class="form-row-2">
              <div class="form-group">
                <label>Total Charges ($) *</label>
                <input type="number" [(ngModel)]="newInvoice.totalAmount" (ngModelChange)="computeDue()" class="modal-input" />
              </div>
              <div class="form-group">
                <label>Insurance Coverage ($)</label>
                <input type="number" [(ngModel)]="newInvoice.insuranceCoveredAmount" (ngModelChange)="computeDue()" class="modal-input" />
              </div>
            </div>

            <div class="form-group">
              <label>Patient Copay / Balance Due ($)</label>
              <input type="number" [(ngModel)]="newInvoice.patientAmountDue" class="modal-input" readonly />
            </div>
          </div>

          <div class="modal-footer">
            <button class="btn btn-outline" (click)="showInvoiceModal = false">Cancel</button>
            <button class="btn btn-primary" (click)="saveInvoice()">Issue Invoice</button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .billing-page { display: flex; flex-direction: column; gap: 20px; }
    .page-header {
      display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 12px;
      h2 { font-size: 1.4rem; font-weight: 700; color: #0f172a; margin: 0; }
      .subtitle { color: #64748b; font-size: 0.85rem; margin-top: 2px; }
    }
    .text-green { color: #059669; }
    .text-danger { color: #dc2626; }
    .balance-banner {
      background: #eff6ff; border: 1px solid #bfdbfe; border-radius: 8px; padding: 12px;
      display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px;
      span { color: #1e40af; font-size: 0.85rem; }
      strong { color: #1e3a8a; font-size: 1.2rem; }
    }
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
      .modal-input {
        height: 38px; border: 1px solid #cbd5e1; border-radius: 6px; padding: 0 12px; font-size: 0.9rem;
        &:focus { outline: none; border-color: #0284c7; }
      }
    }
    .modal-footer { display: flex; justify-content: flex-end; gap: 10px; margin-top: 20px; }
  `]
})
export class BillingComponent implements OnInit {
  invoices: Invoice[] = [];
  patients: Patient[] = [];
  isLoading = true;

  showPaymentModal = false;
  showInvoiceModal = false;
  selectedInvoice: any = null;

  paymentForm = {
    amount: 0,
    paymentMethod: 'CreditCard',
    transactionReference: ''
  };

  newInvoice = {
    patientId: '',
    serviceDescription: '',
    totalAmount: 200,
    insuranceCoveredAmount: 160,
    patientAmountDue: 40
  };

  constructor(
    private apiService: ApiService,
    public authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadInvoices();
    this.apiService.getPatients().subscribe({
      next: (r) => {
        this.patients = (r as any)?.data?.items ?? (r as any)?.Data?.Items ?? (r as any)?.data ?? [];
        this.cdr.detectChanges();
      },
      error: () => {}
    });
  }

  get canCreateInvoice(): boolean {
    const role = this.authService.activeRoleValue;
    return role === 'Admin' || role === 'Receptionist';
  }

  loadInvoices(): void {
    this.isLoading = true;
    this.cdr.detectChanges();
    this.apiService.getInvoices().subscribe({
      next: (res) => {
        this.isLoading = false;
        this.invoices = (res as any)?.data?.items ?? (res as any)?.Data?.Items ?? (res as any)?.data ?? (res as any)?.items ?? [];
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  computeDue(): void {
    this.newInvoice.patientAmountDue = Math.max(0, this.newInvoice.totalAmount - this.newInvoice.insuranceCoveredAmount);
    this.cdr.detectChanges();
  }

  openPaymentModal(inv: Invoice): void {
    this.selectedInvoice = inv;
    this.paymentForm.amount = inv.patientAmountDue - inv.paidAmount;
    this.paymentForm.transactionReference = `TXN-${Math.floor(100000 + Math.random() * 900000)}`;
    this.showPaymentModal = true;
    this.cdr.detectChanges();
  }

  submitPayment(): void {
    if (this.paymentForm.amount <= 0) {
      alert('Payment amount must be greater than zero.');
      return;
    }

    const payload = {
      invoiceId: this.selectedInvoice.id,
      amount: this.paymentForm.amount,
      paymentMethod: this.paymentForm.paymentMethod,
      transactionReference: this.paymentForm.transactionReference
    };

    this.apiService.recordPayment(payload).subscribe({
      next: () => {
        alert('Payment successfully processed!');
        this.showPaymentModal = false;
        this.loadInvoices();
        this.cdr.detectChanges();
      },
      error: (err) => {
        alert(err.error?.message || 'Failed to process payment.');
        this.cdr.detectChanges();
      }
    });
  }

  saveInvoice(): void {
    if (!this.newInvoice.patientId || !this.newInvoice.serviceDescription) {
      alert('Patient and Service description are required.');
      return;
    }

    const payload = {
      patientId: this.newInvoice.patientId,
      totalAmount: this.newInvoice.totalAmount,
      insuranceCoveredAmount: this.newInvoice.insuranceCoveredAmount,
      patientAmountDue: this.newInvoice.patientAmountDue,
      items: [{
        serviceDescription: this.newInvoice.serviceDescription,
        serviceCode: '99214',
        quantity: 1,
        unitPrice: this.newInvoice.totalAmount,
        totalPrice: this.newInvoice.totalAmount
      }]
    };

    this.apiService.createInvoice(payload).subscribe({
      next: () => {
        alert('Invoice created successfully!');
        this.showInvoiceModal = false;
        this.loadInvoices();
        this.cdr.detectChanges();
      },
      error: (err) => {
        alert(err.error?.message || 'Failed to create invoice.');
        this.cdr.detectChanges();
      }
    });
  }
}
