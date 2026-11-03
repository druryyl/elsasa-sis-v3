// @vitest-environment jsdom
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import { useAuthStore } from '@/app/stores/auth.store';
import StudentGuardiansTab from '../components/StudentGuardiansTab.vue';
import EditGuardianModal from '../components/EditGuardianModal.vue';
import AddGuardianModal from '../components/AddGuardianModal.vue';
import * as recordsApi from '../api/records.api';
import type { StudentDetail } from '../types/records.types';

const mockStudentWithGuardians: StudentDetail = {
  id: 'student-100',
  studentNumber: 'STD-2026-0042',
  applicantId: 'applicant-100',
  fullName: 'Farhan Muhammad',
  preferredName: 'Farhan',
  dateOfBirth: '2018-03-20',
  placeOfBirth: 'Bandung',
  gender: 'Male',
  nationality: 'Indonesian',
  governmentIdType: 'NIK',
  governmentIdNumber: '3273010000000042',
  address: 'Jl. Dago No. 42, Bandung',
  phone: '081299998888',
  email: 'farhan@example.com',
  isIndependent: false,
  currentStatus: 'enrolled',
  enrollmentDate: '2026-07-01',
  notes: null,
  createdAt: '2026-07-01T08:00:00Z',
  createdBy: 'admission_officer',
  updatedAt: null,
  updatedBy: null,
  enrollmentHistory: [],
  guardianRelationships: [
    {
      relationshipId: 'rel-1',
      guardianId: 'guard-1',
      fullName: 'Bambang Sudarsono',
      relationshipType: 'father',
      governmentIdType: 'NIK',
      governmentIdNumber: '3273010000000001',
      phone: '081211112222',
      email: 'bambang@example.com',
      address: 'Jl. Dago No. 42, Bandung',
      occupation: 'Lecturer',
      isPrimaryContact: true,
      isEmergencyContact: true,
      isFinancialContact: true,
      isAuthorizedPickup: true,
      isActive: true,
    },
    {
      relationshipId: 'rel-2',
      guardianId: 'guard-2',
      fullName: 'Dewi Lestari',
      relationshipType: 'mother',
      governmentIdType: 'NIK',
      governmentIdNumber: '3273010000000002',
      phone: '081233334444',
      email: 'dewi@example.com',
      address: 'Jl. Dago No. 42, Bandung',
      occupation: 'Doctor',
      isPrimaryContact: false,
      isEmergencyContact: true,
      isFinancialContact: false,
      isAuthorizedPickup: true,
      isActive: true,
    },
    {
      relationshipId: 'rel-3',
      guardianId: 'guard-3',
      fullName: 'Oma Kartini',
      relationshipType: 'grandparent',
      governmentIdType: 'NIK',
      governmentIdNumber: '3273010000000003',
      phone: '081255556666',
      email: null,
      address: 'Jl. Dago No. 42, Bandung',
      occupation: 'Retired',
      isPrimaryContact: false,
      isEmergencyContact: false,
      isFinancialContact: false,
      isAuthorizedPickup: true,
      isActive: false,
    },
  ],
  documents: [],
};

describe('StudentGuardiansTab.spec.ts (SCR-STU-02 Guardian Relationships Tab)', () => {
  let queryClient: QueryClient;
  let pinia: ReturnType<typeof createPinia>;

  const dialogStub = {
    template: `
      <div v-if="visible" class="dialog-stub" :data-testid="$attrs['data-testid']">
        <slot name="header" />
        <slot />
        <slot name="footer" />
      </div>
    `,
    props: ['visible'],
  };

  beforeEach(() => {
    vi.clearAllMocks();

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
      username: 'records_officer',
      email: 'records@example.com',
      displayName: 'Records Officer',
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
        mutations: { retry: false },
      },
    });
  });

  function mountTab(props = {}) {
    return mount(StudentGuardiansTab, {
      props: {
        student: mockStudentWithGuardians,
        canEdit: true,
        ...props,
      },
      global: {
        plugins: [
          pinia,
          [VueQueryPlugin, { queryClient }],
          PrimeVue,
          ToastService,
        ],
        stubs: {
          Dialog: dialogStub,
        },
      },
    });
  }

  it('renders guardian cards and displays primary contact badge on the active primary guardian', () => {
    const wrapper = mountTab();

    const cards = wrapper.findAll('[data-testid^="guardian-card-"]');
    expect(cards).toHaveLength(3);

    // Primary Guardian (rel-1)
    const primaryCard = wrapper.find('[data-testid="guardian-card-rel-1"]');
    expect(primaryCard.find('[data-testid="guardian-name"]').text()).toBe('Bambang Sudarsono');
    expect(primaryCard.find('[data-testid="guardian-relationship-type"]').text()).toBe('father');
    expect(primaryCard.find('[data-testid="badge-primary-contact"]').exists()).toBe(true);
    expect(primaryCard.find('[data-testid="badge-primary-contact"]').text()).toContain('★ Primary Contact');
    expect(primaryCard.find('[data-testid="badge-emergency-contact"]').exists()).toBe(true);
    expect(primaryCard.find('[data-testid="badge-financial-contact"]').exists()).toBe(true);
    expect(primaryCard.find('[data-testid="badge-pickup-contact"]').exists()).toBe(true);
    expect(primaryCard.find('[data-testid="btn-set-primary"]').exists()).toBe(false);

    // Inactive Guardian (rel-3)
    const inactiveCard = wrapper.find('[data-testid="guardian-card-rel-3"]');
    expect(inactiveCard.text()).toContain('Inactive');
  });

  it('renders action buttons for non-primary guardians (Set as Primary Contact and Edit Responsibilities)', () => {
    const wrapper = mountTab();

    const secondCard = wrapper.find('[data-testid="guardian-card-rel-2"]');
    expect(secondCard.find('[data-testid="guardian-name"]').text()).toBe('Dewi Lestari');
    expect(secondCard.find('[data-testid="badge-primary-contact"]').exists()).toBe(false);

    const setPrimaryBtn = secondCard.find('[data-testid="btn-set-primary"]');
    expect(setPrimaryBtn.exists()).toBe(true);
    expect(setPrimaryBtn.text()).toContain('Set as Primary Contact');

    const editBtn = secondCard.find('[data-testid="btn-edit-guardian"]');
    expect(editBtn.exists()).toBe(true);
    expect(editBtn.text()).toContain('Edit Responsibilities');
  });

  it('triggers primary contact transfer when [ Set as Primary Contact ] is clicked', async () => {
    const updateSpy = vi.spyOn(recordsApi, 'updateGuardianRelationship').mockResolvedValue({
      relationshipId: 'rel-2',
      guardianId: 'guard-2',
      isPrimaryContact: true,
      isActive: true,
    });

    const wrapper = mountTab();

    const secondCard = wrapper.find('[data-testid="guardian-card-rel-2"]');
    const setPrimaryBtn = secondCard.find('[data-testid="btn-set-primary"]');
    await setPrimaryBtn.trigger('click');
    await flushPromises();

    expect(updateSpy).toHaveBeenCalledTimes(1);
    expect(updateSpy).toHaveBeenCalledWith(
      'student-100',
      'rel-2',
      expect.objectContaining({
        isPrimaryContact: true,
        relationshipType: 'mother',
        isEmergencyContact: true,
        isFinancialContact: false,
        isAuthorizedPickup: true,
        isActive: true,
        phone: '081233334444',
        email: 'dewi@example.com',
      }),
    );
  });

  it('opens EditGuardianModal when [ Edit Responsibilities ] is clicked on a guardian card', async () => {
    const wrapper = mountTab();

    const editModal = wrapper.findComponent(EditGuardianModal);
    expect(editModal.props('visible')).toBe(false);

    const firstCard = wrapper.find('[data-testid="guardian-card-rel-1"]');
    await firstCard.find('[data-testid="btn-edit-guardian"]').trigger('click');
    await flushPromises();

    expect(editModal.props('visible')).toBe(true);
    expect(editModal.props('guardian')?.relationshipId).toBe('rel-1');
  });

  it('opens AddGuardianModal and emits addGuardian event when [ Add Guardian ] button is clicked', async () => {
    const wrapper = mountTab();

    const addModal = wrapper.findComponent(AddGuardianModal);
    expect(addModal.props('visible')).toBe(false);

    const addBtn = wrapper.find('[data-testid="tab-add-guardian-button"]');
    expect(addBtn.exists()).toBe(true);
    await addBtn.trigger('click');
    await flushPromises();

    expect(addModal.props('visible')).toBe(true);
    expect(wrapper.emitted('addGuardian')).toHaveLength(1);
  });

  it('renders empty state when student has no guardian relationships configured', () => {
    const wrapper = mountTab({
      student: {
        ...mockStudentWithGuardians,
        guardianRelationships: [],
      },
    });

    expect(wrapper.find('[data-testid="empty-guardians"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('No guardians on record');
    expect(wrapper.find('[data-testid="guardians-list"]').exists()).toBe(false);
  });

  it('hides edit and action buttons when canEdit is false', () => {
    const wrapper = mountTab({ canEdit: false });

    expect(wrapper.find('[data-testid="tab-add-guardian-button"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="btn-set-primary"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="btn-edit-guardian"]').exists()).toBe(false);
  });
});
