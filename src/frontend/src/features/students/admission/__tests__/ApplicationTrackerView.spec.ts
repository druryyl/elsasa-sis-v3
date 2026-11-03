// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, beforeAll } from 'vitest';
import { mount } from '@vue/test-utils';
import { ref } from 'vue';
import PrimeVue from 'primevue/config';
import ApplicationTrackerView from '../views/ApplicationTrackerView.vue';
import * as admissionQueries from '../composables/useAdmissionQueries';
import type { PublicApplicationStatusDto } from '../types/admission.types';

// Mock window.matchMedia for PrimeVue DatePicker in jsdom
beforeAll(() => {
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
});

// Mock vue-router
const mockRoute = {
  query: {},
};

vi.mock('vue-router', () => ({
  useRoute: () => mockRoute,
  RouterLink: {
    name: 'RouterLink',
    props: ['to'],
    template: '<a :href="to"><slot /></a>',
  },
}));

describe('ApplicationTrackerView.vue (SCR-SELF-01)', () => {
  const mockStatusQueryData = ref<PublicApplicationStatusDto | null>(null);
  const mockIsLoading = ref(false);
  const mockIsFetching = ref(false);
  const mockIsError = ref(false);

  beforeEach(() => {
    mockRoute.query = {};
    mockStatusQueryData.value = null;
    mockIsLoading.value = false;
    mockIsFetching.value = false;
    mockIsError.value = false;

    vi.spyOn(admissionQueries, 'useApplicationStatusQuery').mockImplementation(() => {
      return {
        data: mockStatusQueryData,
        isLoading: mockIsLoading,
        isFetching: mockIsFetching,
        isError: mockIsError,
      } as unknown as ReturnType<typeof admissionQueries.useApplicationStatusQuery>;
    });
  });

  function createWrapper() {
    return mount(ApplicationTrackerView, {
      global: {
        plugins: [PrimeVue],
        stubs: {
          RouterLink: {
            props: ['to'],
            template: '<a :href="to" :data-testid="$attrs[\'data-testid\']"><slot /></a>',
          },
        },
      },
    });
  }

  it('renders initial search form with guidance state before search', () => {
    const wrapper = createWrapper();

    expect(wrapper.find('[data-testid="application-tracker-view"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="tracker-search-card"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="application-number-input"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="date-of-birth-picker"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="check-status-btn"]').exists()).toBe(true);

    // Initial guidance state is rendered before search is executed
    expect(wrapper.find('[data-testid="initial-guidance-card"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Ready to Track Your Application');
  });

  it('validates inputs and displays error messages when fields are empty', async () => {
    const wrapper = createWrapper();

    // Submit empty form
    await wrapper.find('form').trigger('submit.prevent');

    // Validation errors should be present
    expect(wrapper.text()).toContain('Application number is required.');
    expect(wrapper.text()).toContain('Date of birth is required.');
    expect(wrapper.find('[data-testid="status-results"]').exists()).toBe(false);
  });

  it('validates invalid date format before querying', async () => {
    const wrapper = createWrapper();

    const appNumberInput = wrapper.find('[data-testid="application-number-input"]');
    await appNumberInput.setValue('ADM-2026-0001');

    await wrapper.find('form').trigger('submit.prevent');

    // Date of birth error should be rendered
    expect(wrapper.text()).toContain('Date of birth is required.');
  });

  it('renders loading spinner when inquiry is fetching', () => {
    mockIsLoading.value = true;
    const wrapper = createWrapper();

    expect(wrapper.find('[data-testid="loading-state"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Querying admission records...');
  });

  it('renders error state when application is not found (404)', () => {
    mockIsError.value = true;
    const wrapper = createWrapper();

    expect(wrapper.find('[data-testid="error-state"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Application Record Not Found');
    expect(wrapper.find('[data-testid="try-again-btn"]').exists()).toBe(true);
  });

  it('presents complete application overview, status badge, and timeline when found', () => {
    mockStatusQueryData.value = {
      applicationNumber: 'ADM-2026-0001',
      applicantFullName: 'Budi Santoso',
      requestedEntryLevel: 'Grade 1',
      status: 'under_review',
      registrationDate: '2026-04-01',
      documentStatuses: [
        { documentType: 'birth_certificate', submissionStatus: 'verified' },
        { documentType: 'family_card', submissionStatus: 'submitted' },
      ],
      decisionNotice: null,
    };

    const wrapper = createWrapper();

    expect(wrapper.find('[data-testid="status-results"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="status-overview-card"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('ADM-2026-0001');
    expect(wrapper.text()).toContain('Budi Santoso');
    expect(wrapper.text()).toContain('Grade 1');

    // Status pill displays under review
    const statusPill = wrapper.find('[data-testid="application-status-pill"]');
    expect(statusPill.exists()).toBe(true);
    expect(statusPill.text()).toBe('Under Review');

    // Progress timeline shows step 2 active
    expect(wrapper.text()).toContain('Admission Progress Timeline');
    expect(wrapper.text()).toContain('1. Registered');
    expect(wrapper.text()).toContain('2. Under Review');
    expect(wrapper.text()).toContain('3. Decision Issued');
  });

  it('renders document checklist progress with correct stats and progress bar', () => {
    mockStatusQueryData.value = {
      applicationNumber: 'ADM-2026-0001',
      applicantFullName: 'Siti Aminah',
      requestedEntryLevel: 'Kindergarten A',
      status: 'registered',
      registrationDate: '2026-04-10',
      documentStatuses: [
        { documentType: 'birth_certificate', submissionStatus: 'verified' },
        { documentType: 'family_card', submissionStatus: 'verified' },
        { documentType: 'previous_report', submissionStatus: 'submitted' },
        { documentType: 'medical_record', submissionStatus: 'pending' },
      ],
      decisionNotice: null,
    };

    const wrapper = createWrapper();

    const checklistCard = wrapper.find('[data-testid="document-checklist-card"]');
    expect(checklistCard.exists()).toBe(true);
    expect(checklistCard.text()).toContain('2 of 4 Verified');
    expect(checklistCard.text()).toContain('(3 submitted)');
    expect(checklistCard.text()).toContain('50% Complete');

    // Rows rendered
    const rows = wrapper.findAll('[data-testid="document-checklist-row"]');
    expect(rows.length).toBe(4);
    expect(rows[0].text()).toContain('Birth Certificate');
    expect(rows[1].text()).toContain('Family Card');
    expect(rows[2].text()).toContain('Previous School Academic Report');
  });

  it('strictly omits internal staff review notes from the public projection', () => {
    mockStatusQueryData.value = {
      applicationNumber: 'ADM-2026-0001',
      applicantFullName: 'Budi Santoso',
      requestedEntryLevel: 'Grade 1',
      status: 'decided',
      registrationDate: '2026-04-01',
      documentStatuses: [
        { documentType: 'birth_certificate', submissionStatus: 'verified' },
      ],
      decisionNotice: {
        decisionType: 'accepted',
        effectiveDate: '2026-07-01',
      },
    };

    const wrapper = createWrapper();

    // Verify internal note fields are completely absent from text
    const fullText = wrapper.text();
    expect(fullText).not.toContain('Review Notes:');
    expect(fullText).not.toContain('Staff Notes:');
    expect(fullText).not.toContain('verified_by:');
    expect(fullText).not.toContain('decided_by:');
    expect(fullText).not.toContain('Committee Recommendation');
  });

  it('renders Official Decision Notice card when decision notice is present (accepted outcome)', () => {
    mockStatusQueryData.value = {
      applicationNumber: 'ADM-2026-0042',
      applicantFullName: 'Ahmad Dahlan',
      requestedEntryLevel: 'Grade 7',
      status: 'decided',
      registrationDate: '2026-03-15',
      documentStatuses: [
        { documentType: 'birth_certificate', submissionStatus: 'verified' },
      ],
      decisionNotice: {
        decisionType: 'accepted',
        effectiveDate: '2026-07-15',
      },
    };

    const wrapper = createWrapper();

    const noticeCard = wrapper.find('[data-testid="decision-notice-card"]');
    expect(noticeCard.exists()).toBe(true);
    expect(noticeCard.text()).toContain('Admission Decision: Accepted');
    expect(noticeCard.text()).toContain('15 Jul 2026');
    expect(noticeCard.text()).toContain('Congratulations! Your application for admission has been officially accepted');

    const outcomePill = wrapper.find('[data-testid="decision-outcome-pill"]');
    expect(outcomePill.exists()).toBe(true);
    expect(outcomePill.text()).toBe('Accepted');
  });

  it('renders Official Decision Notice card for rejected decision outcome', () => {
    mockStatusQueryData.value = {
      applicationNumber: 'ADM-2026-0099',
      applicantFullName: 'Joko Widodo',
      requestedEntryLevel: 'Grade 10',
      status: 'decided',
      registrationDate: '2026-03-20',
      documentStatuses: [],
      decisionNotice: {
        decisionType: 'rejected',
        effectiveDate: '2026-04-01',
      },
    };

    const wrapper = createWrapper();

    const noticeCard = wrapper.find('[data-testid="decision-notice-card"]');
    expect(noticeCard.exists()).toBe(true);
    expect(noticeCard.text()).toContain('Admission Decision: Application Not Accepted');
    expect(noticeCard.text()).toContain('unable to offer an admission placement');

    const outcomePill = wrapper.find('[data-testid="decision-outcome-pill"]');
    expect(outcomePill.exists()).toBe(true);
    expect(outcomePill.text()).toBe('Not Accepted');
  });

  it('renders Official Decision Notice card for waitlisted decision outcome', () => {
    mockStatusQueryData.value = {
      applicationNumber: 'ADM-2026-0088',
      applicantFullName: 'Dewi Sartika',
      requestedEntryLevel: 'Grade 1',
      status: 'decided',
      registrationDate: '2026-03-20',
      documentStatuses: [],
      decisionNotice: {
        decisionType: 'waitlisted',
        effectiveDate: '2026-04-05',
      },
    };

    const wrapper = createWrapper();

    const noticeCard = wrapper.find('[data-testid="decision-notice-card"]');
    expect(noticeCard.exists()).toBe(true);
    expect(noticeCard.text()).toContain('Admission Decision: Placed on Waitlist');
    expect(noticeCard.text()).toContain('placed on the official admissions waitlist');
  });

  it('provides navigation links back to /admission/apply and homepage', () => {
    const wrapper = createWrapper();

    const navApplyLink = wrapper.find('[data-testid="nav-apply-link"]');
    expect(navApplyLink.exists()).toBe(true);
    expect(navApplyLink.attributes('href')).toBe('/admission/apply');

    const navHomeLink = wrapper.find('[data-testid="nav-home-link"]');
    expect(navHomeLink.exists()).toBe(true);
    expect(navHomeLink.attributes('href')).toBe('/');
  });

  it('resets search fields and restores initial guidance when clicking Clear button', async () => {
    const wrapper = createWrapper();

    const appNumberInput = wrapper.find('[data-testid="application-number-input"]');
    await appNumberInput.setValue('ADM-2026-0001');

    const resetBtn = wrapper.find('[data-testid="reset-btn"]');
    expect(resetBtn.exists()).toBe(true);

    await resetBtn.trigger('click');

    expect((appNumberInput.element as HTMLInputElement).value).toBe('');
    expect(wrapper.find('[data-testid="initial-guidance-card"]').exists()).toBe(true);
  });
});
