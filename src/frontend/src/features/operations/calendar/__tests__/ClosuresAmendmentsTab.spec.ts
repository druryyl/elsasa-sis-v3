// @vitest-environment jsdom
import { describe, it, expect, beforeEach, beforeAll, vi, afterEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ClosuresAmendmentsTab from '../components/tabs/ClosuresAmendmentsTab.vue';
import { useAuthStore } from '@/app/stores/auth.store';
import * as closureApi from '../api/closure.api';
import * as amendmentApi from '../api/amendment.api';

const mockToastAdd = vi.fn();

vi.mock('primevue/usetoast', () => ({
  useToast: () => ({
    add: mockToastAdd,
  }),
}));

const mockClosures: closureApi.SchoolClosureDto[] = [
  {
    id: 'cl-1',
    academicYearId: 'ay-1',
    closureNumber: 'CL-2025-001',
    reasonCategory: 'weather_disaster',
    reasonDetails: 'Severe tropical storm flooding access roads.',
    startDate: '2025-09-10',
    endDate: '2025-09-12',
    affectedCampus: 'All',
    affectedPopulation: 'all',
    status: 'in_effect',
    authorizedBy: 'Operations Authority',
    authorizedDate: '2025-09-09',
    communicationStatus: 'sent',
    recoveryPlanNotes: 'Makeup classes on Saturday.',
  },
  {
    id: 'cl-2',
    academicYearId: 'ay-1',
    closureNumber: 'CL-2025-002',
    reasonCategory: 'infrastructure_hazard',
    reasonDetails: 'Main power transformer maintenance and overhaul.',
    startDate: '2025-10-05',
    endDate: '2025-10-05',
    affectedCampus: 'North Campus',
    affectedPopulation: 'students_only',
    status: 'lifted',
    authorizedBy: 'Campus Rector',
    authorizedDate: '2025-10-04',
    communicationStatus: 'sent',
  },
];

const mockAmendments: amendmentApi.CalendarAmendmentDto[] = [
  {
    id: 'amend-1',
    calendarId: 'cal-1',
    amendmentNumber: 'AMD-2025-001',
    fromVersion: '1.0',
    toVersion: '1.1',
    amendmentType: 'closure_declaration',
    affectedEntryId: 'entry-10',
    changeSummary: 'Declared campus closure CL-2025-001 as non-instructional days.',
    reason: 'Severe weather alert issued by civil protection.',
    effectiveDate: '2025-09-10',
    authorizedBy: 'Operations Authority',
    authorizedDate: '2025-09-09',
    downstreamImpactReviewed: true,
    createdAt: '2025-09-09T08:30:00Z',
  },
];

const mockCorrections: amendmentApi.RecordCorrectionDto[] = [
  {
    id: 'corr-1',
    academicYearId: 'ay-1',
    targetTableName: 'operations.operational_periods',
    targetRecordId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
    fieldName: 'start_date',
    oldValue: '2025-07-15',
    newValue: '2025-07-20',
    justificationReason: 'Corrected administrative clerical error per Academic Senate decree.',
    authorizedBy: 'Academic Registrar',
    authorizedAt: '2025-07-10T14:00:00Z',
  },
];

describe('ClosuresAmendmentsTab.spec.ts (SCR-OPS-01 Tab 3 Closures & Amendments)', () => {
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
      username: 'admin',
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

    vi.spyOn(closureApi, 'getClosures').mockResolvedValue(mockClosures);
    vi.spyOn(amendmentApi, 'getAmendments').mockResolvedValue(mockAmendments);
    vi.spyOn(amendmentApi, 'getCorrections').mockResolvedValue(mockCorrections);
    vi.spyOn(closureApi, 'liftClosure').mockResolvedValue({
      ...mockClosures[0]!,
      status: 'lifted',
    });
  });

  afterEach(() => {
    queryClient.clear();
    vi.restoreAllMocks();
  });

  function createWrapper(props = { academicYearId: 'ay-1', calendarId: 'cal-1' }) {
    return mount(ClosuresAmendmentsTab, {
      props,
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
      },
    });
  }

  describe('Section 1: Campus Closures Rendering & Actions', () => {
    it('renders the campus closures table with closure rows, status, and categories', async () => {
      const wrapper = createWrapper();
      await flushPromises();

      expect(wrapper.find('[data-testid="section-campus-closures"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="closures-data-table"]').exists()).toBe(true);

      const html = wrapper.html();
      expect(html).toContain('CL-2025-001');
      expect(html).toContain('CL-2025-002');
      expect(html).toContain('Weather Disaster');
      expect(html).toContain('Infrastructure Hazard');
      expect(html).toContain('North Campus');
    });

    it('renders Lift Closure button only for non-lifted closures', async () => {
      const wrapper = createWrapper();
      await flushPromises();

      // cl-1 is in_effect -> Lift button should exist
      const liftBtnCl1 = wrapper.find('[data-testid="btn-lift-closure-cl-1"]');
      expect(liftBtnCl1.exists()).toBe(true);

      // cl-2 is lifted -> Lift button should not exist, shows text "Lifted"
      const liftBtnCl2 = wrapper.find('[data-testid="btn-lift-closure-cl-2"]');
      expect(liftBtnCl2.exists()).toBe(false);
    });

    it('opens lift confirmation dialog and triggers liftClosure mutation on confirm', async () => {
      const wrapper = createWrapper();
      await flushPromises();

      const liftBtn = wrapper.find('[data-testid="btn-lift-closure-cl-1"]');
      await liftBtn.trigger('click');
      await flushPromises();

      // Lift confirmation modal should be visible
      expect(wrapper.find('[data-testid="lift-closure-modal"]').exists()).toBe(true);
      expect(wrapper.html()).toContain('Lifting closure');

      // Click Confirm Lift Closure
      const confirmBtn = wrapper.find('[data-testid="confirm-lift-btn"]');
      await confirmBtn.trigger('click');
      await flushPromises();

      expect(closureApi.liftClosure).toHaveBeenCalledWith('cl-1');
    });

    it('opens DeclareClosureModal when Declare Closure button is clicked', async () => {
      const wrapper = createWrapper();
      await flushPromises();

      const declareBtn = wrapper.find('[data-testid="btn-declare-closure"]');
      expect(declareBtn.exists()).toBe(true);

      await declareBtn.trigger('click');
      await flushPromises();

      expect(wrapper.find('[data-testid="declare-closure-modal"]').exists()).toBe(true);
    });

    it('renders empty state when no closures exist', async () => {
      vi.spyOn(closureApi, 'getClosures').mockResolvedValueOnce([]);

      const wrapper = createWrapper();
      await flushPromises();

      expect(wrapper.find('[data-testid="closures-empty-state"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('No Campus Closures Declared');
    });
  });

  describe('Section 2: Calendar Amendments Timeline & Record Corrections Table', () => {
    it('renders the amendments timeline with version changes, change summaries, and reasons', async () => {
      const wrapper = createWrapper();
      await flushPromises();

      expect(wrapper.find('[data-testid="card-calendar-amendments"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="amendments-timeline"]').exists()).toBe(true);

      const html = wrapper.html();
      expect(html).toContain('v1.0');
      expect(html).toContain('v1.1');
      expect(html).toContain('AMD-2025-001');
      expect(html).toContain('Closure Declared');
      expect(html).toContain('Severe weather alert issued by civil protection.');
    });

    it('renders the operations record corrections table with old/new values and justification', async () => {
      const wrapper = createWrapper();
      await flushPromises();

      expect(wrapper.find('[data-testid="card-record-corrections"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="corrections-data-table"]').exists()).toBe(true);

      const html = wrapper.html();
      expect(html).toContain('operations.operational_periods');
      expect(html).toContain('start_date');
      expect(html).toContain('2025-07-15');
      expect(html).toContain('2025-07-20');
      expect(html).toContain('Academic Senate decree');
      expect(html).toContain('Academic Registrar');
    });

    it('renders empty state for amendments when no amendments are recorded', async () => {
      vi.spyOn(amendmentApi, 'getAmendments').mockResolvedValueOnce([]);

      const wrapper = createWrapper();
      await flushPromises();

      expect(wrapper.find('[data-testid="amendments-empty-state"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('No Calendar Amendments Recorded');
    });

    it('renders empty state for corrections when no corrections exist', async () => {
      vi.spyOn(amendmentApi, 'getCorrections').mockResolvedValueOnce([]);

      const wrapper = createWrapper();
      await flushPromises();

      expect(wrapper.find('[data-testid="corrections-empty-state"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('No Record Corrections Logged');
    });
  });
});
