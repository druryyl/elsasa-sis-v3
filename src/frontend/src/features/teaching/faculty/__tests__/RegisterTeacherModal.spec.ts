// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import { createPinia, setActivePinia } from 'pinia';
import RegisterTeacherModal from '../components/RegisterTeacherModal.vue';
import type { TeachingRoleDto } from '../api/teachingTypes';
import * as facultyApiClient from '../api/teachingApiClient';

const mockRoles: TeachingRoleDto[] = [
  {
    id: 'role-01',
    code: 'PERMANENT',
    name: 'Guru Tetap',
    description: 'Permanent teaching faculty',
    requiresQualificationCheck: true,
    isActive: true,
  },
  {
    id: 'role-02',
    code: 'CONTRACT',
    name: 'Guru Kontrak',
    description: 'Fixed-term contracted teaching staff',
    requiresQualificationCheck: true,
    isActive: true,
  },
];

const commonStubs = {
  Dialog: {
    props: ['visible', 'header', 'appendTo'],
    emits: ['update:visible', 'hide'],
    template: `
      <div v-if="visible" class="p-dialog" data-testid="register-teacher-modal">
        <slot name="header" />
        <slot />
      </div>
    `,
  },
  Select: {
    props: ['modelValue', 'options'],
    emits: ['update:modelValue'],
    template: `
      <select
        :value="modelValue"
        @change="$emit('update:modelValue', $event.target.value)"
      >
        <option v-for="opt in options" :key="opt.value || opt" :value="opt.value || opt">
          {{ opt.label || opt }}
        </option>
      </select>
    `,
  },
};

describe('RegisterTeacherModal.vue', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    if (typeof window !== 'undefined') {
      Object.defineProperty(window, 'matchMedia', {
        writable: true,
        value: vi.fn().mockImplementation((query) => ({
          matches: false,
          media: query,
          onchange: null,
          addListener: vi.fn(),
          removeListener: vi.fn(),
          addEventListener: vi.fn(),
          removeEventListener: vi.fn(),
          dispatchEvent: vi.fn(),
        })),
      });

      window.ResizeObserver =
        window.ResizeObserver ||
        vi.fn().mockImplementation(() => ({
          observe: vi.fn(),
          unobserve: vi.fn(),
          disconnect: vi.fn(),
        }));
    }

    const pinia = createPinia();
    setActivePinia(pinia);

    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
        mutations: { retry: false },
      },
    });

    vi.restoreAllMocks();
  });

  function createWrapper(props: Partial<InstanceType<typeof RegisterTeacherModal>['$props']> = {}) {
    return mount(RegisterTeacherModal, {
      props: {
        visible: true,
        roles: mockRoles,
        defaultCampus: 'Kampus Utama',
        ...props,
      },
      global: {
        plugins: [PrimeVue, ToastService, [VueQueryPlugin, { queryClient }]],
        stubs: commonStubs,
      },
    });
  }

  it('renders registration form fields and HR decoupling helper texts', () => {
    const wrapper = createWrapper();

    expect(wrapper.find('[data-testid="register-teacher-modal"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="input-full-name"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="input-display-name"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="input-teacher-number"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="select-teaching-role"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="select-campus"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="input-department"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="input-email"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="input-phone"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="input-employee-ref"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="input-national-id"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="input-effective-from"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="input-effective-to"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="input-notes"]').exists()).toBe(true);

    // Verify helper text explaining HR decoupling (TD-04)
    expect(wrapper.text()).toContain('Faculty Identity & HR Decoupling (TD-04)');
    expect(wrapper.text()).toContain('Leave empty for visiting faculty, guest lecturers, or external assessors');
  });

  it('fails validation when required fields (full name) are missing', async () => {
    const wrapper = createWrapper();

    // Trigger form submit directly
    const form = wrapper.find('[data-testid="register-teacher-form"]');
    await form.trigger('submit.prevent');
    await flushPromises();

    // Full name is required
    expect(wrapper.text()).toContain('Full name is required.');
  });

  it('fails validation when effectiveTo date is earlier than effectiveFrom date', async () => {
    const wrapper = createWrapper();

    await wrapper.find('[data-testid="input-full-name"]').setValue('Ahmad Dahlan');
    await wrapper.find('[data-testid="input-effective-from"]').setValue('2026-07-01');
    await wrapper.find('[data-testid="input-effective-to"]').setValue('2026-06-01');

    const form = wrapper.find('[data-testid="register-teacher-form"]');
    await form.trigger('submit.prevent');
    await flushPromises();

    expect(wrapper.text()).toContain('Effective to date must be on or after effective from date.');
  });

  it('submits valid registration payload and emits registered and created events', async () => {
    const mockCreatedResponse = {
      id: 'new-tch-123',
      teacherNumber: 'TCH-2026-9999',
      fullName: 'Ahmad Dahlan, M.Pd.',
      standing: 'pending_approval' as const,
    };

    const registerSpy = vi
      .spyOn(facultyApiClient, 'registerTeacher')
      .mockResolvedValueOnce(mockCreatedResponse);

    const wrapper = createWrapper();

    await wrapper.find('[data-testid="input-full-name"]').setValue('Ahmad Dahlan');
    await wrapper.find('[data-testid="input-display-name"]').setValue('Ahmad Dahlan, M.Pd.');
    await wrapper.find('[data-testid="input-teacher-number"]').setValue('TCH-2026-9999');
    await wrapper.find('[data-testid="input-department"]').setValue('Pendidikan Agama');
    await wrapper.find('[data-testid="input-email"]').setValue('ahmad.dahlan@elsasa.sch.id');
    await wrapper.find('[data-testid="input-phone"]').setValue('+628123456789');
    await wrapper.find('[data-testid="input-employee-ref"]').setValue('EMP-2026-999');
    await wrapper.find('[data-testid="input-national-id"]').setValue('3201999888777001');
    await wrapper.find('[data-testid="input-effective-from"]').setValue('2026-07-01');

    const form = wrapper.find('[data-testid="register-teacher-form"]');
    await form.trigger('submit.prevent');
    await flushPromises();

    expect(registerSpy).toHaveBeenCalledWith(
      expect.objectContaining({
        fullName: 'Ahmad Dahlan',
        displayName: 'Ahmad Dahlan, M.Pd.',
        teacherNumber: 'TCH-2026-9999',
        teachingRoleId: 'role-01',
        campusAffiliation: 'Kampus Utama',
        department: 'Pendidikan Agama',
        email: 'ahmad.dahlan@elsasa.sch.id',
        phone: '+628123456789',
        employeeRef: 'EMP-2026-999',
        nationalIdOrExternalRef: '3201999888777001',
        effectiveFrom: '2026-07-01',
      }),
    );

    expect(wrapper.emitted('registered')).toBeTruthy();
    expect(wrapper.emitted('registered')?.[0][0]).toEqual(mockCreatedResponse);
    expect(wrapper.emitted('created')).toBeTruthy();
    expect(wrapper.emitted('update:visible')).toBeTruthy();
    expect(wrapper.emitted('update:visible')?.[0][0]).toBe(false);
  });

  it('closes dialog and emits close when Cancel button is clicked', async () => {
    const wrapper = createWrapper();

    const cancelBtn = wrapper.find('[data-testid="btn-cancel-registration"]');
    await cancelBtn.trigger('click');

    expect(wrapper.emitted('update:visible')).toBeTruthy();
    expect(wrapper.emitted('update:visible')?.[0][0]).toBe(false);
    expect(wrapper.emitted('close')).toBeTruthy();
  });
});
