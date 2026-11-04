import { apiClient } from '@/shared/services/http';
import type { PaginatedResponse } from '@/shared/types/common';
import type {
  AddGuardianPayload,
  AddGuardianResponse,
  ChangeAdministrativePlacementRequest,
  ChangeAdministrativePlacementResponse,
  ChangeStudentStatusRequest,
  ChangeStudentStatusResponse,
  GetStudentAdministrativeHistoryResponse,
  StudentDetail,
  StudentListItem,
  StudentSearchParams,
  UpdateGuardianPayload,
  UpdateIdentityPayload,
  UploadStudentDocumentResponse,
  VerifyDocumentPayload,
} from '../types/records.types';

/**
 * Searches and paginates official student records (SCR-STU-02, P4-S05).
 * GET /api/students
 */
export async function searchStudents(
  params?: StudentSearchParams,
): Promise<PaginatedResponse<StudentListItem>> {
  const cleanParams: Record<string, string | number> = {
    pageNumber: typeof params?.pageNumber === 'number' ? params.pageNumber : 1,
    pageSize: typeof params?.pageSize === 'number' ? params.pageSize : 20,
  };

  if (params?.searchTerm?.trim()) cleanParams.searchTerm = params.searchTerm.trim();
  if (params?.campus && params.campus !== 'all') cleanParams.campus = params.campus.trim();
  if (params?.level && params.level !== 'all') cleanParams.level = params.level.trim();
  if (params?.status && params.status !== 'all') cleanParams.status = params.status.trim();

  const response = (await apiClient.get('/students', {
    params: cleanParams,
  })) as unknown as PaginatedResponse<StudentListItem>;

  return response;
}

/**
 * Retrieves the complete 360-degree authoritative student dossier by student ID (SCR-STU-02, P4-S06).
 * GET /api/students/{id}
 */
export async function getStudentById(id: string): Promise<StudentDetail> {
  const response = (await apiClient.get(
    `/students/${encodeURIComponent(id)}`,
  )) as unknown as StudentDetail;

  return response;
}

/**
 * Updates certified student identity attributes (SCR-STU-02, P5-S07).
 * PUT /api/students/{id}/identity
 */
export async function updateStudentIdentity(
  id: string,
  payload: UpdateIdentityPayload,
): Promise<void> {
  await apiClient.put(`/students/${encodeURIComponent(id)}/identity`, payload);
}

/**
 * Links a new guardian relationship to a student (SCR-STU-02, P5-S08).
 * Enforces guardian deduplication and single active primary contact.
 * POST /api/students/{id}/guardians
 *
 * @returns The unique identifier of the newly established relationship.
 */
export async function addGuardianRelationship(
  studentId: string,
  payload: AddGuardianPayload,
): Promise<string> {
  const response = (await apiClient.post(
    `/students/${encodeURIComponent(studentId)}/guardians`,
    payload,
  )) as AddGuardianResponse | string;

  if (typeof response === 'string') {
    return response;
  }
  return response?.relationshipId || response?.id || '';
}

/**
 * Updates an existing student-guardian relationship and authority flags (SCR-STU-02, P5-S08).
 * PUT /api/students/{id}/guardians/{relationshipId}
 */
export async function updateGuardianRelationship(
  studentId: string,
  relationshipId: string,
  payload: UpdateGuardianPayload,
): Promise<void> {
  await apiClient.put(
    `/students/${encodeURIComponent(studentId)}/guardians/${encodeURIComponent(relationshipId)}`,
    payload,
  );
}

/**
 * Uploads an official certified student document (SCR-STU-02, P6-S09).
 * POST /api/students/{id}/documents
 *
 * @returns The unique identifier of the created document record.
 */
export async function uploadStudentDocument(
  studentId: string,
  file: File,
  documentType: string,
): Promise<string> {
  const formData = new FormData();
  formData.append('file', file);
  formData.append('documentType', documentType);

  const response = (await apiClient.post(
    `/students/${encodeURIComponent(studentId)}/documents`,
    formData,
    {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    },
  )) as UploadStudentDocumentResponse | string;

  if (typeof response === 'string') {
    return response;
  }
  return response?.documentId || response?.id || '';
}

/**
 * Constructs the safe download streaming URL for an official student document (SCR-STU-02, P6-S09).
 * GET /api/students/{id}/documents/{docId}/file
 */
export function getStudentDocumentDownloadUrl(studentId: string, docId: string): string {
  const base = apiClient.defaults.baseURL || '/api';
  const cleanBase = base.endsWith('/') ? base.slice(0, -1) : base;
  return `${cleanBase}/students/${encodeURIComponent(studentId)}/documents/${encodeURIComponent(docId)}/file`;
}

/**
 * Verifies or rejects an official student document (SCR-STU-02, P6-S09).
 * PUT /api/students/{id}/documents/{docId}/verify
 */
export async function verifyStudentDocument(
  studentId: string,
  docId: string,
  statusOrPayload: string | VerifyDocumentPayload,
  notes?: string,
): Promise<void> {
  const payload: VerifyDocumentPayload =
    typeof statusOrPayload === 'string'
      ? {
          submissionStatus: statusOrPayload,
          notes: notes ?? null,
        }
      : statusOrPayload;

  await apiClient.put(
    `/students/${encodeURIComponent(studentId)}/documents/${encodeURIComponent(docId)}/verify`,
    payload,
  );
}

/**
 * Changes a student's administrative placement (SCR-STU-02, P5-S12, STD-WF-05, UC-STD-06).
 * POST /api/students/{studentId}/placements
 */
export async function changeAdministrativePlacement(
  studentId: string,
  payload: ChangeAdministrativePlacementRequest,
): Promise<ChangeAdministrativePlacementResponse> {
  const response = (await apiClient.post(
    `/students/${encodeURIComponent(studentId)}/placements`,
    payload,
  )) as unknown as ChangeAdministrativePlacementResponse;

  return response;
}

/**
 * Changes a student's operational status (SCR-STU-02, P5-S12).
 * POST /api/students/{studentId}/status
 */
export async function changeStudentStatus(
  studentId: string,
  payload: ChangeStudentStatusRequest,
): Promise<ChangeStudentStatusResponse> {
  const response = (await apiClient.post(
    `/students/${encodeURIComponent(studentId)}/status`,
    payload,
  )) as unknown as ChangeStudentStatusResponse;

  return response;
}

/**
 * Retrieves the complete chronological administrative history entries for a student (SCR-STU-02, P5-S12).
 * GET /api/students/{studentId}/history
 */
export async function getStudentAdministrativeHistory(
  studentId: string,
): Promise<GetStudentAdministrativeHistoryResponse> {
  const response = (await apiClient.get(
    `/students/${encodeURIComponent(studentId)}/history`,
  )) as unknown as GetStudentAdministrativeHistoryResponse;

  return response;
}
