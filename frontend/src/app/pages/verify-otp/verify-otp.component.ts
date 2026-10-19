import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-verify-otp',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  template: `
    <div class="auth-page">
      <div class="auth-card">
        <div class="card-header">
          <div class="timer-badge" [class.timer-expired]="timeLeft === 0" [class.timer-warning]="timeLeft > 0 && timeLeft <= 10">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="10"></circle>
              <polyline points="12 6 12 12 16 14"></polyline>
            </svg>
            <span class="seconds">{{ timeLeft }}s</span>
          </div>
          <h2>Enter 6-Digit OTP</h2>
          <p class="subtitle">
            A temporary verification code was sent to <strong>{{ email }}</strong>.<br />
            Strict expiration enforced: <strong>30 seconds</strong>.
          </p>
        </div>

        <div *ngIf="errorMessage" class="alert alert-danger">
          {{ errorMessage }}
        </div>

        <div *ngIf="successMessage" class="alert alert-success">
          {{ successMessage }}
        </div>

        <div *ngIf="timeLeft === 0" class="alert alert-warning">
          <strong>Code Expired!</strong> The 30-second window has elapsed. Please click Resend OTP below to generate a new secure code.
        </div>

        <form [formGroup]="otpForm" (ngSubmit)="onVerify()" class="auth-form">
          <div class="form-group">
            <label for="otp">One-Time Password (OTP)</label>
            <input
              type="text"
              id="otp"
              formControlName="otp"
              placeholder="000000"
              maxlength="6"
              class="otp-input"
              [class.is-invalid]="otpForm.get('otp')?.invalid && otpForm.get('otp')?.touched"
              [disabled]="timeLeft === 0 || isLoading"
            />
            <div *ngIf="otpForm.get('otp')?.invalid && otpForm.get('otp')?.touched" class="field-error">
              Please enter a 6-digit numeric OTP code.
            </div>
          </div>

          <div class="timer-progress-bar">
            <div class="progress-fill" [style.width.%]="(timeLeft / 30) * 100" [class.progress-low]="timeLeft <= 10"></div>
          </div>

          <button
            type="submit"
            class="btn btn-primary submit-btn"
            [disabled]="timeLeft === 0 || otpForm.invalid || isLoading"
          >
            <span *ngIf="isLoading" class="spinner"></span>
            <span *ngIf="!isLoading">Verify OTP</span>
          </button>
        </form>

        <div class="resend-section">
          <span>Didn't receive or code expired?</span>
          <button
            type="button"
            class="btn btn-outline btn-sm resend-btn"
            [disabled]="isResending || (timeLeft > 0 && timeLeft < 28)"
            (click)="onResend()"
          >
            <span *ngIf="isResending" class="spinner spinner-dark"></span>
            <span *ngIf="!isResending">Resend New OTP</span>
          </button>
        </div>

        <div class="dev-hint">
          <strong>Development Mode:</strong> View your active OTP code in the 
          <a routerLink="/dev-mailbox" [queryParams]="{ recipient: email }" target="_blank" class="dev-link">Development Mailbox</a>.
        </div>

        <div class="card-footer">
          <a routerLink="/login" class="back-link">&larr; Back to Login</a>
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
      max-width: 480px;
      background: #ffffff;
      border-radius: 16px;
      border: 1px solid #e2e8f0;
      box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.08);
      padding: 32px;
    }
    .card-header {
      text-align: center;
      margin-bottom: 20px;
    }
    .timer-badge {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      padding: 6px 14px;
      border-radius: 9999px;
      background: #e0f2fe;
      color: #0284c7;
      font-weight: 700;
      font-size: 1rem;
      margin-bottom: 14px;
      transition: all 0.3s;
      &.timer-warning { background: #fef3c7; color: #b45309; }
      &.timer-expired { background: #fee2e2; color: #b91c1c; }
    }
    h2 { font-size: 1.4rem; font-weight: 700; color: #0f172a; }
    .subtitle { color: #64748b; font-size: 0.85rem; margin-top: 6px; }
    .alert {
      padding: 12px;
      border-radius: 8px;
      font-size: 0.85rem;
      margin-bottom: 16px;
      &-danger { background: #fee2e2; color: #991b1b; }
      &-success { background: #d1fae5; color: #065f46; }
      &-warning { background: #fef3c7; color: #92400e; }
    }
    .auth-form { display: flex; flex-direction: column; gap: 16px; }
    .form-group {
      display: flex;
      flex-direction: column;
      gap: 6px;
      label { font-size: 0.85rem; font-weight: 600; color: #334155; text-align: center; }
      .otp-input {
        height: 52px;
        text-align: center;
        font-size: 1.5rem;
        font-weight: 700;
        letter-spacing: 0.4em;
        border: 2px solid #cbd5e1;
        border-radius: 10px;
        color: #0f172a;
        &:focus { outline: none; border-color: #0284c7; box-shadow: 0 0 0 4px rgba(2, 132, 199, 0.15); }
        &.is-invalid { border-color: #ef4444; }
      }
      .field-error { font-size: 0.75rem; color: #ef4444; text-align: center; }
    }
    .timer-progress-bar {
      width: 100%;
      height: 6px;
      background: #f1f5f9;
      border-radius: 9999px;
      overflow: hidden;
      margin-top: -6px;
      .progress-fill {
        height: 100%;
        background: #0284c7;
        transition: width 1s linear;
        &.progress-low { background: #ef4444; }
      }
    }
    .submit-btn { height: 44px; font-weight: 600; font-size: 0.95rem; }
    .resend-section {
      margin-top: 18px;
      display: flex;
      align-items: center;
      justify-content: space-between;
      font-size: 0.85rem;
      color: #64748b;
    }
    .spinner {
      width: 18px; height: 18px;
      border: 2px solid rgba(255, 255, 255, 0.3);
      border-top-color: #fff; border-radius: 50%;
      animation: spin 0.8s linear infinite;
    }
    .spinner-dark {
      border: 2px solid rgba(15, 23, 42, 0.2);
      border-top-color: #0f172a;
    }
    @keyframes spin { to { transform: rotate(360deg); } }
    .dev-hint {
      margin-top: 20px;
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      padding: 10px 14px;
      font-size: 0.8rem;
      color: #475569;
      .dev-link { color: #0284c7; font-weight: 600; text-decoration: none; &:hover { text-decoration: underline; } }
    }
    .card-footer {
      margin-top: 18px;
      text-align: center;
      .back-link { color: #64748b; font-size: 0.85rem; text-decoration: none; font-weight: 500; &:hover { color: #0284c7; } }
    }
  `]
})
export class VerifyOtpComponent implements OnInit, OnDestroy {
  email = '';
  otpForm: FormGroup;
  timeLeft = 30;
  timerInterval: any;
  isLoading = false;
  isResending = false;
  errorMessage = '';
  successMessage = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private fb: FormBuilder,
    private authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {
    this.otpForm = this.fb.group({
      otp: ['', [Validators.required, Validators.pattern('^[0-9]{6}$')]]
    });
  }

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      this.email = params['email'] || '';
      if (!this.email) {
        this.router.navigate(['/forgot-password']);
      } else {
        this.startTimer(30);
      }
      this.cdr.detectChanges();
    });
  }

  ngOnDestroy(): void {
    this.stopTimer();
  }

  startTimer(seconds: number): void {
    this.stopTimer();
    this.timeLeft = seconds;
    this.cdr.detectChanges();
    this.timerInterval = setInterval(() => {
      if (this.timeLeft > 0) {
        this.timeLeft--;
        this.cdr.detectChanges();
      } else {
        this.stopTimer();
        this.errorMessage = 'OTP code has expired. Please request a new 30-second code.';
        this.cdr.detectChanges();
      }
    }, 1000);
  }

  stopTimer(): void {
    if (this.timerInterval) {
      clearInterval(this.timerInterval);
      this.timerInterval = null;
    }
  }

  onVerify(): void {
    if (this.otpForm.invalid || this.timeLeft === 0) return;

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.cdr.detectChanges();

    const otp = this.otpForm.value.otp;

    this.authService.verifyOtp(this.email, otp).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success && res.data?.isValid) {
          this.stopTimer();
          this.successMessage = 'OTP verified! Redirecting to set your new password...';
          this.cdr.detectChanges();
          setTimeout(() => {
            this.router.navigate(['/reset-password'], {
              queryParams: {
                email: this.email,
                resetToken: res.data.resetToken
              }
            });
          }, 800);
        } else {
          this.errorMessage = res.message || 'Verification failed.';
          this.cdr.detectChanges();
        }
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.message || 'Invalid or expired OTP.';
        this.cdr.detectChanges();
      }
    });
  }

  onResend(): void {
    this.isResending = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.cdr.detectChanges();

    this.authService.resendOtp(this.email).subscribe({
      next: (res) => {
        this.isResending = false;
        this.otpForm.reset();
        this.startTimer(30);
        this.successMessage = 'A fresh 30-second OTP has been generated.';
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isResending = false;
        this.errorMessage = err.error?.message || 'Failed to resend OTP.';
        this.cdr.detectChanges();
      }
    });
  }
}
