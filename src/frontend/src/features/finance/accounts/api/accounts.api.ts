import { apiClient } from '@/shared/services/http';
import type { PaginatedResponse } from '@/shared/types/common';
import type {
  AccountSummaryDto,
  SearchStudentAccountsParams,
  StudentFinancialAccountDto,
} from '../types/accounts.types';

/**
 * Retrieves the complete 360-degree student financial account dossier including derived balances and open obligations (TD-02, SCR-FIN-03).
 * GET /api/finance/accounts/{studentId}
 */
export async function getStudentAccount(studentId: string): Promise<AccountSummaryDto> {
  const response = (await apiClient.get(
    `/finance/accounts/${encodeURIComponent(studentId)}`,
  )) as unknown as AccountSummaryDto;

  return response;
}

/**
 * Searches and paginates student financial accounts joined with student demographics and derived open balances (SCR-FIN-03).
 * GET /api/finance/accounts
 */
export async function searchStudentAccounts(
  params?: SearchStudentAccountsParams,
): Promise<PaginatedResponse<StudentFinancialAccountDto>> {
  const cleanParams: Record<string, string | number> = {
    pageNumber: params?.pageNumber ?? 1,
    pageSize: params?.pageSize ?? 20,
  };

  if (params?.searchTerm?.trim()) cleanParams.searchTerm = params.searchTerm.trim();
  if (params?.campus && params.campus !== 'all') cleanParams.campus = params.campus.trim();
  if (params?.grade && params.grade !== 'all') cleanParams.grade = params.grade.trim();
  if (params?.status && params.status !== 'all') cleanParams.status = params.status.trim();

  const response = (await apiClient.get('/finance/accounts', {
    params: cleanParams,
  })) as unknown as PaginatedResponse<StudentFinancialAccountDto>;

  return response;
}
