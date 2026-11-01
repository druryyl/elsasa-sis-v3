import { createApp, type App as VueApp } from 'vue';
import App from '@/App.vue';
import { registerAppProviders, type AppProvidersResult } from '@/app/providers';
import { router } from '@/app/router';
import { useAuthStore } from '@/app/stores/auth.store';
import { logger } from '@/shared/services/logger';

import '@/shared/styles/main.css';

export const appVersion = '0.1.0';

export interface BootstrapResult extends AppProvidersResult {
  app: VueApp;
  router: typeof router;
  mountedElement: Element | null;
}

/**
 * Bootstraps the Single Page Application lifecycle:
 * 1. Calls createApp(App).
 * 2. Registers institutional composition providers (Pinia, VueQuery, PrimeVue, Toast, Dialog).
 * 3. Restores active authentication session from persistent storage when available.
 * 4. Registers Vue Router with global navigation guards.
 * 5. Registers global Vue error handling telemetry.
 * 6. Mounts root application `#app`.
 *
 * @param containerSelector - Target DOM selector for root application mount (default: '#app')
 * @returns Configured application instance and provider references
 */
export function bootstrapApplication(containerSelector: string = '#app'): BootstrapResult {
  const app = createApp(App);

  // 1. Register application providers (Pinia, Vue Query, PrimeVue)
  const providers = registerAppProviders(app);

  // 2. Hydrate client authentication session
  const authStore = useAuthStore(providers.pinia);
  authStore.restoreSession(true);

  // 3. Register Vue Router
  app.use(router);

  // 4. Register global error handler
  app.config.errorHandler = (error, _instance, info) => {
    logger.error('Unhandled Vue application error', {
      error: error instanceof Error ? error.message : String(error),
      info,
    });
  };

  // 5. Mount root application component to DOM container if present
  let mountedElement: Element | null = null;
  if (typeof document !== 'undefined') {
    const el = document.querySelector(containerSelector);
    if (el) {
      app.mount(el);
      mountedElement = el;
    }
  }

  return {
    app,
    router,
    mountedElement,
    ...providers,
  };
}

// Automatically bootstrap when executing in the browser document environment
if (typeof window !== 'undefined' && typeof document !== 'undefined') {
  const rootEl = document.querySelector('#app');
  if (rootEl) {
    bootstrapApplication('#app');
  }
}

export default bootstrapApplication;
