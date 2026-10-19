import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated) {
    router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
    return false;
  }

  const expectedRoles = route.data?.['roles'] as string[] | undefined;
  if (expectedRoles && expectedRoles.length > 0) {
    const activeRole = authService.activeRoleValue;
    const userRoles = authService.currentUserValue?.roles || [];

    const hasPermission = expectedRoles.includes(activeRole) || userRoles.some(r => expectedRoles.includes(r));
    if (!hasPermission) {
      router.navigate(['/access-denied']);
      return false;
    }
  }

  return true;
};
