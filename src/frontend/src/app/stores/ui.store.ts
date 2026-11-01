import { defineStore } from 'pinia';
import { storage } from '@/shared/services/storage';
import type { BreadcrumbItem } from '@/shared/types/navigation';

export const UI_PREFERENCES_STORAGE_KEY = 'ui_preferences';

export type AppTheme = 'light' | 'dark';

export interface UIState {
  isSidebarCollapsed: boolean;
  activeTheme: AppTheme;
  breadcrumbs: BreadcrumbItem[];
}

export interface PersistedUIPreferences {
  isSidebarCollapsed?: boolean;
  activeTheme?: AppTheme;
}

function getInitialUIState(): UIState {
  let isSidebarCollapsed = false;
  let activeTheme: AppTheme = 'light';

  try {
    const persisted = storage.getItem<PersistedUIPreferences>(UI_PREFERENCES_STORAGE_KEY);
    if (persisted) {
      if (typeof persisted.isSidebarCollapsed === 'boolean') {
        isSidebarCollapsed = persisted.isSidebarCollapsed;
      }
      if (persisted.activeTheme === 'light' || persisted.activeTheme === 'dark') {
        activeTheme = persisted.activeTheme;
      }
    }
  } catch {
    // Fallback cleanly on parse errors
  }

  return {
    isSidebarCollapsed,
    activeTheme,
    breadcrumbs: [],
  };
}

export const useUIStore = defineStore('ui', {
  state: (): UIState => getInitialUIState(),

  getters: {
    isDark: (state): boolean => state.activeTheme === 'dark',
    currentBreadcrumbs: (state): BreadcrumbItem[] => state.breadcrumbs,
  },

  actions: {
    toggleSidebar() {
      this.isSidebarCollapsed = !this.isSidebarCollapsed;
      this.persistPreferences();
    },

    setSidebarCollapsed(collapsed: boolean) {
      this.isSidebarCollapsed = collapsed;
      this.persistPreferences();
    },

    setBreadcrumbs(items: BreadcrumbItem[]) {
      this.breadcrumbs = [...items];
    },

    clearBreadcrumbs() {
      this.breadcrumbs = [];
    },

    setTheme(theme: AppTheme) {
      this.activeTheme = theme;
      this.persistPreferences();
    },

    toggleTheme() {
      this.activeTheme = this.activeTheme === 'light' ? 'dark' : 'light';
      this.persistPreferences();
    },

    persistPreferences() {
      storage.setItem<PersistedUIPreferences>(UI_PREFERENCES_STORAGE_KEY, {
        isSidebarCollapsed: this.isSidebarCollapsed,
        activeTheme: this.activeTheme,
      });
    },

    resetPreferences() {
      this.isSidebarCollapsed = false;
      this.activeTheme = 'light';
      this.breadcrumbs = [];
      storage.removeItem(UI_PREFERENCES_STORAGE_KEY);
    },
  },
});
