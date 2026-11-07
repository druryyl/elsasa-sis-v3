/**
 * TypeScript Interfaces, Enums and DTOs for Cohort Academic Review (SCR-ACD-09, ACD-WF-13, UC-ACD-17, UC-ACD-21, UC-ACD-15, P7-S19)
 */

// ============================================================================
// Enums / Const Literal Types
// ============================================================================

export type AcademicStandingStatus =
  | 'good_standing'
  | 'conditional_standing'
  | 'not_in_good_standing';

export const ACADEMIC_STANDINGS: readonly AcademicStandingStatus[] = [
  'good_standing',
  'conditional_standing',
  'not_in_good_standing',
] as const;

export type PromotionRecommendationStatus =
  | 'promotion_eligible'
  | 'retention_recommended'
  | 'conditional_promotion';

export const PROMOTION_RECOMMENDATIONS: readonly PromotionRecommendationStatus[] = [
  'promotion_eligible',
  'retention_recommended',
  'conditional_promotion',
] as const;

export type AcademicAchievementType =
  | 'subject_distinction'
  | 'academic_honors'
  | 'mastery_recognition'
  | 'academic_certification'
  | 'academic_competition';

export const ACHIEVEMENT_TYPES: readonly AcademicAchievementType[] = [
  'subject_distinction',
  'academic_honors',
  'mastery_recognition',
  'academic_certification',
  'academic_competition',
] as const;

// ============================================================================
// Cohort Dynamic Evaluation Proposals (TD-03, GAP-003)
// ============================================================================

/**
 * Dynamic evaluation proposal for a student's academic standing computed by Completion & Passing Threshold Rule.
 */
export interface CohortStandingProposal {
  studentId: string;
  studentName: string;
  studentIdentifier?: string;
  gradeLevel?: string;
  totalUnitsCount: number;
  passedUnitsCount: number;
  failedUnitsCount: number;
  attendancePercentage: number;
  proposedStanding: AcademicStandingStatus;
  policyReference: string;
  proposedConditions?: string | null;
  isException: boolean;
  exceptionReason?: string | null;
  committeeReference?: string | null;
}

/**
 * Dynamic evaluation proposal for a student's progression recommendation.
 */
export interface CohortProgressionProposal {
  studentId: string;
  studentName: string;
  studentIdentifier?: string;
  fromLevel: string;
  proposedRecommendation: PromotionRecommendationStatus;
  recommendedToLevel?: string | null;
  proposedConditions?: string | null;
  supportingEvidence: string;
  academicStandingId?: string | null;
  isException: boolean;
  exceptionReason?: string | null;
  committeeReference?: string | null;
}

// ============================================================================
// Operational Standings & Progression Items
// ============================================================================

/**
 * Authoritative record or payload for single/batch academic standing determination (academics.academic_standings).
 */
export interface StandingItem {
  id?: string;
  studentId: string;
  studentName?: string;
  studentIdentifier?: string;
  academicPeriodId: string;
  standing: AcademicStandingStatus;
  policyReference: string;
  conditions?: string | null;
  isException: boolean;
  exceptionReason?: string | null;
  committeeReference?: string | null;
  authorizedBy?: string;
  determinedAt?: string;
}

/**
 * Authoritative record or payload for single/batch progression recommendation (academics.promotion_recommendations).
 */
export interface ProgressionItem {
  id?: string;
  studentId: string;
  studentName?: string;
  studentIdentifier?: string;
  academicPeriodId: string;
  recommendation: PromotionRecommendationStatus;
  fromLevel: string;
  recommendedToLevel?: string | null;
  conditions?: string | null;
  supportingEvidence: string;
  academicStandingId?: string | null;
  isException: boolean;
  exceptionReason?: string | null;
  committeeReference?: string | null;
  authorizedBy?: string;
  issuedAt?: string;
}

/**
 * Formal academic achievement, distinction, or honor record (academics.academic_achievements).
 */
export interface AcademicAchievement {
  id: string;
  studentId: string;
  studentName?: string;
  studentIdentifier?: string;
  learningUnitId?: string | null;
  learningUnitName?: string | null;
  learningUnitCode?: string | null;
  academicPeriodId: string;
  academicPeriodLabel?: string;
  achievementType: AcademicAchievementType;
  title: string;
  description?: string | null;
  criteriaMet: string;
  supportingEvidence?: string | null;
  recognizedBy: string;
  recognizedAt: string;
  createdAt?: string;
  createdBy?: string;
}

// ============================================================================
// Request / Query Payloads
// ============================================================================

export interface EvaluateCohortParams {
  academicPeriodId: string;
  gradeLevel: string;
}

export interface BatchDetermineStandingRequest {
  academicPeriodId: string;
  items: StandingItem[];
}

export interface BatchDetermineStandingResponse {
  determinedCount: number;
  periodId: string;
}

export interface BatchIssueProgressionRequest {
  academicPeriodId: string;
  items: ProgressionItem[];
}

export interface BatchIssueProgressionResponse {
  issuedCount: number;
  periodId: string;
}

export interface ConferAchievementRequest {
  studentId: string;
  academicPeriodId: string;
  achievementType: AcademicAchievementType;
  title: string;
  criteriaMet: string;
  learningUnitId?: string | null;
  description?: string | null;
  supportingEvidence?: string | null;
  recognizedAt?: string;
}

export interface CorrectStandingRequest {
  standing: AcademicStandingStatus;
  policyReference?: string;
  conditions?: string | null;
  isException?: boolean;
  exceptionReason?: string | null;
  committeeReference?: string | null;
  reason: string;
}

export interface CorrectProgressionRequest {
  recommendation: PromotionRecommendationStatus;
  fromLevel?: string;
  recommendedToLevel?: string | null;
  conditions?: string | null;
  supportingEvidence?: string;
  isException?: boolean;
  exceptionReason?: string | null;
  committeeReference?: string | null;
  reason: string;
}

export interface GetStandingsParams {
  academicPeriodId?: string;
  gradeLevel?: string;
  studentId?: string;
}

export interface GetRecommendationsParams {
  academicPeriodId?: string;
  gradeLevel?: string;
  studentId?: string;
}
