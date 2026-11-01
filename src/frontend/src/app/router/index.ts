import { createRouter, createWebHistory, type Router, type RouterHistory } from 'vue-router';
import { routes } from './routes';
import { setupNavigationGuards } from './guards';

export interface CreateAppRouterOptions {
  history?: RouterHistory;
}

/**
 * Creates and initializes the Vue Router instance with HTML5 web history,
 * hierarchical Stage 1 routes, and authentication/permission navigation guards.
 *
 * @param options - Optional router customization (e.g. MemoryHistory for testing)
 * @returns Configured Router instance
 */
export function createAppRouter(options?: CreateAppRouterOptions): Router {
  const history = options?.history ?? createWebHistory(import.meta.env.BASE_URL || '/');

  const router = createRouter({
    history,
    routes,
    scrollBehavior(_to, _from, savedPosition) {
      if (savedPosition) {
        return savedPosition;
      }
      return { top: 0 };
    },
  });

  setupNavigationGuards(router);

  return router;
}

/**
 * Default singleton application router instance.
 */
export const router: Router = createAppRouter();

export default router;

export * from './routes';
export * from './guards';
