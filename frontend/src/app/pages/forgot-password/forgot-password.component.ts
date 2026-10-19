import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  template: `
    <div class="auth-page">
      <div class="auth-card">
        <div class="card-header">
          <div class="icon-circle">
            <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
              <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
            </svg>
          </div>
          <h2>Reset Password</h2>
          <p class="subtitle">Enter your registered email to receive a secure 30-second One-Time Password (OTP).</p>
        </div>

        <div *ngIf="errorMessage" class="alert alert-danger">
          {{ errorMessage }}
        </div>

        <div *ngIf="successMessage" class="alert alert-success">
          {{ successMessage }}
        </div>

        <form [formGroup]="form" (ngSubmit)="onSubmit()" class="auth-form">
          <div class="form-group">
            <label for="email">Account Email</label>
            <input
              type="email"
              id="email"
              formControlName="email"
              placeholder="e.g. doctor.smith@healthcare.com"
              [class.is-invalid]="form.get('email')?.invalid && form.get('email')?.touched"
            />
            <div *ngIf="form.get('email')?.invalid && form.get('email')?.touched" class="field-error">
              Please enter a valid email address.
            </div>
          </div>

          <button type="submit" class="btn btn-primary submit-btn" [disabled]="isLoading">
            <span *ngIf="isLoading" class="spinner"></span>
            <span *ngIf="!isLoading">Send 30-Second OTP</span>
          </button>
        </form>

        <div class="dev-hint">
          <strong>Testing Tip:</strong> In development, view your OTP message in the 
          <a routerLink="/dev-mailbox" target="_blank" class="dev-link">Development Mailbox</a>.
        </div>

        <div class="card-footer">
          <a routerLink="/login" class="back-link">&larr; Return to Sign In</a>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .auth-page {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: radial-gradient(circle at top, #f0f9ff 0%, #e2e8f0 100%);
      padding: 24px;
    }
    .auth-card {
      width: 100%;
      max-width: 460px;
      background: #ffffff;
      border-radius: 16px;
      border: 1px solid #e2e8f0;
      box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.08);
      padding: 32px;
    }
    .card-header {
      text-align: center;
      margin-bottom: 24px;
    }
    .icon-circle {
      width: 56px;
      height: 56px;
      background: #e0f2fe;
      color: #0284c7;
      border-radius: 14px;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      margin-bottom: 12px;
    }
    h2 { font-size: 1.4rem; font-weight: 700; color: #0f172a; }
    .subtitle { color: #64748b; font-size: 0.875rem; margin-top: 6px; }
    .alert {
      padding: 12px;
      border-radius: 8px;
      font-size: 0.85rem;
      margin-bottom: 18px;
      &-danger { background: #fee2e2; color: #991b1b; }
      &-success { background: #d1fae5; color: #065f46; }
    }
    .auth-form { display: flex; flex-direction: column; gap: 16px; }
    .form-group {
      display: flex;
      flex-direction: column;
      gap: 6px;
      label { font-size: 0.85rem; font-weight: 600; color: #334155; }
      input {
        height: 44px;
        padding: 0 14px;
        border: 1px solid #cbd5e1;
        border-radius: 8px;
        font-size: 0.95rem;
        &:focus { outline: none; border-color: #0284c7; box-shadow: 0 0 0 3px rgba(2, 132, 199, 0.15); }
        &.is-invalid { border-color: #ef4444; }
      }
      .field-error { font-size: 0.75rem; color: #ef4444; }
    }
    .submit-btn { height: 44px; font-weight: 600; font-size: 0.95rem; }
    .spinner {
      width: 18px; height: 18px;
      border: 2px solid rgba(255, 255, 255, 0.3);
      border-top-color: #fff; border-radius: 50%;
      animation: spin 0.8s linear infinite;
    }
    @keyframes spin { to { transform: rotate(360deg); } }
    .dev-hint {
      margin-top: 18px;
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      padding: 10px 14px;
      font-size: 0.8rem;
      color: #475569;
      .dev-link { color: #0284c7; font-weight: 600; text-decoration: none; &:hover { text-decoration: underline; } }
    }
    .card-footer {
      margin-top: 20px;
      text-align: center;
      .back-link { color: #64748b; font-size: 0.85rem; text-decoration: none; font-weight: 500; &:hover { color: #0284c7; } }
    }
  `]
})
export class ForgotPasswordComponent {
  form: FormGroup;
  isLoading = false;
  errorMessage = '';
  successMessage = '';

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]]
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.cdr.detectChanges();

    const email = this.form.value.email;

    this.authService.requestOtp(email).subscribe({
      next: (res) => {
        this.isLoading = false;
        this.successMessage = res.message;
        this.cdr.detectChanges();
        setTimeout(() => {
          this.router.navigate(['/verify-otp'], {
            queryParams: { email }
          });
        }, 800);
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.message || 'Failed to send OTP. Please try again.';
        this.cdr.detectChanges();
      }
    });
  }
}
