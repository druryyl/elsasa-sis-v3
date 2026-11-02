// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createApp, ref } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import * as amendmentApi from '../../api/amendment.api';
import {
  useCalendarAmendmentsQuery,
  useRecordCorrectionsQuery,
  useSubmitCorrectionMutation,
} from '../useAmendmentsQuery';
import {
  amendmentKeys,
  recordCorrectionKeys,
  academicYearKeys,
  operationalPeriodKeys,
  calendarKeys,
} from '../queryKeys';

const mockToast = {
  add: vi.fn(),
  removeGroup: vi.fn(),
  removeAllGroups: vi.fn(),
};

vi.mock('primevue/usetoast', () => ({
  useToast: () => mockToast,
}));

function withSetup<T>(composable: () => T) {
  let result!: T;
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0 },
      mutations: { retry: false },
    },
  });

  const app = createApp({
    setup() {
      result = composable();
      return () => {};
    },
  });

  app.use(VueQueryPlugin, { queryClient });
  app.mount(document.createElement('div'));

  return { result, queryClient, app };
}

describe('Amendments & Corrections Composables (useAmendmentsQuery.ts)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('useCalendarAmendmentsQuery', () => {
    it('fetches versioned amendments for a school calendar', async () => {
      const mockAmendments: amendmentApi.CalendarAmendmentDto[] = [
        {
          id: 'amd-1',
          calendarId: 'cal-1',
          amendmentNumber: 'AMD-001',
          fromVersion: '1.0',
          toVersion: '1.1',
          amendmentType: 'closure_addition',
          affectedEntryId: 'entry-1',
          changeSummary: 'Campus flood closure added',
          reason: 'Emergency weather response',
          effectiveDate: '2026-09-10',
          authorizedBy: 'Operations Authority',
          authorizedDate: '2026-09-09',
          downstreamImpactReviewed: true,
          createdAt: '2026-09-09T10:00:00Z',
        },
      ];
      vi.spyOn(amendmentApi, 'getAmendments').mockResolvedValue(mockAmendments);

      const calId = ref<string | null>(null);
      const { result } = withSetup(() => useCalendarAmendmentsQuery(calId));

      expect(result.isFetching.value).toBe(false);

      calId.value = 'cal-1';
      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));

      expect(amendmentApi.getAmendments).toHaveBeenCalledWith('cal-1');
      expect(result.data.value).toEqual(mockAmendments);
    });
  });

  describe('useRecordCorrectionsQuery', () => {
    it('fetches audit correction log for an academic year context', async () => {
      const mockCorrections: amendmentApi.RecordCorrectionDto[] = [
        {
          id: 'cor-1',
          academicYearId: 'ay-1',
          targetTableName: 'operational_periods',
          targetRecordId: 'p-1',
          fieldName: 'name',
          oldValue: 'Semester 1 Old',
          newValue: 'Semester 1 Official',
          justificationReason: 'Correcting typographical error per decree 42',
          authorizedBy: 'Authority Officer',
          authorizedAt: '2026-08-01T08:00:00Z',
        },
      ];
      vi.spyOn(amendmentApi, 'getCorrections').mockResolvedValue(mockCorrections);

      const ayId = ref('ay-1');
      const { result } = withSetup(() => useRecordCorrectionsQuery(ayId));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));

      expect(amendmentApi.getCorrections).toHaveBeenCalledWith('ay-1');
      expect(result.data.value).toEqual(mockCorrections);
    });
  });

  describe('useSubmitCorrectionMutation', () => {
    it('submits correction and invalidates corrections, amendments, academicYears, periods, calendars, and matrix caches', async () => {
      const correctionResult: amendmentApi.RecordCorrectionDto = {
        id: 'cor-1',
        academicYearId: 'ay-1',
        targetTableName: 'academic_years',
        targetRecordId: 'ay-1',
        fieldName: 'name',
        newValue: 'Tahun Pelajaran 2026/2027',
        justificationReason: 'Official policy update',
        authorizedBy: 'Dean',
        authorizedAt: '2026-07-01T10:00:00Z',
      };
      vi.spyOn(amendmentApi, 'submitCorrection').mockResolvedValue(correctionResult);

      const { result, queryClient } = withSetup(() => useSubmitCorrectionMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        academicYearId: 'ay-1',
        targetTableName: 'academic_years',
        targetRecordId: 'ay-1',
        fieldName: 'name',
        newValue: 'Tahun Pelajaran 2026/2027',
        justificationReason: 'Official policy update',
      });

      expect(amendmentApi.submitCorrection).toHaveBeenCalled();

      // Verify broad invalidation
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: recordCorrectionKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: amendmentKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: academicYearKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: operationalPeriodKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: calendarKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: calendarKeys.matrixAll });

      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Record Correction Submitted',
          detail: "Correction for 'name' authorized successfully.",
        }),
      );
    });
  });
});
