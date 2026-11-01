// @vitest-environment jsdom
import { describe, it, expect, beforeEach } from 'vitest';
import { setActivePinia, createPinia } from 'pinia';
import { useAppContextStore, APP_CONTEXT_STORAGE_KEY } from '../appContext.store';
import { storage } from '@/shared/services/storage';

describe('useAppContextStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    storage.removeItem(APP_CONTEXT_STORAGE_KEY);
  });

  it('initializes with null active campus and academic period', () => {
    const contextStore = useAppContextStore();

    expect(contextStore.activeCampusId).toBeNull();
    expect(contextStore.activeAcademicPeriodId).toBeNull();
    expect(contextStore.hasActiveCampus).toBe(false);
    expect(contextStore.hasActiveAcademicPeriod).toBe(false);
    expect(contextStore.isContextComplete).toBe(false);
  });

  it('sets active campus and updates getters', () => {
    const contextStore = useAppContextStore();

    contextStore.setActiveCampus('campus-uuid-123');
    expect(contextStore.activeCampusId).toBe('campus-uuid-123');
    expect(contextStore.hasActiveCampus).toBe(true);
    expect(contextStore.isContextComplete).toBe(false);
  });

  it('sets active academic period and updates getters', () => {
    const contextStore = useAppContextStore();

    contextStore.setActiveAcademicPeriod('period-uuid-456');
    expect(contextStore.activeAcademicPeriodId).toBe('period-uuid-456');
    expect(contextStore.hasActiveAcademicPeriod).toBe(true);
    expect(contextStore.isContextComplete).toBe(false);
  });

  it('reports isContextComplete when both campus and period are set', () => {
    const contextStore = useAppContextStore();

    contextStore.setActiveCampus('campus-uuid-123');
    contextStore.setActiveAcademicPeriod('period-uuid-456');

    expect(contextStore.isContextComplete).toBe(true);
  });

  it('supports alias methods setCampus and setAcademicPeriod', () => {
    const contextStore = useAppContextStore();

    contextStore.setCampus('campus-alias');
    contextStore.setAcademicPeriod('period-alias');

    expect(contextStore.activeCampusId).toBe('campus-alias');
    expect(contextStore.activeAcademicPeriodId).toBe('period-alias');
  });

  it('resets context properly and clears storage', () => {
    const contextStore = useAppContextStore();

    contextStore.setActiveCampus('campus-uuid-123');
    contextStore.setActiveAcademicPeriod('period-uuid-456');
    expect(contextStore.isContextComplete).toBe(true);

    contextStore.resetContext();

    expect(contextStore.activeCampusId).toBeNull();
    expect(contextStore.activeAcademicPeriodId).toBeNull();
    expect(contextStore.isContextComplete).toBe(false);
    expect(storage.getItem(APP_CONTEXT_STORAGE_KEY)).toBeNull();
  });
});
