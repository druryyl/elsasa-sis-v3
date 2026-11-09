import { apiClient } from '@/shared/services/http';
import type {
  AcademicIntervention,
  AtRiskLearner,
  GetAtRiskLearnersParams,
  GetInterventionsParams,
  InitiateInterventionRequest,
  UpdateInterventionStatusRequest,
} from '../types';

/**
 * REST API client module for Academic Interventions (SCR-ACD-06, UC-ACD-11, P7-S19).
 */

/**
 * Queries live early-warning at-risk learners detected by the On-Demand At-Risk Query Engine.
 * GET /api/v1/academics/interventions/at-risk
 */
export async function getAtRiskLearners(
  params: GetAtRiskLearnersParams,
): Promise<AtRiskLearner[]> {
  const response = (await apiClient.get('/v1/academics/interventions/at-risk', {
    params,
  })) as unknown as AtRiskLearner[];
  return response;
}

/**
 * Queries active and historical academic intervention records with optional period/status filters.
 * GET /api/v1/academics/interventions
 */
export async function getInterventions(
  params?: GetInterventionsParams,
): Promise<AcademicIntervention[]> {
  const response = (await apiClient.get('/v1/academics/interventions', {
    params,
  })) as unknown as AcademicIntervention[];
  return response;
}

/**
 * Retrieves the full detail of an academic intervention plan.
 * GET /api/v1/academics/interventions/{id}
 */
export async function getInterventionById(
  id: string,
): Promise<AcademicIntervention> {
  const response = (await apiClient.get(
    `/v1/academics/interventions/${encodeURIComponent(id)}`,
  )) as unknown as AcademicIntervention;
  return response;
}

/**
 * Initiates and persists a structured academic intervention support plan.
 * POST /api/v1/academics/interventions
 */
export async function initiateIntervention(
  request: InitiateInterventionRequest,
): Promise<AcademicIntervention> {
  const response = (await apiClient.post(
    '/v1/academics/interventions',
    request,
  )) as unknown as AcademicIntervention;
  return response;
}

/**
 * Updates the lifecycle status of an intervention with recorded outcome notes.
 * PATCH /api/v1/academics/interventions/{id}/status
 */
export async function updateInterventionStatus(
  id: string,
  request: UpdateInterventionStatusRequest,
): Promise<AcademicIntervention> {
  const response = (await apiClient.patch(
    `/v1/academics/interventions/${encodeURIComponent(id)}/status`,
    request,
  )) as unknown as AcademicIntervention;
  return response;
}

export const interventionsApi = {
  getAtRiskLearners,
  getInterventions,
  getInterventionById,
  initiateIntervention,
  updateInterventionStatus,
};
