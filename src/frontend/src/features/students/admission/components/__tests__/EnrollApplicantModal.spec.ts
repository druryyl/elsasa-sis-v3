// @vitest-environment jsdom
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import { useAuthStore } from '@/app/stores/auth.store';
import EnrollApplicantModal from '../EnrollApplicantModal.vue';
import * as enrollmentApi from '../../../enrollment/api/enrollment.api';
import type {
  AdmissionApplicationDetailDto,
  AdmissionPeriodOptionDto,
} from '../../types/admission.types';

const mockToastAdd = vi.fn();
vi.mock('primevue/usetoast', () => ({
  useToast: () => ({ add: mockToastAdd }),
}));

const mockPeriods: AdmissionPeriodOptionDto[] = [
  {
    id: 'period-1',
    academicYearId: '00000000-0000-0000-0000-000000002026',
    periodCode: 'PPDB-2026-G1',
    name: 'PPDB 2026/2027 Gelombang 1',
    periodType: 'admission',
    startDate: '2026-01-01',
    endDate: '2026-06-30',
    status: 'active',
  },
];

const mockApplication: AdmissionApplicationDetailDto = {
  id: 'app-100',
  applicationNumber: 'APP-2026-0042',
  applicantId: 'app-bio-1',
  admissionPeriodId: 'period-1',
  academicYearId: '00000000-0000-0000-0000-000000002026',
  applicationType: 'regular',
  requestedEntryLevel: 'Grade 1',
  requestedCampus: 'Main Campus',
  requestedProgram: 'Reguler',
  registrationDate: '2026-02-01',
  status: 'decided',
  createdAt: '2026-02-01T00:00:00Z',
  applicant: {
    id: 'app-bio-1',
    fullName: 'Budi Santoso',
    gender: 'male',
    email: 'budi@example.com',
    phone: '08123456789',
    address: 'Jl. Merdeka No. 10',
  },
  contacts: [
    {
      id: 'contact-1',
      applicationId: 'app-100',
      fullName: 'Siti Rahma',
      relationship: 'mother',
      phone: '08129876543',
      email: 'siti@example.com',
      isPrimary: true,
    },
    {
      id: 'contact-2',
      applicationId: 'app-100',
      fullName: 'Ahmad Santoso',
      relationship: 'father',
      phone: '08121112223',
      email: 'ahmad@example.com',
      isPrimary: false,
    },
  ],
  documents: [],
  assessmentEvidence: [],
  decision: {
    id: 'dec-1',
    applicationId: 'app-100',
    decisionType: 'accepted',
    decisionDate: '2026-02-10',
    effectiveDate: '2026-02-15',
    decidedBy: 'officer-1',
  },
};

describe('EnrollApplicantModal.vue (SCR-STU-01, P9-S14)', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    if (typeof window !== 'undefined') {
      window.ResizeObserver =
        window.ResizeObserver ||
        vi.fn().mockImplementation(() => ({
          observe: vi.fn(),
          unobserve: vi.fn(),
          disconnect: vi.fn(),
        }));
      window.matchMedia =
        window.matchMedia ||
        vi.fn().mockImplementation((query) => ({
          matches: false,
          media: query,
          onchange: null,
          addListener: vi.fn(),
          removeListener: vi.fn(),
          addEventListener: vi.fn(),
          removeEventListener: vi.fn(),
          dispatchEvent: vi.fn(),
        }));
    }
    setActivePinia(createPinia());
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
      },
    });

    const authStore = useAuthStore();
    authStore.setSession('test-token', {
      userId: 'officer-1',
      username: 'enrollment.officer',
      fullName: 'Enrollment Officer',
      roles: ['StudentAdminOfficer'],
      permissions: [
        'Permissions.Students.Admission.View',
        'Permissions.Students.Enrollment.Execute',
      ],
      campusId: 'campus-1',
    });
  });

  const dialogStub = {
    template: `
      <div class="dialog-stub" :data-testid="$attrs['data-testid']">
        <slot />
      </div>
    `,
    props: ['visible'],
  };

  it('renders placement form fields, payment clearance options, and guardian contacts', async () => {
    const wrapper = mount(EnrollApplicantModal, {
      props: {
        visible: true,
        application: mockApplication,
        periods: mockPeriods,
      },
      global: {
        plugins: [[VueQueryPlugin, { queryClient }], PrimeVue],
        stubs: { Dialog: dialogStub },
      },
    });
    await flushPromises();

    // Verify dialog container
    expect(wrapper.find('[data-testid="enroll-applicant-modal"]').exists()).toBe(true);

    // Verify placement fields
    expect(wrapper.find('[data-testid="enroll-campus-select"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="enroll-level-select"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="enroll-academic-year-select"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="enroll-program-select"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="enroll-cohort-input"]').exists()).toBe(true);

    // Verify values prefilled from application
    const campusSelect = wrapper.find('[data-testid="enroll-campus-select"]').element as HTMLSelectElement;
    expect(campusSelect.value).toBe('Main Campus');

    const levelSelect = wrapper.find('[data-testid="enroll-level-select"]').element as HTMLSelectElement;
    expect(levelSelect.value).toBe('Grade 1');

    // Verify payment gate defaults
    expect(wrapper.find('[data-testid="payment-mode-confirmed"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="payment-mode-waived"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="payment-reference-input"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="waiver-reference-input"]').exists()).toBe(false);

    // Verify guardian contact confirmation
    expect(wrapper.find('[data-testid="guardian-item-0"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="guardian-item-1"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Siti Rahma');
    expect(wrapper.text()).toContain('Ahmad Santoso');
  });

  it('toggles payment reference vs waiver reference when switching payment mode', async () => {
    const wrapper = mount(EnrollApplicantModal, {
      props: {
        visible: true,
        application: mockApplication,
        periods: mockPeriods,
      },
      global: {
        plugins: [[VueQueryPlugin, { queryClient }], PrimeVue],
        stubs: { Dialog: dialogStub },
      },
    });
    await flushPromises();

    // Initially Payment Confirmed is selected
    expect(wrapper.find('[data-testid="payment-reference-input"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="waiver-reference-input"]').exists()).toBe(false);

    // Select Authorized Fee Waiver
    const waiveRadio = wrapper.find('[data-testid="payment-mode-waived"]');
    await waiveRadio.trigger('change');
    await flushPromises();

    expect(wrapper.find('[data-testid="payment-reference-input"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="waiver-reference-input"]').exists()).toBe(true);

    // Switch back to Payment Confirmed
    const confirmRadio = wrapper.find('[data-testid="payment-mode-confirmed"]');
    await confirmRadio.trigger('change');
    await flushPromises();

    expect(wrapper.find('[data-testid="payment-reference-input"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="waiver-reference-input"]').exists()).toBe(false);
  });

  it('transfers primary contact designation between guardians when radio is changed', async () => {
    const wrapper = mount(EnrollApplicantModal, {
      props: {
        visible: true,
        application: mockApplication,
        periods: mockPeriods,
      },
      global: {
        plugins: [[VueQueryPlugin, { queryClient }], PrimeVue],
        stubs: { Dialog: dialogStub },
      },
    });
    await flushPromises();

    const radio0 = wrapper.find('[data-testid="primary-guardian-radio-0"]').element as HTMLInputElement;
    const radio1 = wrapper.find('[data-testid="primary-guardian-radio-1"]').element as HTMLInputElement;

    // Initially contact 0 (Siti Rahma) is primary
    expect(radio0.checked).toBe(true);
    expect(radio1.checked).toBe(false);

    // Click radio for contact 1 (Ahmad Santoso)
    await wrapper.find('[data-testid="primary-guardian-radio-1"]').trigger('change');
    await flushPromises();

    expect(radio0.checked).toBe(false);
    expect(radio1.checked).toBe(true);
  });

  it('validates required payment reference and displays error when missing', async () => {
    const enrollSpy = vi.spyOn(enrollmentApi, 'enrollAcceptedApplicant');

    const wrapper = mount(EnrollApplicantModal, {
      props: {
        visible: true,
        application: mockApplication,
        periods: mockPeriods,
      },
      global: {
        plugins: [[VueQueryPlugin, { queryClient }], PrimeVue],
        stubs: { Dialog: dialogStub },
      },
    });
    await flushPromises();

    // Submit with empty payment reference
    await wrapper.find('form').trigger('submit');
    await flushPromises();

    expect(enrollSpy).not.toHaveBeenCalled();
    expect(wrapper.text()).toContain('Payment reference is required');
  });

  it('validates required waiver reference when payment is waived and displays error when missing', async () => {
    const enrollSpy = vi.spyOn(enrollmentApi, 'enrollAcceptedApplicant');

    const wrapper = mount(EnrollApplicantModal, {
      props: {
        visible: true,
        application: mockApplication,
        periods: mockPeriods,
      },
      global: {
        plugins: [[VueQueryPlugin, { queryClient }], PrimeVue],
        stubs: { Dialog: dialogStub },
      },
    });
    await flushPromises();

    // Select fee waiver
    await wrapper.find('[data-testid="payment-mode-waived"]').trigger('change');
    await flushPromises();

    // Submit with empty waiver reference
    await wrapper.find('form').trigger('submit');
    await flushPromises();

    expect(enrollSpy).not.toHaveBeenCalled();
    expect(wrapper.text()).toContain('Waiver reference is required');
  });

  it('submits mutation with correct payload and emits success event on successful response', async () => {
    const enrollSpy = vi.spyOn(enrollmentApi, 'enrollAcceptedApplicant').mockResolvedValue({
      studentId: '00000000-0000-0000-0000-000000000001',
      studentNumber: 'STD-2026-0001',
      enrollmentId: '00000000-0000-0000-0000-000000000002',
    });

    const wrapper = mount(EnrollApplicantModal, {
      props: {
        visible: true,
        application: mockApplication,
        periods: mockPeriods,
      },
      global: {
        plugins: [[VueQueryPlugin, { queryClient }], PrimeVue],
        stubs: { Dialog: dialogStub },
      },
    });
    await flushPromises();

    // Fill cohort and payment reference
    await wrapper.find('[data-testid="enroll-cohort-input"]').setValue('1A');
    await wrapper.find('[data-testid="payment-reference-input"]').setValue('TRX-BCA-2026-98124');

    // Submit form
    await wrapper.find('form').trigger('submit');
    await flushPromises();

    expect(enrollSpy).toHaveBeenCalledTimes(1);
    const payload = enrollSpy.mock.calls[0][0];
    expect(payload.applicantId).toBe('app-bio-1');
    expect(payload.initialCampus).toBe('Main Campus');
    expect(payload.initialLevel).toBe('Grade 1');
    expect(payload.initialAcademicYear).toBe('2026/2027');
    expect(payload.initialCohort).toBe('1A');
    expect(payload.paymentWaived).toBe(false);
    expect(payload.paymentReference).toBe('TRX-BCA-2026-98124');
    expect(payload.guardians).toHaveLength(2);
    expect(payload.guardians?.[0].isPrimaryContact).toBe(true);

    // Verify emitted success event and update:visible
    expect(wrapper.emitted('success')).toBeTruthy();
    expect(wrapper.emitted('success')![0][0]).toEqual({
      studentId: '00000000-0000-0000-0000-000000000001',
      studentNumber: 'STD-2026-0001',
      enrollmentId: '00000000-0000-0000-0000-000000000002',
    });
    expect(wrapper.emitted('update:visible')).toBeTruthy();
    expect(wrapper.emitted('update:visible')![0][0]).toBe(false);
  });
});
