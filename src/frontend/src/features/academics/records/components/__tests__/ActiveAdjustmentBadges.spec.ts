// @vitest-environment jsdom
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import PrimeVue from 'primevue/config';
import Tooltip from 'primevue/tooltip';
import ActiveAdjustmentBadges from '../ActiveAdjustmentBadges.vue';
import * as learningPlansApi from '@/features/academics/learning-plans/api/learningPlansApi';
import type { AcademicPlanAdjustment } from '@/features/academics/learning-plans/types';

describe('ActiveAdjustmentBadges.vue (SCR-ACD-07, P7-S24)', () => {
  let queryClient: QueryClient;

  const mockAdjustments: AcademicPlanAdjustment[] = [
    {
      id: 'adj-1',
      adjustmentType: 'accommodation',
      description: '50% Extra Exam Time',
      rationale: 'Extended time required for written assessments due to IEP assessment',
      effectiveDate: '2026-08-01',
      endDate: '2027-06-30',
      approvedBy: 'Ibu Ratna (VP Academics)',
      approvedDate: '2026-08-05',
    },
    {
      id: 'adj-2',
      adjustmentType: 'substitution',
      description: 'Adaptive PJOK',
      rationale: 'Physical activity adapted per medical practitioner recommendation',
      effectiveDate: '2026-08-01',
      endDate: null,
      approvedBy: 'Dr. Hendra (Curriculum Lead)',
      approvedDate: '2026-08-06',
    },
    {
      id: 'adj-3',
      adjustmentType: 'exemption',
      description: 'Foreign Language Elective',
      rationale: 'Exempted based on external certification',
      effectiveDate: '2026-08-01',
      endDate: null,
      approvedBy: 'Ibu Ratna (VP Academics)',
      approvedDate: '2026-08-05',
    },
  ];

  beforeEach(() => {
    setActivePinia(createPinia());
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
      },
    });
    vi.clearAllMocks();
  });

  function createWrapper(props = {}) {
    return mount(ActiveAdjustmentBadges, {
      props,
      global: {
        plugins: [
          PrimeVue,
          [VueQueryPlugin, { queryClient }],
        ],
        directives: {
          tooltip: Tooltip,
        },
      },
    });
  }

  it('renders badges when adjustments are passed via props', () => {
    const wrapper = createWrapper({
      adjustments: mockAdjustments,
    });

    const root = wrapper.find('[data-testid="student-active-adjustments"]');
    expect(root.exists()).toBe(true);

    const badges = wrapper.findAll('[data-testid^="badge-adjustment-"]');
    expect(badges).toHaveLength(3);

    // Verify accommodation badge
    const accommodationBadge = wrapper.find('[data-testid="badge-adjustment-adj-1"]');
    expect(accommodationBadge.exists()).toBe(true);
    expect(accommodationBadge.text()).toContain('⚡ Accommodation: 50% Extra Exam Time');
    expect(accommodationBadge.attributes('data-adjustment-type')).toBe('accommodation');

    // Verify substitution badge
    const substitutionBadge = wrapper.find('[data-testid="badge-adjustment-adj-2"]');
    expect(substitutionBadge.exists()).toBe(true);
    expect(substitutionBadge.text()).toContain('🔄 Substitution: Adaptive PJOK');
    expect(substitutionBadge.attributes('data-adjustment-type')).toBe('substitution');

    // Verify exemption badge
    const exemptionBadge = wrapper.find('[data-testid="badge-adjustment-adj-3"]');
    expect(exemptionBadge.exists()).toBe(true);
    expect(exemptionBadge.text()).toContain('🛡️ Exemption: Foreign Language Elective');
    expect(exemptionBadge.attributes('data-adjustment-type')).toBe('exemption');
  });

  it('includes rationale and approving authority in hover tooltip (title attribute)', () => {
    const wrapper = createWrapper({
      adjustments: mockAdjustments,
    });

    const accommodationBadge = wrapper.find('[data-testid="badge-adjustment-adj-1"]');
    const titleAttr = accommodationBadge.attributes('title');

    expect(titleAttr).toBeDefined();
    expect(titleAttr).toContain('Rationale: Extended time required for written assessments due to IEP assessment');
    expect(titleAttr).toContain('Approving Authority: Ibu Ratna (VP Academics)');
  });

  it('queries active learning plan when studentId is passed without adjustments', async () => {
    const planRes = {
      planId: 'plan-budi',
      studentId: 'student-budi',
      academicPeriodId: 'period-1',
      academicPeriodLabel: '2026/2027 Odd',
      version: 2,
      status: 'active' as const,
      constituentUnits: [],
      adjustments: [mockAdjustments[0]],
    };
    vi.spyOn(learningPlansApi, 'getActiveLearningPlan').mockResolvedValue(planRes);
    vi.spyOn(learningPlansApi.learningPlansApi, 'getActiveLearningPlan').mockResolvedValue(planRes);

    const wrapper = createWrapper({
      studentId: 'student-budi',
      academicPeriodId: 'period-1',
    });

    await flushPromises();

    const accommodationBadge = wrapper.find('[data-testid="badge-adjustment-adj-1"]');
    expect(accommodationBadge.exists()).toBe(true);
    expect(accommodationBadge.text()).toContain('⚡ Accommodation: 50% Extra Exam Time');
  });

  it('renders nothing when there are no active adjustments', async () => {
    vi.spyOn(learningPlansApi, 'getActiveLearningPlan').mockResolvedValue(null);
    vi.spyOn(learningPlansApi.learningPlansApi, 'getActiveLearningPlan').mockResolvedValue(null);

    const wrapper = createWrapper({
      studentId: 'student-regular',
      academicPeriodId: 'period-1',
    });

    await flushPromises();

    expect(wrapper.find('[data-testid="student-active-adjustments"]').exists()).toBe(false);
  });
});
