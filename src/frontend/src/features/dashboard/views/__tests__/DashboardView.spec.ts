// @vitest-environment jsdom
import { describe, it, expect, beforeEach } from 'vitest';
import { mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import PrimeVue from 'primevue/config';
import DashboardView from '../DashboardView.vue';
// eslint-disable-next-line no-restricted-imports -- ADR-FE-002: Pinia session store in @/app/stores
import { useAuthStore } from '@/app/stores/auth.store';
// eslint-disable-next-line no-restricted-imports -- ADR-FE-002: Pinia session store in @/app/stores
import { useAppContextStore } from '@/app/stores/appContext.store';

describe('DashboardView.vue', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  function createWrapper() {
    return mount(DashboardView, {
      global: {
        plugins: [PrimeVue],
        stubs: {
          RouterLink: {
            template: '<a :href="to" :data-testid="$attrs[\'data-testid\']"><slot /></a>',
            props: ['to'],
          },
        },
      },
    });
  }

  it('renders dashboard view shell and header', () => {
    const wrapper = createWrapper();

    expect(wrapper.find('[data-testid="dashboard-view"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Administrative Dashboard');
  });

  it('displays session greeting with user full name and role', () => {
    const authStore = useAuthStore();
    authStore.setSession('token-123', {
      userId: 'usr-42',
      username: 'drury',
      fullName: 'Drury Academic Officer',
      roles: ['AcademicAdmin'],
      permissions: ['*'],
      campusId: 'campus-main',
    });

    const wrapper = createWrapper();

    const greetingCard = wrapper.find('[data-testid="session-greeting-card"]');
    expect(greetingCard.exists()).toBe(true);
    expect(greetingCard.text()).toContain('Drury Academic Officer');
    expect(greetingCard.text()).toContain('AcademicAdmin');
    expect(greetingCard.text()).toContain('usr-42');
  });

  it('displays active campus and academic period indicators', () => {
    const appContext = useAppContextStore();
    appContext.setActiveCampus('campus-east');
    appContext.setActiveAcademicPeriod('AY 2026/2027 Term 1');

    const wrapper = createWrapper();

    const activeCampus = wrapper.find('[data-testid="active-campus-label"]');
    expect(activeCampus.text()).toContain('East Campus (Kampus B)');

    const activePeriod = wrapper.find('[data-testid="active-period-label"]');
    expect(activePeriod.text()).toContain('AY 2026/2027 Term 1');
  });

  it('displays operational system status tile', () => {
    const wrapper = createWrapper();

    const statusCard = wrapper.find('[data-testid="system-status-card"]');
    expect(statusCard.exists()).toBe(true);
    expect(statusCard.text()).toContain('Operational');
    expect(statusCard.text()).toContain('Healthy');
    expect(statusCard.text()).toContain('Minimal APIs & Dapper connected');
  });

  it('renders navigation shortcut to Operations calendar workspace', () => {
    const wrapper = createWrapper();

    const operationsSection = wrapper.find('[data-testid="operations-shortcuts-section"]');
    expect(operationsSection.exists()).toBe(true);

    const calendarShortcut = wrapper.find('[data-testid="op-calendar"]');
    expect(calendarShortcut.exists()).toBe(true);
    expect(calendarShortcut.attributes('href')).toBe('/operations/calendar');
    expect(calendarShortcut.text()).toContain('Academic Calendar & Periods');
  });

  it('renders Stage 1 domain workspace shortcuts for Students and Finance', () => {
    const wrapper = createWrapper();

    const admissionShortcut = wrapper.find('[data-testid="student-admission"]');
    expect(admissionShortcut.exists()).toBe(true);
    expect(admissionShortcut.attributes('href')).toBe('/students/admission');

    const enrollmentShortcut = wrapper.find('[data-testid="student-enrollment"]');
    expect(enrollmentShortcut.exists()).toBe(true);
    expect(enrollmentShortcut.attributes('href')).toBe('/students/enrollment');

    const obligationsShortcut = wrapper.find('[data-testid="finance-obligations"]');
    expect(obligationsShortcut.exists()).toBe(true);
    expect(obligationsShortcut.attributes('href')).toBe('/finance/obligations');

    const paymentsShortcut = wrapper.find('[data-testid="finance-payments"]');
    expect(paymentsShortcut.exists()).toBe(true);
    expect(paymentsShortcut.attributes('href')).toBe('/finance/payments');
  });
});
