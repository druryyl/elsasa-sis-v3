// @vitest-environment jsdom
import { describe, it, expect, beforeAll, beforeEach, afterEach, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import PrimeVue from 'primevue/config';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import DashboardView from '../DashboardView.vue';
import * as dashboardApi from '../../api/dashboard.api';
import {
  createExecutiveDashboardFixture,
  ensureMatchMediaPolyfill,
} from '../../__tests__/dashboard.fixtures';
import { useAuthStore } from '@/app/stores/auth.store';
import { useAppContextStore } from '@/app/stores/appContext.store';

vi.mock('../../api/dashboard.api', () => ({
  getExecutiveDashboard: vi.fn(),
  getDomainKpiDashboard: vi.fn(),
}));

describe('DashboardView.vue', () => {
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

  function createWrapper() {
    return mount(DashboardView, {
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

  it('renders executive header, session greeting, quick launchers, and skeleton placeholders while loading', () => {
    vi.mocked(dashboardApi.getExecutiveDashboard).mockImplementation(
      () => new Promise(() => undefined),
    );

    const authStore = useAuthStore();
    authStore.setSession('token-123', {
      userId: 'usr-42',
      username: 'drury',
      fullName: 'Drury Academic Officer',
      roles: ['Admin', 'AcademicCoordinator'],
      permissions: ['*'],
      campusId: 'campus-main',
    });

    const appContext = useAppContextStore();
    appContext.setActiveCampus('campus-east');
    appContext.setActiveAcademicPeriod('per-sem2');

    const wrapper = createWrapper();

    expect(wrapper.find('[data-testid="dashboard-view"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Executive Overview');

    const greetingCard = wrapper.find('[data-testid="session-greeting-card"]');
    expect(greetingCard.exists()).toBe(true);
    expect(greetingCard.text()).toContain('Drury Academic Officer');
    expect(greetingCard.text()).toContain('Admin, AcademicCoordinator');
    expect(wrapper.find('[data-testid="active-campus-label"]').text()).toContain(
      'East Campus (Kampus B)',
    );
    expect(wrapper.find('[data-testid="active-period-label"]').text()).toContain('per-sem2');

    expect(wrapper.find('[data-testid="headline-kpis-skeleton"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="domain-health-skeleton"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="needs-attention-skeleton"]').exists()).toBe(true);
  });

  it('renders 5 headline KPI cards, 6 domain health cards including Stage 3 Development, filter bar, and NeedsAttentionPanel', async () => {
    const fixture = createExecutiveDashboardFixture();
    vi.mocked(dashboardApi.getExecutiveDashboard).mockResolvedValue(fixture);

    const wrapper = createWrapper();
    await flushPromises();

    expect(wrapper.find('[data-testid="kpi-filter-bar"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="active-campus-label"]').text()).toContain(
      'All Campuses (Semua Kampus)',
    );
    expect(wrapper.find('[data-testid="active-period-label"]').text()).toContain(
      'AY-2026-2027 · Semester 2',
    );

    // 5 Headline KPI cards
    const headlineGrid = wrapper.find('[data-testid="headline-kpis-grid"]');
    expect(headlineGrid.exists()).toBe(true);
    expect(headlineGrid.findAll('[data-testid^="kpi-metric-card-"]')).toHaveLength(5);
    for (const code of ['STD-01', 'ACA-01', 'FIN-01', 'TCH-01', 'OPS-01']) {
      expect(headlineGrid.find(`[data-testid="kpi-metric-card-${code}"]`).exists()).toBe(true);
    }

    // 6 Domain Health cards (5 operational + Stage 3 Development placeholder)
    const domainGrid = wrapper.find('[data-testid="domain-health-grid"]');
    expect(domainGrid.exists()).toBe(true);
    expect(domainGrid.findAll('[data-testid^="domain-health-card-"]')).toHaveLength(6);

    const developmentCard = domainGrid.find('[data-testid="domain-health-card-development"]');
    expect(developmentCard.exists()).toBe(true);
    expect(developmentCard.text()).toContain('Stage 3 Pending');
    expect(developmentCard.find('[data-testid="domain-health-link"]').attributes('href')).toBe(
      '/dashboard/development',
    );

    // NeedsAttentionPanel
    const attentionPanel = wrapper.find('[data-testid="needs-attention-panel"]');
    expect(attentionPanel.exists()).toBe(true);
    expect(attentionPanel.find('[data-testid="attention-item-FIN_OVERDUE_90_PLUS"]').exists()).toBe(
      true,
    );
    expect(attentionPanel.find('[data-testid="attention-item-TCH_BLOCKING_CONFLICT"]').exists()).toBe(
      true,
    );
    expect(
      attentionPanel.find('[data-testid="attention-item-OPS_UNREVIEWED_AMENDMENT"]').exists(),
    ).toBe(true);
  });

  it('displays restricted badge for inaccessible domains and updates query filters when school level changes', async () => {
    const initialFixture = createExecutiveDashboardFixture({
      domainSummaries: createExecutiveDashboardFixture().domainSummaries.map((card) =>
        card.domainKey === 'finance' ? { ...card, isAccessible: false } : card,
      ),
    });

    vi.mocked(dashboardApi.getExecutiveDashboard).mockResolvedValue(initialFixture);

    const appContext = useAppContextStore();
    appContext.setActiveCampus('campus-main');

    const wrapper = createWrapper();
    await flushPromises();

    const restrictedFinance = wrapper.find('[data-testid="domain-health-card-finance"]');
    expect(restrictedFinance.find('[data-testid="domain-health-restricted"]').exists()).toBe(true);
    expect(restrictedFinance.find('[data-testid="domain-health-link"]').exists()).toBe(false);

    // Click ELC school level pill to verify reactive TanStack Query refetch with updated filter
    await wrapper.find('[data-testid="school-level-pill-ELC"]').trigger('click');
    await flushPromises();

    expect(dashboardApi.getExecutiveDashboard).toHaveBeenLastCalledWith(
      expect.objectContaining({
        campus: 'campus-main',
        schoolLevel: 'ELC',
      }),
    );
  });

  it('renders error banner on initial fetch failure and recovers when clicking Retry', async () => {
    vi.mocked(dashboardApi.getExecutiveDashboard).mockRejectedValueOnce(
      new Error('Executive KPI endpoint unavailable'),
    );

    const wrapper = createWrapper();
    await flushPromises();

    const errorState = wrapper.find('[data-testid="dashboard-error-state"]');
    expect(errorState.exists()).toBe(true);
    expect(errorState.text()).toContain('Failed to load Executive KPI Dashboard');
    expect(errorState.text()).toContain('Executive KPI endpoint unavailable');

    vi.mocked(dashboardApi.getExecutiveDashboard).mockResolvedValueOnce(
      createExecutiveDashboardFixture(),
    );
    await wrapper.find('[data-testid="dashboard-retry-button"]').trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-testid="dashboard-error-state"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="headline-kpis-grid"]').exists()).toBe(true);
    expect(wrapper.findAll('[data-testid^="kpi-metric-card-"]')).toHaveLength(5);
  });
});
