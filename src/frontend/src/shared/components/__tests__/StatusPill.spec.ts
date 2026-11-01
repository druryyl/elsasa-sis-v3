// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import StatusPill from '../StatusPill.vue';

describe('StatusPill.vue', () => {
  it('maps "active" and "accepted" to emerald badge style', () => {
    const wrapperActive = mount(StatusPill, {
      props: { status: 'active' },
    });
    expect(wrapperActive.attributes('data-variant')).toBe('emerald');
    expect(wrapperActive.classes()).toContain('bg-emerald-50');
    expect(wrapperActive.classes()).toContain('text-emerald-700');
    expect(wrapperActive.text()).toBe('Active');

    const wrapperAccepted = mount(StatusPill, {
      props: { status: 'accepted' },
    });
    expect(wrapperAccepted.attributes('data-variant')).toBe('emerald');
    expect(wrapperAccepted.text()).toBe('Accepted');
  });

  it('maps "pending" to amber badge style', () => {
    const wrapper = mount(StatusPill, {
      props: { status: 'pending' },
    });
    expect(wrapper.attributes('data-variant')).toBe('amber');
    expect(wrapper.classes()).toContain('bg-amber-50');
    expect(wrapper.classes()).toContain('text-amber-700');
    expect(wrapper.text()).toBe('Pending');
  });

  it('maps "rejected" to rose badge style', () => {
    const wrapper = mount(StatusPill, {
      props: { status: 'rejected' },
    });
    expect(wrapper.attributes('data-variant')).toBe('rose');
    expect(wrapper.classes()).toContain('bg-rose-50');
    expect(wrapper.classes()).toContain('text-rose-700');
    expect(wrapper.text()).toBe('Rejected');
  });

  it('maps "closed" to slate badge style', () => {
    const wrapper = mount(StatusPill, {
      props: { status: 'closed' },
    });
    expect(wrapper.attributes('data-variant')).toBe('slate');
    expect(wrapper.classes()).toContain('bg-slate-100');
    expect(wrapper.classes()).toContain('text-slate-700');
    expect(wrapper.text()).toBe('Closed');
  });

  it('handles case-insensitivity and trim', () => {
    const wrapper = mount(StatusPill, {
      props: { status: '  ACTIVE  ' },
    });
    expect(wrapper.attributes('data-variant')).toBe('emerald');
    expect(wrapper.text()).toBe('Active');
  });

  it('formats compound status strings into readable title case', () => {
    const wrapperUnderReview = mount(StatusPill, {
      props: { status: 'under_review' },
    });
    expect(wrapperUnderReview.attributes('data-variant')).toBe('amber');
    expect(wrapperUnderReview.text()).toBe('Under Review');

    const wrapperOnLeave = mount(StatusPill, {
      props: { status: 'on-leave' },
    });
    expect(wrapperOnLeave.attributes('data-variant')).toBe('amber');
    expect(wrapperOnLeave.text()).toBe('On Leave');
  });

  it('supports explicit variant override', () => {
    const wrapper = mount(StatusPill, {
      props: {
        status: 'custom-status',
        variant: 'rose',
        label: 'Critical Alert',
      },
    });
    expect(wrapper.attributes('data-variant')).toBe('rose');
    expect(wrapper.text()).toBe('Critical Alert');
  });

  it('renders custom label and default slot content', () => {
    const wrapperSlot = mount(StatusPill, {
      props: { status: 'active' },
      slots: {
        default: 'Custom Active Content',
      },
    });
    expect(wrapperSlot.text()).toBe('Custom Active Content');
  });

  it('renders and hides dot indicator based on dot prop', () => {
    const wrapperWithDot = mount(StatusPill, {
      props: { status: 'active', dot: true },
    });
    expect(wrapperWithDot.find('[data-testid="status-pill-dot"]').exists()).toBe(true);

    const wrapperWithoutDot = mount(StatusPill, {
      props: { status: 'active', dot: false },
    });
    expect(wrapperWithoutDot.find('[data-testid="status-pill-dot"]').exists()).toBe(false);
  });

  it('applies small size classes when size is sm', () => {
    const wrapper = mount(StatusPill, {
      props: { status: 'active', size: 'sm' },
    });
    expect(wrapper.classes()).toContain('py-0.5');
  });
});
