import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap, catchError, of, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { ApiResponse, ForgotPasswordResponse, LoginResponse, UserProfile, VerifyOtpResponse } from '../models/app.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = 'http://localhost:5000/api/auth';
  private readonly TOKEN_KEY = 'hc_access_token';
  private readonly REFRESH_KEY = 'hc_refresh_token';
  private readonly USER_KEY = 'hc_user_profile';
  private readonly ACTIVE_ROLE_KEY = 'hc_active_role';

  private currentUserSubject = new BehaviorSubject<UserProfile | null>(this.getStoredUser());
  public currentUser$ = this.currentUserSubject.asObservable();

  private activeRoleSubject = new BehaviorSubject<string>(this.getStoredActiveRole());
  public activeRole$ = this.activeRoleSubject.asObservable();

  constructor(private http: HttpClient, private router: Router) {}

  public get currentUserValue(): UserProfile | null {
    return this.currentUserSubject.value;
  }

  public get activeRoleValue(): string {
    return this.activeRoleSubject.value;
  }

  public get isAuthenticated(): boolean {
    return !!this.getAccessToken();
  }

  public getAccessToken(): string | null {
    return sessionStorage.getItem(this.TOKEN_KEY);
  }

  private getRefreshToken(): string | null {
    return sessionStorage.getItem(this.REFRESH_KEY);
  }

  public login(credentials: { email: string; password: string; rememberMe?: boolean }): Observable<ApiResponse<LoginResponse>> {
    return this.http.post<ApiResponse<LoginResponse>>(`${this.apiUrl}/login`, credentials).pipe(
      tap(res => {
        if (res.success && res.data) {
          this.setSession(res.data);
        }
      })
    );
  }

  public switchRole(role: string): Observable<ApiResponse<LoginResponse>> {
    return this.http.post<ApiResponse<LoginResponse>>(`${this.apiUrl}/switch-role`, { role }).pipe(
      tap(res => {
        if (res.success && res.data) {
          this.setSession(res.data);
        }
      })
    );
  }

  public requestOtp(email: string): Observable<ApiResponse<ForgotPasswordResponse>> {
    return this.http.post<ApiResponse<ForgotPasswordResponse>>(`${this.apiUrl}/forgot-password`, { email });
  }

  public verifyOtp(email: string, otp: string): Observable<ApiResponse<VerifyOtpResponse>> {
    return this.http.post<ApiResponse<VerifyOtpResponse>>(`${this.apiUrl}/verify-otp`, { email, otp });
  }

  public resendOtp(email: string): Observable<ApiResponse<ForgotPasswordResponse>> {
    return this.http.post<ApiResponse<ForgotPasswordResponse>>(`${this.apiUrl}/resend-otp`, { email });
  }

  public resetPassword(payload: { email: string; resetToken: string; newPassword: string; confirmPassword: string }): Observable<ApiResponse<boolean>> {
    return this.http.post<ApiResponse<boolean>>(`${this.apiUrl}/reset-password`, payload);
  }

  public logout(): void {
    const refreshToken = this.getRefreshToken();
    this.clearSession();
    if (refreshToken) {
      this.http.post(`${this.apiUrl}/logout`, { refreshToken }).subscribe({
        next: () => {},
        error: () => {}
      });
    }
    this.router.navigate(['/login']);
  }

  public checkMe(): Observable<ApiResponse<UserProfile>> {
    return this.http.get<ApiResponse<UserProfile>>(`${this.apiUrl}/me`).pipe(
      tap(res => {
        if (res.success && res.data) {
          this.currentUserSubject.next(res.data);
          sessionStorage.setItem(this.USER_KEY, JSON.stringify(res.data));
        }
      }),
      catchError(err => {
        this.clearSession();
        return throwError(() => err);
      })
    );
  }

  private setSession(authResult: LoginResponse): void {
    const res = authResult as any;
    const accessToken = res.accessToken || res.AccessToken;
    const refreshToken = res.refreshToken || res.RefreshToken;
    const user = res.user || res.User;
    const activeRole = res.activeRole || res.ActiveRole;

    sessionStorage.setItem(this.TOKEN_KEY, accessToken);
    sessionStorage.setItem(this.REFRESH_KEY, refreshToken);
    sessionStorage.setItem(this.USER_KEY, JSON.stringify(user));
    sessionStorage.setItem(this.ACTIVE_ROLE_KEY, activeRole);

    this.currentUserSubject.next(user);
    this.activeRoleSubject.next(activeRole);
  }

  private clearSession(): void {
    sessionStorage.removeItem(this.TOKEN_KEY);
    sessionStorage.removeItem(this.REFRESH_KEY);
    sessionStorage.removeItem(this.USER_KEY);
    sessionStorage.removeItem(this.ACTIVE_ROLE_KEY);

    this.currentUserSubject.next(null);
    this.activeRoleSubject.next('');
  }

  private getStoredUser(): UserProfile | null {
    try {
      const data = sessionStorage.getItem(this.USER_KEY);
      return data ? JSON.parse(data) : null;
    } catch {
      return null;
    }
  }

  private getStoredActiveRole(): string {
    return sessionStorage.getItem(this.ACTIVE_ROLE_KEY) || '';
  }
}
