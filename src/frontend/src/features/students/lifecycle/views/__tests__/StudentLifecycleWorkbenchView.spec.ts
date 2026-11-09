// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount } from '@vue/test-utils';
import { ref } from 'vue';
import StudentLifecycleWorkbenchView from '../StudentLifecycleWorkbenchView.vue';

// Mock TanStack query hook
vi.mock('@/features/students/records/composables/useStudentRecordsQueries', () => ({
  useStudentSearchQuery: vi.fn(() => ({
    data: ref({
      items: [
        {
          id: 'stu-1',
          studentNumber: 'HAG-2026-001',
          fullName: 'Rina Kusuma',
          grade: 'Grade 4',
          level: 'Primary',
          campus: 'Hagios Main Campus',
          status: 'Active',
        },
        {
          id: 'stu-2',
          studentNumber: 'HAG-2026-002',
          fullName: 'Budi Santoso',
          grade: 'Grade 4',
          level: 'Primary',
          campus: 'Hagios Main Campus',
          status: 'Active',
        },
      ],
      totalCount: 2,
    }),
    isLoading: ref(false),
    refetch: vi.fn(),
  })),
}));

// Mock lifecycle recommendation composable
vi.mock('../../composables/useProgressionRecommendation', () => ({
  useStudentProgressionRecommendation: vi.fn((studentIdRef) => ({
    data: ref([]),
    latestRecommendation: ref({
      id: 'rec-1',
      studentId: 'stu-1',
      academicPeriodId: 'p-1',
      recommendation: 'promotion_eligible',
      fromLevel: 'Grade 4',
      recommendedToLevel: 'Grade 5',
      conditions: null,
      supportingEvidence: 'All units passed',
      isException: false,
      committeeReference: 'SK-AKAD/2026/01',
      authorizedBy: 'Academic Authority',
      issuedAt: '2026-06-20T10:00:00Z',
    }),
    isLoading: ref(false),
  })),
}));

describe('StudentLifecycleWorkbenchView.vue', () => {
  it('mounts workbench and renders student list with progression badge', () => {
    const wrapper = mount(StudentLifecycleWorkbenchView, {
      global: {
        stubs: {
          PageHeader: {
            template: '<header><h1>{{ title }}</h1><p>{{ subtitle }}</p><slot name="actions"></slot></header>',
            props: ['title', 'subtitle'],
          },
          DataTable: {
            template: '<div class="p-datatable"><slot></slot></div>',
            props: ['value'],
          },
          Column: {
            template: '<div class="p-column"><slot name="body" :data="{ id: \'stu-1\', fullName: \'Rina Kusuma\', studentNumber: \'HAG-2026-001\', grade: \'Grade 4\', campus: \'Hagios Main Campus\' }"></slot></div>',
            props: ['header'],
          },
          Button: {
            template: '<button><slot></slot>{{ label }}</button>',
            props: ['label'],
          },
          InputText: true,
          Select: true,
          Tag: true,
          EmptyState: true,
          StudentPlacementDrawer: true,
        },
      },
    });

    expect(wrapper.find('[data-testid="student-lifecycle-workbench-view"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Student Lifecycle Workbench');
    expect(wrapper.text()).toContain('SCR-STU-03');
    expect(wrapper.find('[data-testid="input-search-student"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="btn-open-rollover-drawer"]').exists()).toBe(true);
  });
});
