// @vitest-environment jsdom
import { describe, it, expect, beforeEach, beforeAll, vi, afterEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import PeriodsTreeTab from '../components/tabs/PeriodsTreeTab.vue';
import { CreatePeriodModal, ApplyPresetModal, CorrectRecordModal } from '../components/modals';
import { useAuthStore } from '@/app/stores/auth.store';
import { CALENDAR_PERMISSIONS } from '../constants/permissions';
import * as academicYearApi from '../api/academicYear.api';
import * as operationalPeriodApi from '../api/operationalPeriod.api';

const mockToastAdd = vi.fn();

vi.mock('primevue/usetoast', () => ({
  useToast: () => ({
    add: mockToastAdd,
  }),
}));

const mockAcademicYear: academicYearApi.AcademicYearDto = {
  id: 'ay-active-1',
  code: 'AY-2026-2027',
  name: 'Tahun Pelajaran 2026/2027',
  startDate: '2026-07-01',
  endDate: '2027-06-30',
  status: 'active',
  isActive: true,
  approvedBy: 'Dr. Jane Doe',
  approvedDate: '2026-06-15',
  notes: 'Adopted per Senate Resolution 42',
};

const mockPeriods: operationalPeriodApi.OperationalPeriodDto[] = [
  {
    id: 'period-sem-1',
    academicYearId: 'ay-active-1',
    periodCode: 'SEM-1',
    name: 'Semester 1 (Ganjil)',
    periodType: 'semester',
    startDate: '2026-07-15',
    endDate: '2026-12-20',
    parentPeriodId: null,
    campusContext: 'All',
    status: 'active',
    notes: 'Main semester 1',
  },
  {
    id: 'period-term-1',
    academicYearId: 'ay-active-1',
    periodCode: 'TERM-1A',
    name: 'Term 1 Mid-Semester',
    periodType: 'term',
    startDate: '2026-07-15',
    endDate: '2026-09-30',
    parentPeriodId: 'period-sem-1',
    campusContext: 'All',
    status: 'active',
  },
  {
    id: 'period-adm-1',
    academicYearId: 'ay-active-1',
    periodCode: 'ADM-2026',
    name: 'Early Bird Admission Window',
    periodType: 'admission_window',
    startDate: '2026-08-01',
    endDate: '2026-08-31',
    parentPeriodId: 'period-sem-1',
    campusContext: 'All',
    status: 'active',
  },
  {
    id: 'period-sem-2',
    academicYearId: 'ay-active-1',
    periodCode: 'SEM-2',
    name: 'Semester 2 (Genap)',
    periodType: 'semester',
    startDate: '2027-01-05',
    endDate: '2027-06-25',
    parentPeriodId: null,
    campusContext: 'All',
    status: 'draft',
  },
];

describe('PeriodsTreeTab.spec.ts (Tab 2: Academic Years & Hierarchical Periods)', () => {
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
        queries: {
          retry: false,
          gcTime: Infinity,
        },
      },
    });

    vi.spyOn(academicYearApi, 'getAcademicYears').mockResolvedValue([mockAcademicYear]);
    vi.spyOn(academicYearApi, 'getAcademicYearById').mockResolvedValue(mockAcademicYear);
    vi.spyOn(operationalPeriodApi, 'getPeriods').mockResolvedValue(mockPeriods);

    const authStore = useAuthStore();
    authStore.setSession('jwt.mock.token', {
      userId: 'usr-1',
      username: 'headmaster',
      fullName: 'Headmaster Academic',
      roles: ['OperationsAuthority'],
      permissions: [
        CALENDAR_PERMISSIONS.VIEW,
        CALENDAR_PERMISSIONS.MANAGE,
        CALENDAR_PERMISSIONS.APPROVE,
        CALENDAR_PERMISSIONS.CORRECT_RECORD,
      ],
      campusId: null,
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
    queryClient.clear();
  });

  function renderComponent(academicYearId = 'ay-active-1') {
    return mount(PeriodsTreeTab, {
      props: {
        academicYearId,
      },
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: {
          teleport: true,
        },
      },
    });
  }

  it('renders the Academic Year summary card with code, name, dates, and approver info', async () => {
    const wrapper = renderComponent();
    await flushPromises();

    expect(wrapper.find('[data-testid="academic-year-summary-card"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="ay-summary-code"]').text()).toContain('AY-2026-2027');
    expect(wrapper.find('[data-testid="ay-summary-name"]').text()).toContain(
      'Tahun Pelajaran 2026/2027',
    );
    expect(wrapper.find('[data-testid="ay-summary-dates"]').text()).toContain('2026-07-01');
    expect(wrapper.find('[data-testid="ay-summary-dates"]').text()).toContain('2027-06-30');
    expect(wrapper.find('[data-testid="ay-summary-approver"]').text()).toContain('Dr. Jane Doe');
    expect(wrapper.find('[data-testid="ay-summary-notes"]').text()).toContain(
      'Senate Resolution 42',
    );
  });

  it('renders primary Tab 2 action buttons: Add Period, Apply Preset, Correct Record', async () => {
    const wrapper = renderComponent();
    await flushPromises();

    expect(wrapper.find('[data-testid="tab-action-add-period-btn"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="tab-action-apply-preset-btn"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="tab-action-correct-record-btn"]').exists()).toBe(true);
  });

  it('renders hierarchical TreeTable with operational periods', async () => {
    const wrapper = renderComponent();
    await flushPromises();

    expect(wrapper.find('[data-testid="periods-treetable"]').exists()).toBe(true);
    const text = wrapper.find('[data-testid="periods-treetable"]').text();
    expect(text).toContain('Semester 1 (Ganjil)');
    expect(text).toContain('SEM-1');
    expect(text).toContain('Semester 2 (Genap)');
    expect(text).toContain('SEM-2');
  });

  it('opens CreatePeriodModal when Add Period button is clicked', async () => {
    const wrapper = renderComponent();
    await flushPromises();

    const addBtn = wrapper.find('[data-testid="tab-action-add-period-btn"]');
    await addBtn.trigger('click');
    await flushPromises();

    const createModal = wrapper.findComponent(CreatePeriodModal);
    expect(createModal.exists()).toBe(true);
    expect(createModal.props('visible')).toBe(true);
  });

  it('opens ApplyPresetModal when Apply Preset button is clicked', async () => {
    const wrapper = renderComponent();
    await flushPromises();

    const presetBtn = wrapper.find('[data-testid="tab-action-apply-preset-btn"]');
    await presetBtn.trigger('click');
    await flushPromises();

    const presetModal = wrapper.findComponent(ApplyPresetModal);
    expect(presetModal.exists()).toBe(true);
    expect(presetModal.props('visible')).toBe(true);
  });

  it('opens CorrectRecordModal when Correct Record button is clicked', async () => {
    const wrapper = renderComponent();
    await flushPromises();

    const correctBtn = wrapper.find('[data-testid="tab-action-correct-record-btn"]');
    await correctBtn.trigger('click');
    await flushPromises();

    const correctModal = wrapper.findComponent(CorrectRecordModal);
    expect(correctModal.exists()).toBe(true);
    expect(correctModal.props('visible')).toBe(true);
  });

  it('opens CorrectRecordModal with target academic_years when Correct Year Record is clicked', async () => {
    const wrapper = renderComponent();
    await flushPromises();

    const correctYearBtn = wrapper.find('[data-testid="action-correct-year-btn"]');
    expect(correctYearBtn.exists()).toBe(true);
    await correctYearBtn.trigger('click');
    await flushPromises();

    const correctModal = wrapper.findComponent(CorrectRecordModal);
    expect(correctModal.exists()).toBe(true);
    expect(correctModal.props('visible')).toBe(true);
    expect(correctModal.props('targetTableName')).toBe('academic_years');
    expect(correctModal.props('targetRecordId')).toBe('ay-active-1');
  });

  it('renders empty state when academic year has no operational periods', async () => {
    vi.spyOn(operationalPeriodApi, 'getPeriods').mockResolvedValue([]);

    const wrapper = renderComponent();
    await flushPromises();

    expect(wrapper.find('[data-testid="periods-tree-empty"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="empty-apply-preset-btn"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="empty-add-period-btn"]').exists()).toBe(true);
  });

  it('renders Activate Period button for draft operational periods and invokes mutation on click', async () => {
    const activateSpy = vi
      .spyOn(operationalPeriodApi, 'activatePeriod')
      .mockResolvedValue({ ...mockPeriods[3], status: 'active' });

    const wrapper = renderComponent();
    await flushPromises();

    // period-sem-2 has status: 'draft', others have 'active'
    const activateBtns = wrapper.findAll('[data-testid="row-activate-period-btn"]');
    expect(activateBtns.length).toBe(1);

    await activateBtns[0].trigger('click');
    await flushPromises();

    expect(activateSpy).toHaveBeenCalledWith('period-sem-2');
  });

  it('does not render Activate Period button for active periods without draft or approved status', async () => {
    vi.spyOn(operationalPeriodApi, 'getPeriods').mockResolvedValue([mockPeriods[0]]);

    const wrapper = renderComponent();
    await flushPromises();

    const activateBtns = wrapper.findAll('[data-testid="row-activate-period-btn"]');
    expect(activateBtns.length).toBe(0);
  });

  it('does not render Activate Period button when user lacks MANAGE permission', async () => {
    const authStore = useAuthStore();
    authStore.setSession('jwt.mock.token', {
      userId: 'usr-readonly',
      username: 'observer',
      fullName: 'Observer User',
      roles: ['OperationsViewer'],
      permissions: [CALENDAR_PERMISSIONS.VIEW],
      campusId: null,
    });

    const wrapper = renderComponent();
    await flushPromises();

    const activateBtns = wrapper.findAll('[data-testid="row-activate-period-btn"]');
    expect(activateBtns.length).toBe(0);
  });
});
