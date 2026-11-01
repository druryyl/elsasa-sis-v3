import type { App } from 'vue';
import { createPinia, type Pinia } from 'pinia';
import { VueQueryPlugin, type VueQueryPluginOptions, type QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import ConfirmationService from 'primevue/confirmationservice';
import { primevueConfig } from '@/app/config/primevue';
import { queryClient as defaultQueryClient } from './queryClient';

export interface AppProvidersOptions {
  pinia?: Pinia;
  queryClient?: QueryClient;
}

export interface AppProvidersResult {
  pinia: Pinia;
  queryClient: QueryClient;
}

/**
 * Registers institutional application-level composition providers:
 * 1. Pinia client state store
 * 2. TanStack Vue Query server state plugin with configured QueryClient
 * 3. PrimeVue UI library with Aura theme and Tailwind CSS layer order
 * 4. PrimeVue ToastService and ConfirmationService
 *
 * @param app - The Vue application instance
 * @param options - Optional overrides for Pinia or QueryClient (useful for testing)
 * @returns Configured provider instances
 */
export function registerAppProviders(app: App, options?: AppProvidersOptions): AppProvidersResult {
  const pinia = options?.pinia ?? createPinia();
  const queryClient = options?.queryClient ?? defaultQueryClient;

  const vueQueryPluginOptions: VueQueryPluginOptions = {
    queryClient,
  };

  app.use(pinia);
  app.use(VueQueryPlugin, vueQueryPluginOptions);
  app.use(PrimeVue, primevueConfig);
  app.use(ToastService);
  app.use(ConfirmationService);

  return {
    pinia,
    queryClient,
  };
}

export * from './queryClient';
