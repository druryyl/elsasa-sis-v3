// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import KpiBreakdownBarChart from '../components/KpiBreakdownBarChart.vue';
import type { KpiBreakdownDto } from '../types/dashboard.types';
import { SAMPLE_AGING_BREAKDOWN, SAMPLE_FUNNEL_BREAKDOWN } from './dashboard.fixtures';

function mountChart(breakdown: KpiBreakdownDto, maxItems?: number) {
  return mount(KpiBreakdownBarChart, {
    props: { breakdown, maxItems },
    global: {
      stubs: {
        RouterLink: {
          template:
            '<a :href="to" :data-testid="$attrs[\'data-testid\']" :role="$attrs.role" :class="$attrs.class"><slot /></a>',
          props: ['to'],
        },
      },
    },
  });
}

describe('KpiBreakdownBarChart.vue', () => {
  it('renders chart header, breakdown badge, item labels, counts, percentages, and formatted amounts', () => {
    const wrapper = mountChart(SAMPLE_AGING_BREAKDOWN);

    expect(wrapper.find('[data-testid="kpi-breakdown-chart-FIN-AGING"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Receivables Aging Distribution');
    expect(wrapper.text()).toContain('Breakdown');

    const currentRow = wrapper.find('[data-testid="breakdown-row-current"]');
    expect(currentRow.exists()).toBe(true);
    expect(currentRow.text()).toContain('Current');
    expect(currentRow.text()).toContain('Rp 16.0M');
    expect(currentRow.text()).toContain('4');
    expect(currentRow.text()).toContain('(40.0%)');

    const overdue90Row = wrapper.find('[data-testid="breakdown-row-90-plus"]');
    expect(overdue90Row.text()).toContain('90+ Days');
    expect(overdue90Row.text()).toContain('Rp 12.5M');
    expect(overdue90Row.text()).toContain('(20.0%)');
  });

  it('sets SVG bar rect width proportional to clamped item percentage for horizontal-bar charts', () => {
    const clampedBreakdown: KpiBreakdownDto = {
      ...SAMPLE_AGING_BREAKDOWN,
      items: [
        { ...SAMPLE_AGING_BREAKDOWN.items[0]!, key: 'over', percentage: 135 },
        { ...SAMPLE_AGING_BREAKDOWN.items[1]!, key: 'mid', percentage: 42.5 },
        { ...SAMPLE_AGING_BREAKDOWN.items[2]!, key: 'neg', percentage: -12 },
      ],
    };

    const wrapper = mountChart(clampedBreakdown);

    const overBarRects = wrapper.find('[data-testid="breakdown-row-over"]').findAll('rect');
    const midBarRects = wrapper.find('[data-testid="breakdown-row-mid"]').findAll('rect');
    const negBarRects = wrapper.find('[data-testid="breakdown-row-neg"]').findAll('rect');

    expect(overBarRects[1]!.attributes('width')).toBe('100');
    expect(midBarRects[1]!.attributes('width')).toBe('42.5');
    expect(negBarRects[1]!.attributes('width')).toBe('0');
  });

  it('normalizes funnel bar widths relative to max item count and displays Funnel badge', () => {
    const wrapper = mountChart(SAMPLE_FUNNEL_BREAKDOWN);

    expect(wrapper.text()).toContain('Funnel');

    const submittedRects = wrapper.find('[data-testid="breakdown-row-submitted"]').findAll('rect');
    const acceptedRects = wrapper.find('[data-testid="breakdown-row-accepted"]').findAll('rect');

    expect(submittedRects[1]!.attributes('width')).toBe('100');
    expect(acceptedRects[1]!.attributes('width')).toBe('50');
  });

  it('binds interactive RouterLink targets for items with drillDownRoute and falls back to div otherwise', () => {
    const mixedBreakdown: KpiBreakdownDto = {
      ...SAMPLE_AGING_BREAKDOWN,
      items: [
        SAMPLE_AGING_BREAKDOWN.items[0]!,
        {
          ...SAMPLE_AGING_BREAKDOWN.items[1]!,
          key: 'no-route',
          drillDownRoute: '',
        },
      ],
    };

    const wrapper = mountChart(mixedBreakdown);

    const linkedRow = wrapper.find('[data-testid="breakdown-row-current"]');
    expect(linkedRow.element.tagName.toLowerCase()).toBe('a');
    expect(linkedRow.attributes('href')).toBe('/finance/obligations?aging=Current');

    const staticRow = wrapper.find('[data-testid="breakdown-row-no-route"]');
    expect(staticRow.element.tagName.toLowerCase()).toBe('div');
    expect(staticRow.attributes('href')).toBeUndefined();
  });

  it('respects maxItems limit and renders empty-state message when items are empty', () => {
    const limitedWrapper = mountChart(SAMPLE_AGING_BREAKDOWN, 2);
    expect(limitedWrapper.findAll('[role="listitem"]')).toHaveLength(2);
    expect(limitedWrapper.find('[data-testid="breakdown-row-90-plus"]').exists()).toBe(false);

    const emptyWrapper = mountChart({ ...SAMPLE_AGING_BREAKDOWN, items: [] });
    expect(emptyWrapper.text()).toContain('No breakdown records match the current filter criteria.');
  });
});
