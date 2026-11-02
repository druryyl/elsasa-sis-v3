// @vitest-environment jsdom
import { describe, it, expect, beforeEach, beforeAll, vi, afterEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import DeclareClosureModal from '../DeclareClosureModal.vue';
import { useAuthStore } from '@/app/stores/auth.store';
import * as closureApi from '../../../api/closure.api';

const mockToastAdd = vi.fn();

vi.mock('primevue/usetoast', () => ({
  useToast: () => ({
    add: mockToastAdd,
  }),
}));

const mockCreatedClosure: closureApi.SchoolClosureDto = {
  id: 'cl-new-1',
  academicYearId: 'ay-1',
  closureNumber: 'CL-2025-003',
  reasonCategory: 'weather_disaster',
  reasonDetails: 'Typhoon alert requiring full campus shutdown.',
  startDate: '2025-11-10',
  endDate: '2025-11-12',
  affectedCampus: 'All',
  affectedPopulation: 'all',
  status: 'in_effect',
  authorizedBy: 'Operations Authority',
  authorizedDate: '2025-11-09',
  communicationStatus: 'pending',
  recoveryPlanNotes: 'Synchronous online make-up sessions.',
};

describe('DeclareClosureModal.vue', () => {
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
    vi.clearAllMocks();
    mockToastAdd.mockReset();

    const pinia = createPinia();
    setActivePinia(pinia);

    const authStore = useAuthStore();
    authStore.setSession('mock-token', {
      userId: 'usr-1',
      username: 'ops_admin',
      fullName: 'Operations Authority',
      roles: ['OperationsAuthority'],
      permissions: ['*'],
      campusId: null,
    });

    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
      },
    });

    vi.spyOn(closureApi, 'declareClosure').mockResolvedValue(mockCreatedClosure);
  });

  afterEach(() => {
    queryClient.clear();
    vi.restoreAllMocks();
  });

  function createWrapper(props = { visible: true, academicYearId: 'ay-1' }) {
    return mount(DeclareClosureModal, {
      props,
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
      },
    });
  }

  it('renders all form input controls when visible', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    expect(wrapper.find('[data-testid="declare-closure-modal"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="modal-closure-category-select"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="modal-closure-start-input"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="modal-closure-end-input"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="modal-closure-campus-input"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="modal-closure-population-select"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="modal-closure-justification-input"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="modal-closure-recovery-input"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="modal-submit-closure-btn"]').exists()).toBe(true);
  });

  it('validates required dates and justification length before submission', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    // Trigger submit with empty dates
    await wrapper.find('form').trigger('submit.prevent');
    await flushPromises();

    expect(wrapper.find('[data-testid="closure-error-banner"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Both start date and end date are required');
    expect(closureApi.declareClosure).not.toHaveBeenCalled();
  });

  it('validates date chronology (end date >= start date)', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    await wrapper.find('[data-testid="modal-closure-start-input"]').setValue('2025-11-15');
    await wrapper.find('[data-testid="modal-closure-end-input"]').setValue('2025-11-10');
    await wrapper.find('[data-testid="modal-closure-justification-input"]').setValue('Valid justification message');

    await wrapper.find('form').trigger('submit.prevent');
    await flushPromises();

    expect(wrapper.find('[data-testid="closure-error-banner"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('End date cannot be earlier than start date');
    expect(closureApi.declareClosure).not.toHaveBeenCalled();
  });

  it('submits valid closure form and emits declared event', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    await wrapper.find('[data-testid="modal-closure-category-select"]').setValue('weather_disaster');
    await wrapper.find('[data-testid="modal-closure-start-input"]').setValue('2025-11-10');
    await wrapper.find('[data-testid="modal-closure-end-input"]').setValue('2025-11-12');
    await wrapper.find('[data-testid="modal-closure-campus-input"]').setValue('All');
    await wrapper.find('[data-testid="modal-closure-population-select"]').setValue('all');
    await wrapper.find('[data-testid="modal-closure-justification-input"]').setValue('Typhoon alert requiring full campus shutdown.');
    await wrapper.find('[data-testid="modal-closure-recovery-input"]').setValue('Synchronous online make-up sessions.');

    await wrapper.find('form').trigger('submit.prevent');
    await flushPromises();

    expect(closureApi.declareClosure).toHaveBeenCalledWith({
      academicYearId: 'ay-1',
      reasonCategory: 'weather_disaster',
      reasonDetails: 'Typhoon alert requiring full campus shutdown.',
      startDate: '2025-11-10',
      endDate: '2025-11-12',
      affectedCampus: 'All',
      affectedPopulation: 'all',
      authorizedBy: 'ops_admin',
      recoveryPlanNotes: 'Synchronous online make-up sessions.',
    });

    expect(wrapper.emitted('declared')).toBeTruthy();
    expect(wrapper.emitted('declared')?.[0]?.[0]).toEqual(mockCreatedClosure);
  });
});
