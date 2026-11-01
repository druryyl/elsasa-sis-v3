// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import { mount, RouterLinkStub } from '@vue/test-utils';
import PageHeader from '../layout/PageHeader.vue';

describe('PageHeader.vue', () => {
  it('renders view title and descriptive subtitle', () => {
    const wrapper = mount(PageHeader, {
      props: {
        title: 'Academic Calendar Workspace',
        subtitle: 'Manage terms, exam schedules, and holiday exceptions',
      },
      global: {
        stubs: {
          RouterLink: RouterLinkStub,
        },
      },
    });

    expect(wrapper.find('[data-testid="page-header-title"]').text()).toBe(
      'Academic Calendar Workspace',
    );
    expect(wrapper.find('[data-testid="page-header-subtitle"]').text()).toBe(
      'Manage terms, exam schedules, and holiday exceptions',
    );
  });

  it('renders breadcrumbs navigation with links and current item', () => {
    const breadcrumbs = [
      { label: 'Dashboard', to: '/dashboard' },
      { label: 'Operations', to: '/operations' },
      { label: 'Calendar', current: true },
    ];

    const wrapper = mount(PageHeader, {
      props: {
        title: 'Calendar',
        breadcrumbs,
      },
      global: {
        stubs: {
          RouterLink: RouterLinkStub,
        },
      },
    });

    const nav = wrapper.find('[data-testid="page-header-breadcrumbs"]');
    expect(nav.exists()).toBe(true);
    expect(nav.text()).toContain('Dashboard');
    expect(nav.text()).toContain('Operations');
    expect(nav.text()).toContain('Calendar');

    // Current crumb has aria-current="page"
    const currentCrumb = wrapper.find('[aria-current="page"]');
    expect(currentCrumb.exists()).toBe(true);
    expect(currentCrumb.text()).toBe('Calendar');
  });

  it('renders actions slot content', () => {
    const wrapper = mount(PageHeader, {
      props: {
        title: 'Student Admissions',
      },
      slots: {
        actions: '<button class="action-btn">New Application</button>',
      },
      global: {
        stubs: {
          RouterLink: RouterLinkStub,
        },
      },
    });

    const actions = wrapper.find('[data-testid="page-header-actions"]');
    expect(actions.exists()).toBe(true);
    expect(actions.find('.action-btn').text()).toBe('New Application');
  });

  it('supports custom title and subtitle slots', () => {
    const wrapper = mount(PageHeader, {
      props: {
        title: 'Fallback Title',
      },
      slots: {
        title: '<h1 class="custom-title">Custom Header Title</h1>',
        subtitle: '<p class="custom-sub">Custom Subtitle Content</p>',
      },
      global: {
        stubs: {
          RouterLink: RouterLinkStub,
        },
      },
    });

    expect(wrapper.find('.custom-title').text()).toBe('Custom Header Title');
    expect(wrapper.find('.custom-sub').text()).toBe('Custom Subtitle Content');
  });
});
