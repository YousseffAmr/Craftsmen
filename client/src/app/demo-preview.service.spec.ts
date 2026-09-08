import { beforeEach, describe, expect, it } from 'vitest';
import { DemoPreviewService } from './demo-preview.service';

const storage = new Map<string, string>();
const localStorageMock = {
  getItem: (key: string) => storage.get(key) ?? null,
  setItem: (key: string, value: string) => storage.set(key, value),
  removeItem: (key: string) => storage.delete(key),
  clear: () => storage.clear(),
};

describe('DemoPreviewService', () => {
  beforeEach(() => {
    storage.clear();
    Object.defineProperty(globalThis, 'localStorage', {
      value: localStorageMock,
      configurable: true,
    });
  });

  it('should enable demo mode for the matching role and block other preview routes', () => {
    const service = new DemoPreviewService();

    service.setRole('Craftsman');

    expect(service.isDemoModeEnabled()).toBe(true);
    expect(service.isRoleAllowedForRoute('/craftsman/crafts', 'Craftsman')).toBe(true);
    expect(service.isRoleAllowedForRoute('/customer/craftsmen', 'Craftsman')).toBe(false);
    expect(service.isRoleAllowedForRoute('/admin/craftsmen', 'Craftsman')).toBe(false);
  });
});
