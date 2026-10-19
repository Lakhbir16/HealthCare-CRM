import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../core/services/api.service';

@Component({
  selector: 'app-audit-logs',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="audit-page">
      <div class="page-header">
        <div>
          <h2>Security &amp; HIPAA Audit Logs</h2>
          <p class="subtitle">Complete chronological trace of authentication, clinical record access, and modifications.</p>
        </div>

        <button class="btn btn-outline btn-sm" (click)="loadLogs()">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M21.5 2v6h-6M21.34 15.57a10 10 0 1 1-.57-8.38l5.67-5.67"></path>
          </svg>
          Refresh Logs
        </button>
      </div>

      <div class="compliance-banner card">
        <div class="shield-icon">
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"></path>
          </svg>
        </div>
        <div class="shield-text">
          <strong>HIPAA Safeguards &sect; 164.312(b) Audit Controls:</strong>
          <span>All logins, role changes, patient record views, prescription writes, and OTP events are recorded with immutable UTC timestamps and user attribution.</span>
        </div>
      </div>

      <div *ngIf="isLoading" class="loading-state">
        <div class="spinner"></div>
        <span>Retrieving audit event stream...</span>
      </div>

      <div *ngIf="!isLoading" class="table-container">
        <table class="data-table">
          <thead>
            <tr>
              <th>Timestamp (UTC)</th>
              <th>Action</th>
              <th>Entity</th>
              <th>Entity ID</th>
              <th>Details &amp; Audit Trail</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let log of logs">
              <td><span class="timestamp-tag">{{ log.timestampUtc | date:'medium' }}</span></td>
              <td>
                <span class="badge" [ngClass]="{
                  'badge-success': log.action.includes('SUCCESS') || log.action.includes('CREATED'),
                  'badge-danger': log.action.includes('FAILED') || log.action.includes('LOCKED') || log.action.includes('UNAUTHORIZED'),
                  'badge-primary': log.action.includes('OTP') || log.action.includes('ROLE'),
                  'badge-neutral': !log.action.includes('SUCCESS') && !log.action.includes('FAILED')
                }">{{ log.action }}</span>
              </td>
              <td><strong>{{ log.entityName || '-' }}</strong></td>
              <td><span class="guid-tag">{{ log.entityId || '-' }}</span></td>
              <td>{{ log.details || '-' }}</td>
            </tr>
            <tr *ngIf="logs.length === 0">
              <td colspan="5" class="empty-cell">No audit log records recorded yet.</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  `,
  styles: [`
    .audit-page { display: flex; flex-direction: column; gap: 20px; }
    .page-header {
      display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 12px;
      h2 { font-size: 1.4rem; font-weight: 700; color: #0f172a; margin: 0; }
      .subtitle { color: #64748b; font-size: 0.85rem; margin-top: 2px; }
    }
    .compliance-banner {
      display: flex; align-items: center; gap: 14px; background: #f0fdf4; border: 1px solid #bbf7d0; padding: 14px;
      .shield-icon { color: #16a34a; flex-shrink: 0; }
      .shield-text {
        display: flex; flex-direction: column; font-size: 0.82rem; color: #166534;
        strong { font-weight: 700; margin-bottom: 2px; }
      }
    }
    .timestamp-tag { font-family: monospace; font-size: 0.8rem; color: #334155; }
    .guid-tag { font-family: monospace; font-size: 0.72rem; color: #64748b; }
    .loading-state { display: flex; align-items: center; justify-content: center; gap: 12px; padding: 40px; color: #64748b; }
    .spinner { width: 22px; height: 22px; border: 2px solid #cbd5e1; border-top-color: #0284c7; border-radius: 50%; animation: spin 0.8s linear infinite; }
    @keyframes spin { to { transform: rotate(360deg); } }
    .empty-cell { text-align: center; padding: 40px; color: #94a3b8; }
  `]
})
export class AuditLogsComponent implements OnInit {
  logs: any[] = [];
  isLoading = true;

  constructor(
    private apiService: ApiService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadLogs();
  }

  loadLogs(): void {
    this.isLoading = true;
    this.cdr.detectChanges();
    this.apiService.getAuditLogs().subscribe({
      next: (res) => {
        this.isLoading = false;
        this.logs = (res as any)?.data?.items ?? (res as any)?.Data?.Items ?? (res as any)?.data ?? (res as any)?.items ?? [];
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }
}
