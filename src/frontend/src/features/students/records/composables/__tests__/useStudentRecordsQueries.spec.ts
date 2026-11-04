// @vitest-environment jsdom
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createApp, ref } from 'vue';
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query';
import * as recordsApi from '../../api/records.api';
import {
  useStudentSearchQuery,
  useStudentDossierQuery,
  useStudentAdministrativeHistoryQuery,
} from '../useStudentRecordsQueries';
import type {
  GetStudentAdministrativeHistoryResponse,
  StudentDetail,
  StudentListItem,
  StudentSearchParams,
} from '../../types/records.types';
import type { PaginatedResponse } from '@/shared/types/common';

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

describe('useStudentRecordsQueries', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('useStudentSearchQuery', () => {
    const mockListResponse: PaginatedResponse<StudentListItem> = {
      items: [
        {
          id: 'stu-1',
          studentNumber: 'STD-2026-0001',
          fullName: 'Siti Rahma',
          preferredName: 'Rahma',
          currentStatus: 'active',
          enrollmentDate: '2026-02-15',
          initialCampus: 'Main Campus',
          initialLevel: 'Grade 1',
          isIndependent: false,
          primaryGuardianName: 'Ahmad Faiz',
          primaryGuardianPhone: '+628123456789',
        },
      ],
      totalCount: 1,
      pageNumber: 1,
      pageSize: 20,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
    };

    it('fetches student list with reactive params and key [students, records, list, params]', async () => {
      vi.spyOn(recordsApi, 'searchStudents').mockResolvedValue(mockListResponse);

      const params = ref<StudentSearchParams>({
        searchTerm: 'Siti',
        campus: 'Main Campus',
        pageNumber: 1,
        pageSize: 20,
      });

      const { result } = withSetup(() => useStudentSearchQuery(params));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));

      expect(recordsApi.searchStudents).toHaveBeenCalledWith(params.value);
      expect(result.data.value).toEqual(mockListResponse);

      // React to params update
      params.value = { ...params.value, searchTerm: 'Ahmad' };
      await vi.waitFor(() =>
        expect(recordsApi.searchStudents).toHaveBeenCalledWith(
          expect.objectContaining({ searchTerm: 'Ahmad' }),
        ),
      );
    });

    it('honors enabled option', async () => {
      vi.spyOn(recordsApi, 'searchStudents').mockResolvedValue(mockListResponse);
      const isEnabled = ref(false);

      const { result } = withSetup(() =>
        useStudentSearchQuery(undefined, { enabled: isEnabled }),
      );

      expect(result.isFetching.value).toBe(false);
      expect(recordsApi.searchStudents).not.toHaveBeenCalled();

      isEnabled.value = true;
      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(recordsApi.searchStudents).toHaveBeenCalled();
    });
  });

  describe('useStudentDossierQuery', () => {
    const mockDetail: StudentDetail = {
      id: 'stu-1',
      studentNumber: 'STD-2026-0001',
      applicantId: 'apl-1',
      fullName: 'Siti Rahma',
      preferredName: 'Rahma',
      dateOfBirth: '2019-04-12',
      placeOfBirth: 'Jakarta',
      gender: 'female',
      nationality: 'Indonesian',
      governmentIdType: 'NIK',
      governmentIdNumber: '3201015204190001',
      address: 'Jl. Melati No. 10',
      phone: '+628111222333',
      email: 'siti@example.com',
      isIndependent: false,
      currentStatus: 'active',
      enrollmentDate: '2026-02-15',
      notes: null,
      createdAt: '2026-02-15T08:00:00Z',
      createdBy: 'officer@elsasa.sch.id',
      updatedAt: null,
      updatedBy: null,
      enrollmentHistory: [],
      guardians: [],
      documents: [],
    };

    it('does not execute when studentId is null or empty', () => {
      vi.spyOn(recordsApi, 'getStudentById').mockResolvedValue(mockDetail);
      const studentId = ref<string | null>(null);

      const { result } = withSetup(() => useStudentDossierQuery(studentId));

      expect(result.isFetching.value).toBe(false);
      expect(recordsApi.getStudentById).not.toHaveBeenCalled();
    });

    it('fetches complete 360° dossier with key [students, records, detail, studentId]', async () => {
      vi.spyOn(recordsApi, 'getStudentById').mockResolvedValue(mockDetail);
      const studentId = ref('stu-1');

      const { result } = withSetup(() => useStudentDossierQuery(studentId));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));

      expect(recordsApi.getStudentById).toHaveBeenCalledWith('stu-1');
      expect(result.data.value).toEqual(mockDetail);
    });
  });

  describe('useStudentAdministrativeHistoryQuery', () => {
    const mockHistoryResponse: GetStudentAdministrativeHistoryResponse = {
      studentId: 'stu-1',
      entries: [
        {
          id: 'hist-1',
          studentId: 'stu-1',
          eventType: 'enrollment_completed',
          eventDate: '2026-02-15',
          detail: 'Enrolled at Main Campus Grade 1',
          authorizedBy: 'officer@elsasa.sch.id',
          decisionReference: 'ENR-2026-0001',
          isCorrection: false,
          correctsEntryId: null,
          createdAt: '2026-02-15T08:00:00Z',
          createdBy: 'officer@elsasa.sch.id',
        },
      ],
    };

    it('does not execute when studentId is null or empty', () => {
      vi.spyOn(recordsApi, 'getStudentAdministrativeHistory').mockResolvedValue(mockHistoryResponse);
      const studentId = ref<string | null>(null);

      const { result } = withSetup(() => useStudentAdministrativeHistoryQuery(studentId));

      expect(result.isFetching.value).toBe(false);
      expect(recordsApi.getStudentAdministrativeHistory).not.toHaveBeenCalled();
    });

    it('fetches administrative history timeline with key [students, records, detail, studentId, history]', async () => {
      vi.spyOn(recordsApi, 'getStudentAdministrativeHistory').mockResolvedValue(mockHistoryResponse);
      const studentId = ref('stu-1');

      const { result } = withSetup(() => useStudentAdministrativeHistoryQuery(studentId));

      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));

      expect(recordsApi.getStudentAdministrativeHistory).toHaveBeenCalledWith('stu-1');
      expect(result.data.value).toEqual(mockHistoryResponse);
    });

    it('honors enabled option', async () => {
      vi.spyOn(recordsApi, 'getStudentAdministrativeHistory').mockResolvedValue(mockHistoryResponse);
      const isEnabled = ref(false);
      const studentId = ref('stu-1');

      const { result } = withSetup(() =>
        useStudentAdministrativeHistoryQuery(studentId, { enabled: isEnabled }),
      );

      expect(result.isFetching.value).toBe(false);
      expect(recordsApi.getStudentAdministrativeHistory).not.toHaveBeenCalled();

      isEnabled.value = true;
      await vi.waitFor(() => expect(result.isSuccess.value).toBe(true));
      expect(recordsApi.getStudentAdministrativeHistory).toHaveBeenCalledWith('stu-1');
    });
  });
});

