// @vitest-environment jsdom
import { describe, it, expect, beforeEach, afterEach, beforeAll, vi } from 'vitest';
import { mount, flushPromises, type VueWrapper } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import PrimeVue from 'primevue/config';
import Select from 'primevue/select';
import { ref } from 'vue';
import FinancialPolicyWorkspaceView from '../policies/views/FinancialPolicyWorkspaceView.vue';
import CreatePolicyModal from '../policies/components/CreatePolicyModal.vue';
import CreateChargeRuleModal from '../policies/components/CreateChargeRuleModal.vue';
import { useAuthStore } from '@/app/stores/auth.store';
import { FINANCE_POLICY_PERMISSIONS } from '../policies/constants/permissions';
import type {
  FinancialPolicyDto,
  PolicyCategoryDto,
} from '../policies/types/policies.types';

// Mock TanStack Vue Query composables
const mockPoliciesList = ref<FinancialPolicyDto[]>([]);
const mockPolicyDetail = ref<FinancialPolicyDto | null>(null);
const mockPolicyCategories = ref<PolicyCategoryDto[]>([]);
const mockIsLoadingCategories = ref(false);

const mockRefetchPolicies = vi.fn();
const mockRefetchDetail = vi.fn();
const mockApproveMutateAsync = vi.fn().mockResolvedValue({
  id: 'pol-1',
  policyCode: 'FP-2026-001',
  name: 'SPP Policy',
  status: 'active',
  approvingAuthority: 'Finance Authority',
  approvedDate: '2026-11-04',
  effectiveDate: '2026-07-01',
  notes: null,
  updatedAt: null,
  updatedBy: null,
});
const mockCreatePolicyMutateAsync = vi.fn().mockResolvedValue({
  id: 'pol-created',
  policyCode: 'FP-2026-002',
  name: 'New Draft Policy',
  category: 'tuition',
  businessPurpose: 'Tuition collection',
  applicableContext: null,
  effectiveDate: '2026-07-01',
  endDate: null,
  policyVersion: '1.0',
  status: 'draft',
  supersedesPolicyId: null,
  notes: null,
  createdAt: '2026-11-04T00:00:00Z',
  createdBy: 'test-admin',
});
const mockCreateChargeRuleMutateAsync = vi.fn().mockResolvedValue({
  id: 'rule-created',
  ruleCode: 'CR-2026-NEW',
  policyId: 'pol-1',
  name: 'New Charge Rule',
  chargeType: 'tuition',
  isMandatory: true,
  determinationMethod: 'fixed_amount',
  baseAmount: 1000000,
  currencyCode: 'IDR',
  recurrencePattern: 'monthly',
  dueRuleType: 'fixed_day_of_month',
  dueRuleValue: '10',
  applicableGrade: null,
  applicableProgram: null,
  status: 'active',
  notes: null,
  createdAt: '2026-11-04T00:00:00Z',
  createdBy: 'test-admin',
});

vi.mock('../policies/composables/useFinancePolicies', () => ({
  useFinancePolicyCategoriesQuery: () => ({
    data: mockPolicyCategories,
    isLoading: mockIsLoadingCategories,
  }),
  useFinancePoliciesQuery: () => ({
    data: mockPoliciesList,
    isLoading: ref(false),
    isFetching: ref(false),
    refetch: mockRefetchPolicies,
  }),
  useFinancePolicyDetailQuery: () => ({
    data: mockPolicyDetail,
    isLoading: ref(false),
    isFetching: ref(false),
    refetch: mockRefetchDetail,
  }),
  useApprovePolicyMutation: (opts?: any) => ({
    mutateAsync: async (args: any) => {
      const res = await mockApproveMutateAsync(args);
      opts?.onSuccess?.(res, args);
      return res;
    },
    isPending: ref(false),
  }),
  useCreatePolicyMutation: (opts?: any) => ({
    mutateAsync: async (args: any) => {
      const res = await mockCreatePolicyMutateAsync(args);
      opts?.onSuccess?.(res, args);
      return res;
    },
    isPending: ref(false),
  }),
  useCreateChargeRuleMutation: (opts?: any) => ({
    mutateAsync: async (args: any) => {
      const res = await mockCreateChargeRuleMutateAsync(args);
      opts?.onSuccess?.(res, args);
      return res;
    },
    isPending: ref(false),
  }),
}));

const mockRouter = {
  push: vi.fn(),
  replace: vi.fn(),
};

vi.mock('vue-router', async (importOriginal) => {
  const actual = await importOriginal<typeof import('vue-router')>();
  return {
    ...actual,
    useRouter: () => mockRouter,
    useRoute: () => ({ path: '/finance/policies', params: {}, query: {} }),
  };
});

describe('FinancialPolicyWorkspaceView.vue (SCR-FIN-04, /finance/policies)', () => {
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

      global.ResizeObserver = class ResizeObserver {
        observe() {}
        unobserve() {}
        disconnect() {}
      };
    }
  });

  let activeWrapper: VueWrapper | null = null;

  afterEach(() => {
    activeWrapper?.unmount();
    activeWrapper = null;
    document.body.innerHTML = '';
  });

  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
    document.body.innerHTML = '';

    mockIsLoadingCategories.value = false;
    mockPolicyCategories.value = [
      {
        code: 'tuition',
        name: 'Tuition (SPP)',
        description: 'Regular academic and instructional fees',
        displayOrder: 1,
        isActive: true,
      },
      {
        code: 'admission_fee',
        name: 'Admission Fee (Uang Pangkal)',
        description: 'One-time enrollment and registration fees',
        displayOrder: 2,
        isActive: true,
      },
      {
        code: 'facilities',
        name: 'Facilities (Sarana & Prasarana)',
        description: 'Campus development, building, and facility maintenance fees',
        displayOrder: 3,
        isActive: true,
      },
      {
        code: 'activities',
        name: 'Activities (Kegiatan Siswa)',
        description: 'Co-curricular activities, field trips, and student events',
        displayOrder: 4,
        isActive: true,
      },
      {
        code: 'general_fee',
        name: 'General Institutional Fee (Biaya Umum)',
        description: 'Institutional and operational charges including materials and books',
        displayOrder: 5,
        isActive: true,
      },
    ];

    mockPoliciesList.value = [
      {
        id: 'pol-1',
        policyCode: 'FP-2026-001',
        name: 'Kebijakan Biaya Pendidikan TP 2026/2027',
        category: 'tuition',
        businessPurpose: 'Menetapkan standar biaya SPP dan uang gedung',
        applicableContext: 'Seluruh Kampus',
        effectiveDate: '2026-07-01',
        endDate: '2027-06-30',
        policyVersion: '1.0',
        status: 'draft',
        approvingAuthority: null,
        approvedDate: null,
        supersedesPolicyId: null,
        notes: 'Draft menunggu persetujuan Direktur Keuangan',
        createdAt: '2026-11-04T00:00:00Z',
        createdBy: 'finance_officer',
        rules: [
          {
            id: 'rule-1',
            ruleCode: 'CR-2026-SPP-SD',
            policyId: 'pol-1',
            name: 'SPP Bulanan SD',
            chargeType: 'tuition',
            isMandatory: true,
            determinationMethod: 'fixed_amount',
            baseAmount: 750000,
            currencyCode: 'IDR',
            recurrencePattern: 'monthly',
            dueRuleType: 'fixed_day_of_month',
            dueRuleValue: '10',
            applicableGrade: 'Grade 1',
            applicableProgram: 'regular',
            status: 'active',
          },
        ],
      },
      {
        id: 'pol-2',
        policyCode: 'FP-2025-001',
        name: 'Kebijakan Biaya Pendidikan TP 2025/2026',
        category: 'tuition',
        businessPurpose: 'Kebijakan tahun lalu',
        applicableContext: null,
        effectiveDate: '2025-07-01',
        endDate: '2026-06-30',
        policyVersion: '1.0',
        status: 'superseded',
        approvingAuthority: 'Direktur Keuangan',
        approvedDate: '2025-06-15',
        supersedesPolicyId: null,
        notes: null,
        createdAt: '2025-06-01T00:00:00Z',
        createdBy: 'finance_officer',
        rules: [],
      },
    ];

    mockPolicyDetail.value = mockPoliciesList.value[0];
  });

  function createWrapper() {
    activeWrapper = mount(FinancialPolicyWorkspaceView, {
      attachTo: document.body,
      global: {
        plugins: [PrimeVue],
        stubs: {
          RouterLink: {
            template: '<a :href="to"><slot /></a>',
            props: ['to'],
          },
        },
      },
    });
    return activeWrapper;
  }

  it('renders workspace page header and filter panel', () => {
    const wrapper = createWrapper();

    expect(wrapper.find('[data-testid="policies-page-header"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Financial Policy Workspace');
    expect(wrapper.find('[data-testid="policy-filter-panel"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="status-pills-bar"]').exists()).toBe(true);
  });

  it('renders list of policies with policy code, version badge, and status pill', () => {
    const wrapper = createWrapper();

    const masterCard = wrapper.find('[data-testid="policies-master-card"]');
    expect(masterCard.exists()).toBe(true);
    expect(masterCard.text()).toContain('FP-2026-001');
    expect(masterCard.text()).toContain('FP-2025-001');
    expect(masterCard.text()).toContain('v1.0');
    expect(masterCard.text()).toContain('Kebijakan Biaya Pendidikan TP 2026/2027');
    expect(masterCard.text()).toContain('Tuition (SPP)');
  });

  it('resolves category display name for FP-2026-002 (general_fee) in master list, detail dossier, and filter options', async () => {
    const fp002Policy: FinancialPolicyDto = {
      id: 'pol-3',
      policyCode: 'FP-2026-002',
      name: 'Hagios Institutional Fee Policy 2026/2027',
      category: 'general_fee',
      businessPurpose: 'Institutional and operational fee governance',
      applicableContext: 'All Campuses',
      effectiveDate: '2026-07-01',
      endDate: '2027-06-30',
      policyVersion: '1.0',
      status: 'active',
      approvingAuthority: 'Direktur Keuangan',
      approvedDate: '2026-06-15',
      supersedesPolicyId: null,
      notes: null,
      createdAt: '2026-06-01T00:00:00Z',
      createdBy: 'finance_officer',
      rules: [],
    };
    mockPoliciesList.value = [...mockPoliciesList.value, fp002Policy];
    mockPolicyDetail.value = fp002Policy;

    const wrapper = createWrapper();
    await flushPromises();

    const filterSelect = wrapper.findComponent(Select);
    expect(filterSelect.props('options')).toEqual([
      { label: 'All Categories', value: '' },
      { label: 'Tuition (SPP)', value: 'tuition' },
      { label: 'Admission Fee (Uang Pangkal)', value: 'admission_fee' },
      { label: 'Facilities (Sarana & Prasarana)', value: 'facilities' },
      { label: 'Activities (Kegiatan Siswa)', value: 'activities' },
      { label: 'General Institutional Fee (Biaya Umum)', value: 'general_fee' },
    ]);

    const masterCard = wrapper.find('[data-testid="policies-master-card"]');
    expect(masterCard.text()).toContain('FP-2026-002');
    expect(masterCard.text()).toContain('General Institutional Fee (Biaya Umum)');

    const detailCard = wrapper.find('[data-testid="policy-detail-card"]');
    expect(detailCard.text()).toContain('FP-2026-002');
    expect(detailCard.text()).toContain('General Institutional Fee (Biaya Umum)');
  });

  it('displays selected policy detail dossier and attached charge rules table', () => {
    const wrapper = createWrapper();

    const detailCard = wrapper.find('[data-testid="policy-detail-card"]');
    expect(detailCard.exists()).toBe(true);
    expect(detailCard.text()).toContain('FP-2026-001');
    expect(detailCard.text()).toContain('Kebijakan Biaya Pendidikan TP 2026/2027');
    expect(detailCard.text()).toContain('Menetapkan standar biaya SPP dan uang gedung');

    // Charge rules table
    const rulesSection = wrapper.find('[data-testid="attached-charge-rules-section"]');
    expect(rulesSection.exists()).toBe(true);
    expect(rulesSection.text()).toContain('CR-2026-SPP-SD');
    expect(rulesSection.text()).toContain('SPP Bulanan SD');
    expect(rulesSection.text()).toContain('Rp 750.000');
  });

  it('shows formal approval action button for draft policy when user has APPROVE permission', () => {
    const authStore = useAuthStore();
    authStore.setSession('test-token', {
      userId: 'user-fa',
      username: 'fin_authority',
      fullName: 'Finance Authority Lead',
      roles: ['FinanceAuthority'],
      permissions: [FINANCE_POLICY_PERMISSIONS.APPROVE, FINANCE_POLICY_PERMISSIONS.VIEW],
      campusId: null,
    });

    const wrapper = createWrapper();

    const approveButton = wrapper.find('[data-testid="approve-policy-button"]');
    expect(approveButton.exists()).toBe(true);
    expect(approveButton.text()).toContain('Approve Policy');
  });

  it('hides formal approval action button when user lacks APPROVE permission', () => {
    const authStore = useAuthStore();
    authStore.setSession('test-token', {
      userId: 'user-officer',
      username: 'fin_officer',
      fullName: 'Finance Staff',
      roles: ['FinanceOfficer'],
      permissions: [FINANCE_POLICY_PERMISSIONS.VIEW, FINANCE_POLICY_PERMISSIONS.MANAGE],
      campusId: null,
    });

    const wrapper = createWrapper();

    const approveButton = wrapper.find('[data-testid="approve-policy-button"]');
    expect(approveButton.exists()).toBe(false);
  });

  it('opens approval confirmation dialog and submits formal approval', async () => {
    const authStore = useAuthStore();
    authStore.setSession('test-token', {
      userId: 'user-fa',
      username: 'fin_authority',
      fullName: 'Finance Authority Lead',
      roles: ['FinanceAuthority'],
      permissions: [FINANCE_POLICY_PERMISSIONS.APPROVE, FINANCE_POLICY_PERMISSIONS.VIEW],
      campusId: null,
    });

    const wrapper = createWrapper();

    const approveButton = wrapper.find('[data-testid="approve-policy-button"]');
    await approveButton.trigger('click');
    await flushPromises();

    // Dialog is appended to body by PrimeVue
    const confirmButton = document.querySelector('[data-testid="confirm-approval-button"]');
    expect(confirmButton).not.toBeNull();

    (confirmButton as HTMLButtonElement).click();
    await flushPromises();

    expect(mockApproveMutateAsync).toHaveBeenCalledWith(
      expect.objectContaining({
        id: 'pol-1',
      }),
    );
  });

  it('opens policy creation dialog when [New Policy] button is clicked by authorized user', async () => {
    const authStore = useAuthStore();
    authStore.setSession('test-token', {
      userId: 'user-manager',
      username: 'fin_manager',
      fullName: 'Finance Manager',
      roles: ['FinanceManager'],
      permissions: [FINANCE_POLICY_PERMISSIONS.MANAGE, FINANCE_POLICY_PERMISSIONS.VIEW],
      campusId: null,
    });

    const wrapper = createWrapper();
    const newPolicyBtn = wrapper.find('[data-testid="new-policy-button"]');
    expect(newPolicyBtn.exists()).toBe(true);

    await newPolicyBtn.trigger('click');
    await flushPromises();

    expect(document.querySelector('[data-testid="create-policy-dialog"]')).not.toBeNull();
  });
});

describe('CreatePolicyModal.vue', () => {
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
    }
  });

  let activeWrapper: VueWrapper | null = null;

  afterEach(() => {
    activeWrapper?.unmount();
    activeWrapper = null;
    document.body.innerHTML = '';
  });

  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
    document.body.innerHTML = '';

    mockIsLoadingCategories.value = false;
    mockPolicyCategories.value = [
      {
        code: 'tuition',
        name: 'Tuition (SPP)',
        description: 'Regular academic and instructional fees',
        displayOrder: 1,
        isActive: true,
      },
      {
        code: 'general_fee',
        name: 'General Institutional Fee (Biaya Umum)',
        description: 'Institutional and operational charges including materials and books',
        displayOrder: 5,
        isActive: true,
      },
    ];
  });

  it('renders modal dialog and form fields when visible is true', async () => {
    const wrapper = (activeWrapper = mount(CreatePolicyModal, {
      attachTo: document.body,
      props: {
        visible: true,
      },
      global: {
        plugins: [PrimeVue],
      },
    }));
    await flushPromises();

    expect(document.querySelector('[data-testid="create-policy-dialog"]')).not.toBeNull();
    expect(document.querySelector('[data-testid="policy-code-input"]')).not.toBeNull();
    expect(document.querySelector('[data-testid="policy-name-input"]')).not.toBeNull();
    expect(document.querySelector('[data-testid="policy-category-select"]')).not.toBeNull();
    expect(document.querySelector('[data-testid="policy-purpose-input"]')).not.toBeNull();

    const categorySelect = wrapper.findComponent(Select);
    expect(categorySelect.props('placeholder')).toBe('Select Category');
    expect(categorySelect.props('options')).toEqual([
      { value: 'tuition', label: 'Tuition (SPP)' },
      { value: 'general_fee', label: 'General Institutional Fee (Biaya Umum)' },
    ]);
  });

  it('submits valid policy creation data after explicitly selecting a category', async () => {
    const wrapper = (activeWrapper = mount(CreatePolicyModal, {
      attachTo: document.body,
      props: {
        visible: true,
      },
      global: {
        plugins: [PrimeVue],
      },
    }));
    await flushPromises();

    const codeInput = document.querySelector('[data-testid="policy-code-input"]') as HTMLInputElement;
    const nameInput = document.querySelector('[data-testid="policy-name-input"]') as HTMLInputElement;
    const purposeInput = document.querySelector('[data-testid="policy-purpose-input"]') as HTMLTextAreaElement;

    codeInput.value = 'FP-2026-002';
    codeInput.dispatchEvent(new Event('input'));
    nameInput.value = 'New Draft Policy';
    nameInput.dispatchEvent(new Event('input'));
    purposeInput.value = 'Detailed purpose for fee collection and governance';
    purposeInput.dispatchEvent(new Event('input'));

    const categorySelect = wrapper.findComponent(Select);
    categorySelect.vm.$emit('update:modelValue', 'general_fee');
    await flushPromises();

    const submitBtn = document.querySelector('[data-testid="submit-policy-button"]') as HTMLButtonElement;
    expect(submitBtn).not.toBeNull();
    submitBtn.click();
    await flushPromises();

    expect(mockCreatePolicyMutateAsync).toHaveBeenCalledWith(
      expect.objectContaining({
        policyCode: 'FP-2026-002',
        name: 'New Draft Policy',
        category: 'general_fee',
        businessPurpose: 'Detailed purpose for fee collection and governance',
      }),
    );
    expect(wrapper.emitted('created')).toBeTruthy();
  });

  it('blocks policy creation submission when category is empty', async () => {
    const wrapper = (activeWrapper = mount(CreatePolicyModal, {
      attachTo: document.body,
      props: {
        visible: true,
      },
      global: {
        plugins: [PrimeVue],
      },
    }));
    await flushPromises();

    const codeInput = document.querySelector('[data-testid="policy-code-input"]') as HTMLInputElement;
    const nameInput = document.querySelector('[data-testid="policy-name-input"]') as HTMLInputElement;
    const purposeInput = document.querySelector('[data-testid="policy-purpose-input"]') as HTMLTextAreaElement;

    codeInput.value = 'FP-2026-002';
    codeInput.dispatchEvent(new Event('input'));
    nameInput.value = 'New Draft Policy';
    nameInput.dispatchEvent(new Event('input'));
    purposeInput.value = 'Detailed purpose for fee collection and governance';
    purposeInput.dispatchEvent(new Event('input'));
    await flushPromises();

    const submitBtn = document.querySelector('[data-testid="submit-policy-button"]') as HTMLButtonElement;
    expect(submitBtn).not.toBeNull();
    submitBtn.click();
    await flushPromises();

    expect(mockCreatePolicyMutateAsync).not.toHaveBeenCalled();
    expect(wrapper.emitted('created')).toBeFalsy();
    expect(document.body.textContent).toContain('Category is required.');
  });
});

describe('CreateChargeRuleModal.vue', () => {
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
    }
  });

  let activeWrapper: VueWrapper | null = null;

  afterEach(() => {
    activeWrapper?.unmount();
    activeWrapper = null;
    document.body.innerHTML = '';
  });

  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
    document.body.innerHTML = '';
  });

  it('renders modal dialog and charge rule parameters when visible is true', async () => {
    activeWrapper = mount(CreateChargeRuleModal, {
      attachTo: document.body,
      props: {
        visible: true,
        policyId: 'pol-123',
        policyCode: 'FP-2026-001',
        policyName: 'Kebijakan Biaya Pendidikan',
      },
      global: {
        plugins: [PrimeVue],
      },
    });
    await flushPromises();

    expect(document.querySelector('[data-testid="create-charge-rule-dialog"]')).not.toBeNull();
    expect(document.querySelector('[data-testid="rule-code-input"]')).not.toBeNull();
    expect(document.querySelector('[data-testid="rule-name-input"]')).not.toBeNull();
    expect(document.querySelector('[data-testid="rule-base-amount-input"]')).not.toBeNull();
    expect(document.querySelector('[data-testid="rule-due-value-input"]')).not.toBeNull();
  });
});
