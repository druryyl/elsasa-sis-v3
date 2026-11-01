// @vitest-environment jsdom
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import PrimeVue from 'primevue/config';
import NotFoundView from '../NotFoundView.vue';

const mockPush = vi.fn();
const mockBack = vi.fn();

vi.mock('vue-router', () => ({
  useRouter: () => ({
    push: mockPush,
    back: mockBack,
  }),
}));

describe('NotFoundView.vue', () => {
  beforeEach(() => {
    mockPush.mockReset();
    mockBack.mockReset();
  });

  function createWrapper() {
    return mount(NotFoundView, {
      global: {
        plugins: [PrimeVue],
      },
    });
  }

  it('renders HTTP 404 not found view elements', () => {
    const wrapper = createWrapper();

    expect(wrapper.find('[data-testid="not-found-view"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Page Not Found');
    expect(wrapper.text()).toContain('HTTP 404');
    expect(wrapper.text()).toContain('The requested URL path could not be located');
  });

  it('navigates to /dashboard when clicking Return to Dashboard button', async () => {
    const wrapper = createWrapper();

    const returnBtn = wrapper.find('[data-testid="return-dashboard-btn"]');
    expect(returnBtn.exists()).toBe(true);

    await returnBtn.trigger('click');

    expect(mockPush).toHaveBeenCalledWith('/dashboard');
  });

  it('navigates back when clicking Go Back button', async () => {
    const wrapper = createWrapper();

    const backBtn = wrapper.find('[data-testid="go-back-btn"]');
    expect(backBtn.exists()).toBe(true);

    await backBtn.trigger('click');

    expect(mockBack).toHaveBeenCalled();
  });
});
