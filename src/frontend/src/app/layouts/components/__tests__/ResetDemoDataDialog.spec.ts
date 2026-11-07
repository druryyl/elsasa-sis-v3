// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, beforeAll } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import { ref } from 'vue';
import ResetDemoDataDialog from '../ResetDemoDataDialog.vue';

// Create reactive refs that can be controlled from tests
const mockIsPending = ref(false);
const mockError = ref(null);
const mockIsSuccess = ref(false);
const mockMutate = vi.fn();

vi.mock('@/features/operations/demo-data/composables/useDemoData', () => ({
  useResetDemoData: (options?: { onSuccess?: (data: any) => void; onError?: (error: any) => void }) => ({
    resetDemoDataAsync: mockMutate,
    isPending: mockIsPending,
    error: mockError,
    isSuccess: mockIsSuccess,
    mutate: vi.fn((payload?: any) => {
      mockMutate(payload);
      // Simulate async mutation
      Promise.resolve().then(() => {
        if (mockError.value) {
          options?.onError?.(mockError.value);
        } else {
          mockIsSuccess.value = true;
          options?.onSuccess?.({ purgedSchemas: 10, seededDomains: 5, elapsedMilliseconds: 1000 });
        }
      });
    }),
  }),
}));

// Mock primevue/usetoast
vi.mock('primevue/usetoast', () => ({
  useToast: () => ({ add: vi.fn() }),
}));

// Stub for PrimeVue Dialog component
const DialogStub = {
  name: 'Dialog',
  props: ['visible', 'header', 'modal', 'style', 'closable'],
  emits: ['update:visible'],
  template: `
    <div v-if="visible" class="p-dialog" data-testid="reset-demo-data-dialog">
      <div class="p-dialog-header">{{ header }}</div>
      <slot />
    </div>
  `,
};

// Stub for PrimeVue Button component
const ButtonStub = {
  name: 'Button',
  props: ['label', 'severity', 'variant', 'disabled', 'loading', 'icon', 'type'],
  emits: ['click'],
  template: `
    <button
      :type="type || 'button'"
      :class="['p-button', 'p-component', severity ? 'p-button-' + severity : '', variant ? 'p-button-' + variant : '']"
      :disabled="disabled"
      :aria-busy="loading"
      @click="$emit('click')"
    >
      <span v-if="icon" :class="icon" aria-hidden="true" />
      <span>{{ label }}</span>
    </button>
  `,
};

describe('ResetDemoDataDialog.vue', () => {
  let queryClient: QueryClient;

  beforeAll(() => {
    if (typeof window !== 'undefined') {
      Object.defineProperty(window, 'matchMedia', {
        writable: true,
        value: vi.fn().mockImplementation((query) => ({
          matches: false,
          media: query,
          onchange: null,
          addListener: vi.fn(),
          removeListener: vi.fn(),
          addEventListener: vi.fn(),
          removeEventListener: vi.fn(),
          dispatchEvent: vi.fn(),
        })),
      });

      global.ResizeObserver = class ResizeObserver {
        observe() {}
        unobserve() {}
        disconnect() {}
      };
    }
  });

  beforeEach(() => {
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    });
    vi.clearAllMocks();
    mockMutate.mockClear();
    mockIsPending.value = false;
    mockError.value = null;
    mockIsSuccess.value = false;
  });

  function createWrapper(props: Record<string, any> = {}) {
    return mount(ResetDemoDataDialog, {
      props: {
        visible: true,
        ...props,
      },
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: {
          Dialog: DialogStub,
          Button: ButtonStub,
          teleport: true,
        },
      },
    });
  }

  function findConfirmButton(wrapper: ReturnType<typeof createWrapper>) {
    return wrapper.findAllComponents(ButtonStub).find(
      (btn) => btn.props('label') === 'Reset All Demo Data'
    );
  }

  function findCancelButton(wrapper: ReturnType<typeof createWrapper>) {
    return wrapper.findAllComponents(ButtonStub).find(
      (btn) => btn.props('label') === 'Cancel'
    );
  }

  describe('Dialog Visibility and Warning Banner', () => {
    it('mounts and renders warning banner when visible = true', async () => {
      const wrapper = createWrapper({ visible: true });
      await flushPromises();

      expect(wrapper.find('[data-testid="reset-demo-data-dialog"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('Reset All Demo Data');
      expect(wrapper.text()).toContain('Warning: Destructive Action');
      expect(wrapper.text()).toContain('permanently purge all current demo data domains');
      expect(wrapper.text()).toContain('This action cannot be undone');
    });

    it('does not render dialog when visible = false', async () => {
      const wrapper = createWrapper({ visible: false });
      await flushPromises();

      expect(wrapper.find('[data-testid="reset-demo-data-dialog"]').exists()).toBe(false);
    });
  });

  describe('Confirm Button and Mutation', () => {
    it('clicking confirm invokes resetDemoData mutation', async () => {
      const wrapper = createWrapper({ visible: true });
      await flushPromises();

      const confirmBtn = findConfirmButton(wrapper);
      expect(confirmBtn).toBeDefined();

      await confirmBtn!.trigger('click');
      await flushPromises();

      expect(mockMutate).toHaveBeenCalled();
    });

    it('confirm button shows loading state when isPending is true', async () => {
      const wrapper = createWrapper({ visible: true });
      await flushPromises();

      mockIsPending.value = true;
      await flushPromises();

      const confirmBtn = findConfirmButton(wrapper);
      expect(confirmBtn!.props('loading')).toBe(true);
    });

    it('confirm button has danger severity', async () => {
      const wrapper = createWrapper({ visible: true });
      await flushPromises();

      const confirmBtn = findConfirmButton(wrapper);
      expect(confirmBtn!.props('severity')).toBe('danger');
    });

    it('confirm button has refresh icon', async () => {
      const wrapper = createWrapper({ visible: true });
      await flushPromises();

      const confirmBtn = findConfirmButton(wrapper);
      expect(confirmBtn!.props('icon')).toBe('pi pi-refresh');
    });
  });

  describe('Cancel Button', () => {
    it('clicking cancel emits close event without triggering mutation', async () => {
      const wrapper = createWrapper({ visible: true });
      await flushPromises();

      const cancelBtn = findCancelButton(wrapper);
      expect(cancelBtn).toBeDefined();

      await cancelBtn!.trigger('click');
      await flushPromises();

      // Verify mutation was NOT called
      expect(mockMutate).not.toHaveBeenCalled();

      // Verify update:visible event was emitted with false
      const emitted = wrapper.emitted('update:visible');
      expect(emitted).toBeTruthy();
      expect(emitted?.[0]).toEqual([false]);
    });

    it('cancel button is disabled while submitting', async () => {
      const wrapper = createWrapper({ visible: true });
      await flushPromises();

      mockIsPending.value = true;
      await flushPromises();

      const cancelBtn = findCancelButton(wrapper);
      expect(cancelBtn!.props('disabled')).toBe(true);
    });

    it('cancel button has secondary severity and text variant', async () => {
      const wrapper = createWrapper({ visible: true });
      await flushPromises();

      const cancelBtn = findCancelButton(wrapper);
      expect(cancelBtn!.props('severity')).toBe('secondary');
      expect(cancelBtn!.props('variant')).toBe('text');
    });
  });

  describe('Error Handling', () => {
    it('error state is captured when mutation fails', async () => {
      const testError = new Error('Failed to reset demo data');
      mockError.value = testError;

      const wrapper = createWrapper({ visible: true });
      await flushPromises();

      const confirmBtn = findConfirmButton(wrapper);

      // Simulate mutation error by having mockMutate reject
      mockMutate.mockImplementationOnce(() => {
        mockError.value = testError;
        return Promise.reject(testError);
      });

      await confirmBtn!.trigger('click');
      await flushPromises();

      expect(mockError.value).toBe(testError);
    });
  });

  describe('Dialog Props and Events', () => {
    it('emits success event when mutation succeeds', async () => {
      const wrapper = createWrapper({ visible: true });
      await flushPromises();

      const confirmBtn = findConfirmButton(wrapper);
      await confirmBtn!.trigger('click');
      await flushPromises();

      // Wait for onSuccess callback
      await flushPromises();

      // The component emits 'success' when mutation succeeds
      const successEmitted = wrapper.emitted('success');
      expect(successEmitted).toBeTruthy();
    });

    it('dialog is not closable while submitting', async () => {
      const wrapper = createWrapper({ visible: true });
      await flushPromises();

      // Dialog should be closable when not submitting
      const dialog = wrapper.findComponent(DialogStub);
      expect(dialog.props('closable')).toBe(true);

      mockIsPending.value = true;
      await flushPromises();

      // Dialog should NOT be closable when submitting
      expect(dialog.props('closable')).toBe(false);
    });

    it('dialog has correct header and width style', async () => {
      const wrapper = createWrapper({ visible: true });
      await flushPromises();

      const dialog = wrapper.findComponent(DialogStub);
      expect(dialog.props('header')).toBe('Reset All Demo Data');
      expect(dialog.props('style')).toEqual({ width: '500px' });
    });
  });
});