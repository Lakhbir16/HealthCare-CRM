import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const token = authService.getAccessToken();

  let authReq = req;
  if (token) {
    authReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        // If 401 on protected route, redirect to login without triggering multiple loops
        if (!req.url.includes('/api/auth/login') && !req.url.includes('/api/auth/logout')) {
          if (authService.isAuthenticated) {
            authService.logout();
          } else {
            router.navigate(['/login']);
          }
        }
      }
      return throwError(() => error);
    })
  );
};
