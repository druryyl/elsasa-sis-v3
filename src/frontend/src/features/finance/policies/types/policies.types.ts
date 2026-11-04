/**
 * TypeScript types and DTOs for Financial Policies & Charge Rules (GAP-006, TD-06, ADR-004).
 */

export type PolicyCategory =
  | 'tuition'
  | 'admission'
  | 'facilities'
  | 'activities'
  | 'general';

export type PolicyStatus = 'draft' | 'active' | 'superseded' | 'archived';

export type RecurrencePattern = 'one_time' | 'monthly' | 'term' | 'annual';

export type DueRuleType =
  | 'days_after_issuance'
  | 'fixed_day_of_month'
  | 'fixed_calendar_date';

export type DeterminationMethod = 'fixed_amount' | 'custom_formula';

/**
 * Concrete Charge Rule attached to a governing Financial Policy.
 */
export interface ChargeRuleDto {
  id: string;
  ruleCode: string;
  policyId: string;
  name: string;
  chargeType: string;
  isMandatory: boolean;
  determinationMethod: DeterminationMethod | string;
  baseAmount: number;
  currencyCode: string;
  recurrencePattern: RecurrencePattern | string;
  dueRuleType: DueRuleType | string;
  dueRuleValue: string;
  applicableGrade: string | null;
  applicableProgram: string | null;
  status: string;
  notes?: string | null;
  createdAt?: string;
  createdBy?: string | null;
  updatedAt?: string | null;
  updatedBy?: string | null;
}

/**
 * Summary or Detailed projection representing a Financial Policy aggregate.
 */
export interface FinancialPolicyDto {
  id: string;
  policyCode: string;
  name: string;
  category: PolicyCategory | string;
  businessPurpose: string;
  applicableContext: string | null;
  effectiveDate: string;
  endDate: string | null;
  policyVersion: string;
  status: PolicyStatus | string;
  approvingAuthority: string | null;
  approvedDate: string | null;
  supersedesPolicyId: string | null;
  notes: string | null;
  createdAt: string;
  createdBy: string | null;
  updatedAt?: string | null;
  updatedBy?: string | null;
  rules: ChargeRuleDto[];
}

/**
 * Command payload for creating a new financial policy draft.
 */
export interface CreateFinancialPolicyRequest {
  policyCode: string;
  name: string;
  category: PolicyCategory | string;
  businessPurpose: string;
  applicableContext?: string | null;
  effectiveDate: string;
  endDate?: string | null;
  policyVersion?: string;
  supersedesPolicyId?: string | null;
  notes?: string | null;
}

/**
 * Response payload produced upon creating a financial policy draft.
 */
export interface CreateFinancialPolicyResponse {
  id: string;
  policyCode: string;
  name: string;
  category: string;
  businessPurpose: string;
  applicableContext: string | null;
  effectiveDate: string;
  endDate: string | null;
  policyVersion: string;
  status: string;
  supersedesPolicyId: string | null;
  notes: string | null;
  createdAt: string;
  createdBy: string | null;
}

/**
 * Command payload for formally approving a financial policy draft.
 */
export interface ApproveFinancialPolicyRequest {
  approvingAuthority?: string | null;
  notes?: string | null;
}

/**
 * Response payload produced upon formal approval of a financial policy.
 */
export interface ApproveFinancialPolicyResponse {
  id: string;
  policyCode: string;
  name: string;
  status: string;
  approvingAuthority: string;
  approvedDate: string;
  effectiveDate: string;
  notes: string | null;
  updatedAt: string | null;
  updatedBy: string | null;
}

/**
 * Command payload for creating a concrete charge rule.
 */
export interface CreateChargeRuleRequest {
  ruleCode: string;
  name: string;
  chargeType: string;
  baseAmount: number;
  currencyCode?: string;
  recurrencePattern?: RecurrencePattern | string;
  dueRuleType?: DueRuleType | string;
  dueRuleValue?: string;
  isMandatory?: boolean;
  determinationMethod?: DeterminationMethod | string;
  applicableGrade?: string | null;
  applicableProgram?: string | null;
  notes?: string | null;
}

/**
 * Response payload produced upon creating a concrete charge rule.
 */
export interface CreateChargeRuleResponse {
  id: string;
  ruleCode: string;
  policyId: string;
  name: string;
  chargeType: string;
  isMandatory: boolean;
  determinationMethod: string;
  baseAmount: number;
  currencyCode: string;
  recurrencePattern: string;
  dueRuleType: string;
  dueRuleValue: string;
  applicableGrade: string | null;
  applicableProgram: string | null;
  status: string;
  notes: string | null;
  createdAt: string;
  createdBy: string | null;
}

/**
 * Filter parameters for querying financial policies.
 */
export interface FinancialPolicyFilters {
  status?: string;
  category?: string;
}
