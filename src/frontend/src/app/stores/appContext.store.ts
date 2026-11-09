import { defineStore } from 'pinia';
import { storage } from '@/shared/services/storage';

export const APP_CONTEXT_STORAGE_KEY = 'app_context';

export interface AppContextState {
  activeCampusId: string | null;
  activeAcademicPeriodId: string | null;
}

export interface PersistedAppContext {
  activeCampusId: string | null;
  activeAcademicPeriodId: string | null;
}

function getInitialAppContextState(): AppContextState {
  try {
    const persisted = storage.getItem<PersistedAppContext>(APP_CONTEXT_STORAGE_KEY);
    if (persisted) {
      return {
        activeCampusId: persisted.activeCampusId ?? null,
        activeAcademicPeriodId: persisted.activeAcademicPeriodId ?? null,
      };
    }
  } catch {
    // Fallback cleanly on parse errors
  }

  return {
    activeCampusId: null,
    activeAcademicPeriodId: null,
  };
}

export const useAppContextStore = defineStore('appContext', {
  state: (): AppContextState => getInitialAppContextState(),

  getters: {
    hasActiveCampus: (state): boolean => Boolean(state.activeCampusId),
    hasActiveAcademicPeriod: (state): boolean => Boolean(state.activeAcademicPeriodId),
    isContextComplete: (state): boolean =>
      Boolean(state.activeCampusId && state.activeAcademicPeriodId),
  },

  actions: {
    setActiveCampus(id: string | null) {
      this.activeCampusId = id;
      this.persistContext();
    },

    setActiveAcademicPeriod(id: string | null) {
      this.activeAcademicPeriodId = id;
      this.persistContext();
    },

    setCampus(id: string | null) {
      this.setActiveCampus(id);
    },

    setAcademicPeriod(id: string | null) {
      this.setActiveAcademicPeriod(id);
    },

    resetContext() {
      this.activeCampusId = null;
      this.activeAcademicPeriodId = null;
      storage.removeItem(APP_CONTEXT_STORAGE_KEY);
    },

    /**
     * Clears all application context state and persisted cache.
     * Alias of resetContext(); exposed for reactive reset workflows (P6-S12).
     */
    clear() {
      this.resetContext();
    },

    persistContext() {
      storage.setItem<PersistedAppContext>(APP_CONTEXT_STORAGE_KEY, {
        activeCampusId: this.activeCampusId,
        activeAcademicPeriodId: this.activeAcademicPeriodId,
      });
    },
  },
});
