// @vitest-environment jsdom
import { describe, it, expect, beforeEach, beforeAll, vi, afterEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import {
  CreatePeriodModal,
  ApplyPresetModal,
  CorrectRecordModal,
} from '../index';
import { useAuthStore } from '@/app/stores/auth.store';
import { CALENDAR_PERMISSIONS } from '../../../constants/permissions';
import * as academicYearApi from '../../../api/academicYear.api';
import * as operationalPeriodApi from '../../../api/operationalPeriod.api';
import * as amendmentApi from '../../../api/amendment.api';

const mockToastAdd = vi.fn();

vi.mock('primevue/usetoast', () => ({
  useToast: () => ({
    add: mockToastAdd,
  }),
}));

const VALID_AY_ID = '11111111-1111-1111-1111-111111111111';
const VALID_PERIOD_ID = '22222222-2222-2222-2222-222222222222';

describe('Operational Period & Record Correction Modals', () => {
  let queryClient: QueryClient;

  beforeAll(() => {
    if (typeof window !== 'undefined') {
      window.matchMedia = (query: string) =>
        ({
          matches: false,
          media: query,
          onchange: null,
          addListener: () => {},
          removeListener: () => {},
          addEventListener: () => {},
          removeEventListener: () => {},
          dispatchEvent: () => false,
        }) as unknown as MediaQueryList;

      global.ResizeObserver = class ResizeObserver {
        observe() {}
        unobserve() {}
        disconnect() {}
      };
    }
  });

  beforeEach(() => {
    if (typeof window !== 'undefined') {
      window.matchMedia = (query: string) =>
        ({
          matches: false,
          media: query,
          onchange: null,
          addListener: () => {},
          removeListener: () => {},
          addEventListener: () => {},
          removeEventListener: () => {},
          dispatchEvent: () => false,
        }) as unknown as MediaQueryList;
    }

    const pinia = createPinia();
    setActivePinia(pinia);

    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: Infinity },
      },
    });

    const authStore = useAuthStore();
    authStore.setSession('jwt.mock.token', {
      userId: 'usr-1',
      username: 'headmaster',
      fullName: 'Headmaster Academic',
      roles: ['OperationsAuthority'],
      permissions: [CALENDAR_PERMISSIONS.MANAGE, CALENDAR_PERMISSIONS.CORRECT_RECORD],
      campusId: null,
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
    queryClient.clear();
  });

  describe('CreatePeriodModal.vue', () => {
    it('renders input fields for period code, name, dates, type, and parent', async () => {
      const wrapper = mount(CreatePeriodModal, {
        props: {
          visible: true,
          academicYearId: VALID_AY_ID,
          periods: [],
        },
        global: {
          plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
          stubs: { teleport: true },
        },
      });
      await flushPromises();

      expect(wrapper.find('[data-testid="period-code-input"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="period-name-input"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="period-start-date-input"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="period-end-date-input"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="period-type-select"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="parent-period-select"]').exists()).toBe(true);
    });

    it('submits valid period data calling createPeriod API', async () => {
      const createSpy = vi.spyOn(operationalPeriodApi, 'createPeriod').mockResolvedValue({
        id: VALID_PERIOD_ID,
        academicYearId: VALID_AY_ID,
        periodCode: 'SEM-1',
        name: 'Semester 1',
        periodType: 'semester',
        startDate: '2026-07-15',
        endDate: '2026-12-20',
        parentPeriodId: null,
        campusContext: 'All',
        status: 'draft',
      });

      const wrapper = mount(CreatePeriodModal, {
        props: {
          visible: true,
          academicYearId: VALID_AY_ID,
          periods: [],
        },
        global: {
          plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
          stubs: { teleport: true },
        },
      });
      await flushPromises();

      await wrapper.find('[data-testid="period-code-input"]').setValue('SEM-1');
      await wrapper.find('[data-testid="period-name-input"]').setValue('Semester 1');
      await wrapper.find('[data-testid="period-start-date-input"]').setValue('2026-07-15');
      await wrapper.find('[data-testid="period-end-date-input"]').setValue('2026-12-20');

      await (wrapper.vm as { onSubmit: () => Promise<void> }).onSubmit();
      await flushPromises();

      expect(createSpy).toHaveBeenCalledWith(
        VALID_AY_ID,
        expect.objectContaining({
          periodCode: 'SEM-1',
          name: 'Semester 1',
          startDate: '2026-07-15',
          endDate: '2026-12-20',
        }),
      );
    });
  });

  describe('ApplyPresetModal.vue', () => {
    it('renders preset summary and calls applyPreset on confirm', async () => {
      const presetSpy = vi.spyOn(academicYearApi, 'applyPreset').mockResolvedValue({
        academicYearId: VALID_AY_ID,
        presetName: 'Standard Indonesian 2-Semester',
        periodsCreated: 2,
      });

      const wrapper = mount(ApplyPresetModal, {
        props: {
          visible: true,
          academicYearId: VALID_AY_ID,
          academicYearCode: 'AY-2026-2027',
        },
        global: {
          plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
          stubs: { teleport: true },
        },
      });
      await flushPromises();

      expect(wrapper.text()).toContain('Standard Indonesian 2-Semester Structure');
      expect(wrapper.text()).toContain('AY-2026-2027');

      const confirmBtn = wrapper.find('[data-testid="modal-confirm-preset-btn"]');
      expect(confirmBtn.exists()).toBe(true);
      await confirmBtn.trigger('click');
      await flushPromises();

      expect(presetSpy).toHaveBeenCalledWith(VALID_AY_ID);
      expect(wrapper.emitted('update:visible')?.[0]).toEqual([false]);
      expect(wrapper.emitted('applied')?.[0]).toBeDefined();
    });
  });

  describe('CorrectRecordModal.vue (UC-OPS-13)', () => {
    it('renders target table, record, field, and justification inputs', async () => {
      const wrapper = mount(CorrectRecordModal, {
        props: {
          visible: true,
          academicYearId: VALID_AY_ID,
          targetTableName: 'operational_periods',
          targetRecordId: VALID_PERIOD_ID,
          fieldName: 'name',
          oldValue: 'Old Name',
        },
        global: {
          plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
          stubs: { teleport: true },
        },
      });
      await flushPromises();

      expect(wrapper.find('[data-testid="target-table-select"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="field-name-select"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="new-value-input"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="justification-reason-input"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="authorized-by-input"]').exists()).toBe(true);
    });

    it('submits authorized record correction calling submitCorrection API', async () => {
      const correctSpy = vi.spyOn(amendmentApi, 'submitCorrection').mockResolvedValue({
        id: '33333333-3333-3333-3333-333333333333',
        academicYearId: VALID_AY_ID,
        targetTableName: 'operational_periods',
        targetRecordId: VALID_PERIOD_ID,
        fieldName: 'name',
        oldValue: 'Old Name',
        newValue: 'Corrected Period Name',
        justificationReason: 'Correction authorized per Ministry guidelines',
        authorizedBy: 'headmaster',
        authorizedAt: '2026-11-02T00:00:00Z',
      });

      const wrapper = mount(CorrectRecordModal, {
        props: {
          visible: true,
          academicYearId: VALID_AY_ID,
          targetTableName: 'operational_periods',
          targetRecordId: VALID_PERIOD_ID,
          fieldName: 'name',
          oldValue: 'Old Name',
          periods: [
            {
              id: VALID_PERIOD_ID,
              academicYearId: VALID_AY_ID,
              periodCode: 'SEM-1',
              name: 'Old Name',
              periodType: 'semester',
              startDate: '2026-07-15',
              endDate: '2026-12-20',
              parentPeriodId: null,
              campusContext: 'All',
              status: 'active',
            },
          ],
        },
        global: {
          plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
          stubs: { teleport: true },
        },
      });
      await flushPromises();

      await wrapper.find('[data-testid="new-value-input"]').setValue('Corrected Period Name');
      await wrapper
        .find('[data-testid="justification-reason-input"]')
        .setValue('Correction authorized per Ministry guidelines');

      await (wrapper.vm as { onSubmit: () => Promise<void> }).onSubmit();
      await flushPromises();

      expect(correctSpy).toHaveBeenCalledWith(
        expect.objectContaining({
          targetTableName: 'operational_periods',
          targetRecordId: VALID_PERIOD_ID,
          fieldName: 'name',
          newValue: 'Corrected Period Name',
          justificationReason: 'Correction authorized per Ministry guidelines',
        }),
      );
      expect(wrapper.emitted('update:visible')?.[0]).toEqual([false]);
      expect(wrapper.emitted('corrected')?.[0]).toBeDefined();
    });
  });
});
