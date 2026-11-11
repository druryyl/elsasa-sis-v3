// @vitest-environment jsdom
import { describe, it, expect, beforeAll, beforeEach, afterEach, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import PrimeVue from 'primevue/config';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import DomainKpiDashboardShell from '../components/DomainKpiDashboardShell.vue';
import * as dashboardApi from '../api/dashboard.api';
import {
  createDomainKpiDashboardFixture,
  ensureMatchMediaPolyfill,
} from './dashboard.fixtures';
import { useAppContextStore } from '@/app/stores/appContext.store';

vi.mock('../api/dashboard.api', () => ({
  getExecutiveDashboard: vi.fn(),
  getDomainKpiDashboard: vi.fn(),
}));

describe('DomainKpiDashboardShell.vue', () => {
  let queryClient: QueryClient;

  beforeAll(() => {
    ensureMatchMediaPolyfill();
  });

  beforeEach(() => {
    localStorage.clear();
    setActivePinia(createPinia());
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
      },
    });
    vi.clearAllMocks();
  });

  afterEach(() => {
    queryClient.clear();
    localStorage.clear();
  });

  function mountShell() {
    return mount(DomainKpiDashboardShell, {
      props: {
        domain: 'finance',
        title: 'Finance KPI Dashboard',
        subtitle: 'Tuition collection, receivables aging, and payment verification telemetry.',
      },
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: {
          RouterLink: {
            template:
              '<a :href="to" :data-testid="$attrs[\'data-testid\']" :class="$attrs.class"><slot /></a>',
            props: ['to'],
          },
        },
      },
    });
  }

  it('renders domain KPI cards, horizontal & stacked breakdowns, and attention signals from API payload', async () => {
    const fixture = createDomainKpiDashboardFixture();
    vi.mocked(dashboardApi.getDomainKpiDashboard).mockResolvedValue(fixture);

    const appContext = useAppContextStore();
    appContext.setActiveCampus('campus-main');

    const wrapper = mountShell();
    await flushPromises();

    expect(dashboardApi.getDomainKpiDashboard).toHaveBeenCalledWith(
      'finance',
      expect.objectContaining({ campus: 'campus-main' }),
    );
    expect(wrapper.find('[data-testid="finance-kpi-dashboard-view"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="back-to-executive-link"]').attributes('href')).toBe(
      '/dashboard',
    );
    expect(wrapper.find('[data-testid="kpi-metric-card-FIN-01"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="kpi-metric-card-FIN-02"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="kpi-breakdown-chart-FIN-AGING"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="kpi-breakdown-chart-TCH-WORKLOAD"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="attention-item-FIN_OVERDUE_90_PLUS"]').exists()).toBe(true);
  });

  it('renders skeleton states while loading and error-retry state on rejection', async () => {
    vi.mocked(dashboardApi.getDomainKpiDashboard).mockRejectedValueOnce(
      new Error('Finance KPI service timeout'),
    );

    const wrapper = mountShell();

    expect(wrapper.find('[data-testid="domain-headline-kpis-skeleton"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="domain-breakdowns-skeleton"]').exists()).toBe(true);

    await flushPromises();

    const errorBanner = wrapper.find('[data-testid="domain-dashboard-error-state"]');
    expect(errorBanner.exists()).toBe(true);
    expect(errorBanner.text()).toContain('Finance KPI service timeout');

    vi.mocked(dashboardApi.getDomainKpiDashboard).mockResolvedValueOnce(
      createDomainKpiDashboardFixture(),
    );
    await wrapper.find('[data-testid="domain-dashboard-retry-button"]').trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-testid="domain-dashboard-error-state"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="kpi-metric-card-FIN-01"]').exists()).toBe(true);
  });
});
