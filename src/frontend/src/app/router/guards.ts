import type { Router } from 'vue-router';
import { useAuthStore } from '@/app/stores/auth.store';
import { isAuthorizedForAnyRule } from './dashboardAccess';

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

    // 2. OR-based policy barrier (e.g. Level 2 domain dashboards: allowed permission OR allowed role, excluding self-service roles)
    if (to.meta.authMode === 'any') {
      if (!isAuthorizedForAnyRule(to.meta, authStore)) {
        return '/403';
      }
    } else {
      // 2a. Permission barrier: Route requires a specific actor permission
      if (to.meta.permission) {
        const isAllowed =
          authStore.hasPermission(to.meta.permission) ||
          (to.meta.permission === 'academics.curriculum.view' &&
            (authStore.hasPermission('Permissions.Academics.Curriculum.View') ||
              authStore.hasAnyRole(['AcademicCoordinator', 'AcademicAuthority', 'Admin']))) ||
          ((to.meta.permission === 'academics:learning-plans:view' ||
            to.meta.permission === 'academics.learning_plans.view' ||
            to.meta.permission === 'Permissions.Academics.LearningPlans.View') &&
            (authStore.hasPermission('Permissions.Academics.LearningPlans.View') ||
              authStore.hasPermission('academics:learning-plans:view') ||
              authStore.hasPermission('academics.learning_plans.view') ||
              authStore.hasAnyRole([
                'AcademicCoordinator',
                'AcademicAuthority',
                'Teacher',
                'StudentDevelopmentOfficer',
                'Admin',
              ]))) ||
          (to.meta.permission === 'academics.participations.view' &&
            (authStore.hasPermission('Permissions.Academics.Participations.View') ||
              authStore.hasAnyRole(['AcademicCoordinator', 'AcademicAuthority', 'Admin']))) ||
          (to.meta.permission === 'academics.activities.view' &&
            (authStore.hasPermission('Permissions.Academics.Activities.View') ||
              authStore.hasAnyRole(['Teacher', 'AcademicCoordinator', 'AcademicAuthority', 'Admin']))) ||
          (to.meta.permission === 'academics.assignments.view' &&
            (authStore.hasPermission('Permissions.Academics.Assignments.View') ||
              authStore.hasAnyRole(['Teacher', 'AcademicCoordinator', 'AcademicAuthority', 'Admin']))) ||
          (to.meta.permission === 'academics.interventions.view' &&
            (authStore.hasPermission('Permissions.Academics.Interventions.View') ||
              authStore.hasAnyRole(['AcademicCoordinator', 'Teacher', 'AcademicAuthority', 'Admin']))) ||
          (to.meta.permission === 'academics.standings.view' &&
            (authStore.hasPermission('Permissions.Academics.Standings.View') ||
              authStore.hasAnyRole(['AcademicAuthority', 'Admin']))) ||
          ((to.meta.permission === 'Permissions.Teaching.Faculty.View' ||
            to.meta.permission === 'teaching.faculty.view') &&
            (authStore.hasPermission('Permissions.Teaching.Faculty.View') ||
              authStore.hasPermission('teaching.faculty.view') ||
              authStore.hasAnyRole(['TeachingAuthority', 'TeachingCoordinator', 'Admin']))) ||
          ((to.meta.permission === 'Permissions.Teaching.Assignments.View' ||
            to.meta.permission === 'teaching.assignments.view') &&
            (authStore.hasPermission('Permissions.Teaching.Assignments.View') ||
              authStore.hasPermission('teaching.assignments.view') ||
              authStore.hasAnyRole(['Teacher', 'TeachingCoordinator', 'TeachingAuthority', 'Admin']))) ||
          ((to.meta.permission === 'Permissions.Teaching.Workload.View' ||
            to.meta.permission === 'teaching.workload.view') &&
            (authStore.hasPermission('Permissions.Teaching.Workload.View') ||
              authStore.hasPermission('teaching.workload.view') ||
              authStore.hasAnyRole(['TeachingCoordinator', 'TeachingAuthority', 'Admin']))) ||
          ((to.meta.permission === 'Permissions.Teaching.Availability.Declare' ||
            to.meta.permission === 'teaching.availability.declare') &&
            (authStore.hasPermission('Permissions.Teaching.Availability.Declare') ||
              authStore.hasPermission('teaching.availability.declare') ||
              authStore.hasAnyRole(['Teacher', 'TeachingCoordinator', 'Admin'])));

        if (!isAllowed) {
          return '/403';
        }
      }

      // 2b. Role barrier: Route requires specific role(s)
      if (to.meta.roles && Array.isArray(to.meta.roles) && to.meta.roles.length > 0) {
        if (!authStore.hasAnyRole(to.meta.roles as string[])) {
          return '/403';
        }
      }
    }

    // 4. Document title resolution
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
