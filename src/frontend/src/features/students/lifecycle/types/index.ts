/**
 * TypeScript interfaces and types for Student Lifecycle Workbench (SCR-STU-03, STD-WF-08, P8-S24).
 * Captures authoritative academic progression recommendations consumed from Academics domain.
 */

export type PromotionRecommendationStatus =
  | 'promotion_eligible'
  | 'retention_recommended'
  | 'conditional_promotion';

export const PROMOTION_RECOMMENDATION_STATUSES: readonly PromotionRecommendationStatus[] = [
  'promotion_eligible',
  'retention_recommended',
  | 'conditional_promotion',
] as const;

/**
 * Authoritative progression recommendation DTO retrieved from Academics (academics.promotion_recommendations).
 */
export interface ProgressionRecommendationDto {
  id: string;
  studentId: string;
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
  authorizedBy: string;
  issuedAt: string;
}
