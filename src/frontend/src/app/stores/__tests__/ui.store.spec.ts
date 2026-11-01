// @vitest-environment jsdom
import { describe, it, expect, beforeEach } from 'vitest';
import { setActivePinia, createPinia } from 'pinia';
import { useUIStore, UI_PREFERENCES_STORAGE_KEY } from '../ui.store';
import { storage } from '@/shared/services/storage';
import type { BreadcrumbItem } from '@/shared/types/navigation';

describe('useUIStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    storage.removeItem(UI_PREFERENCES_STORAGE_KEY);
  });

  it('initializes with default sidebar, theme, and empty breadcrumbs', () => {
    const uiStore = useUIStore();

    expect(uiStore.isSidebarCollapsed).toBe(false);
    expect(uiStore.activeTheme).toBe('light');
    expect(uiStore.isDark).toBe(false);
    expect(uiStore.breadcrumbs).toEqual([]);
    expect(uiStore.currentBreadcrumbs).toEqual([]);
  });

  it('toggles sidebar collapse state', () => {
    const uiStore = useUIStore();

    uiStore.toggleSidebar();
    expect(uiStore.isSidebarCollapsed).toBe(true);

    uiStore.toggleSidebar();
    expect(uiStore.isSidebarCollapsed).toBe(false);
  });

  it('sets sidebar collapse state explicitly', () => {
    const uiStore = useUIStore();

    uiStore.setSidebarCollapsed(true);
    expect(uiStore.isSidebarCollapsed).toBe(true);

    uiStore.setSidebarCollapsed(false);
    expect(uiStore.isSidebarCollapsed).toBe(false);
  });

  it('manages breadcrumbs list', () => {
    const uiStore = useUIStore();
    const items: BreadcrumbItem[] = [
      { label: 'Dashboard', to: '/dashboard' },
      { label: 'Students', to: '/students' },
      { label: 'Admission Queue', current: true },
    ];

    uiStore.setBreadcrumbs(items);
    expect(uiStore.breadcrumbs).toHaveLength(3);
    expect(uiStore.breadcrumbs[0].label).toBe('Dashboard');
    expect(uiStore.currentBreadcrumbs).toEqual(items);

    uiStore.clearBreadcrumbs();
    expect(uiStore.breadcrumbs).toEqual([]);
  });

  it('manages theme and toggleTheme', () => {
    const uiStore = useUIStore();

    uiStore.setTheme('dark');
    expect(uiStore.activeTheme).toBe('dark');
    expect(uiStore.isDark).toBe(true);

    uiStore.toggleTheme();
    expect(uiStore.activeTheme).toBe('light');
    expect(uiStore.isDark).toBe(false);
  });

  it('resets preferences to default values and clears storage', () => {
    const uiStore = useUIStore();
    uiStore.setSidebarCollapsed(true);
    uiStore.setTheme('dark');

    uiStore.resetPreferences();

    expect(uiStore.isSidebarCollapsed).toBe(false);
    expect(uiStore.activeTheme).toBe('light');
    expect(storage.getItem(UI_PREFERENCES_STORAGE_KEY)).toBeNull();
  });
});
