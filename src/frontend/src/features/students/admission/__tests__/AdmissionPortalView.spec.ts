// @vitest-environment jsdom
import { describe, it, expect, beforeEach, beforeAll, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import AdmissionPortalView from '../views/AdmissionPortalView.vue';
import * as admissionApi from '../api/admission.api';
import type {
  AdmissionPeriodOptionDto,
  RegisterApplicationResultDto,
} from '../types/admission.types';

const mockPush = vi.fn();
vi.mock('vue-router', () => ({
  useRouter: () => ({
    push: mockPush,
  }),
  useRoute: () => ({
    meta: { requiresAuth: false, wide: true },
  }),
  RouterLink: {
    template: '<a><slot /></a>',
  },
}));

vi.mock('primevue/usetoast', () => ({
  useToast: () => ({
    add: vi.fn(),
  }),
}));

const mockActivePeriods: AdmissionPeriodOptionDto[] = [
  {
    id: 'f87a3b3a-5f5c-4d8e-9d2a-8b1a2c3d4e5f',
    academicYearId: 'ay-2026-2027',
    periodCode: 'ADM-2026-REG',
    name: 'Academic Year 2026/2027 Admission Window',
    periodType: 'admission_period',
    startDate: '2026-01-01',
    endDate: '2026-08-31',
    status: 'active',
  },
];

describe('AdmissionPortalView.vue (SCR-SELF-01)', () => {
  let queryClient: QueryClient;

  beforeAll(() => {
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

      // Mock navigator.clipboard
      Object.assign(navigator, {
        clipboard: {
          writeText: vi.fn().mockResolvedValue(undefined),
        },
      });
    }
  });

  beforeEach(() => {
    setActivePinia(createPinia());
    queryClient = new QueryClient({
      defaultOptions: {
        queries: {
          retry: false,
        },
      },
    });
    mockPush.mockReset();
    vi.clearAllMocks();

    vi.spyOn(admissionApi, 'getActiveAdmissionPeriods').mockResolvedValue(mockActivePeriods);
  });

  function createWrapper() {
    return mount(AdmissionPortalView, {
      global: {
        plugins: [PrimeVue, [VueQueryPlugin, { queryClient }]],
        stubs: {
          teleport: true,
        },
      },
    });
  }

  it('renders wizard shell, header, and 5-step visual indicator', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    expect(wrapper.find('[data-testid="admission-portal-wizard"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="step-indicator"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="wizard-step-1"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Admission Application Portal');
    expect(wrapper.text()).toContain('Step 1: Select Admission Period & Requested Grade');
  });

  it('blocks navigation from Step 1 to Step 2 when required inputs are empty', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    // Click next without selecting period or entry level
    const nextBtn = wrapper.find('[data-testid="btn-next"]');
    await nextBtn.trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-testid="wizard-step-1"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="wizard-step-2"]').exists()).toBe(false);
    expect(wrapper.text()).toContain('Admission period is required.');
    expect(wrapper.text()).toContain('Requested entry level is required.');
  });

  it('advances from Step 1 to Step 2 when valid period and grade are selected', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    // Select valid period and grade level by triggering v-model updates
    const vm = wrapper.vm as unknown as {
      step1: {
        admissionPeriodId: string;
        requestedEntryLevel: string;
      };
      goToNextStep: () => void;
    };

    vm.step1.admissionPeriodId = mockActivePeriods[0].id;
    vm.step1.requestedEntryLevel = 'Grade 1';

    const nextBtn = wrapper.find('[data-testid="btn-next"]');
    await nextBtn.trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-testid="wizard-step-2"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Step 2: Applicant Biographical Information');
  });

  it('blocks navigation from Step 2 when applicant name or birth date are invalid', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const vm = wrapper.vm as unknown as {
      step1: { admissionPeriodId: string; requestedEntryLevel: string };
    };
    vm.step1.admissionPeriodId = mockActivePeriods[0].id;
    vm.step1.requestedEntryLevel = 'Grade 1';

    await wrapper.find('[data-testid="btn-next"]').trigger('click');
    await flushPromises();

    // On Step 2, click Next with empty applicant fields
    await wrapper.find('[data-testid="btn-next"]').trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-testid="wizard-step-2"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Applicant full name is required.');
  });

  it('advances through Step 3 (Guardian Contacts) and allows adding contacts', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const vm = wrapper.vm as unknown as {
      step1: { admissionPeriodId: string; requestedEntryLevel: string };
      step2: { fullName: string; dateOfBirth: string };
      contacts: Array<{ fullName: string; relationship: string; phone: string; email: string }>;
      addContact: () => void;
    };

    vm.step1.admissionPeriodId = mockActivePeriods[0].id;
    vm.step1.requestedEntryLevel = 'Grade 1';
    await wrapper.find('[data-testid="btn-next"]').trigger('click');
    await flushPromises();

    vm.step2.fullName = 'Bambang Sudirman';
    vm.step2.dateOfBirth = '2019-05-15';
    await wrapper.find('[data-testid="btn-next"]').trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-testid="wizard-step-3"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Step 3: Guardian & Family Contact Details');

    // Fill primary contact
    vm.contacts[0].fullName = 'Sudirman';
    vm.contacts[0].relationship = 'Father';
    vm.contacts[0].phone = '+628123456789';
    vm.contacts[0].email = 'sudirman@example.com';

    // Advance to Step 4 (Documents)
    await wrapper.find('[data-testid="btn-next"]').trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-testid="wizard-step-4"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Step 4: Credential Document Uploads');
  });

  it('advances to Step 5 (Summary & Review) and renders full dossier breakdown', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const vm = wrapper.vm as unknown as {
      step1: { admissionPeriodId: string; requestedEntryLevel: string };
      step2: { fullName: string; dateOfBirth: string };
      contacts: Array<{ fullName: string; relationship: string; phone: string; email: string }>;
      currentStep: number;
      maxStepReached: number;
    };

    vm.step1.admissionPeriodId = mockActivePeriods[0].id;
    vm.step1.requestedEntryLevel = 'Grade 1';
    vm.step2.fullName = 'Bambang Sudirman';
    vm.step2.dateOfBirth = '2019-05-15';
    vm.contacts[0].fullName = 'Sudirman';
    vm.contacts[0].relationship = 'Father';
    vm.contacts[0].phone = '+628123456789';
    vm.contacts[0].email = 'sudirman@example.com';

    vm.currentStep = 5;
    vm.maxStepReached = 5;
    await flushPromises();

    expect(wrapper.find('[data-testid="wizard-step-5"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Step 5: Summary Review & Confirmation');
    expect(wrapper.text()).toContain('Bambang Sudirman');
    expect(wrapper.text()).toContain('Grade 1');
    expect(wrapper.text()).toContain('Sudirman (Father)');
  });

  it('submits application, uploads documents, and displays confirmation card with application number', async () => {
    const mockRegistrationResponse: RegisterApplicationResultDto = {
      id: 'app-guid-1234',
      applicationNumber: 'ADM-2026-0042',
      applicantId: 'applicant-guid-5678',
      status: 'registered',
      registrationDate: '2026-02-01',
    };

    vi.spyOn(admissionApi, 'registerApplication').mockResolvedValue(mockRegistrationResponse);
    vi.spyOn(admissionApi, 'uploadApplicantDocument').mockResolvedValue({
      id: 'doc-1',
      applicationId: 'app-guid-1234',
      documentType: 'birth_certificate',
      submissionStatus: 'submitted',
      fileName: 'test.pdf',
      fileSizeBytes: 1024,
      contentType: 'application/pdf',
      fileReference: 'storage/documents/admission/app-guid-1234/test.pdf',
    });

    const wrapper = createWrapper();
    await flushPromises();

    const vm = wrapper.vm as unknown as {
      step1: { admissionPeriodId: string; requestedEntryLevel: string };
      step2: { fullName: string; dateOfBirth: string };
      contacts: Array<{ fullName: string; relationship: string; phone: string; email: string }>;
      selectedFiles: Record<string, { file: File; name: string; size: number } | null>;
      confirmationAccepted: boolean;
      currentStep: number;
      maxStepReached: number;
      handleSubmitApplication: () => Promise<void>;
    };

    vm.step1.admissionPeriodId = mockActivePeriods[0].id;
    vm.step1.requestedEntryLevel = 'Grade 1';
    vm.step2.fullName = 'Ahmad Pratama';
    vm.step2.dateOfBirth = '2018-08-10';
    vm.contacts[0].fullName = 'Budi Pratama';
    vm.contacts[0].relationship = 'Father';
    vm.contacts[0].phone = '+628123456789';
    vm.contacts[0].email = 'budi@example.com';

    // Mock a selected file on step 4
    const mockFile = new File(['mock content'], 'birth_cert.pdf', { type: 'application/pdf' });
    vm.selectedFiles.birth_certificate = {
      file: mockFile,
      name: 'birth_cert.pdf',
      size: 1024,
    };

    vm.currentStep = 5;
    vm.maxStepReached = 5;
    vm.confirmationAccepted = true;
    await flushPromises();

    // Trigger submit
    await vm.handleSubmitApplication();
    await flushPromises();

    expect(admissionApi.registerApplication).toHaveBeenCalledWith(
      expect.objectContaining({
        admissionPeriodId: mockActivePeriods[0].id,
        fullName: 'Ahmad Pratama',
        dateOfBirth: '2018-08-10',
        requestedEntryLevel: 'Grade 1',
      }),
      true, // isPublicPortal
    );

    expect(admissionApi.uploadApplicantDocument).toHaveBeenCalledWith(
      'app-guid-1234',
      'birth_certificate',
      mockFile,
    );

    // Confirmation Card Verification
    expect(wrapper.find('[data-testid="confirmation-card"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="assigned-application-number"]').text()).toBe('ADM-2026-0042');
    expect(wrapper.text()).toContain('Application Submitted Successfully!');
  });

  it('navigates to status tracker (/admission/status) when button is clicked', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const vm = wrapper.vm as unknown as {
      isSubmitted: boolean;
      submittedApplication: RegisterApplicationResultDto;
      navigateToTracker: () => void;
    };

    vm.isSubmitted = true;
    vm.submittedApplication = {
      id: 'app-guid-1234',
      applicationNumber: 'ADM-2026-0042',
      applicantId: 'applicant-guid-5678',
      status: 'registered',
      registrationDate: '2026-02-01',
    };
    await flushPromises();

    const trackerBtn = wrapper.find('[data-testid="btn-go-to-tracker"]');
    expect(trackerBtn.exists()).toBe(true);
    await trackerBtn.trigger('click');

    expect(mockPush).toHaveBeenCalledWith('/admission/status');
  });
});
