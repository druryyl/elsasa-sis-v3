// @vitest-environment jsdom
import { describe, it, expect, beforeEach } from 'vitest';
import { setActivePinia, createPinia } from 'pinia';
import { useAuthStore, AUTH_STORAGE_KEY, type UserSession } from '../auth.store';
import { storage } from '@/shared/services/storage';

describe('useAuthStore', () => {
  const mockUser: UserSession = {
    userId: '11111111-1111-1111-1111-111111111111',
    username: 'admission_officer',
    fullName: 'Jane Doe',
    roles: ['AdmissionOfficer'],
    permissions: ['Permissions.Students.Admission.View', 'Permissions.Students.Admission.Review'],
    campusId: 'campus-01',
  };

  const superAdminUser: UserSession = {
    userId: '22222222-2222-2222-2222-222222222222',
    username: 'superadmin',
    fullName: 'Super Admin',
    roles: ['SuperAdmin'],
    permissions: [],
    campusId: null,
  };

  const wildcardUser: UserSession = {
    userId: '33333333-3333-3333-3333-333333333333',
    username: 'wildcard_user',
    fullName: 'Wildcard User',
    roles: ['Auditor'],
    permissions: ['*'],
    campusId: null,
  };

  beforeEach(() => {
    setActivePinia(createPinia());
    storage.removeItem(AUTH_STORAGE_KEY);
  });

  describe('Initial State', () => {
    it('initializes with unauthenticated empty state', () => {
      const authStore = useAuthStore();

      expect(authStore.isAuthenticated).toBe(false);
      expect(authStore.user).toBeNull();
      expect(authStore.token).toBeNull();
      expect(authStore.permissions).toEqual([]);
      expect(authStore.roles).toEqual([]);
      expect(authStore.currentUser).toBeNull();
      expect(authStore.currentUserId).toBeNull();
      expect(authStore.currentCampusId).toBeNull();
    });
  });

  describe('Session Lifecycle', () => {
    it('sets session correctly and updates state and getters', () => {
      const authStore = useAuthStore();
      const token = 'mock-jwt-token-xyz';

      authStore.setSession(token, mockUser);

      expect(authStore.isAuthenticated).toBe(true);
      expect(authStore.token).toBe(token);
      expect(authStore.user).toEqual(mockUser);
      expect(authStore.permissions).toEqual(mockUser.permissions);
      expect(authStore.roles).toEqual(mockUser.roles);
      expect(authStore.currentUser).toEqual(mockUser);
      expect(authStore.currentUserId).toBe(mockUser.userId);
      expect(authStore.currentCampusId).toBe('campus-01');
    });

    it('clears session on logout', () => {
      const authStore = useAuthStore();
      authStore.setSession('mock-token', mockUser);
      expect(authStore.isAuthenticated).toBe(true);

      authStore.logout();

      expect(authStore.isAuthenticated).toBe(false);
      expect(authStore.token).toBeNull();
      expect(authStore.user).toBeNull();
      expect(authStore.permissions).toEqual([]);
      expect(authStore.roles).toEqual([]);
      expect(authStore.currentUser).toBeNull();
      expect(authStore.currentUserId).toBeNull();
      expect(authStore.currentCampusId).toBeNull();
    });

    it('clears session on handleSessionExpired', () => {
      const authStore = useAuthStore();
      authStore.setSession('mock-token', mockUser);
      expect(authStore.isAuthenticated).toBe(true);

      authStore.handleSessionExpired();

      expect(authStore.isAuthenticated).toBe(false);
      expect(authStore.token).toBeNull();
      expect(authStore.user).toBeNull();
      expect(authStore.permissions).toEqual([]);
      expect(authStore.roles).toEqual([]);
    });
  });

  describe('Permission Checking', () => {
    it('returns false when unauthenticated', () => {
      const authStore = useAuthStore();

      expect(authStore.hasPermission('Permissions.Students.Admission.View')).toBe(false);
      expect(authStore.hasPermission('')).toBe(false);
    });

    it('evaluates exact permission matches correctly', () => {
      const authStore = useAuthStore();
      authStore.setSession('token', mockUser);

      expect(authStore.hasPermission('Permissions.Students.Admission.View')).toBe(true);
      expect(authStore.hasPermission('Permissions.Students.Admission.Review')).toBe(true);
      expect(authStore.hasPermission('Permissions.Finance.Payments.Receive')).toBe(false);
    });

    it('grants access when wildcard permission is present', () => {
      const authStore = useAuthStore();
      authStore.setSession('token', wildcardUser);

      expect(authStore.hasPermission('Permissions.Students.Admission.View')).toBe(true);
      expect(authStore.hasPermission('Permissions.Operations.Calendar.Manage')).toBe(true);
      expect(authStore.hasPermission('Any.Arbitrary.Permission')).toBe(true);
    });

    it('grants universal access to SuperAdmin role', () => {
      const authStore = useAuthStore();
      authStore.setSession('token', superAdminUser);

      expect(authStore.hasPermission('Permissions.Students.Admission.View')).toBe(true);
      expect(authStore.hasPermission('Permissions.Finance.Obligations.Manage')).toBe(true);
    });
  });

  describe('Role Checking', () => {
    it('returns false when unauthenticated', () => {
      const authStore = useAuthStore();

      expect(authStore.hasAnyRole(['AdmissionOfficer'])).toBe(false);
      expect(authStore.hasRole('AdmissionOfficer')).toBe(false);
    });

    it('evaluates hasAnyRole and hasRole correctly for authenticated user', () => {
      const authStore = useAuthStore();
      authStore.setSession('token', mockUser);

      expect(authStore.hasAnyRole(['AdmissionOfficer'])).toBe(true);
      expect(authStore.hasAnyRole(['FinanceOfficer', 'AdmissionOfficer'])).toBe(true);
      expect(authStore.hasAnyRole(['FinanceOfficer', 'Headmaster'])).toBe(false);
      expect(authStore.hasAnyRole([])).toBe(false);

      expect(authStore.hasRole('AdmissionOfficer')).toBe(true);
      expect(authStore.hasRole('FinanceOfficer')).toBe(false);
      expect(authStore.hasRole('')).toBe(false);
    });

    it('grants universal access to SuperAdmin for role checks', () => {
      const authStore = useAuthStore();
      authStore.setSession('token', superAdminUser);

      expect(authStore.hasAnyRole(['AdmissionOfficer'])).toBe(true);
      expect(authStore.hasRole('FinanceOfficer')).toBe(true);
    });
  });

  describe('Storage Persistence Fallback', () => {
    it('persists and restores session when manually requested', () => {
      const authStore = useAuthStore();
      authStore.setSession('manual-token', mockUser);

      authStore.persistSession(true);

      const saved = storage.getItem<{ token: string; user: UserSession }>(AUTH_STORAGE_KEY);
      expect(saved).not.toBeNull();
      expect(saved?.token).toBe('manual-token');
      expect(saved?.user.username).toBe('admission_officer');

      // Reset store state
      authStore.logout();
      expect(authStore.isAuthenticated).toBe(false);

      // Restore session
      storage.setItem(AUTH_STORAGE_KEY, saved);
      const restored = authStore.restoreSession(true);
      expect(restored).toBe(true);
      expect(authStore.isAuthenticated).toBe(true);
      expect(authStore.token).toBe('manual-token');
      expect(authStore.user?.username).toBe('admission_officer');
      expect(authStore.permissions).toEqual(mockUser.permissions);
    });

    it('returns false when restoreSession is called without stored session', () => {
      const authStore = useAuthStore();
      storage.removeItem(AUTH_STORAGE_KEY);

      const restored = authStore.restoreSession(true);
      expect(restored).toBe(false);
      expect(authStore.isAuthenticated).toBe(false);
    });
  });
});
