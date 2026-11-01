import { describe, it, expect } from 'vitest';
import { createApp, defineComponent } from 'vue';
import { VUE_QUERY_CLIENT } from '@tanstack/vue-query';
import { createPinia } from 'pinia';
import { registerAppProviders } from '../index';
import { createQueryClient } from '../queryClient';

const TestApp = defineComponent({
  template: '<div>App</div>',
});

describe('registerAppProviders', () => {
  it('registers Pinia, VueQueryPlugin, PrimeVue, ToastService, and ConfirmationService', () => {
    const app = createApp(TestApp);

    const { pinia, queryClient } = registerAppProviders(app);

    expect(pinia).toBeDefined();
    expect(queryClient).toBeDefined();

    // Verify PrimeVue and its services attached to Vue globalProperties
    expect(app.config.globalProperties.$primevue).toBeDefined();
    expect(app.config.globalProperties.$toast).toBeDefined();
    expect(app.config.globalProperties.$confirm).toBeDefined();
  });

  it('provides the configured QueryClient to the Vue dependency injection tree', () => {
    const app = createApp(TestApp);
    const customQueryClient = createQueryClient();

    registerAppProviders(app, { queryClient: customQueryClient });

    const provides = (
      app as unknown as { _context: { provides: Record<string | symbol, unknown> } }
    )._context.provides;

    // TanStack Vue Query provides the client under VUE_QUERY_CLIENT
    expect(provides[VUE_QUERY_CLIENT]).toBe(customQueryClient);
  });

  it('allows passing custom Pinia and QueryClient instances', () => {
    const app = createApp(TestApp);
    const customPinia = createPinia();
    const customQueryClient = createQueryClient();

    const result = registerAppProviders(app, {
      pinia: customPinia,
      queryClient: customQueryClient,
    });

    expect(result.pinia).toBe(customPinia);
    expect(result.queryClient).toBe(customQueryClient);
  });
});
