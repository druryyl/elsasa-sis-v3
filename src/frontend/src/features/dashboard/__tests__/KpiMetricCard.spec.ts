// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import KpiMetricCard from '../components/KpiMetricCard.vue';
import type { KpiMetricCardDto } from '../types/dashboard.types';

const baseMetric: KpiMetricCardDto = {
  code: 'FIN-01',
  label: 'Net Tuition Collection Rate',
  domain: 'finance',
  value: 88.5,
  formattedValue: '88.5%',
  unit: 'percentage',
  priorValue: 84.3,
  deltaPercent: 4.2,
  deltaLabel: '+4.2% vs Sem 1',
  trendDirection: 'up',
  status: 'healthy',
  targetLabel: 'Target: >= 85%',
  subtitle: 'Rp 177.0M collected of Rp 200.0M net billed',
  drillDownRoute: '/finance/obligations?status=paid',
};

function mountMetricCard(metric: KpiMetricCardDto, showDomainBadge = false) {
  return mount(KpiMetricCard, {
    props: { metric, showDomainBadge },
    global: {
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

describe('KpiMetricCard.vue', () => {
  it('renders formatted metric value, code, label, subtitle, and target label', () => {
    const wrapper = mountMetricCard(baseMetric, true);

    expect(wrapper.find('[data-testid="kpi-metric-code"]').text()).toBe('FIN-01');
    expect(wrapper.find('[data-testid="kpi-metric-domain"]').text()).toBe('finance');
    expect(wrapper.find('[data-testid="kpi-metric-label"]').text()).toBe(
      'Net Tuition Collection Rate',
    );
    expect(wrapper.find('[data-testid="kpi-metric-value"]').text()).toBe('88.5%');
    expect(wrapper.find('[data-testid="kpi-metric-subtitle"]').text()).toBe(
      'Rp 177.0M collected of Rp 200.0M net billed',
    );
    expect(wrapper.find('[data-testid="kpi-metric-target"]').text()).toBe('Target: >= 85%');
  });

  it('hides domain badge when showDomainBadge is false', () => {
    const wrapper = mountMetricCard(baseMetric, false);

    expect(wrapper.find('[data-testid="kpi-metric-domain"]').exists()).toBe(false);
  });

  it.each([
    { status: 'healthy', expectedVariant: 'emerald', expectedClass: 'bg-emerald-50', expectedLabel: 'Healthy' },
    { status: 'warning', expectedVariant: 'amber', expectedClass: 'bg-amber-50', expectedLabel: 'Warning' },
    { status: 'critical', expectedVariant: 'rose', expectedClass: 'bg-rose-50', expectedLabel: 'Critical' },
    { status: 'neutral', expectedVariant: 'sky', expectedClass: 'bg-sky-50', expectedLabel: 'Neutral' },
  ])(
    'applies semantic StatusPill classes and label for status "$status"',
    ({ status, expectedVariant, expectedClass, expectedLabel }) => {
      const wrapper = mountMetricCard({ ...baseMetric, status });
      const pill = wrapper.find('[data-testid="kpi-metric-status"]');

      expect(pill.exists()).toBe(true);
      expect(pill.attributes('data-variant')).toBe(expectedVariant);
      expect(pill.classes()).toContain(expectedClass);
      expect(pill.text()).toContain(expectedLabel);
    },
  );

  it.each([
    { trendDirection: 'up', deltaLabel: '+4.2% vs Sem 1', expectedClass: 'bg-emerald-50', textClass: 'text-emerald-700' },
    { trendDirection: 'down', deltaLabel: '-2.1% vs Sem 1', expectedClass: 'bg-rose-50', textClass: 'text-rose-700' },
    { trendDirection: 'flat', deltaLabel: '0.0% vs Sem 1', expectedClass: 'bg-slate-100', textClass: 'text-slate-600' },
  ])(
    'renders trend delta label and directional styling for "$trendDirection"',
    ({ trendDirection, deltaLabel, expectedClass, textClass }) => {
      const wrapper = mountMetricCard({ ...baseMetric, trendDirection, deltaLabel });
      const deltaChip = wrapper.find('[data-testid="kpi-metric-delta"]');

      expect(deltaChip.exists()).toBe(true);
      expect(deltaChip.text()).toContain(deltaLabel);
      expect(deltaChip.classes()).toContain(expectedClass);
      expect(deltaChip.classes()).toContain(textClass);
    },
  );

  it('omits trend delta chip when deltaLabel is null', () => {
    const wrapper = mountMetricCard({ ...baseMetric, deltaLabel: null, deltaPercent: null });

    expect(wrapper.find('[data-testid="kpi-metric-delta"]').exists()).toBe(false);
  });

  it('renders drill-down navigation link pointing to metric.drillDownRoute', () => {
    const wrapper = mountMetricCard(baseMetric);
    const link = wrapper.find('[data-testid="kpi-metric-drilldown"]');

    expect(link.exists()).toBe(true);
    expect(link.attributes('href')).toBe('/finance/obligations?status=paid');
    expect(link.text()).toContain('Inspect Workbench');
  });

  it('omits footer drill-down link when drillDownRoute is empty', () => {
    const wrapper = mountMetricCard({ ...baseMetric, drillDownRoute: '' });

    expect(wrapper.find('[data-testid="kpi-metric-drilldown"]').exists()).toBe(false);
  });
});
