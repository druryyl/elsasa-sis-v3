// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import KpiStackedProgressBar from '../components/KpiStackedProgressBar.vue';
import { SAMPLE_STACKED_BREAKDOWN } from './dashboard.fixtures';

function mountStackedBar(items = SAMPLE_STACKED_BREAKDOWN.items) {
  return mount(KpiStackedProgressBar, {
    props: {
      title: SAMPLE_STACKED_BREAKDOWN.title,
      subtitle: SAMPLE_STACKED_BREAKDOWN.subtitle,
      items,
      totalLabel: '6 total',
    },
    global: {
      stubs: {
        RouterLink: {
          template:
            '<a :href="to" :data-testid="$attrs[\'data-testid\']" :style="$attrs.style" :class="$attrs.class"><slot /></a>',
          props: ['to'],
        },
      },
    },
  });
}

describe('KpiStackedProgressBar.vue', () => {
  it('renders header, total label, proportional stacked segments, and interactive legend links', () => {
    const wrapper = mountStackedBar();

    expect(wrapper.find('[data-testid="kpi-stacked-progress-bar"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Teacher Workload Utilization');
    expect(wrapper.text()).toContain('6 total');

    const normalSeg = wrapper.find('[data-testid="stacked-segment-normal"]');
    const breachedSeg = wrapper.find('[data-testid="stacked-segment-breached"]');

    expect(normalSeg.attributes('href')).toBe('/teaching/workload?status=normal');
    expect(breachedSeg.attributes('href')).toBe('/teaching/workload?status=breached');
    expect(normalSeg.attributes('style')).toContain('width: 66.7%');
    expect(breachedSeg.attributes('style')).toContain('width: 33.3%');

    const legendNormal = wrapper.find('[data-testid="stacked-legend-item-normal"]');
    expect(legendNormal.text()).toContain('Normal');
    expect(legendNormal.text()).toContain('4');
    expect(legendNormal.text()).toContain('(67%)');
  });

  it('renders empty state when no items are provided', () => {
    const wrapper = mountStackedBar([]);

    expect(wrapper.text()).toContain('No distribution data available.');
    expect(wrapper.find('[data-testid="stacked-bar-track"]').exists()).toBe(false);
  });
});
