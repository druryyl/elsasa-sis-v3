// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import { VueQueryPlugin, QueryClient } from '@tanstack/vue-query';
import ProgressionRecommendationBadge from '../ProgressionRecommendationBadge.vue';
import type { ProgressionRecommendationDto } from '../../types';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: false },
  },
});

function mountBadge(props: any) {
  return mount(ProgressionRecommendationBadge, {
    props,
    global: {
      plugins: [[VueQueryPlugin, { queryClient }]],
      stubs: {
        Tag: {
          template: '<span class="p-tag" :data-severity="severity"><slot name="icon"></slot>{{ value }}</span>',
          props: ['severity', 'value'],
        },
      },
    },
  });
}

describe('ProgressionRecommendationBadge.vue', () => {
  const promotionEligible: ProgressionRecommendationDto = {
    id: 'rec-1',
    studentId: 'stu-1',
    academicPeriodId: 'period-1',
    recommendation: 'promotion_eligible',
    fromLevel: 'Grade 4',
    recommendedToLevel: 'Grade 5',
    conditions: null,
    supportingEvidence: 'All Grade 4 PACE units completed with score >= 75.00%.',
    academicStandingId: 'standing-1',
    isException: false,
    exceptionReason: null,
    committeeReference: 'SK-AKAD/2026/01',
    authorizedBy: 'Dr. Hendra Wijaya',
    issuedAt: '2026-06-20T10:00:00Z',
  };

  const conditionalPromotion: ProgressionRecommendationDto = {
    id: 'rec-2',
    studentId: 'stu-2',
    academicPeriodId: 'period-1',
    recommendation: 'conditional_promotion',
    fromLevel: 'Grade 4',
    recommendedToLevel: 'Grade 5',
    conditions: 'Must complete holiday remedial packet for Mathematics before Term 1 rollover.',
    supportingEvidence: '1 unit below passing threshold (Math). Attendance 88%.',
    academicStandingId: 'standing-2',
    isException: true,
    exceptionReason: 'Approved conditional promotion under medical compassionate grounds.',
    committeeReference: 'SK-DG/2026/06-01',
    authorizedBy: 'Dewan Guru Committee',
    issuedAt: '2026-06-20T11:00:00Z',
  };

  const retentionRecommended: ProgressionRecommendationDto = {
    id: 'rec-3',
    studentId: 'stu-3',
    academicPeriodId: 'period-1',
    recommendation: 'retention_recommended',
    fromLevel: 'Grade 4',
    recommendedToLevel: null,
    conditions: null,
    supportingEvidence: '3 core units below KKM. Attendance 68% below 75% threshold.',
    academicStandingId: 'standing-3',
    isException: false,
    exceptionReason: null,
    committeeReference: 'SK-DG/2026/RET-02',
    authorizedBy: 'Academic Authority',
    issuedAt: '2026-06-20T12:00:00Z',
  };

  it('renders Promotion Eligible badge (green / success) with destination grade and unblock message', () => {
    const wrapper = mountBadge({
      recommendation: promotionEligible,
      showDetails: true,
    });

    expect(wrapper.find('[data-testid="badge-promotion_eligible"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Promotion Eligible');
    expect(wrapper.find('[data-testid="progression-levels"]').text()).toContain('Grade 4');
    expect(wrapper.find('[data-testid="progression-levels"]').text()).toContain('Grade 5');
    expect(wrapper.find('[data-testid="authorizing-authority"]').text()).toContain('Dr. Hendra Wijaya');
    expect(wrapper.find('[data-testid="decree-reference-badge"]').text()).toContain('SK-AKAD/2026/01');
    expect(wrapper.find('[data-testid="unblock-message"]').text()).toContain('unblocks administrative promotion');
  });

  it('renders Conditional Promotion badge (yellow / warn) with conditions callout and SK Dewan Guru decree', () => {
    const wrapper = mountBadge({
      recommendation: conditionalPromotion,
      showDetails: true,
    });

    expect(wrapper.find('[data-testid="badge-conditional_promotion"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Conditional Promotion');
    expect(wrapper.find('[data-testid="conditional-stipulations-callout"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="conditional-stipulations-callout"]').text()).toContain('Must complete holiday remedial packet');
    expect(wrapper.find('[data-testid="decree-reference-badge"]').text()).toContain('SK-DG/2026/06-01');
    expect(wrapper.find('[data-testid="exception-override-banner"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="exception-override-banner"]').text()).toContain('medical compassionate grounds');
    expect(wrapper.find('[data-testid="unblock-message"]').text()).toContain('unblocks conditional promotion');
  });

  it('renders Retention Recommended badge (red / danger) with retention unblock guidance', () => {
    const wrapper = mountBadge({
      recommendation: retentionRecommended,
      showDetails: true,
    });

    expect(wrapper.find('[data-testid="badge-retention_recommended"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Retention Recommended');
    expect(wrapper.find('[data-testid="progression-levels"]').text()).toContain('Retain');
    expect(wrapper.find('[data-testid="decree-reference-badge"]').text()).toContain('SK-DG/2026/RET-02');
    expect(wrapper.find('[data-testid="unblock-message"]').text()).toContain('Retention in current level (Grade 4) recommended');
  });

  it('renders compact badge mode for roster and tabular display', () => {
    const wrapper = mountBadge({
      recommendation: conditionalPromotion,
      compact: true,
    });

    expect(wrapper.find('[data-testid="progression-badge-compact"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="badge-conditional_promotion"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="decree-reference-tag"]').text()).toContain('SK-DG/2026/06-01');
    expect(wrapper.find('[data-testid="progression-recommendation-card"]').exists()).toBe(false);
  });

  it('renders neutral state when no recommendation is recorded', () => {
    const wrapper = mountBadge({
      recommendation: null,
    });

    expect(wrapper.find('[data-testid="badge-none"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('No Recommendation Recorded');
    expect(wrapper.find('[data-testid="unblock-message"]').text()).toContain('No academic progression recommendation');
  });
});
