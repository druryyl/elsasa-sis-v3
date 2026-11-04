import { apiClient } from '@/shared/services/http';
import type { PaginatedResponse } from '@/shared/types/common';
import type {
  AcademicRequirement,
  AcademicRequirementFilters,
  ApproveLearningStructureResponse,
  ArchiveLearningStructureResponse,
  CreateAcademicRequirementRequest,
  CreateLearningStructureRequest,
  CreateLearningUnitRequest,
  LearningStructure,
  LearningStructureFilters,
  LearningStructureSummary,
  LearningUnit,
  LearningUnitFilters,
  SubmitLearningStructureResponse,
  SupersedeStructureRequest,
  UpdateAcademicRequirementRequest,
  UpdateLearningStructureRequest,
  UpdateLearningUnitRequest,
} from '../types/curriculum.types';

// ============================================================================
// Learning Structures Management API
// ============================================================================

/**
 * Retrieves a paginated list of learning structure frameworks with optional filtering (P5-S14).
 * GET /api/v1/academics/learning-structures
 */
export async function getLearningStructures(
  filters?: LearningStructureFilters,
): Promise<PaginatedResponse<LearningStructureSummary>> {
  const params: Record<string, string | number> = {};

  if (filters?.academicPeriodId) {
    params.academicPeriodId = filters.academicPeriodId;
  }
  if (filters?.status) {
    params.status = filters.status;
  }
  if (filters?.search?.trim()) {
    params.search = filters.search.trim();
  }
  if (typeof filters?.page === 'number' && filters.page > 0) {
    params.page = filters.page;
  }
  if (typeof filters?.pageSize === 'number' && filters.pageSize > 0) {
    params.pageSize = filters.pageSize;
  }

  const response = (await apiClient.get('/v1/academics/learning-structures', {
    params,
  })) as unknown as PaginatedResponse<LearningStructureSummary>;

  return response;
}

/**
 * Retrieves detailed information of a specific learning structure framework by identifier (P5-S14).
 * GET /api/v1/academics/learning-structures/{id}
 */
export async function getLearningStructureById(id: string): Promise<LearningStructure> {
  const response = (await apiClient.get(
    `/v1/academics/learning-structures/${encodeURIComponent(id)}`,
  )) as unknown as LearningStructure;

  return response;
}

/**
 * Drafts a new learning structure framework for an academic period (P5-S14).
 * POST /api/v1/academics/learning-structures
 */
export async function createLearningStructure(
  request: CreateLearningStructureRequest,
): Promise<LearningStructure> {
  const response = (await apiClient.post(
    '/v1/academics/learning-structures',
    request,
  )) as unknown as LearningStructure;

  return response;
}

/**
 * Updates non-structural metadata (name, description, notes) of a learning structure (P5-S14).
 * PUT /api/v1/academics/learning-structures/{id}
 */
export async function updateLearningStructure(
  id: string,
  request: UpdateLearningStructureRequest,
): Promise<LearningStructure> {
  const response = (await apiClient.put(
    `/v1/academics/learning-structures/${encodeURIComponent(id)}`,
    request,
  )) as unknown as LearningStructure;

  return response;
}

// ============================================================================
// Learning Structures Governance Lifecycle API
// ============================================================================

/**
 * Submits a draft learning structure framework for formal authority review (P5-S14, Criterion 1).
 * POST /api/v1/academics/learning-structures/{id}/submit
 */
export async function submitLearningStructure(
  id: string,
): Promise<SubmitLearningStructureResponse> {
  const response = (await apiClient.post(
    `/v1/academics/learning-structures/${encodeURIComponent(id)}/submit`,
    {},
  )) as unknown as SubmitLearningStructureResponse;

  return response;
}

/**
 * Formally approves and activates a learning structure framework (P5-S14, Criterion 1).
 * POST /api/v1/academics/learning-structures/{id}/approve
 */
export async function approveLearningStructure(
  id: string,
): Promise<ApproveLearningStructureResponse> {
  const response = (await apiClient.post(
    `/v1/academics/learning-structures/${encodeURIComponent(id)}/approve`,
    {},
  )) as unknown as ApproveLearningStructureResponse;

  return response;
}

/**
 * Branches an active learning structure into a new draft successor version via deep-cloning (P5-S14, TD-03).
 * POST /api/v1/academics/learning-structures/{id}/supersede
 */
export async function supersedeLearningStructure(
  id: string,
  request: SupersedeStructureRequest,
): Promise<LearningStructure> {
  const response = (await apiClient.post(
    `/v1/academics/learning-structures/${encodeURIComponent(id)}/supersede`,
    request,
  )) as unknown as LearningStructure;

  return response;
}

/**
 * Archives a superseded or inactive learning structure framework (P5-S14, TD-06).
 * POST /api/v1/academics/learning-structures/{id}/archive
 */
export async function archiveLearningStructure(
  id: string,
): Promise<ArchiveLearningStructureResponse> {
  const response = (await apiClient.post(
    `/v1/academics/learning-structures/${encodeURIComponent(id)}/archive`,
    {},
  )) as unknown as ArchiveLearningStructureResponse;

  return response;
}

// ============================================================================
// Learning Units API
// ============================================================================

/**
 * Queries all learning units belonging to a learning structure (P5-S14, TD-04).
 * GET /api/v1/academics/learning-structures/{structureId}/units
 */
export async function getLearningUnits(
  structureId: string,
  filters?: LearningUnitFilters,
): Promise<LearningUnit[]> {
  const params: Record<string, string> = {};

  if (filters?.unitType) {
    params.unitType = filters.unitType;
  }
  if (filters?.parentUnitId) {
    params.parentUnitId = filters.parentUnitId;
  }
  if (filters?.level) {
    params.level = filters.level;
  }
  if (filters?.search?.trim()) {
    params.search = filters.search.trim();
  }

  const response = (await apiClient.get(
    `/v1/academics/learning-structures/${encodeURIComponent(structureId)}/units`,
    { params },
  )) as unknown as LearningUnit[];

  return Array.isArray(response) ? response : [];
}

/**
 * Retrieves a single learning unit by identifier with parent unit details (P5-S14).
 * GET /api/v1/academics/learning-units/{id}
 */
export async function getLearningUnitById(id: string): Promise<LearningUnit> {
  const response = (await apiClient.get(
    `/v1/academics/learning-units/${encodeURIComponent(id)}`,
  )) as unknown as LearningUnit;

  return response;
}

/**
 * Creates a new learning unit under a learning structure framework (P5-S14).
 * POST /api/v1/academics/learning-structures/{structureId}/units
 */
export async function createLearningUnit(
  structureId: string,
  request: CreateLearningUnitRequest,
): Promise<LearningUnit> {
  const response = (await apiClient.post(
    `/v1/academics/learning-structures/${encodeURIComponent(structureId)}/units`,
    request,
  )) as unknown as LearningUnit;

  return response;
}

/**
 * Updates an existing learning unit (P5-S14).
 * PUT /api/v1/academics/learning-units/{id}
 */
export async function updateLearningUnit(
  id: string,
  request: UpdateLearningUnitRequest,
): Promise<LearningUnit> {
  const response = (await apiClient.put(
    `/v1/academics/learning-units/${encodeURIComponent(id)}`,
    request,
  )) as unknown as LearningUnit;

  return response;
}

/**
 * Removes a learning unit from a draft learning structure framework (P5-S14).
 * DELETE /api/v1/academics/learning-units/{id}
 */
export async function deleteLearningUnit(id: string): Promise<void> {
  await apiClient.delete(`/v1/academics/learning-units/${encodeURIComponent(id)}`);
}

// ============================================================================
// Academic Requirements API
// ============================================================================

/**
 * Queries academic requirements governing a learning structure framework (P5-S14, TD-05).
 * GET /api/v1/academics/learning-structures/{structureId}/requirements
 */
export async function getAcademicRequirements(
  structureId: string,
  filters?: AcademicRequirementFilters,
): Promise<AcademicRequirement[]> {
  const params: Record<string, string> = {};

  if (filters?.scope) {
    params.scope = filters.scope;
  }
  if (filters?.type) {
    params.type = filters.type;
  }

  const response = (await apiClient.get(
    `/v1/academics/learning-structures/${encodeURIComponent(structureId)}/requirements`,
    { params },
  )) as unknown as { requirements?: AcademicRequirement[] } | AcademicRequirement[];

  if (Array.isArray(response)) {
    return response;
  }
  if (response && typeof response === 'object' && Array.isArray(response.requirements)) {
    return response.requirements;
  }
  return [];
}

/**
 * Inspects a single academic requirement by identifier (P5-S14).
 * GET /api/v1/academics/academic-requirements/{id}
 */
export async function getAcademicRequirementById(id: string): Promise<AcademicRequirement> {
  const response = (await apiClient.get(
    `/v1/academics/academic-requirements/${encodeURIComponent(id)}`,
  )) as unknown as AcademicRequirement;

  return response;
}

/**
 * Creates an academic requirement anchored to a learning structure framework (P5-S14).
 * POST /api/v1/academics/learning-structures/{structureId}/requirements
 */
export async function createAcademicRequirement(
  structureId: string,
  request: CreateAcademicRequirementRequest,
): Promise<AcademicRequirement> {
  const response = (await apiClient.post(
    `/v1/academics/learning-structures/${encodeURIComponent(structureId)}/requirements`,
    request,
  )) as unknown as AcademicRequirement;

  return response;
}

/**
 * Updates an academic requirement criteria description and metadata (P5-S14).
 * PUT /api/v1/academics/academic-requirements/{id}
 */
export async function updateAcademicRequirement(
  id: string,
  request: UpdateAcademicRequirementRequest,
): Promise<AcademicRequirement> {
  const response = (await apiClient.put(
    `/v1/academics/academic-requirements/${encodeURIComponent(id)}`,
    request,
  )) as unknown as AcademicRequirement;

  return response;
}

/**
 * Deletes an academic requirement from a draft learning structure (P5-S14).
 * DELETE /api/v1/academics/academic-requirements/{id}
 */
export async function deleteAcademicRequirement(id: string): Promise<void> {
  await apiClient.delete(`/v1/academics/academic-requirements/${encodeURIComponent(id)}`);
}
