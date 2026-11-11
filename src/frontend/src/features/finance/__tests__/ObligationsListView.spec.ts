// @vitest-environment jsdom
import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ObligationsListView from '../obligations/views/ObligationsListView.vue';
import * as obligationsApi from '../obligations/api/obligations.api';
import type { FinancialObligationDto } from '../obligations/types/obligations.types';

let currentRouteQuery: Record<string, string> = {};

const mockPush = vi.fn().mockResolvedValue(true);
vi.mock('vue-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('vue-router')>();
  return {
    ...actual,
    useRouter: () => ({
      push: mockPush,
    }),
    useRoute: () => ({
      params: {},
      query: currentRouteQuery,
    }),
  };
});

const mockToastAdd = vi.fn();
vi.mock('primevue/usetoast', () => ({
  useToast: () => ({
    add: mockToastAdd,
  }),
}));

describe('ObligationsListView.vue', () => {
  let queryClient: QueryClient;

  const mockObligations: FinancialObligationDto[] = [
    {
      id: 'obl-1',
      obligationNumber: 'OBL-2026-001',
      studentAccountId: 'acc-1',
      studentId: 'stu-1',
      studentNumber: '2026-001',
      studentFullName: 'Grace Santoso',
      policyId: 'pol-1',
      policyCode: 'POL-TUITION',
      chargeRuleId: 'cr-1',
      chargeRuleCode: 'CR-SPP',
      description: 'SPP Bulanan - August 2026',
      chargeType: 'tuition',
      isMandatory: true,
      originalAmount: 1500000,
      outstandingAmount: 1500000,
      agingBracket: '90+ Days',
      currencyCode: 'IDR',
      issueDate: '2026-08-08',
      dueDate: '2026-08-08',
      academicPeriodId: 'sem-2',
      academicPeriodLabel: 'Semester 2 TP 2026/2027',
      status: 'assessed',
      authorizedBy: 'SYSTEM',
      notes: null,
      createdAt: '2026-08-08T00:00:00Z',
      schedules: [],
    },
    {
      id: 'obl-2',
      obligationNumber: 'OBL-2026-002',
      studentAccountId: 'acc-2',
      studentId: 'stu-2',
      studentNumber: '2026-002',
      studentFullName: 'Budi Kurniawan',
      policyId: 'pol-1',
      policyCode: 'POL-TUITION',
      chargeRuleId: 'cr-1',
      chargeRuleCode: 'CR-SPP',
      description: 'SPP Bulanan - August 2026',
      chargeType: 'tuition',
      isMandatory: true,
      originalAmount: 1500000,
      outstandingAmount: 1500000,
      agingBracket: '90+ Days',
      currencyCode: 'IDR',
      issueDate: '2026-08-09',
      dueDate: '2026-08-09',
      academicPeriodId: 'sem-2',
      academicPeriodLabel: 'Semester 2 TP 2026/2027',
      status: 'assessed',
      authorizedBy: 'SYSTEM',
      notes: null,
      createdAt: '2026-08-09T00:00:00Z',
      schedules: [],
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    currentRouteQuery = {};
    if (!window.matchMedia) {
      Object.defineProperty(window, 'matchMedia', {
        writable: true,
        value: vi.fn().mockImplementation((query: string) => ({
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
    }
    const pinia = createPinia();
    setActivePinia(pinia);
    queryClient = new QueryClient({
      defaultOptions: {
        queries: {
          retry: false,
        },
      },
    });
  });

  afterEach(() => {
    queryClient.clear();
  });

  function mountView() {
    return mount(ObligationsListView, {
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: {
          PageHeader: {
            template: '<header><slot /><slot name="actions" /></header>',
          },
          LoadingSpinner: {
            template: '<div data-testid="loading-spinner" />',
          },
          EmptyState: {
            template: '<div data-testid="empty-state">{{ title }}</div>',
            props: ['title', 'description'],
          },
          StatusPill: {
            template: '<span data-testid="status-pill">{{ status }}</span>',
            props: ['status'],
          },
        },
      },
    });
  }

  it('hydrates aging filter from ?aging=90%2B%20Days and requests matching obligations', async () => {
    currentRouteQuery = { aging: '90+ Days' };

    const getObligationsSpy = vi.spyOn(obligationsApi, 'getObligations').mockResolvedValue({
      items: mockObligations,
      pageNumber: 1,
      pageSize: 100,
      totalCount: 2,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
    });

    const wrapper = mountView();
    await flushPromises();

    expect(getObligationsSpy).toHaveBeenCalledWith(
      expect.objectContaining({
        aging: '90+ Days',
      }),
    );

    const countElement = wrapper.find('[data-testid="obligations-count"]');
    expect(countElement.text()).toBe('2');

    expect(wrapper.text()).toContain('Grace Santoso');
    expect(wrapper.text()).toContain('Budi Kurniawan');
    expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(false);
  });
});
