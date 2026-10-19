import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent {
  loginForm: FormGroup;
  showPassword = false;
  isLoading = false;
  errorMessage = '';

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required]],
      rememberMe: [false]
    });
  }

  togglePassword(): void {
    this.showPassword = !this.showPassword;
    this.cdr.detectChanges();
  }

  quickFill(role: string): void {
    switch (role) {
      case 'admin':
        this.loginForm.patchValue({ email: 'admin@healthcare.com', password: 'Admin@12345' });
        break;
      case 'doctor':
        this.loginForm.patchValue({ email: 'doctor.smith@healthcare.com', password: 'Doctor@12345' });
        break;
      case 'nurse':
        this.loginForm.patchValue({ email: 'nurse.sarah@healthcare.com', password: 'Nurse@12345' });
        break;
      case 'receptionist':
        this.loginForm.patchValue({ email: 'receptionist@healthcare.com', password: 'Recep@12345' });
        break;
      case 'patient':
        this.loginForm.patchValue({ email: 'patient.john@healthcare.com', password: 'Patient@12345' });
        break;
    }
    this.cdr.detectChanges();
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    this.authService.login(this.loginForm.value).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success) {
          this.router.navigate(['/dashboard']);
        } else {
          this.errorMessage = res.message || 'Login failed.';
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.message || 'Invalid email or password.';
        this.cdr.detectChanges();
      }
    });
  }
}
