import { defineStore } from 'pinia';
import { storage } from '@/shared/services/storage';

export const AUTH_STORAGE_KEY = 'auth_session';

/**
 * Active authenticated user session identity.
 * Aligns with backend ICurrentUser claims and JWT payload.
 */
export interface UserSession {
  userId: string;
  username: string;
  fullName: string;
  roles: string[];
  permissions: string[];
  campusId: string | null;
  /** Student identifier from the authenticated JWT claim (present for student personas). */
  studentId?: string | null;
}

export interface AuthState {
  user: UserSession | null;
  token: string | null;
  permissions: string[];
  roles: string[];
}

export interface PersistedAuthSession {
  token: string;
  user: UserSession;
}

/**
 * Returns true if client auth persistence fallback via localStorage is enabled.
 * Default in production is in-memory only (ADR-FE-002, Section 13.2).
 */
export function isAuthPersistenceEnabled(): boolean {
  if (typeof window === 'undefined') {
    return false;
  }
  return import.meta.env?.VITE_AUTH_PERSISTENCE === 'storage';
}

function getInitialAuthState(): AuthState {
  if (isAuthPersistenceEnabled()) {
    try {
      const persisted = storage.getItem<PersistedAuthSession>(AUTH_STORAGE_KEY);
      if (persisted?.token && persisted?.user) {
        return {
          token: persisted.token,
          user: persisted.user,
          permissions: Array.isArray(persisted.user.permissions)
            ? [...persisted.user.permissions]
            : [],
          roles: Array.isArray(persisted.user.roles) ? [...persisted.user.roles] : [],
        };
      }
    } catch {
      // Fallback cleanly to empty state on storage deserialization failure
    }
  }

  return {
    user: null,
    token: null,
    permissions: [],
    roles: [],
  };
}

export const useAuthStore = defineStore('auth', {
  state: (): AuthState => getInitialAuthState(),

  getters: {
    /**
     * True when a valid token and user session exist in client state.
     */
    isAuthenticated: (state): boolean => Boolean(state.token && state.user),

    /**
     * Evaluates whether the current actor holds the required permission.
     * SuperAdmin role or '*' wildcard grants universal access.
     */
    hasPermission: (state) => {
      return (permission: string): boolean => {
        if (!state.user || !permission) {
          return false;
        }
        if (state.permissions.includes('*') || state.roles.includes('SuperAdmin')) {
          return true;
        }
        return state.permissions.includes(permission);
      };
    },

    /**
     * Evaluates whether the current actor possesses at least one of the specified roles.
     * SuperAdmin role grants universal access.
     */
    hasAnyRole: (state) => {
      return (roles: string[]): boolean => {
        if (!state.user || !roles || roles.length === 0) {
          return false;
        }
        if (state.roles.includes('SuperAdmin')) {
          return true;
        }
        return roles.some((role) => state.roles.includes(role));
      };
    },

    /**
     * Evaluates whether the current actor has a specific role.
     */
    hasRole: (state) => {
      return (role: string): boolean => {
        if (!state.user || !role) {
          return false;
        }
        if (state.roles.includes('SuperAdmin')) {
          return true;
        }
        return state.roles.includes(role);
      };
    },

    currentUser: (state): UserSession | null => state.user,
    currentUserId: (state): string | null => state.user?.userId ?? null,
    currentCampusId: (state): string | null => state.user?.campusId ?? null,
  },

  actions: {
    /**
     * Initializes the client authenticated session with token and user details.
     */
    setSession(token: string, user: UserSession) {
      this.token = token;
      this.user = user;
      this.permissions = Array.isArray(user.permissions) ? [...user.permissions] : [];
      this.roles = Array.isArray(user.roles) ? [...user.roles] : [];

      if (isAuthPersistenceEnabled()) {
        storage.setItem<PersistedAuthSession>(AUTH_STORAGE_KEY, { token, user });
      }
    },

    /**
     * Clears all session credentials and resets client auth state to unauthenticated.
     */
    logout() {
      this.token = null;
      this.user = null;
      this.permissions = [];
      this.roles = [];

      storage.removeItem(AUTH_STORAGE_KEY);
    },

    /**
     * Invoked globally when the HTTP client encounters an unrecoverable 401 Unauthorized.
     */
    handleSessionExpired() {
      this.logout();
    },

    /**
     * Restores session state from persistent storage when enabled or explicitly forced.
     */
    restoreSession(force: boolean = false): boolean {
      if (force || isAuthPersistenceEnabled()) {
        const persisted = storage.getItem<PersistedAuthSession>(AUTH_STORAGE_KEY);
        if (persisted?.token && persisted?.user) {
          this.token = persisted.token;
          this.user = persisted.user;
          this.permissions = Array.isArray(persisted.user.permissions)
            ? [...persisted.user.permissions]
            : [];
          this.roles = Array.isArray(persisted.user.roles) ? [...persisted.user.roles] : [];
          return true;
        }
      }
      return false;
    },

    /**
     * Manually persists current active session to storage (e.g. for dev workflow tooling).
     */
    persistSession(force: boolean = false): void {
      if ((force || isAuthPersistenceEnabled()) && this.token && this.user) {
        storage.setItem<PersistedAuthSession>(AUTH_STORAGE_KEY, {
          token: this.token,
          user: this.user,
        });
      }
    },
  },
});
