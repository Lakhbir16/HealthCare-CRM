import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-access-denied',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="status-page">
      <div class="card status-card">
        <div class="icon-danger">
          <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"></circle>
            <line x1="4.93" y1="4.93" x2="19.07" y2="19.07"></line>
          </svg>
        </div>
        <h2>Access Denied (403)</h2>
        <p>You do not have the required role or permission policy to access this clinical/administrative resource.</p>
        <div class="actions">
          <a routerLink="/dashboard" class="btn btn-primary">Return to Dashboard</a>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .status-page { min-height: 70vh; display: flex; align-items: center; justify-content: center; }
    .status-card { text-align: center; max-width: 440px; padding: 40px 24px; }
    .icon-danger { color: #ef4444; margin-bottom: 16px; }
    h2 { font-size: 1.4rem; color: #0f172a; margin-bottom: 8px; }
    p { color: #64748b; font-size: 0.9rem; margin-bottom: 24px; }
    .actions { display: flex; justify-content: center; }
  `]
})
export class AccessDeniedComponent {}

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="status-page">
      <div class="card status-card">
        <div class="icon-info">
          <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"></circle>
            <path d="M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3"></path>
            <line x1="12" y1="17" x2="12.01" y2="17"></line>
          </svg>
        </div>
        <h2>Page Not Found (404)</h2>
        <p>The requested healthcare portal resource could not be found.</p>
        <div class="actions">
          <a routerLink="/dashboard" class="btn btn-primary">Return to Dashboard</a>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .status-page { min-height: 70vh; display: flex; align-items: center; justify-content: center; }
    .status-card { text-align: center; max-width: 440px; padding: 40px 24px; }
    .icon-info { color: #0284c7; margin-bottom: 16px; }
    h2 { font-size: 1.4rem; color: #0f172a; margin-bottom: 8px; }
    p { color: #64748b; font-size: 0.9rem; margin-bottom: 24px; }
    .actions { display: flex; justify-content: center; }
  `]
})
export class NotFoundComponent {}
