// @vitest-environment jsdom
import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import PrimeVue from 'primevue/config';
import CalendarWorkspaceView from '../CalendarWorkspaceView.vue';
import * as academicYearApi from '../../api/academicYear.api';
import { ApiError } from '@/shared/types/common';

const mockToastAdd = vi.fn();

vi.mock('primevue/usetoast', () => ({
  useToast: () => ({
    add: mockToastAdd,
  }),
}));

describe('CalendarWorkspaceView.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockToastAdd.mockReset();

    // Default healthy mock
    vi.spyOn(academicYearApi, 'checkBackendHealth').mockResolvedValue({
      status: 'Healthy',
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  function createWrapper() {
    return mount(CalendarWorkspaceView, {
      global: {
        plugins: [PrimeVue],
        stubs: {
          RouterLink: {
            template: '<a><slot /></a>',
          },
        },
      },
    });
  }

  async function submitForm(wrapper: ReturnType<typeof createWrapper>) {
    await wrapper.find('form[data-testid="create-academic-year-form"]').trigger('submit');
    await flushPromises();
    await new Promise((resolve) => setTimeout(resolve, 50));
    await flushPromises();
  }

  it('renders workspace view, header, and child cards', async () => {
    const wrapper = createWrapper();
    await flushPromises();

    expect(wrapper.find('[data-testid="calendar-workspace-view"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="page-header"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="backend-health-card"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="create-academic-year-card"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="created-records-card"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="empty-records-state"]').exists()).toBe(true);
  });

  describe('Backend Health Connectivity Probe', () => {
    it('checks health on mount and displays Healthy status pill', async () => {
      const wrapper = createWrapper();
      await flushPromises();

      expect(academicYearApi.checkBackendHealth).toHaveBeenCalledTimes(1);

      const statusPill = wrapper.find('[data-testid="backend-health-status"]');
      expect(statusPill.exists()).toBe(true);
      expect(statusPill.text()).toContain('Healthy (HTTP 200)');
    });

    it('handles health check error gracefully and displays Unreachable pill', async () => {
      vi.spyOn(academicYearApi, 'checkBackendHealth').mockRejectedValueOnce(
        new Error('Connection refused'),
      );

      const wrapper = createWrapper();
      await flushPromises();

      const statusPill = wrapper.find('[data-testid="backend-health-status"]');
      expect(statusPill.exists()).toBe(true);
      expect(statusPill.text()).toContain('Unreachable / Error');
    });

    it('refreshes health status on button click', async () => {
      const wrapper = createWrapper();
      await flushPromises();

      expect(academicYearApi.checkBackendHealth).toHaveBeenCalledTimes(1);

      const refreshBtn = wrapper.find('[data-testid="refresh-health-btn"]');
      await refreshBtn.trigger('click');
      await flushPromises();

      expect(academicYearApi.checkBackendHealth).toHaveBeenCalledTimes(2);
    });
  });

  describe('Form Validation & Submission', () => {
    it('displays client-side validation errors when submitting empty form without calling API', async () => {
      const createSpy = vi.spyOn(academicYearApi, 'createAcademicYear');
      const wrapper = createWrapper();
      await flushPromises();

      await submitForm(wrapper);

      expect(createSpy).not.toHaveBeenCalled();

      const alerts = wrapper.findAll('[role="alert"]');
      expect(alerts.length).toBeGreaterThanOrEqual(1);

      const alertTexts = alerts.map((a) => a.text());
      expect(alertTexts.some((t) => t.includes('Code is required.'))).toBe(true);
      expect(alertTexts.some((t) => t.includes('Name is required.'))).toBe(true);
      expect(alertTexts.some((t) => t.includes('Start date is required.'))).toBe(true);
      expect(alertTexts.some((t) => t.includes('End date is required.'))).toBe(true);
    });

    it('populates fields via Quick Fill Valid button', async () => {
      const wrapper = createWrapper();
      await flushPromises();

      const quickFillBtn = wrapper.find('[data-testid="quick-fill-valid-btn"]');
      await quickFillBtn.trigger('click');
      await flushPromises();

      const codeInput = wrapper.find<HTMLInputElement>('[data-testid="code-input"]');
      const nameInput = wrapper.find<HTMLInputElement>('[data-testid="name-input"]');
      const startDateInput = wrapper.find<HTMLInputElement>('[data-testid="start-date-input"]');
      const endDateInput = wrapper.find<HTMLInputElement>('[data-testid="end-date-input"]');

      expect(codeInput.element.value).toBe('AY-2026-2027');
      expect(nameInput.element.value).toBe('Academic Year 2026/2027');
      expect(startDateInput.element.value).toBe('2026-07-01');
      expect(endDateInput.element.value).toBe('2027-06-30');
    });

    it('submits valid payload, triggers success toast, and displays created entity in history', async () => {
      const mockResult: academicYearApi.AcademicYearDto = {
        id: '67890-test-id',
        code: 'AY-2026-2027',
        name: 'Academic Year 2026/2027',
        startDate: '2026-07-01',
        endDate: '2027-06-30',
        isActive: false,
      };

      const createSpy = vi
        .spyOn(academicYearApi, 'createAcademicYear')
        .mockResolvedValueOnce(mockResult);

      const wrapper = createWrapper();
      await flushPromises();

      // Quick fill valid values
      await wrapper.find('[data-testid="quick-fill-valid-btn"]').trigger('click');
      await flushPromises();

      // Submit
      await submitForm(wrapper);

      expect(createSpy).toHaveBeenCalledWith({
        code: 'AY-2026-2027',
        name: 'Academic Year 2026/2027',
        startDate: '2026-07-01',
        endDate: '2027-06-30',
      });

      // Verify success toast
      expect(mockToastAdd).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Academic Year Created',
          detail: expect.stringContaining('67890-test-id'),
        }),
      );

      // Verify success alert banner
      const successBanner = wrapper.find('[data-testid="creation-success-alert"]');
      expect(successBanner.exists()).toBe(true);
      expect(successBanner.text()).toContain('67890-test-id');

      // Verify created history list
      expect(wrapper.find('[data-testid="empty-records-state"]').exists()).toBe(false);
      const items = wrapper.findAll('[data-testid="created-record-item"]');
      expect(items.length).toBe(1);
      expect(items[0]?.text()).toContain('AY-2026-2027');
    });

    it('maps HTTP 422 ProblemDetails validation errors onto form fields and displays error toast', async () => {
      const problemDetailsError = new ApiError(
        422,
        'Validation Error',
        'One or more validation errors occurred.',
        'Operations.Calendar.AcademicYear.InvalidDateRange',
        {
          code: ['Code cannot exceed 32 characters.'],
          endDate: ['End date must be greater than start date.'],
        },
      );

      vi.spyOn(academicYearApi, 'createAcademicYear').mockRejectedValueOnce(problemDetailsError);

      const wrapper = createWrapper();
      await flushPromises();

      // Fill in valid syntax so client schema passes
      await wrapper.find('[data-testid="quick-fill-valid-btn"]').trigger('click');
      await flushPromises();

      // Submit
      await submitForm(wrapper);

      // Verify error toast
      expect(mockToastAdd).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'error',
          summary: 'Validation Error',
        }),
      );

      // Verify server validation error banner
      const errorBanner = wrapper.find('[data-testid="validation-error-banner"]');
      expect(errorBanner.exists()).toBe(true);
      expect(errorBanner.text()).toContain('One or more validation errors occurred.');
      expect(errorBanner.text()).toContain('Code cannot exceed 32 characters.');
      expect(errorBanner.text()).toContain('End date must be greater than start date.');

      // Verify field errors mapped onto inputs
      const alerts = wrapper.findAll('[role="alert"]');
      const alertTexts = alerts.map((a) => a.text());
      expect(alertTexts.some((t) => t.includes('Code cannot exceed 32 characters.'))).toBe(true);
      expect(alertTexts.some((t) => t.includes('End date must be greater than start date.'))).toBe(
        true,
      );
    });

    it('resets form fields and clears errors on Reset click', async () => {
      const wrapper = createWrapper();
      await flushPromises();

      await wrapper.find('[data-testid="quick-fill-valid-btn"]').trigger('click');
      await flushPromises();

      const codeInput = wrapper.find<HTMLInputElement>('[data-testid="code-input"]');
      expect(codeInput.element.value).toBe('AY-2026-2027');

      await wrapper.find('[data-testid="reset-form-btn"]').trigger('click');
      await flushPromises();

      expect(codeInput.element.value).toBe('');
    });
  });
});
