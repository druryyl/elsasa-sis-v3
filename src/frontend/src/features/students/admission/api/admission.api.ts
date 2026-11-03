import { apiClient } from '@/shared/services/http';
import type {
  AddAssessmentEvidencePayload,
  AddAssessmentEvidenceResultDto,
  AdmissionApplicationDetailDto,
  AdmissionApplicationListItemDto,
  AdmissionPeriodOptionDto,
  AdmissionQueueFilterParams,
  PublicApplicationStatusDto,
  RecordAdmissionDecisionPayload,
  RecordAdmissionDecisionResultDto,
  RegisterApplicationPayload,
  RegisterApplicationResultDto,
  StartApplicationReviewResultDto,
  UpdateAssessmentEvidencePayload,
  UpdateAssessmentEvidenceResultDto,
  UploadApplicantDocumentResultDto,
  VerifyApplicantDocumentPayload,
  VerifyApplicantDocumentResultDto,
  WithdrawApplicationPayload,
  WithdrawApplicationResultDto,
} from '../types/admission.types';

/**
 * Retrieves the work queue list of admission applications matching the given filter parameters (SCR-STU-01).
 * GET /api/students/admission/applications
 */
export async function getAdmissionApplicationsList(
  params?: AdmissionQueueFilterParams,
): Promise<AdmissionApplicationListItemDto[]> {
  const cleanParams: Record<string, string | number> = {
    page: typeof params?.page === 'number' ? params.page : 1,
    pageSize: typeof params?.pageSize === 'number' ? params.pageSize : 50,
  };

  if (params) {
    if (params.admissionPeriodId) cleanParams.admissionPeriodId = params.admissionPeriodId;
    if (params.status && params.status !== 'all') cleanParams.status = params.status;
    if (params.requestedEntryLevel && params.requestedEntryLevel !== 'all') {
      cleanParams.requestedEntryLevel = params.requestedEntryLevel;
    }
    if (params.searchTerm?.trim()) cleanParams.searchTerm = params.searchTerm.trim();
  }

  const response = (await apiClient.get('/students/admission/applications', {
    params: cleanParams,
  })) as unknown as AdmissionApplicationListItemDto[];

  return Array.isArray(response) ? response : [];
}

/**
 * Retrieves the complete admission application dossier by ID (SCR-STU-01).
 * GET /api/students/admission/applications/{id}
 */
export async function getAdmissionApplicationById(
  id: string,
): Promise<AdmissionApplicationDetailDto> {
  const response = (await apiClient.get(
    `/students/admission/applications/${encodeURIComponent(id)}`,
  )) as unknown as AdmissionApplicationDetailDto;

  return response;
}

/**
 * Public self-service lookup of application status by application number and birth date (SCR-SELF-01).
 * GET /api/students/admission/applications/status
 */
export async function getApplicationStatusByNumber(params: {
  applicationNumber: string;
  dateOfBirth: string;
}): Promise<PublicApplicationStatusDto> {
  const response = (await apiClient.get('/students/admission/applications/status', {
    params: {
      applicationNumber: params.applicationNumber.trim(),
      dateOfBirth: params.dateOfBirth.trim(),
    },
  })) as unknown as PublicApplicationStatusDto;

  return response;
}

/**
 * Registers an admission application via either staff intake or public portal endpoint.
 * POST /api/students/admission/applications (staff)
 * POST /api/students/admission/portal/applications (public portal)
 */
export async function registerApplication(
  payload: RegisterApplicationPayload,
  isPublicPortal = false,
): Promise<RegisterApplicationResultDto> {
  const url = isPublicPortal
    ? '/students/admission/portal/applications'
    : '/students/admission/applications';

  const response = (await apiClient.post(url, payload)) as unknown as RegisterApplicationResultDto;
  return response;
}

/**
 * Transitions an application from 'registered' to 'under_review'.
 * POST /api/students/admission/applications/{id}/start-review
 */
export async function startApplicationReview(
  id: string,
): Promise<StartApplicationReviewResultDto> {
  const response = (await apiClient.post(
    `/students/admission/applications/${encodeURIComponent(id)}/start-review`,
    {},
  )) as unknown as StartApplicationReviewResultDto;

  return response;
}

/**
 * Records an official immutable admission decision (accepted, rejected, waitlisted, deferred, etc.).
 * POST /api/students/admission/applications/{id}/decisions
 */
export async function recordAdmissionDecision(
  id: string,
  payload: RecordAdmissionDecisionPayload,
): Promise<RecordAdmissionDecisionResultDto> {
  const response = (await apiClient.post(
    `/students/admission/applications/${encodeURIComponent(id)}/decisions`,
    payload,
  )) as unknown as RecordAdmissionDecisionResultDto;

  return response;
}

/**
 * Withdraws an active admission application with justification notes.
 * POST /api/students/admission/applications/{id}/withdraw
 */
export async function withdrawApplication(
  id: string,
  payload: WithdrawApplicationPayload,
): Promise<WithdrawApplicationResultDto> {
  const response = (await apiClient.post(
    `/students/admission/applications/${encodeURIComponent(id)}/withdraw`,
    payload,
  )) as unknown as WithdrawApplicationResultDto;

  return response;
}

/**
 * Uploads an applicant document file via multipart form data (ADR-004, ADR-005).
 * POST /api/students/admission/applications/{id}/documents
 */
export async function uploadApplicantDocument(
  applicationId: string,
  documentType: string,
  file: File,
): Promise<UploadApplicantDocumentResultDto> {
  const formData = new FormData();
  formData.append('documentType', documentType);
  formData.append('file', file);

  const response = (await apiClient.post(
    `/students/admission/applications/${encodeURIComponent(applicationId)}/documents`,
    formData,
    {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    },
  )) as unknown as UploadApplicantDocumentResultDto;

  return response;
}

/**
 * Constructs the download URL for an uploaded applicant credential document.
 * GET /api/students/admission/applications/{id}/documents/{docId}/file
 */
export function downloadApplicantDocumentUrl(
  applicationId: string,
  documentId: string,
): string {
  const base = apiClient.defaults.baseURL || '/api';
  const cleanBase = base.endsWith('/') ? base.slice(0, -1) : base;
  return `${cleanBase}/students/admission/applications/${encodeURIComponent(applicationId)}/documents/${encodeURIComponent(documentId)}/file`;
}

/**
 * Downloads applicant document content as a binary Blob.
 */
export async function downloadApplicantDocumentFile(
  applicationId: string,
  documentId: string,
): Promise<Blob> {
  const response = (await apiClient.get(
    `/students/admission/applications/${encodeURIComponent(applicationId)}/documents/${encodeURIComponent(documentId)}/file`,
    {
      responseType: 'blob',
    },
  )) as unknown as Blob;

  return response;
}

/**
 * Verifies or rejects an applicant credential document.
 * PUT /api/students/admission/applications/{id}/documents/{docId}/verify
 */
export async function verifyApplicantDocument(
  applicationId: string,
  documentId: string,
  payload: VerifyApplicantDocumentPayload,
): Promise<VerifyApplicantDocumentResultDto> {
  const response = (await apiClient.put(
    `/students/admission/applications/${encodeURIComponent(applicationId)}/documents/${encodeURIComponent(documentId)}/verify`,
    payload,
  )) as unknown as VerifyApplicantDocumentResultDto;

  return response;
}

/**
 * Attaches a manual entrance assessment diagnostic result to an application.
 * POST /api/students/admission/applications/{id}/assessment-evidence
 */
export async function addAssessmentEvidence(
  applicationId: string,
  payload: AddAssessmentEvidencePayload,
): Promise<AddAssessmentEvidenceResultDto> {
  const response = (await apiClient.post(
    `/students/admission/applications/${encodeURIComponent(applicationId)}/assessment-evidence`,
    payload,
  )) as unknown as AddAssessmentEvidenceResultDto;

  return response;
}

/**
 * Updates an existing manual entrance assessment evidence record.
 * PUT /api/students/admission/applications/{id}/assessment-evidence/{evidenceId}
 */
export async function updateAssessmentEvidence(
  applicationId: string,
  evidenceId: string,
  payload: UpdateAssessmentEvidencePayload,
): Promise<UpdateAssessmentEvidenceResultDto> {
  const response = (await apiClient.put(
    `/students/admission/applications/${encodeURIComponent(applicationId)}/assessment-evidence/${encodeURIComponent(evidenceId)}`,
    payload,
  )) as unknown as UpdateAssessmentEvidenceResultDto;

  return response;
}

/**
 * Deletes an entrance assessment evidence record from an application.
 * DELETE /api/students/admission/applications/{id}/assessment-evidence/{evidenceId}
 */
export async function deleteAssessmentEvidence(
  applicationId: string,
  evidenceId: string,
): Promise<void> {
  await apiClient.delete(
    `/students/admission/applications/${encodeURIComponent(applicationId)}/assessment-evidence/${encodeURIComponent(evidenceId)}`,
  );
}

/**
 * Fetches operational periods from operations calendar to populate admission period dropdowns.
 * Queries active academic years and extracts open admission window periods.
 */
export async function getActiveAdmissionPeriods(): Promise<AdmissionPeriodOptionDto[]> {
  let academicYears: Array<{ id: string; status?: string }>;

  try {
    const res = (await apiClient.get('/operations/academic-years', {
      params: { status: 'active' },
    })) as unknown as Array<{ id: string; status?: string }>;
    academicYears = Array.isArray(res) ? res : [];
  } catch {
    academicYears = [];
  }

  if (academicYears.length === 0) {
    try {
      const allRes = (await apiClient.get('/operations/academic-years')) as unknown as Array<{
        id: string;
        status?: string;
      }>;
      academicYears = Array.isArray(allRes) ? allRes : [];
    } catch {
      academicYears = [];
    }
  }

  if (academicYears.length === 0) {
    return [];
  }

  const periodPromises = academicYears.map(async (ay) => {
    try {
      const periodsRes = (await apiClient.get(
        `/operations/academic-years/${encodeURIComponent(ay.id)}/periods`,
      )) as unknown as AdmissionPeriodOptionDto[] | { periods?: AdmissionPeriodOptionDto[] };

      if (Array.isArray(periodsRes)) {
        return periodsRes;
      }
      if (
        periodsRes &&
        typeof periodsRes === 'object' &&
        Array.isArray((periodsRes as { periods?: AdmissionPeriodOptionDto[] }).periods)
      ) {
        return (periodsRes as { periods: AdmissionPeriodOptionDto[] }).periods;
      }
      return [];
    } catch {
      return [];
    }
  });

  const periodLists = await Promise.all(periodPromises);
  const allPeriods = periodLists.flat();

  const activeWindows = allPeriods.filter(
    (p) =>
      p.periodType === 'admission_window' &&
      (p.status === 'active' || p.status === 'approved'),
  );

  if (activeWindows.length > 0) {
    return activeWindows;
  }

  const anyWindows = allPeriods.filter((p) => p.periodType === 'admission_window');
  if (anyWindows.length > 0) {
    return anyWindows;
  }

  return allPeriods.filter((p) => p.status === 'active' || p.status === 'approved');
}

