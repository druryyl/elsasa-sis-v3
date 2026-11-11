// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import NeedsAttentionPanel from '../components/NeedsAttentionPanel.vue';
import type { AttentionSignalDto } from '../types/dashboard.types';
import { SAMPLE_ATTENTION_SIGNALS } from './dashboard.fixtures';

function mountPanel(
  signals: AttentionSignalDto[],
  props: {
    title?: string;
    subtitle?: string;
    emptyMessage?: string;
    maxVisible?: number;
  } = {},
) {
  return mount(NeedsAttentionPanel, {
    props: {
      signals,
      ...props,
    },
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

describe('NeedsAttentionPanel.vue', () => {
  it('renders signals in prioritized order with critical and warning summary counters', () => {
    const wrapper = mountPanel(SAMPLE_ATTENTION_SIGNALS);

    expect(wrapper.find('[data-testid="attention-critical-summary"]').text()).toContain('2 Critical');
    expect(wrapper.find('[data-testid="attention-warning-summary"]').text()).toContain('1 Warning');

    const listItems = wrapper.findAll('[data-testid^="attention-item-"]');
    expect(listItems).toHaveLength(3);
    expect(listItems[0]!.attributes('data-testid')).toBe('attention-item-FIN_OVERDUE_90_PLUS');
    expect(listItems[1]!.attributes('data-testid')).toBe('attention-item-TCH_BLOCKING_CONFLICT');
    expect(listItems[2]!.attributes('data-testid')).toBe('attention-item-OPS_UNREVIEWED_AMENDMENT');
  });

  it('displays severity tag badges, domain tags, affected counts, impact labels, and descriptions', () => {
    const wrapper = mountPanel(SAMPLE_ATTENTION_SIGNALS);

    const criticalItem = wrapper.find('[data-testid="attention-item-FIN_OVERDUE_90_PLUS"]');
    const criticalBadge = criticalItem.find('[data-testid="attention-severity-badge"]');
    expect(criticalBadge.text()).toContain('Critical');
    expect(criticalBadge.attributes('data-variant')).toBe('rose');
    expect(criticalBadge.classes()).toContain('bg-rose-50');
    expect(criticalItem.find('[data-testid="attention-domain-badge"]').text()).toBe('finance');
    expect(criticalItem.find('[data-testid="attention-affected-count"]').text()).toBe('2 affected');
    expect(criticalItem.find('[data-testid="attention-impact-label"]').text()).toBe('Rp 12.5M overdue');
    expect(criticalItem.find('[data-testid="attention-title"]').text()).toBe(
      'Severe Delinquency Accounts (90+ Days)',
    );

    const warningItem = wrapper.find('[data-testid="attention-item-OPS_UNREVIEWED_AMENDMENT"]');
    const warningBadge = warningItem.find('[data-testid="attention-severity-badge"]');
    expect(warningBadge.text()).toContain('Warning');
    expect(warningBadge.attributes('data-variant')).toBe('amber');
    expect(warningBadge.classes()).toContain('bg-amber-50');
    expect(warningItem.find('[data-testid="attention-domain-badge"]').text()).toBe('operations');
    expect(warningItem.find('[data-testid="attention-affected-count"]').text()).toBe('1 affected');
  });

  it('renders action buttons bound to each signal drillDownRoute with severity-appropriate styling', () => {
    const wrapper = mountPanel(SAMPLE_ATTENTION_SIGNALS);

    const criticalLink = wrapper
      .find('[data-testid="attention-item-FIN_OVERDUE_90_PLUS"]')
      .find('[data-testid="attention-action-link"]');
    expect(criticalLink.attributes('href')).toBe('/finance/obligations?aging=90%2B%20Days');
    expect(criticalLink.text()).toContain('Review Overdue Accounts');
    expect(criticalLink.classes()).toContain('bg-rose-600');

    const warningLink = wrapper
      .find('[data-testid="attention-item-OPS_UNREVIEWED_AMENDMENT"]')
      .find('[data-testid="attention-action-link"]');
    expect(warningLink.attributes('href')).toBe('/operations/calendar?tab=amendments&reviewed=false');
    expect(warningLink.text()).toContain('Review Amendments');
    expect(warningLink.classes()).toContain('bg-white');
  });

  it('truncates visible items via maxVisible while preserving full summary counts', () => {
    const wrapper = mountPanel(SAMPLE_ATTENTION_SIGNALS, { maxVisible: 2 });

    expect(wrapper.findAll('[data-testid^="attention-item-"]')).toHaveLength(2);
    expect(wrapper.find('[data-testid="attention-critical-summary"]').text()).toContain('2 Critical');
    expect(wrapper.find('[data-testid="attention-warning-summary"]').text()).toContain('1 Warning');
  });

  it('renders healthy clear state when signals array is empty', () => {
    const wrapper = mountPanel([], {
      emptyMessage: 'No active exceptions for this semester.',
    });

    expect(wrapper.find('[data-testid="needs-attention-empty"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="needs-attention-empty"]').text()).toContain(
      'No active exceptions for this semester.',
    );
    expect(wrapper.text()).toContain('Clear');
    expect(wrapper.find('[data-testid="needs-attention-list"]').exists()).toBe(false);
  });
});
