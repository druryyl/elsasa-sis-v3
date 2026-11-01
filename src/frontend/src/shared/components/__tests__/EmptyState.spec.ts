// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import EmptyState from '../feedback/EmptyState.vue';
import { SearchX } from 'lucide-vue-next';

describe('EmptyState.vue', () => {
  it('renders title and description', () => {
    const wrapper = mount(EmptyState, {
      props: {
        title: 'Tidak Ada Data Siswa',
        description: 'Belum ada data siswa yang terdaftar pada periode akademik ini.',
      },
    });

    expect(wrapper.find('[data-testid="empty-state-title"]').text()).toBe('Tidak Ada Data Siswa');
    expect(wrapper.find('[data-testid="empty-state-description"]').text()).toBe(
      'Belum ada data siswa yang terdaftar pada periode akademik ini.',
    );
  });

  it('renders default icon when none provided', () => {
    const wrapper = mount(EmptyState, {
      props: {
        title: 'No Data',
      },
    });

    const iconContainer = wrapper.find('[data-testid="empty-state-icon-container"]');
    expect(iconContainer.exists()).toBe(true);
    expect(iconContainer.find('svg').exists()).toBe(true);
  });

  it('renders custom icon component when icon prop is provided', () => {
    const wrapper = mount(EmptyState, {
      props: {
        title: 'Hasil Pencarian Kosong',
        icon: SearchX,
      },
    });

    const iconContainer = wrapper.find('[data-testid="empty-state-icon-container"]');
    expect(iconContainer.find('svg').exists()).toBe(true);
  });

  it('renders action buttons in actions slot', () => {
    const wrapper = mount(EmptyState, {
      props: {
        title: 'Belum ada kalender',
      },
      slots: {
        actions: '<button class="custom-btn">Tambah Kalender Baru</button>',
      },
    });

    const actionsContainer = wrapper.find('[data-testid="empty-state-actions"]');
    expect(actionsContainer.exists()).toBe(true);
    expect(actionsContainer.find('.custom-btn').exists()).toBe(true);
    expect(actionsContainer.find('.custom-btn').text()).toBe('Tambah Kalender Baru');
  });

  it('applies bordered styling when bordered prop is true', () => {
    const wrapper = mount(EmptyState, {
      props: {
        title: 'Bordered empty state',
        bordered: true,
      },
    });

    expect(wrapper.classes()).toContain('border-dashed');
    expect(wrapper.classes()).toContain('border-2');
  });
});
