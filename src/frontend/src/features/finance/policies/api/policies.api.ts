import { apiClient } from '@/shared/services/http';
import type {
  ApproveFinancialPolicyRequest,
  ApproveFinancialPolicyResponse,
  CreateChargeRuleRequest,
  CreateChargeRuleResponse,
  CreateFinancialPolicyRequest,
  CreateFinancialPolicyResponse,
  FinancialPolicyDto,
  FinancialPolicyFilters,
} from '../types/policies.types';

/**
 * Queries financial policies matching optional status and category filters (FIN-WF-01, UC-FIN-10).
 * GET /api/finance/policies
 */
export async function getPolicies(
  filters?: FinancialPolicyFilters,
): Promise<FinancialPolicyDto[]> {
  const cleanParams: Record<string, string> = {};
  if (filters?.status && filters.status !== 'all') {
    cleanParams.status = filters.status.trim();
  }
  if (filters?.category && filters.category !== 'all') {
    cleanParams.category = filters.category.trim();
  }

  const response = (await apiClient.get('/finance/policies', {
    params: cleanParams,
  })) as unknown as FinancialPolicyDto[];

  return response;
}

/**
 * Retrieves a detailed financial policy aggregate dossier including all associated charge rules.
 * GET /api/finance/policies/{id}
 */
export async function getPolicyById(id: string): Promise<FinancialPolicyDto> {
  const response = (await apiClient.get(
    `/finance/policies/${encodeURIComponent(id)}`,
  )) as unknown as FinancialPolicyDto;

  return response;
}

/**
 * Creates a new financial policy draft (FIN-WF-01, UC-FIN-10, SCR-FIN-04).
 * POST /api/finance/policies
 */
export async function createPolicy(
  request: CreateFinancialPolicyRequest,
): Promise<CreateFinancialPolicyResponse> {
  const response = (await apiClient.post(
    '/finance/policies',
    request,
  )) as unknown as CreateFinancialPolicyResponse;

  return response;
}

/**
 * Formally approves a financial policy draft, transitioning it to active status (Finance Authority).
 * POST /api/finance/policies/{id}/approve
 */
export async function approvePolicy(
  id: string,
  request?: ApproveFinancialPolicyRequest,
): Promise<ApproveFinancialPolicyResponse> {
  const response = (await apiClient.post(
    `/finance/policies/${encodeURIComponent(id)}/approve`,
    request ?? {},
  )) as unknown as ApproveFinancialPolicyResponse;

  return response;
}

/**
 * Adds an operational charge rule anchored to a governing financial policy (SCR-FIN-04).
 * POST /api/finance/policies/{id}/rules
 */
export async function createChargeRule(
  policyId: string,
  request: CreateChargeRuleRequest,
): Promise<CreateChargeRuleResponse> {
  const response = (await apiClient.post(
    `/finance/policies/${encodeURIComponent(policyId)}/rules`,
    request,
  )) as unknown as CreateChargeRuleResponse;

  return response;
}
