import { Injectable } from '@angular/core';

export type DemoPreviewRole = 'Craftsman' | 'Customer' | 'Admin';

@Injectable({ providedIn: 'root' })
export class DemoPreviewService {
  private readonly storageKey = 'craftconnect_demo_preview_role';

  // TEMPORARY DEMO MODE: this is frontend-only and can be removed later by deleting this service and its callers.
  setRole(role: DemoPreviewRole): void {
    localStorage.setItem(this.storageKey, role);
  }

  clearRole(): void {
    localStorage.removeItem(this.storageKey);
  }

  getRole(): DemoPreviewRole | null {
    const role = localStorage.getItem(this.storageKey);
    if (role === 'Craftsman' || role === 'Customer' || role === 'Admin') {
      return role;
    }

    return null;
  }

  isDemoModeEnabled(): boolean {
    return this.getRole() !== null;
  }

  getPreviewRoutesForRole(role: DemoPreviewRole): string[] {
    const routes: Record<DemoPreviewRole, string[]> = {
      Craftsman: [
        '/craftsman/crafts',
        '/craftsman/requests',
        '/craftsman/board',
        '/craftsman/day-sheet',
        '/craftsman/notifications',
        '/craftsman/profile',
        '/craftsman/status',
      ],
      Customer: ['/customer/craftsmen', '/customer/requests', '/customer/notifications', '/customer/profile'],
      Admin: ['/admin/craftsmen'],
    };

    return routes[role];
  }

  isRoleAllowedForRoute(route: string, role: DemoPreviewRole): boolean {
    return this.getRole() === role && this.getPreviewRoutesForRole(role).includes(route);
  }

  getDefaultRoute(role: DemoPreviewRole): string {
    return this.getPreviewRoutesForRole(role)[0];
  }
}
