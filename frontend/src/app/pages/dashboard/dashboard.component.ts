import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
import { ApiService } from '../../core/services/api.service';
import { DashboardStats, UserProfile } from '../../core/models/app.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {
  currentUser: UserProfile | null = null;
  activeRole = '';
  stats: DashboardStats | null = null;
  isLoading = true;

  // Modals state
  showVitalsModal = false;
  showAppointmentModal = false;

  // New Vitals Form
  vitalsForm = {
    patientId: '',
    bloodPressureSystolic: 120,
    bloodPressureDiastolic: 80,
    heartRateBpm: 72,
    temperatureFahrenheit: 98.6,
    respiratoryRateBpm: 16,
    oxygenSaturationPercent: 98,
    heightInches: 68,
    weightPounds: 165,
    notes: ''
  };

  constructor(
    public authService: AuthService,
    private apiService: ApiService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
      this.cdr.detectChanges();
    });

    this.authService.activeRole$.subscribe(role => {
      this.activeRole = role;
      if (role && this.authService.isAuthenticated) {
        this.loadDashboardData();
      }
    });
  }

  loadDashboardData(): void {
    this.isLoading = true;
    this.cdr.detectChanges();
    this.apiService.getDashboardStats().subscribe({
      next: (res) => {
        this.isLoading = false;
        const data = res?.data ?? (res as any)?.Data ?? (res as any);
        if (data && typeof data === 'object') {
          this.stats = data;
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Failed to load dashboard data:', err);
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  openRecordVitals(patientId?: string): void {
    if (patientId) {
      this.vitalsForm.patientId = patientId;
    } else if (this.stats?.recentAppointments?.length) {
      this.vitalsForm.patientId = this.stats.recentAppointments[0].patientId;
    }
    this.showVitalsModal = true;
    this.cdr.detectChanges();
  }

  saveVitals(): void {
    if (!this.vitalsForm.patientId) {
      alert('Please select or specify a Patient ID.');
      return;
    }

    this.apiService.recordVitals(this.vitalsForm.patientId, this.vitalsForm as any).subscribe({
      next: (res) => {
        alert('Vital signs successfully recorded!');
        this.showVitalsModal = false;
        this.loadDashboardData();
        this.cdr.detectChanges();
      },
      error: (err) => {
        alert(err.error?.message || 'Failed to record vitals.');
        this.cdr.detectChanges();
      }
    });
  }
}
