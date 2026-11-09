// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, beforeAll } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import { ref } from 'vue';
import { useAuthStore } from '@/app/stores/auth.store';
import LearningProgressTab from '../LearningProgressTab.vue';
import * as learningProgressComposables from '../../composables/useLearningProgress';
import * as curriculumComposables from '@/features/academics/curriculum/composables/useCurriculum';
import type { LearningProgress } from '../../types';

vi.mock('primevue/usetoast', () => ({
  useToast: () => ({ add: vi.fn() }),
}));

// Dialog stub for test rendering
const DialogStub = {
  props: ['visible', 'header', 'style', 'modal'],
  emits: ['update:visible'],
  template: `
    <div v-if="visible" class="p-dialog" :data-testid="$attrs['data-testid'] || 'p-dialog'">
      <div class="p-dialog-header">{{ header }}</div>
      <div class="p-dialog-content"><slot /></div>
      <div class="p-dialog-footer"><slot name="footer" /></div>
    </div>
  `,
};

const mockLearningUnits = [
  { id: 'unit-math-04', code: 'MATH-04', title: 'Grade 4 Mathematics', unitType: 'subject', level: 'Grade 4', creditWeight: 4 },
  { id: 'unit-pakd-04', code: 'PAKD-04', title: 'Grade 4 Christian Education', unitType: 'subject', level: 'Grade 4', creditWeight: 2 },
];

const mockRoster: LearningProgress[] = [
  {
    id: 'prog-1',
    progressId: 'prog-1',
    studentId: 'stu-rina',
    studentName: 'Rina Kusuma',
    studentNumber: 'STD-4001',
    learningUnitId: 'unit-math-04',
    learningUnitCode: 'MATH-04',
    learningUnitName: 'Grade 4 Mathematics',
    academicPeriodId: '2026-ganjil',
    participationId: 'part-rina',
    participationStatus: 'in_progress',
    sectionLabel: '4A',
    status: 'completed',
    progressStatus: 'completed',
    basis: 'assessment_evidence',
    operativeScore: 92.5,
    gradeLetter: 'A',
    completionDate: '2026-11-05',
    notes: 'Mastery achieved on all topics',
  },
  {
    id: 'prog-2',
    progressId: 'prog-2',
    studentId: 'stu-budi',
    studentName: 'Budi Santoso',
    studentNumber: 'STD-4002',
    learningUnitId: 'unit-math-04',
    learningUnitCode: 'MATH-04',
    learningUnitName: 'Grade 4 Mathematics',
    academicPeriodId: '2026-ganjil',
    participationId: 'part-budi',
    participationStatus: 'in_progress',
    sectionLabel: '4A',
    status: 'needs_remediation',
    progressStatus: 'needs_remediation',
    basis: 'learning_work',
    operativeScore: 58.0,
    gradeLetter: 'D',
    completionDate: null,
    notes: 'Math tutoring intervention assigned',
  },
  {
    id: 'prog-3',
    progressId: 'prog-3',
    studentId: 'stu-timotius',
    studentName: 'Timotius',
    studentNumber: 'STD-4003',
    learningUnitId: 'unit-math-04',
    learningUnitCode: 'MATH-04',
    learningUnitName: 'Grade 4 Mathematics',
    academicPeriodId: '2026-ganjil',
    participationId: 'part-timotius',
    participationStatus: 'in_progress',
    sectionLabel: '4B',
    status: 'exempted',
    progressStatus: 'exempted',
    basis: 'authorized_decision',
    operativeScore: null,
    gradeLetter: null,
    completionDate: '2026-08-15',
    notes: 'Transferred credit recognized per SK-DG/2026/06-01',
  },
];

describe('LearningProgressTab.vue (SCR-ACD-07, UC-ACD-12)', () => {
  let queryClient: QueryClient;
  let authStore: ReturnType<typeof useAuthStore>;
  const mockMutateRecord = vi.fn().mockResolvedValue({ id: 'prog-new', progressStatus: 'completed' });
  const mockMutateConclude = vi.fn().mockResolvedValue({
    participationId: 'part-rina',
    progressId: 'prog-1',
    studentId: 'stu-rina',
    learningUnitId: 'unit-math-04',
    academicPeriodId: '2026-ganjil',
    participationStatus: 'completed',
    progressStatus: 'completed',
    completionDate: '2026-11-07',
    concludedAt: '2026-11-07T12:00:00Z',
    concludedBy: 'coordinator',
  });

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

  beforeEach(() => {
    setActivePinia(createPinia());
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
        mutations: { retry: false },
      },
    });

    vi.clearAllMocks();

    authStore = useAuthStore();
    authStore.setSession('mock-token', {
      id: 'coord-1',
      username: 'coordinator',
      email: 'coordinator@school.test',
      fullName: 'Academic Coordinator User',
      isActive: true,
      roles: ['AcademicCoordinator'],
      permissions: [
        'academics.progress.view',
        'academics.progress.record',
        'academics.progress.conclude',
      ],
      campusId: null,
    });

    vi.spyOn(curriculumComposables, 'useLearningUnits').mockReturnValue({
      learningUnits: ref(mockLearningUnits),
      isLoading: ref(false),
      isError: ref(false),
      error: ref(null),
      refetch: vi.fn(),
    } as any);

    vi.spyOn(learningProgressComposables, 'useUnitProgressQuery').mockReturnValue({
      data: ref(mockRoster),
      isLoading: ref(false),
      isError: ref(false),
      error: ref(null),
      refetch: vi.fn(),
    } as any);

    vi.spyOn(learningProgressComposables, 'useRecordProgressMutation').mockReturnValue({
      mutateAsync: mockMutateRecord,
      isPending: ref(false),
    } as any);

    vi.spyOn(learningProgressComposables, 'useConcludeParticipationMutation').mockReturnValue({
      mutateAsync: mockMutateConclude,
      isPending: ref(false),
    } as any);
  });

  function createWrapper(props = {}) {
    return mount(LearningProgressTab, {
      props: {
        defaultPeriodId: '2026-ganjil',
        ...props,
      },
      global: {
        plugins: [PrimeVue, ToastService],
        stubs: {
          Dialog: DialogStub,
          Select: {
            props: ['modelValue', 'options', 'optionLabel', 'optionValue', 'placeholder'],
            emits: ['update:modelValue'],
            template: `
              <select
                :value="modelValue"
                @change="$emit('update:modelValue', $event.target.value)"
                class="p-select"
              >
                <option v-for="opt in options" :key="optionValue ? opt[optionValue] : opt" :value="optionValue ? opt[optionValue] : opt">
                  {{ optionLabel ? opt[optionLabel] : opt }}
                </option>
              </select>
            `,
          },
          InputText: {
            props: ['modelValue', 'type', 'placeholder'],
            emits: ['update:modelValue'],
            template: `
              <input
                :type="type || 'text'"
                :value="modelValue"
                :placeholder="placeholder"
                @input="$emit('update:modelValue', $event.target.value)"
                class="p-inputtext"
              />
            `,
          },
          Textarea: {
            props: ['modelValue', 'rows', 'placeholder'],
            emits: ['update:modelValue'],
            template: `
              <textarea
                :value="modelValue"
                :rows="rows"
                :placeholder="placeholder"
                @input="$emit('update:modelValue', $event.target.value)"
                class="p-textarea"
              />
            `,
          },
          Button: {
            props: ['label', 'disabled', 'loading'],
            template: `
              <button :disabled="disabled || loading" class="p-button">
                <slot />
              </button>
            `,
          },
        },
      },
    });
  }

  it('renders student roster by learning unit and period', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    expect(wrapper.find('[data-testid="learning-progress-table"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Rina Kusuma');
    expect(wrapper.text()).toContain('Budi Santoso');
    expect(wrapper.text()).toContain('Timotius');
  });

  it('displays Phase-2 minimal progress badges and operational basis tags', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    // Rina Kusuma: completed, assessment_evidence
    expect(wrapper.find('[data-testid="badge-status-stu-rina"]').text()).toContain('Completed');
    expect(wrapper.find('[data-testid="tag-basis-stu-rina"]').text()).toContain('Assessment Evidence');

    // Budi Santoso: needs_remediation, learning_work
    expect(wrapper.find('[data-testid="badge-status-stu-budi"]').text()).toContain('Needs Remediation');
    expect(wrapper.find('[data-testid="tag-basis-stu-budi"]').text()).toContain('Learning Work');

    // Timotius: exempted, authorized_decision
    expect(wrapper.find('[data-testid="badge-status-stu-timotius"]').text()).toContain('Exempted');
    expect(wrapper.find('[data-testid="tag-basis-stu-timotius"]').text()).toContain('Authorized Decision');
  });

  it('supports explicit out-of-period completion date stamping', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const dateInput = wrapper.find('[data-testid="input-completion-date-stu-budi"]');
    expect(dateInput.exists()).toBe(true);

    // Coordinator stamps an explicit out-of-period completion date (e.g. for post-term remediation or PACE module)
    await dateInput.setValue('2026-11-20');
    await dateInput.trigger('change');
    await flushPromises();

    expect(mockMutateRecord).toHaveBeenCalledWith(
      expect.objectContaining({
        studentId: 'stu-budi',
        completionDate: '2026-11-20',
        status: 'completed',
      }),
    );
  });

  it('opens confirmation dialog verifying passing criteria when Conclude Participation is clicked (UC-ACD-12)', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const concludeBtn = wrapper.find('[data-testid="btn-conclude-stu-budi"]');
    expect(concludeBtn.exists()).toBe(true);

    await concludeBtn.trigger('click');
    await flushPromises();

    const dialog = wrapper.find('[data-testid="dialog-conclude-participation"]');
    expect(dialog.exists()).toBe(true);
    expect(dialog.text()).toContain('Passing Criteria Verified');
    expect(dialog.find('[data-testid="input-conclude-completion-date"]').exists()).toBe(true);

    // Confirm conclusion
    const confirmBtn = wrapper.find('[data-testid="btn-confirm-conclude"]');
    await confirmBtn.trigger('click');
    await flushPromises();

    expect(mockMutateConclude).toHaveBeenCalledWith(
      expect.objectContaining({
        studentId: 'stu-budi',
        participationId: 'part-budi',
      }),
    );
    expect(wrapper.emitted('participationConcluded')).toBeTruthy();
  });

  it('opens Record Prior Learning dialog and submits with authorized_decision basis', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    const rplBtn = wrapper.find('[data-testid="btn-record-prior-learning"]');
    expect(rplBtn.exists()).toBe(true);

    await rplBtn.trigger('click');
    await flushPromises();

    const dialog = wrapper.find('[data-testid="dialog-record-prior-learning"]');
    expect(dialog.exists()).toBe(true);
    expect(dialog.text()).toContain('Institutional Recognition (Authorized Decision)');

    // Fill RPL form
    await wrapper.find('[data-testid="input-rpl-student-id"]').setValue('stu-transfer-1');
    await wrapper.find('[data-testid="input-rpl-notes"]').setValue('Transferred credits from Cambridge secondary');

    const submitBtn = wrapper.find('[data-testid="btn-confirm-rpl"]');
    await submitBtn.trigger('click');
    await flushPromises();

    expect(mockMutateRecord).toHaveBeenCalledWith(
      expect.objectContaining({
        studentId: 'stu-transfer-1',
        basis: 'authorized_decision',
        notes: 'Transferred credits from Cambridge secondary',
      }),
    );
    expect(wrapper.emitted('progressRecorded')).toBeTruthy();
  });
});
