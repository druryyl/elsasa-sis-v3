// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach, beforeAll } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import PrimeVue from 'primevue/config';
import ToastService from 'primevue/toastservice';
import { ref } from 'vue';
import StandingCorrectionModal from '../components/StandingCorrectionModal.vue';
import * as cohortReviewComposables from '../composables/useCohortReview';
import type { StandingItem, ProgressionItem } from '../types';

vi.mock('primevue/usetoast', () => ({
  useToast: () => ({ add: vi.fn() }),
}));

// PrimeVue Dialog stub
const DialogStub = {
  props: ['visible', 'header', 'modal', 'draggable', 'dataTestid'],
  emits: ['update:visible'],
  template: `
    <div v-if="visible" class="p-dialog" :data-testid="dataTestid || 'standing-correction-modal'">
      <div class="p-dialog-header"><slot name="header" /></div>
      <div class="p-dialog-content"><slot /></div>
      <div class="p-dialog-footer"><slot name="footer" /></div>
    </div>
  `,
};

describe('StandingCorrectionModal.vue (UC-ACD-15, P8-S22, P9-S27)', () => {
  let mockMutateCorrectStanding: ReturnType<typeof vi.fn>;
  let mockMutateCorrectProgression: ReturnType<typeof vi.fn>;

  const mockStandingResult: StandingItem = {
    id: 'std-123',
    studentId: 'stu-budi',
    studentName: 'Budi Santoso',
    studentNumber: 'STD-4002',
    gradeLevel: 'Grade 4',
    academicPeriodId: '2026-ganjil',
    standing: 'good_standing',
    policyReference: 'SK-AKAD/2026/POLICY-01',
    conditions: null,
    isException: false,
    exceptionReason: null,
    committeeReference: null,
    authorizedBy: 'Academic Authority',
    determinedAt: '2026-11-07T12:00:00Z',
    createdAt: '2026-11-07T12:00:00Z',
    updatedAt: '2026-11-07T12:00:00Z',
  };

  const mockProgressionResult: ProgressionItem = {
    id: 'rec-456',
    studentId: 'stu-budi',
    studentName: 'Budi Santoso',
    studentNumber: 'STD-4002',
    gradeLevel: 'Grade 4',
    academicPeriodId: '2026-ganjil',
    recommendation: 'promotion_eligible',
    fromLevel: 'Grade 4',
    recommendedToLevel: 'Grade 5',
    conditions: null,
    supportingEvidence: 'Completed summer remediation coursework',
    isException: false,
    exceptionReason: null,
    committeeReference: null,
    authorizedBy: 'Academic Authority',
    issuedAt: '2026-11-07T12:00:00Z',
    createdAt: '2026-11-07T12:00:00Z',
    updatedAt: '2026-11-07T12:00:00Z',
  };

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
    vi.clearAllMocks();

    mockMutateCorrectStanding = vi.fn();
    mockMutateCorrectProgression = vi.fn();

    vi.spyOn(cohortReviewComposables, 'useCorrectStandingMutation').mockImplementation((options) => {
      return {
        mutate: vi.fn((vars) => {
          mockMutateCorrectStanding(vars);
          options?.onSuccess?.(mockStandingResult, vars);
        }),
        isPending: ref(false),
      } as any;
    });

    vi.spyOn(cohortReviewComposables, 'useCorrectProgressionMutation').mockImplementation((options) => {
      return {
        mutate: vi.fn((vars) => {
          mockMutateCorrectProgression(vars);
          options?.onSuccess?.(mockProgressionResult, vars);
        }),
        isPending: ref(false),
      } as any;
    });
  });

  function createWrapper(props = {}) {
    return mount(StandingCorrectionModal, {
      props: {
        visible: true,
        targetType: 'standing',
        targetId: 'std-123',
        studentName: 'Budi Santoso',
        studentIdentifier: 'STD-4002',
        initialStanding: 'conditional_standing',
        initialPolicyReference: 'SK-AKAD/2026/POLICY-01',
        initialConditions: 'Complete math tutoring',
        ...props,
      },
      global: {
        plugins: [PrimeVue, ToastService],
        stubs: {
          Dialog: DialogStub,
          Select: {
            props: ['modelValue', 'options', 'optionLabel', 'optionValue'],
            emits: ['update:modelValue'],
            template: `
              <select
                :value="modelValue"
                @change="$emit('update:modelValue', $event.target.value)"
                class="p-select"
                :data-testid="$attrs['data-testid']"
              >
                <option v-for="opt in options" :key="optionValue ? opt[optionValue] : opt.value || opt" :value="optionValue ? opt[optionValue] : opt.value || opt">
                  {{ optionLabel ? opt[optionLabel] : opt.label || opt }}
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
                :data-testid="$attrs['data-testid']"
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
                :data-testid="$attrs['data-testid']"
              />
            `,
          },
          Checkbox: {
            props: ['modelValue', 'binary', 'inputId'],
            emits: ['update:modelValue'],
            template: `
              <input
                type="checkbox"
                :id="inputId"
                :checked="modelValue"
                @change="$emit('update:modelValue', $event.target.checked)"
                :data-testid="$attrs['data-testid']"
              />
            `,
          },
          Button: {
            props: ['label', 'disabled', 'loading'],
            template: `
              <button :disabled="disabled || loading" class="p-button">
                <slot>{{ label }}</slot>
              </button>
            `,
          },
        },
      },
    });
  }

  describe('Standing Correction Mode', () => {
    it('renders student summary and initial standing values', async () => {
      const wrapper = createWrapper();
      await flushPromises();

      expect(wrapper.find('[data-testid="standing-correction-modal"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('Budi Santoso');
      expect(wrapper.text()).toContain('STD-4002');
      expect(wrapper.text()).toContain('Standing Determination');
      expect(wrapper.find('[data-testid="select-correction-standing"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="input-correction-policy-ref"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="textarea-correction-conditions"]').exists()).toBe(true);
    });

    it('validates mandatory reason minimum length of 10 characters', async () => {
      const wrapper = createWrapper();
      await flushPromises();

      // Submit with empty reason
      const submitBtn = wrapper.find('[data-testid="btn-submit-correction"]');
      await submitBtn.trigger('click');
      await flushPromises();

      expect(mockMutateCorrectStanding).not.toHaveBeenCalled();
      expect(wrapper.text()).toContain('Correction reason must be at least 10 characters for audit compliance.');

      // Submit with reason too short (< 10 chars)
      const reasonInput = wrapper.find('[data-testid="textarea-correction-reason"]');
      await reasonInput.setValue('too short');
      await submitBtn.trigger('click');
      await flushPromises();

      expect(mockMutateCorrectStanding).not.toHaveBeenCalled();
      expect(wrapper.text()).toContain('Correction reason must be at least 10 characters for audit compliance.');
    });

    it('captures exception decree fields when marked as exception and submits correction', async () => {
      const wrapper = createWrapper();
      await flushPromises();

      // Check exception checkbox
      const chkException = wrapper.find('[data-testid="checkbox-correction-exception"]');
      await chkException.setValue(true);
      await flushPromises();

      expect(wrapper.find('[data-testid="input-correction-committee-ref"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="input-correction-exception-reason"]').exists()).toBe(true);

      // Enter decree reference and exception reason
      await wrapper.find('[data-testid="input-correction-committee-ref"]').setValue('SK-DG/2026/06-01');
      await wrapper.find('[data-testid="input-correction-exception-reason"]').setValue('Council committee granted exception');

      // Fill valid audit reason
      const reasonInput = wrapper.find('[data-testid="textarea-correction-reason"]');
      await reasonInput.setValue('Re-evaluation of student academic standing following council decree approval.');

      const submitBtn = wrapper.find('[data-testid="btn-submit-correction"]');
      await submitBtn.trigger('click');
      await flushPromises();

      expect(mockMutateCorrectStanding).toHaveBeenCalledWith({
        id: 'std-123',
        request: expect.objectContaining({
          isException: true,
          committeeReference: 'SK-DG/2026/06-01',
          exceptionReason: 'Council committee granted exception',
          reason: 'Re-evaluation of student academic standing following council decree approval.',
        }),
      });

      expect(wrapper.emitted('corrected')).toBeTruthy();
      expect(wrapper.emitted('update:visible')![0][0]).toBe(false);
    });

    it('submits valid standing correction and emits corrected event', async () => {
      const wrapper = createWrapper();
      await flushPromises();

      // Change standing to good_standing
      const standingSelect = wrapper.find('[data-testid="select-correction-standing"]');
      await standingSelect.setValue('good_standing');

      // Clear conditions
      const conditionsInput = wrapper.find('[data-testid="textarea-correction-conditions"]');
      await conditionsInput.setValue('');

      // Fill valid audit reason
      const reasonInput = wrapper.find('[data-testid="textarea-correction-reason"]');
      await reasonInput.setValue('Student successfully remediated math modules and achieved passing mastery.');

      const submitBtn = wrapper.find('[data-testid="btn-submit-correction"]');
      await submitBtn.trigger('click');
      await flushPromises();

      expect(mockMutateCorrectStanding).toHaveBeenCalledWith({
        id: 'std-123',
        request: expect.objectContaining({
          standing: 'good_standing',
          policyReference: 'SK-AKAD/2026/POLICY-01',
          conditions: null,
          isException: false,
          reason: 'Student successfully remediated math modules and achieved passing mastery.',
        }),
      });

      expect(wrapper.emitted('corrected')).toBeTruthy();
      expect(wrapper.emitted('corrected')![0][0]).toEqual(mockStandingResult);
      expect(wrapper.emitted('update:visible')![0][0]).toBe(false);
    });
  });

  describe('Progression Recommendation Correction Mode', () => {
    it('renders progression fields and submits valid progression correction', async () => {
      const wrapper = createWrapper({
        targetType: 'progression',
        targetId: 'rec-456',
        initialRecommendation: 'conditional_promotion',
        initialFromLevel: 'Grade 4',
        initialRecommendedToLevel: 'Grade 5',
        initialSupportingEvidence: 'Pending summer coursework',
      });
      await flushPromises();

      expect(wrapper.text()).toContain('Progression Recommendation');
      expect(wrapper.find('[data-testid="select-correction-recommendation"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="input-correction-target-level"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="textarea-correction-evidence"]').exists()).toBe(true);

      // Change recommendation to promotion_eligible
      const recSelect = wrapper.find('[data-testid="select-correction-recommendation"]');
      await recSelect.setValue('promotion_eligible');

      const targetLevelInput = wrapper.find('[data-testid="input-correction-target-level"]');
      await targetLevelInput.setValue('Grade 5');

      const evidenceInput = wrapper.find('[data-testid="textarea-correction-evidence"]');
      await evidenceInput.setValue('Summer coursework completed with 85% score');

      // Attempt submit without reason
      const submitBtn = wrapper.find('[data-testid="btn-submit-correction"]');
      await submitBtn.trigger('click');
      await flushPromises();

      expect(mockMutateCorrectProgression).not.toHaveBeenCalled();
      expect(wrapper.text()).toContain('Correction reason must be at least 10 characters for audit compliance.');

      // Provide valid audit reason
      const reasonInput = wrapper.find('[data-testid="textarea-correction-reason"]');
      await reasonInput.setValue('Correction after verification of summer school completion certificate.');

      await submitBtn.trigger('click');
      await flushPromises();

      expect(mockMutateCorrectProgression).toHaveBeenCalledWith({
        id: 'rec-456',
        request: expect.objectContaining({
          recommendation: 'promotion_eligible',
          fromLevel: 'Grade 4',
          recommendedToLevel: 'Grade 5',
          supportingEvidence: 'Summer coursework completed with 85% score',
          reason: 'Correction after verification of summer school completion certificate.',
        }),
      });

      expect(wrapper.emitted('corrected')).toBeTruthy();
      expect(wrapper.emitted('corrected')![0][0]).toEqual(mockProgressionResult);
      expect(wrapper.emitted('update:visible')![0][0]).toBe(false);
    });
  });
});
