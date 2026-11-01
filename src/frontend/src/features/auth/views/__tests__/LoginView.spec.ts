// @vitest-environment jsdom
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import PrimeVue from 'primevue/config';
import LoginView from '../LoginView.vue';
// eslint-disable-next-line no-restricted-imports -- ADR-FE-002: Pinia session store in @/app/stores
import { useAuthStore } from '@/app/stores/auth.store';

const mockPush = vi.fn();
const mockRoute = {
  query: {} as Record<string, string>,
};

vi.mock('vue-router', () => ({
  useRouter: () => ({
    push: mockPush,
  }),
  useRoute: () => mockRoute,
}));

describe('LoginView.vue', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    mockPush.mockReset();
    mockRoute.query = {};
  });

  function createWrapper() {
    return mount(LoginView, {
      global: {
        plugins: [PrimeVue],
      },
    });
  }

  async function submitForm(wrapper: ReturnType<typeof createWrapper>) {
    await wrapper.find('form').trigger('submit');
    await flushPromises();
    await new Promise((resolve) => setTimeout(resolve, 50));
    await flushPromises();
  }

  it('renders login view structure and form fields', () => {
    const wrapper = createWrapper();

    expect(wrapper.find('[data-testid="login-view"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="login-form"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="username-input"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="password-input"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="login-submit-btn"]').exists()).toBe(true);
  });

  it('displays validation errors when submitting empty credentials', async () => {
    const wrapper = createWrapper();

    await submitForm(wrapper);

    const errors = wrapper.findAll('[role="alert"]');
    expect(errors.length).toBeGreaterThanOrEqual(1);

    const errorTexts = errors.map((el) => el.text());
    expect(errorTexts.some((t) => t.includes('Username is required'))).toBe(true);
    expect(errorTexts.some((t) => t.includes('Password is required'))).toBe(true);
    expect(mockPush).not.toHaveBeenCalled();
  });

  it('authenticates user and redirects to default /dashboard on valid submit', async () => {
    const authStore = useAuthStore();
    const wrapper = createWrapper();

    const usernameInput = wrapper.find('[data-testid="username-input"]');
    const passwordInput = wrapper.find('[data-testid="password-input"]');

    await usernameInput.setValue('johndoe');
    await passwordInput.setValue('validPassword123');

    await submitForm(wrapper);

    expect(authStore.isAuthenticated).toBe(true);
    expect(authStore.currentUser?.username).toBe('johndoe');
    expect(authStore.token).toBeTruthy();
    expect(mockPush).toHaveBeenCalledWith('/dashboard');
  });

  it('assigns SuperAdmin privileges and wildcard permission when logging in as admin', async () => {
    const authStore = useAuthStore();
    const wrapper = createWrapper();

    await wrapper.find('[data-testid="username-input"]').setValue('admin');
    await wrapper.find('[data-testid="password-input"]').setValue('adminpass');

    await submitForm(wrapper);

    expect(authStore.isAuthenticated).toBe(true);
    expect(authStore.hasRole('SuperAdmin')).toBe(true);
    expect(authStore.hasPermission('Any.Random.Permission')).toBe(true);
    expect(mockPush).toHaveBeenCalledWith('/dashboard');
  });

  it('redirects to query redirect parameter when provided', async () => {
    mockRoute.query = { redirect: '/operations/calendar' };
    const wrapper = createWrapper();

    await wrapper.find('[data-testid="username-input"]').setValue('officer');
    await wrapper.find('[data-testid="password-input"]').setValue('password');

    await submitForm(wrapper);

    expect(mockPush).toHaveBeenCalledWith('/operations/calendar');
  });
});
