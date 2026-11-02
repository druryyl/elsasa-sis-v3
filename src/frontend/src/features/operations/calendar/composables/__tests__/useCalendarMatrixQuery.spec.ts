// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createApp, ref } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import * as calendarApi from '../../api/calendar.api';
import {
  useCalendarsQuery,
  useActiveCalendarQuery,
  useCalendarMatrixQuery,
  useCreateCalendarMutation,
  usePublishCalendarMutation,
  useWithdrawCalendarMutation,
  useCreateCalendarEntryMutation,
  useDeleteCalendarEntryMutation,
} from '../useCalendarMatrixQuery';
import { calendarKeys, amendmentKeys } from '../queryKeys';

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

describe('Calendar Matrix & Entries Composables (useCalendarMatrixQuery.ts)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('useCalendarsQuery & useActiveCalendarQuery', () => {
    it('fetches calendars list for an academic year', async () => {
      const mockCalendars: calendarApi.SchoolCalendarDto[] = [
        {
          id: 'cal-1',
          academicYearId: 'ay-1',
          name: 'Academic Calendar',
          scopeType: 'institution',
          scopeReference: 'All',
          version: '1.0',
          status: 'draft',
          publicationStatus: 'draft',
        },
      ];
      vi.spyOn(calendarApi, 'getCalendars').mockResolvedValue(mockCalendars);

      const ayId = ref('ay-1');
      const { result } = withSetup(() => useCalendarsQuery(ayId));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(calendarApi.getCalendars).toHaveBeenCalledWith('ay-1');
      expect(result.data.value).toEqual(mockCalendars);
    });

    it('fetches active calendar for an academic year', async () => {
      const mockActive: calendarApi.SchoolCalendarDto = {
        id: 'cal-1',
        academicYearId: 'ay-1',
        name: 'Active Calendar',
        scopeType: 'institution',
        scopeReference: 'All',
        version: '1.0',
        status: 'approved',
        publicationStatus: 'published',
      };
      vi.spyOn(calendarApi, 'getActiveCalendar').mockResolvedValue(mockActive);

      const ayId = ref('ay-1');
      const { result } = withSetup(() => useActiveCalendarQuery(ayId));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(calendarApi.getActiveCalendar).toHaveBeenCalledWith('ay-1');
      expect(result.data.value).toEqual(mockActive);
    });
  });

  describe('useCalendarMatrixQuery', () => {
    it('only fetches when calendarId and date parameters are provided', async () => {
      const mockMatrix: calendarApi.CalendarMatrixDayDto[] = [
        {
          date: '2026-07-15',
          dayOfWeek: 3,
          isInstructional: true,
          designation: 'instructional_day',
        },
      ];
      vi.spyOn(calendarApi, 'getMatrixProjection').mockResolvedValue(mockMatrix);

      const calId = ref<string | null>(null);
      const params = ref<calendarApi.MatrixProjectionParams | null>(null);

      const { result } = withSetup(() => useCalendarMatrixQuery(calId, params));
      expect(result.isFetching.value).toBe(false);

      calId.value = 'cal-1';
      params.value = { startDate: '2026-07-01', endDate: '2026-07-31' };

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(calendarApi.getMatrixProjection).toHaveBeenCalledWith('cal-1', {
        startDate: '2026-07-01',
        endDate: '2026-07-31',
      });
      expect(result.data.value).toEqual(mockMatrix);
    });
  });

  describe('usePublishCalendarMutation & useWithdrawCalendarMutation', () => {
    it('publishes calendar and invalidates calendars, matrix, and amendments caches', async () => {
      const publishedCalendar: calendarApi.SchoolCalendarDto = {
        id: 'cal-1',
        academicYearId: 'ay-1',
        name: 'Published Calendar',
        scopeType: 'institution',
        scopeReference: 'All',
        version: '1.0',
        status: 'approved',
        publicationStatus: 'published',
      };
      vi.spyOn(calendarApi, 'publishCalendar').mockResolvedValue(publishedCalendar);

      const { result, queryClient } = withSetup(() => usePublishCalendarMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync('cal-1');

      expect(calendarApi.publishCalendar).toHaveBeenCalledWith('cal-1');
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: calendarKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: calendarKeys.matrixAll });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: amendmentKeys.all });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Calendar Published',
        }),
      );
    });

    it('withdraws calendar and invalidates caches', async () => {
      const withdrawn: calendarApi.SchoolCalendarDto = {
        id: 'cal-1',
        academicYearId: 'ay-1',
        name: 'Calendar',
        scopeType: 'institution',
        scopeReference: 'All',
        version: '1.0',
        status: 'draft',
        publicationStatus: 'withdrawn',
      };
      vi.spyOn(calendarApi, 'withdrawCalendar').mockResolvedValue(withdrawn);

      const { result, queryClient } = withSetup(() => useWithdrawCalendarMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync('cal-1');

      expect(calendarApi.withdrawCalendar).toHaveBeenCalledWith('cal-1');
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: calendarKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: calendarKeys.matrixAll });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Calendar Withdrawn',
        }),
      );
    });
  });

  describe('useCreateCalendarEntryMutation & useDeleteCalendarEntryMutation', () => {
    it('creates calendar entry and automatically invalidates matrix, amendments, and calendars caches', async () => {
      const createdEntry: calendarApi.CalendarEntryDto = {
        id: 'entry-1',
        calendarId: 'cal-1',
        entryDate: '2026-08-17',
        endDate: null,
        designation: 'holiday',
        name: 'Independence Day',
        isInstructional: false,
        appliesTo: 'All',
      };
      vi.spyOn(calendarApi, 'createCalendarEntry').mockResolvedValue(createdEntry);

      const { result, queryClient } = withSetup(() => useCreateCalendarEntryMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        calendarId: 'cal-1',
        payload: {
          entryDate: '2026-08-17',
          designation: 'holiday',
          name: 'Independence Day',
          isInstructional: false,
        },
      });

      expect(calendarApi.createCalendarEntry).toHaveBeenCalledWith('cal-1', expect.anything());
      // Invalidation pipeline verification
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: calendarKeys.matrixAll });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: amendmentKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: calendarKeys.all });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Calendar Entry Added',
        }),
      );
    });

    it('deletes calendar entry and invalidates matrix, amendments, and calendars caches', async () => {
      vi.spyOn(calendarApi, 'deleteCalendarEntry').mockResolvedValue(undefined);

      const { result, queryClient } = withSetup(() => useDeleteCalendarEntryMutation());
      const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

      await result.mutateAsync({
        calendarId: 'cal-1',
        entryId: 'entry-1',
      });

      expect(calendarApi.deleteCalendarEntry).toHaveBeenCalledWith('cal-1', 'entry-1');
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: calendarKeys.matrixAll });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: amendmentKeys.all });
      expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: calendarKeys.all });
      expect(mockToast.add).toHaveBeenCalledWith(
        expect.objectContaining({
          severity: 'success',
          summary: 'Calendar Entry Removed',
        }),
      );
    });
  });
});
