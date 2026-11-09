// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import AssessmentStatusBadge from '../components/AssessmentStatusBadge.vue';
import ResultStatusBadge from '../components/ResultStatusBadge.vue';

describe('AssessmentStatusBadge', () => {
  it('renders planned status with sky variant', () => {
    const wrapper = mount(AssessmentStatusBadge, {
      props: { status: 'planned' },
    });
    expect(wrapper.text()).toContain('Planned');
    expect(wrapper.find('[data-testid="assessment-status-badge"]').attributes('data-variant')).toBe('sky');
  });

  it('renders conducted status with emerald variant', () => {
    const wrapper = mount(AssessmentStatusBadge, {
      props: { status: 'conducted' },
    });
    expect(wrapper.text()).toContain('Conducted');
    expect(wrapper.find('[data-testid="assessment-status-badge"]').attributes('data-variant')).toBe('emerald');
  });

  it('renders cancelled status with rose variant', () => {
    const wrapper = mount(AssessmentStatusBadge, {
      props: { status: 'cancelled' },
    });
    expect(wrapper.text()).toContain('Cancelled');
    expect(wrapper.find('[data-testid="assessment-status-badge"]').attributes('data-variant')).toBe('rose');
  });

  it('renders fallback for unknown status', () => {
    const wrapper = mount(AssessmentStatusBadge, {
      props: { status: 'archived' },
    });
    expect(wrapper.text()).toContain('archived');
    expect(wrapper.find('[data-testid="assessment-status-badge"]').attributes('data-variant')).toBe('slate');
  });
});

describe('ResultStatusBadge', () => {
  it('renders provisional status with amber variant', () => {
    const wrapper = mount(ResultStatusBadge, {
      props: { status: 'provisional' },
    });
    expect(wrapper.text()).toContain('Provisional');
    expect(wrapper.find('[data-testid="result-status-badge"]').attributes('data-variant')).toBe('amber');
  });

  it('renders approved status with sky variant', () => {
    const wrapper = mount(ResultStatusBadge, {
      props: { status: 'approved' },
    });
    expect(wrapper.text()).toContain('Approved');
    expect(wrapper.find('[data-testid="result-status-badge"]').attributes('data-variant')).toBe('sky');
  });

  it('renders published status with emerald variant', () => {
    const wrapper = mount(ResultStatusBadge, {
      props: { status: 'published' },
    });
    expect(wrapper.text()).toContain('Published');
    expect(wrapper.find('[data-testid="result-status-badge"]').attributes('data-variant')).toBe('emerald');
  });
});
