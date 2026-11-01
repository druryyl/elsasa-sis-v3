import type { Router } from 'vue-router';
import { useAuthStore } from '@/app/stores/auth.store';

export const APP_TITLE_SUFFIX = 'Elsasa SIS';

/**
 * Resolves the browser document title based on the route title and application branding.
 * - Formats `${routeTitle} — Elsasa SIS` when a title is specified.
 * - Avoids duplicate suffix if route title already ends with `Elsasa SIS`.
 * - Falls back to environment title or default application branding when missing.
 */
export function resolveDocumentTitle(routeTitle?: string): string {
  const defaultAppTitle = import.meta.env.VITE_APP_TITLE || APP_TITLE_SUFFIX;
  if (!routeTitle) {
    return defaultAppTitle;
  }
  if (routeTitle.endsWith(APP_TITLE_SUFFIX)) {
    return routeTitle;
  }
  return `${routeTitle} — ${APP_TITLE_SUFFIX}`;
}

/**
 * Installs global Vue Router navigation guards enforcing authentication
 * and actor permission rules (ADR-FE-007).
 *
 * Guard behavior:
 * 1. If `to.meta.requiresAuth !== false` and `!authStore.isAuthenticated`,
 *    redirects to `/auth/login?redirect=${encodeURIComponent(to.fullPath)}`.
 * 2. If `to.meta.permission` is specified and `!authStore.hasPermission(to.meta.permission)`,
 *    redirects to `/403`.
 * 3. Sets the browser document title to `${to.meta.title} — Elsasa SIS`.
 *
 * @param router - Configured Vue Router instance
 */
export function setupNavigationGuards(router: Router): void {
  router.beforeEach((to) => {
    const authStore = useAuthStore();

    // 1. Unauthenticated barrier: Protected routes default to requiring auth
    if (to.meta.requiresAuth !== false && !authStore.isAuthenticated) {
      return `/auth/login?redirect=${encodeURIComponent(to.fullPath)}`;
    }

    // 2. Permission barrier: Route requires a specific actor permission
    if (to.meta.permission && !authStore.hasPermission(to.meta.permission)) {
      return '/403';
    }

    // 3. Document title resolution
    if (typeof document !== 'undefined') {
      document.title = resolveDocumentTitle(to.meta.title);
    }

    return true;
  });

  router.afterEach((to) => {
    if (typeof document !== 'undefined') {
      document.title = resolveDocumentTitle(to.meta.title);
    }
  });
}
