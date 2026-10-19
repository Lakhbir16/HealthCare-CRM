import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  template: `
    <div class="auth-page">
      <div class="auth-card">
        <div class="card-header">
          <div class="icon-circle">
            <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M12 2a10 10 0 1 0 10 10H12V2z"></path>
              <path d="M12 12 2.1 10.5"></path>
              <path d="M12 12l4.9 8.5"></path>
            </svg>
          </div>
          <h2>Create New Password</h2>
          <p class="subtitle">Enter a new secure password for <strong>{{ email }}</strong>.</p>
        </div>

        <div *ngIf="errorMessage" class="alert alert-danger">
          {{ errorMessage }}
        </div>

        <div *ngIf="successMessage" class="alert alert-success">
          {{ successMessage }}
        </div>

        <form [formGroup]="resetForm" (ngSubmit)="onSubmit()" class="auth-form">
          <div class="form-group">
            <label for="newPassword">New Password</label>
            <div class="input-wrapper">
              <input
                [type]="showPassword ? 'text' : 'password'"
                id="newPassword"
                formControlName="newPassword"
                placeholder="Enter new password"
                [class.is-invalid]="resetForm.get('newPassword')?.invalid && resetForm.get('newPassword')?.touched"
              />
              <button type="button" class="eye-toggle" (click)="showPassword = !showPassword">
                <svg *ngIf="!showPassword" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path>
                  <circle cx="12" cy="12" r="3"></circle>
                </svg>
                <svg *ngIf="showPassword" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <line x1="1" y1="1" x2="23" y2="23"></line>
                  <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"></path>
                </svg>
              </button>
            </div>
            <div class="password-guidance">
              Min. 8 characters, uppercase, lowercase, number, and special symbol.
            </div>
          </div>

          <div class="form-group">
            <label for="confirmPassword">Confirm New Password</label>
            <div class="input-wrapper">
              <input
                [type]="showPassword ? 'text' : 'password'"
                id="confirmPassword"
                formControlName="confirmPassword"
                placeholder="Confirm new password"
                [class.is-invalid]="resetForm.hasError('mismatch') && resetForm.get('confirmPassword')?.touched"
              />
            </div>
            <div *ngIf="resetForm.hasError('mismatch') && resetForm.get('confirmPassword')?.touched" class="field-error">
              Passwords do not match.
            </div>
          </div>

          <button type="submit" class="btn btn-primary submit-btn" [disabled]="resetForm.invalid || isLoading">
            <span *ngIf="isLoading" class="spinner"></span>
            <span *ngIf="!isLoading">Update Password &amp; Sign In</span>
          </button>
        </form>

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
    .card-header { text-align: center; margin-bottom: 24px; }
    .icon-circle {
      width: 56px; height: 56px;
      background: #e0f2fe; color: #0284c7;
      border-radius: 14px; display: inline-flex;
      align-items: center; justify-content: center;
      margin-bottom: 12px;
    }
    h2 { font-size: 1.4rem; font-weight: 700; color: #0f172a; }
    .subtitle { color: #64748b; font-size: 0.85rem; margin-top: 6px; }
    .alert {
      padding: 12px; border-radius: 8px; font-size: 0.85rem; margin-bottom: 16px;
      &-danger { background: #fee2e2; color: #991b1b; }
      &-success { background: #d1fae5; color: #065f46; }
    }
    .auth-form { display: flex; flex-direction: column; gap: 16px; }
    .form-group {
      display: flex; flex-direction: column; gap: 6px;
      label { font-size: 0.85rem; font-weight: 600; color: #334155; }
      .input-wrapper {
        position: relative; display: flex; align-items: center;
        input {
          width: 100%; height: 44px; padding: 0 14px;
          border: 1px solid #cbd5e1; border-radius: 8px; font-size: 0.95rem;
          &:focus { outline: none; border-color: #0284c7; box-shadow: 0 0 0 3px rgba(2, 132, 199, 0.15); }
          &.is-invalid { border-color: #ef4444; }
        }
        .eye-toggle {
          position: absolute; right: 12px; color: #64748b; display: flex; padding: 4px;
          &:hover { color: #0f172a; }
        }
      }
      .password-guidance { font-size: 0.75rem; color: #64748b; margin-top: 2px; }
      .field-error { font-size: 0.75rem; color: #ef4444; }
    }
    .submit-btn { height: 44px; font-weight: 600; font-size: 0.95rem; margin-top: 6px; }
    .spinner {
      width: 18px; height: 18px; border: 2px solid rgba(255, 255, 255, 0.3);
      border-top-color: #fff; border-radius: 50%; animation: spin 0.8s linear infinite;
    }
    @keyframes spin { to { transform: rotate(360deg); } }
    .card-footer { margin-top: 20px; text-align: center; .back-link { color: #64748b; font-size: 0.85rem; text-decoration: none; font-weight: 500; &:hover { color: #0284c7; } } }
  `]
})
export class ResetPasswordComponent implements OnInit {
  email = '';
  resetToken = '';
  resetForm: FormGroup;
  showPassword = false;
  isLoading = false;
  errorMessage = '';
  successMessage = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private fb: FormBuilder,
    private authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {
    this.resetForm = this.fb.group({
      newPassword: ['', [
        Validators.required,
        Validators.minLength(8),
        Validators.pattern('^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[@$!%*?&#^()_+\\-=\\[\\]{};:\'",.<>\\/])[A-Za-z\\d@$!%*?&#^()_+\\-=\\[\\]{};:\'",.<>\\/]{8,}$')
      ]],
      confirmPassword: ['', [Validators.required]]
    }, { validators: this.passwordMatchValidator });
  }

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      this.email = params['email'] || '';
      this.resetToken = params['resetToken'] || '';
      if (!this.email || !this.resetToken) {
        this.router.navigate(['/forgot-password']);
      }
      this.cdr.detectChanges();
    });
  }

  passwordMatchValidator(g: FormGroup) {
    return g.get('newPassword')?.value === g.get('confirmPassword')?.value
      ? null : { mismatch: true };
  }

  onSubmit(): void {
    if (this.resetForm.invalid) {
      this.resetForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.cdr.detectChanges();

    const payload = {
      email: this.email,
      resetToken: this.resetToken,
      newPassword: this.resetForm.value.newPassword,
      confirmPassword: this.resetForm.value.confirmPassword
    };

    this.authService.resetPassword(payload).subscribe({
      next: (res) => {
        this.isLoading = false;
        this.successMessage = 'Password reset successful! Redirecting to login...';
        this.cdr.detectChanges();
        setTimeout(() => {
          this.router.navigate(['/login']);
        }, 1200);
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.message || 'Password reset failed or token expired.';
        this.cdr.detectChanges();
      }
    });
  }
}
