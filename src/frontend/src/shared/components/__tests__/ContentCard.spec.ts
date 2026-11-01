// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import ContentCard from '../layout/ContentCard.vue';

describe('ContentCard.vue', () => {
  it('renders card container with slate border and white background', () => {
    const wrapper = mount(ContentCard, {
      slots: {
        default: '<p class="content">Card Body Content</p>',
      },
    });

    const card = wrapper.find('[data-testid="content-card"]');
    expect(card.exists()).toBe(true);
    expect(card.classes()).toContain('bg-white');
    expect(card.classes()).toContain('border-slate-200');
    expect(wrapper.find('.content').text()).toBe('Card Body Content');
  });

  it('renders header with title and subtitle when provided', () => {
    const wrapper = mount(ContentCard, {
      props: {
        title: 'Academic Period Overview',
        subtitle: 'Details and active configuration for AY 2026/2027',
      },
    });

    const header = wrapper.find('[data-testid="content-card-header"]');
    expect(header.exists()).toBe(true);
    expect(wrapper.find('[data-testid="content-card-title"]').text()).toBe(
      'Academic Period Overview',
    );
    expect(wrapper.find('[data-testid="content-card-subtitle"]').text()).toBe(
      'Details and active configuration for AY 2026/2027',
    );
  });

  it('renders header actions slot', () => {
    const wrapper = mount(ContentCard, {
      props: {
        title: 'Student Enrollments',
      },
      slots: {
        'header-actions': '<button class="export-btn">Export CSV</button>',
      },
    });

    const actions = wrapper.find('[data-testid="content-card-header-actions"]');
    expect(actions.exists()).toBe(true);
    expect(actions.find('.export-btn').text()).toBe('Export CSV');
  });

  it('renders footer slot when provided', () => {
    const wrapper = mount(ContentCard, {
      slots: {
        footer: '<button class="save-btn">Save Changes</button>',
      },
    });

    const footer = wrapper.find('[data-testid="content-card-footer"]');
    expect(footer.exists()).toBe(true);
    expect(footer.classes()).toContain('border-slate-200');
    expect(footer.find('.save-btn').text()).toBe('Save Changes');
  });

  it('removes padding from body when noPadding is true', () => {
    const wrapperDefault = mount(ContentCard);
    expect(wrapperDefault.find('[data-testid="content-card-body"]').classes()).toContain('p-6');

    const wrapperNoPadding = mount(ContentCard, {
      props: { noPadding: true },
    });
    expect(wrapperNoPadding.find('[data-testid="content-card-body"]').classes()).not.toContain(
      'p-6',
    );
  });
});
