// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import LoadingSpinner from '../feedback/LoadingSpinner.vue';

describe('LoadingSpinner.vue', () => {
  it('renders accessible status role and sr-only label', () => {
    const wrapper = mount(LoadingSpinner, {
      props: { label: 'Memuat data siswa...' },
    });

    const statusEl = wrapper.find('[data-testid="loading-spinner"]');
    expect(statusEl.exists()).toBe(true);
    expect(statusEl.attributes('role')).toBe('status');
    expect(statusEl.attributes('aria-live')).toBe('polite');

    const srOnly = wrapper.find('.sr-only');
    expect(srOnly.exists()).toBe(true);
    expect(srOnly.text()).toBe('Memuat data siswa...');
  });

  it('renders visible text when text prop is provided', () => {
    const wrapper = mount(LoadingSpinner, {
      props: { text: 'Sedang memproses...' },
    });

    const textEl = wrapper.find('[data-testid="loading-spinner-text"]');
    expect(textEl.exists()).toBe(true);
    expect(textEl.text()).toBe('Sedang memproses...');
  });

  it('applies sm, md, lg size classes to the spinner svg', () => {
    const wrapperSm = mount(LoadingSpinner, { props: { size: 'sm' } });
    expect(wrapperSm.find('svg').classes()).toContain('h-4');
    expect(wrapperSm.find('svg').classes()).toContain('w-4');

    const wrapperMd = mount(LoadingSpinner, { props: { size: 'md' } });
    expect(wrapperMd.find('svg').classes()).toContain('h-6');
    expect(wrapperMd.find('svg').classes()).toContain('w-6');

    const wrapperLg = mount(LoadingSpinner, { props: { size: 'lg' } });
    expect(wrapperLg.find('svg').classes()).toContain('h-10');
    expect(wrapperLg.find('svg').classes()).toContain('w-10');
  });

  it('applies color classes according to color prop', () => {
    const wrapperTeal = mount(LoadingSpinner, { props: { color: 'teal' } });
    expect(wrapperTeal.find('svg').classes()).toContain('text-elsasa-teal-600');

    const wrapperPrimary = mount(LoadingSpinner, { props: { color: 'primary' } });
    expect(wrapperPrimary.find('svg').classes()).toContain('text-elsasa-navy-900');

    const wrapperWhite = mount(LoadingSpinner, { props: { color: 'white' } });
    expect(wrapperWhite.find('svg').classes()).toContain('text-white');
  });

  it('applies centering layout classes when center prop is true', () => {
    const wrapper = mount(LoadingSpinner, { props: { center: true } });
    expect(wrapper.classes()).toContain('justify-center');
    expect(wrapper.classes()).toContain('items-center');
  });
});
