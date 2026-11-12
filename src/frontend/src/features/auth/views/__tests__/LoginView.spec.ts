// @vitest-environment jsdom
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import PrimeVue from 'primevue/config';
import LoginView from '../LoginView.vue';
// eslint-disable-next-line no-restricted-imports -- ADR-FE-002: Pinia session store in @/app/stores
import { useAuthStore } from '@/app/stores/auth.store';

const { mockLogin } = vi.hoisted(() => ({ mockLogin: vi.fn() }));

vi.mock('../../api/auth.api', () => ({
  login: mockLogin,
}));

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
    mockLogin.mockReset();
    mockRoute.query = {};
    mockLogin.mockImplementation(async (username: string) => ({
      token: 'jwt-token',
      user: {
        userId: 'usr-1',
        username,
        fullName: 'Test User',
        roles: ['AcademicStaff'],
        permissions: [],
        campusId: 'campus-main',
        studentId: null,
      },
    }));
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
    mockLogin.mockResolvedValueOnce({
      token: 'jwt-admin',
      user: {
        userId: '00000000-0000-0000-0000-000000000001',
        username: 'admin',
        fullName: 'Super Administrator',
        roles: ['SuperAdmin', 'Admin'],
        permissions: ['*'],
        campusId: 'campus-main',
        studentId: null,
      },
    });

    const authStore = useAuthStore();
    const wrapper = createWrapper();

    await wrapper.find('[data-testid="username-input"]').setValue('admin');
    await wrapper.find('[data-testid="password-input"]').setValue('Demo123!');

    await submitForm(wrapper);

    expect(mockLogin).toHaveBeenCalledWith('admin', 'Demo123!');
    expect(authStore.isAuthenticated).toBe(true);
    expect(authStore.hasRole('SuperAdmin')).toBe(true);
    expect(authStore.hasPermission('Any.Random.Permission')).toBe(true);
    expect(mockPush).toHaveBeenCalledWith('/dashboard');
  });

  it('stores the student identity claim when logging in as a student', async () => {
    mockLogin.mockResolvedValueOnce({
      token: 'jwt-student',
      user: {
        userId: '00000000-0000-0000-0001-000000000001',
        username: 'student.jonathan',
        fullName: 'Jonathan Pratama',
        roles: ['Student'],
        permissions: ['academics.assignments.view'],
        campusId: 'campus-main',
        studentId: '00000000-0000-0000-0001-000000000001',
      },
    });

    const authStore = useAuthStore();
    const wrapper = createWrapper();

    await wrapper.find('[data-testid="username-input"]').setValue('student.jonathan');
    await wrapper.find('[data-testid="password-input"]').setValue('Demo123!');

    await submitForm(wrapper);

    expect(authStore.currentUser?.roles).toContain('Student');
    expect(authStore.currentUser?.studentId).toBe('00000000-0000-0000-0001-000000000001');
    expect(mockPush).toHaveBeenCalledWith('/dashboard');
  });

  it('displays the backend authentication error and does not redirect', async () => {
    mockLogin.mockRejectedValueOnce(new Error('Invalid username or password.'));

    const wrapper = createWrapper();

    await wrapper.find('[data-testid="username-input"]').setValue('unknown');
    await wrapper.find('[data-testid="password-input"]').setValue('bad-password');

    await submitForm(wrapper);

    expect(wrapper.find('[data-testid="auth-error-banner"]').text()).toContain(
      'Invalid username or password.',
    );
    expect(mockPush).not.toHaveBeenCalled();
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
