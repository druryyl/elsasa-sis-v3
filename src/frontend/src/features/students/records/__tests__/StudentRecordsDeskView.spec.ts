// @vitest-environment jsdom
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { mount, flushPromises, RouterLinkStub } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import { useAuthStore } from '@/app/stores/auth.store';
import StudentRecordsDeskView from '../views/StudentRecordsDeskView.vue';
import * as recordsApi from '../api/records.api';
import type { StudentDetail, StudentListItem } from '../types/records.types';
import type { PaginatedResponse } from '@/shared/types/common';
import { routes } from '@/app/router/routes';

// Mock Vue Router
const mockPush = vi.fn();
let currentRouteParams: Record<string, string> = {};

vi.mock('vue-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('vue-router')>();
  return {
    ...actual,
    useRoute: () => ({ params: currentRouteParams, query: {} }),
    useRouter: () => ({ push: mockPush }),
  };
});

const mockStudents: StudentListItem[] = [
  {
    id: 'student-1',
    studentNumber: 'STD-2026-0001',
    fullName: 'Ahmad Faiz',
    preferredName: 'Faiz',
    currentStatus: 'enrolled',
    enrollmentDate: '2026-07-01',
    initialCampus: 'Main Campus (Kampus A)',
    initialLevel: 'Grade 1',
    isIndependent: false,
    primaryGuardianName: 'Umar Faiz',
    primaryGuardianPhone: '081234567890',
  },
  {
    id: 'student-2',
    studentNumber: 'STD-2026-0002',
    fullName: 'Siti Rahma',
    preferredName: null,
    currentStatus: 'active',
    enrollmentDate: '2026-07-01',
    initialCampus: 'East Campus (Kampus B)',
    initialLevel: 'Grade 7',
    isIndependent: true,
    primaryGuardianName: null,
    primaryGuardianPhone: null,
  },
];

const mockDossier: StudentDetail = {
  id: 'student-1',
  studentNumber: 'STD-2026-0001',
  applicantId: 'applicant-1',
  fullName: 'Ahmad Faiz',
  preferredName: 'Faiz',
  dateOfBirth: '2019-05-15',
  placeOfBirth: 'Jakarta',
  gender: 'Male',
  nationality: 'Indonesian',
  governmentIdType: 'NIK',
  governmentIdNumber: '3171010000000001',
  address: 'Jl. Merdeka No. 10, Jakarta',
  phone: '081234567890',
  email: 'ahmad.faiz@example.com',
  isIndependent: true,
  currentStatus: 'enrolled',
  enrollmentDate: '2026-07-01',
  notes: 'Transferred from kindergarten with honors.',
  createdAt: '2026-07-01T08:00:00Z',
  createdBy: 'admin_officer',
  updatedAt: null,
  updatedBy: null,
  enrollmentHistory: [
    {
      enrollmentDate: '2026-07-01',
      campus: 'Main Campus (Kampus A)',
      level: 'Grade 1',
      academicYear: '2026/2027',
      program: 'Reguler',
      cohort: '1A',
      approvedBy: 'admin_officer',
      paymentConfirmed: true,
      paymentReference: 'TRX-2026-001',
      paymentWaived: false,
      waiverReference: null,
    },
  ],
  guardianRelationships: [
    {
      relationshipId: 'rel-1',
      guardianId: 'guardian-1',
      fullName: 'Umar Faiz',
      relationshipType: 'father',
      governmentIdType: 'NIK',
      governmentIdNumber: '3171010000000099',
      phone: '081234567890',
      email: 'umar@example.com',
      address: 'Jl. Merdeka No. 10, Jakarta',
      occupation: 'Engineer',
      isPrimaryContact: true,
      isEmergencyContact: true,
      isFinancialContact: true,
      isAuthorizedPickup: true,
      isActive: true,
    },
  ],
  documents: [
    {
      id: 'doc-1',
      documentType: 'Birth Certificate',
      submissionStatus: 'verified',
      submittedDate: '2026-07-01',
      verifiedDate: '2026-07-02',
      verifiedBy: 'verifier_officer',
      fileName: 'birth_cert.pdf',
      fileSizeBytes: 204800,
      contentType: 'application/pdf',
      fileReference: 'storage/documents/students/student-1/birth_cert.pdf',
    },
  ],
};

describe('StudentRecordsDeskView.vue (SCR-STU-02 Master Shell)', () => {
  let queryClient: QueryClient;
  let pinia: ReturnType<typeof createPinia>;

  beforeEach(() => {
    currentRouteParams = {};
    mockPush.mockClear();

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

    pinia = createPinia();
    setActivePinia(pinia);

    const authStore = useAuthStore();
    authStore.setSession('test-token', {
      id: 'u-1',
      username: 'officer_user',
      email: 'officer@example.com',
      displayName: 'Officer User',
      roles: ['StudentAdminOfficer'],
      permissions: [
        'Permissions.Students.Records.View',
        'Permissions.Students.Records.Edit',
      ],
      isActive: true,
    });

    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
      },
    });

    vi.spyOn(recordsApi, 'searchStudents').mockResolvedValue({
      items: mockStudents,
      pageNumber: 1,
      pageSize: 50,
      totalCount: 2,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
    } as PaginatedResponse<StudentListItem>);

    vi.spyOn(recordsApi, 'getStudentById').mockResolvedValue(mockDossier);
    vi.spyOn(recordsApi, 'getStudentAdministrativeHistory').mockResolvedValue({
      studentId: 'student-1',
      entries: [],
    });
  });

  function mountComponent() {
    return mount(StudentRecordsDeskView, {
      global: {
        plugins: [
          pinia,
          [VueQueryPlugin, { queryClient }],
          PrimeVue,
          ToastService,
        ],
        stubs: {
          RouterLink: RouterLinkStub,
          teleport: true,
          Dialog: {
            template: `
              <div v-if="visible" class="dialog-stub" :data-testid="$attrs['data-testid']">
                <slot name="header" />
                <slot />
                <slot name="footer" />
              </div>
            `,
            props: ['visible'],
          },
        },
      },
    });
  }

  it('renders Two-Pane Master-Detail shell with left pane and right pane', async () => {
    const wrapper = mountComponent();
    await flushPromises();

    expect(wrapper.find('[data-testid="student-records-desk-view"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="records-left-pane"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="records-right-pane"]').exists()).toBe(true);
  });

  it('renders summary metrics counters in the left pane (Total, Enrolled, Active)', async () => {
    const wrapper = mountComponent();
    await flushPromises();

    expect(wrapper.find('[data-testid="counter-total"]').text()).toBe('2');
    expect(wrapper.find('[data-testid="counter-enrolled"]').text()).toBe('1');
    expect(wrapper.find('[data-testid="counter-active"]').text()).toBe('1');
  });

  it('renders search input, filter selects, and reset filter button in the left pane', async () => {
    const wrapper = mountComponent();
    await flushPromises();

    expect(wrapper.find('[data-testid="search-input"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="filter-campus"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="filter-level"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="filter-status"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="reset-filters-button"]').exists()).toBe(true);
  });

  it('filters results when typing into search input and reset button restores defaults', async () => {
    const searchSpy = vi.spyOn(recordsApi, 'searchStudents');
    const wrapper = mountComponent();
    await flushPromises();

    const searchInput = wrapper.find('[data-testid="search-input"]');
    await searchInput.setValue('Faiz');
    await flushPromises();

    // Verify reset button clears search input
    const resetBtn = wrapper.find('[data-testid="reset-filters-button"]');
    await resetBtn.trigger('click');
    await flushPromises();

    expect((wrapper.find('[data-testid="search-input"]').element as HTMLInputElement).value).toBe('');
    expect(searchSpy).toHaveBeenCalled();
  });

  it('renders scrollable candidate list with student number, full name, level, campus, and status pill', async () => {
    const wrapper = mountComponent();
    await flushPromises();

    const candidateCards = wrapper.findAll('[data-testid^="candidate-card-"]');
    expect(candidateCards.length).toBe(2);

    const firstCard = wrapper.find('[data-testid="candidate-card-student-1"]');
    expect(firstCard.find('[data-testid="candidate-number"]').text()).toBe('STD-2026-0001');
    expect(firstCard.find('[data-testid="candidate-name"]').text()).toContain('Ahmad Faiz');
    expect(firstCard.find('[data-testid="candidate-status"]').exists()).toBe(true);
    expect(firstCard.find('[data-testid="candidate-placement"]').text()).toContain('Grade 1');
    expect(firstCard.find('[data-testid="candidate-placement"]').text()).toContain('Main Campus (Kampus A)');
  });

  it('renders empty prompt in the right pane when no student is selected', async () => {
    const wrapper = mountComponent();
    await flushPromises();

    expect(wrapper.find('[data-testid="empty-dossier-prompt"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Select a student to inspect records');
    expect(wrapper.find('[data-testid="student-dossier-panel"]').exists()).toBe(false);
  });

  it('selecting a student pushes route and renders 360° student dossier panel', async () => {
    const wrapper = mountComponent();
    await flushPromises();

    const firstCard = wrapper.find('[data-testid="candidate-card-student-1"]');
    await firstCard.trigger('click');
    await flushPromises();

    expect(mockPush).toHaveBeenCalledWith({
      name: 'students.records.detail',
      params: { id: 'student-1' },
    });

    expect(wrapper.find('[data-testid="student-dossier-panel"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="dossier-student-number"]').text()).toBe('STD-2026-0001');
    expect(wrapper.find('[data-testid="dossier-full-name"]').text()).toBe('Ahmad Faiz');
    expect(wrapper.find('[data-testid="dossier-status-badge"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="dossier-independent-badge"]').exists()).toBe(true);
  });

  it('renders dossier quick action buttons (Edit Identity, Add Guardian, Upload Document)', async () => {
    currentRouteParams = { id: 'student-1' };
    const wrapper = mountComponent();
    await flushPromises();

    const editBtn = wrapper.find('[data-testid="edit-identity-button"]');
    const addGuardianBtn = wrapper.find('[data-testid="add-guardian-button"]');
    const uploadDocBtn = wrapper.find('[data-testid="upload-document-button"]');

    expect(editBtn.exists()).toBe(true);
    expect(addGuardianBtn.exists()).toBe(true);
    expect(uploadDocBtn.exists()).toBe(true);

    // Clicking quick action buttons opens modal placeholders
    await editBtn.trigger('click');
    await flushPromises();
    expect(wrapper.find('[data-testid="edit-identity-dialog"]').exists()).toBe(true);

    await addGuardianBtn.trigger('click');
    await flushPromises();
    expect(wrapper.find('[data-testid="add-guardian-dialog"]').exists()).toBe(true);

    await uploadDocBtn.trigger('click');
    await flushPromises();
    expect(wrapper.find('[data-testid="upload-document-dialog"]').exists()).toBe(true);
  });

  it('renders all dossier navigation tabs and panels including Placement and Administrative History', async () => {
    currentRouteParams = { id: 'student-1' };
    const wrapper = mountComponent();
    await flushPromises();

    expect(wrapper.find('[data-testid="tab-identity"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="tab-placement"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="tab-guardians"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="tab-documents"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="tab-enrollment"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="tab-history"]').exists()).toBe(true);

    // Default tab is Certified Identity
    expect(wrapper.find('[data-testid="panel-identity"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="panel-identity"]').text()).toContain('3171010000000001');
    expect(wrapper.find('[data-testid="panel-identity"]').text()).toContain('Jl. Merdeka No. 10, Jakarta');
  });

  it('asserts Placement and Administrative History tabs are rendered in the desk tab navigation', async () => {
    currentRouteParams = { id: 'student-1' };
    const wrapper = mountComponent();
    await flushPromises();

    const placementTab = wrapper.find('[data-testid="tab-placement"]');
    expect(placementTab.exists()).toBe(true);
    expect(placementTab.text()).toContain('Placement');

    const historyTab = wrapper.find('[data-testid="tab-history"]');
    expect(historyTab.exists()).toBe(true);
    expect(historyTab.text()).toContain('Administrative History');
  });

  it('asserts header status badge displays current status and triggers status change modal', async () => {
    currentRouteParams = { id: 'student-1' };
    const wrapper = mountComponent();
    await flushPromises();

    const statusBadge = wrapper.find('[data-testid="dossier-status-badge"]');
    expect(statusBadge.exists()).toBe(true);
    expect(statusBadge.text().toLowerCase()).toContain('enrolled');

    const changeStatusBtn = wrapper.find('[data-testid="change-status-button"]');
    expect(changeStatusBtn.exists()).toBe(true);
    expect(changeStatusBtn.attributes('disabled')).toBeUndefined();

    // Trigger status change modal
    await changeStatusBtn.trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-testid="change-status-modal"]').exists()).toBe(true);
  });

  it('renders empty candidate state when no search matches found', async () => {
    vi.spyOn(recordsApi, 'searchStudents').mockResolvedValueOnce({
      items: [],
      pageNumber: 1,
      pageSize: 50,
      totalCount: 0,
      totalPages: 0,
      hasPreviousPage: false,
      hasNextPage: false,
    });

    const wrapper = mountComponent();
    await flushPromises();

    expect(wrapper.find('[data-testid="empty-candidates"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('No students found');
  });
});

describe('Router Configuration (P10-S16)', () => {
  it('configures /students/enrollment redirect to /students/records', () => {
    const appShellRoute = routes.find((r) => r.path === '/' && r.children);
    expect(appShellRoute).toBeDefined();

    const studentsParent = appShellRoute?.children?.find((r) => r.path === 'students');
    expect(studentsParent).toBeDefined();

    const enrollmentRoute = studentsParent?.children?.find((r) => r.path === 'enrollment');
    expect(enrollmentRoute).toBeDefined();
    expect(enrollmentRoute?.redirect).toBe('/students/records');
  });

  it('configures /students/records and /students/records/:id routes', () => {
    const appShellRoute = routes.find((r) => r.path === '/' && r.children);
    const studentsParent = appShellRoute?.children?.find((r) => r.path === 'students');

    const deskRoute = studentsParent?.children?.find((r) => r.path === 'records');
    expect(deskRoute).toBeDefined();
    expect(deskRoute?.name).toBe('students.records.desk');
    expect(deskRoute?.meta?.permission).toBe('Permissions.Students.Records.View');
    expect(deskRoute?.meta?.title).toBe('Student Records Desk');

    const detailRoute = studentsParent?.children?.find((r) => r.path === 'records/:id');
    expect(detailRoute).toBeDefined();
    expect(detailRoute?.name).toBe('students.records.detail');
    expect(detailRoute?.meta?.permission).toBe('Permissions.Students.Records.View');
    expect(detailRoute?.meta?.title).toBe('Student Records Desk');
  });
});
