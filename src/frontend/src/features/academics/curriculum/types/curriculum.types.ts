/**
 * Authoritative TypeScript interfaces and types for the Academics Curriculum module (ADR-004, ADR-005, P5-S14).
 * Reflects backend vertical slice DTOs, domain models, and SQL Server projections.
 */

// ============================================================================
// Lifecycle & Domain Enums / Type Unions
// ============================================================================

export type LearningStructureStatus =
  | 'draft'
  | 'submitted'
  | 'active'
  | 'superseded'
  | 'archived';

export type LearningUnitStatus = 'draft' | 'active' | 'archived';

export type LearningUnitType =
  | 'subject'
  | 'course'
  | 'module'
  | 'pace'
  | 'project'
  | 'competency'
  | 'unit'
  | 'other';

export type AcademicRequirementScope = 'program' | 'level' | 'learning_unit';

export type AcademicRequirementType =
  | 'completion'
  | 'prerequisite'
  | 'credit'
  | 'progression'
  | 'graduation'
  | 'assessment_policy';

export type AcademicRequirementStatus = 'draft' | 'active' | 'superseded' | 'archived';

// ============================================================================
// Learning Structure Framework Models
// ============================================================================

/**
 * Authoritative learning structure framework detail projection (SCR-ACD-03).
 */
export interface LearningStructure {
  id: string;
  name: string;
  description?: string | null;
  academicPeriodId: string;
  academicPeriodLabel: string;
  version: string;
  effectiveDate: string;
  endDate?: string | null;
  status: LearningStructureStatus | string;
  approvedBy?: string | null;
  approvedDate?: string | null;
  supersedesId?: string | null;
  predecessorName?: string | null;
  predecessorVersion?: string | null;
  notes?: string | null;
  createdAt: string;
  createdBy: string;
  updatedAt?: string | null;
  updatedBy?: string | null;
  unitCount?: number;
  requirementCount?: number;
  hasPredecessor?: boolean;
}

/**
 * Learning structure summary item for paginated inquiries and selection dropdowns.
 */
export interface LearningStructureSummary {
  id: string;
  name: string;
  description?: string | null;
  academicPeriodId: string;
  academicPeriodLabel: string;
  version: string;
  effectiveDate: string;
  endDate?: string | null;
  status: LearningStructureStatus | string;
  approvedBy?: string | null;
  approvedDate?: string | null;
  supersedesId?: string | null;
  createdAt: string;
  createdBy: string;
  updatedAt?: string | null;
  updatedBy?: string | null;
  unitCount: number;
  requirementCount: number;
  hasPredecessor: boolean;
}

/**
 * Payload for drafting a new learning structure framework.
 */
export interface CreateLearningStructureRequest {
  name: string;
  description?: string | null;
  academicPeriodId: string;
  version: string;
  effectiveDate: string;
  endDate?: string | null;
  notes?: string | null;
}

/**
 * Payload for updating non-structural metadata of a learning structure.
 */
export interface UpdateLearningStructureRequest {
  name: string;
  description?: string | null;
  notes?: string | null;
}

/**
 * Payload to branch an active learning structure into a new draft version.
 */
export interface SupersedeStructureRequest {
  newVersion: string;
  effectiveDate: string;
  academicPeriodId?: string | null;
  academicPeriodLabel?: string | null;
}

/**
 * Lifecycle response returned when a framework is submitted for authority review.
 */
export interface SubmitLearningStructureResponse {
  id: string;
  status: string;
  updatedAt: string;
  updatedBy: string;
}

/**
 * Lifecycle response returned when a framework is formally approved and activated.
 */
export interface ApproveLearningStructureResponse {
  id: string;
  status: string;
  approvedBy: string;
  approvedDate: string;
  updatedAt: string;
  updatedBy: string;
}

/**
 * Lifecycle response returned when a framework is archived.
 */
export interface ArchiveLearningStructureResponse {
  id: string;
  status: string;
  updatedAt: string;
  updatedBy: string;
}

// ============================================================================
// Learning Unit Models & Tree Composition
// ============================================================================

/**
 * Learning unit entity representing subjects, modules, paces, competencies, or courses.
 */
export interface LearningUnit {
  id: string;
  learningStructureId: string;
  parentUnitId?: string | null;
  parentCode?: string | null;
  parentName?: string | null;
  parentUnitType?: string | null;
  code: string;
  name: string;
  unitType: LearningUnitType | string;
  level?: string | null;
  description?: string | null;
  creditWeight?: number | null;
  status: LearningUnitStatus | string;
  notes?: string | null;
  createdAt: string;
  createdBy: string;
  updatedAt?: string | null;
  updatedBy?: string | null;
}

/**
 * Hierarchical tree node projection for PrimeVue Tree / TreeTable and nested visualization.
 */
export interface LearningUnitTreeNode extends LearningUnit {
  children?: LearningUnitTreeNode[];
  key?: string;
  data?: LearningUnit;
}

/**
 * Payload for creating a learning unit within a learning structure framework.
 */
export interface CreateLearningUnitRequest {
  code: string;
  name: string;
  unitType: LearningUnitType | string;
  parentUnitId?: string | null;
  level?: string | null;
  description?: string | null;
  creditWeight?: number | null;
  notes?: string | null;
}

/**
 * Payload for updating a learning unit.
 */
export interface UpdateLearningUnitRequest {
  code: string;
  name: string;
  unitType: LearningUnitType | string;
  parentUnitId?: string | null;
  level?: string | null;
  description?: string | null;
  creditWeight?: number | null;
  notes?: string | null;
}

// ============================================================================
// Academic Requirement Models
// ============================================================================

/**
 * Academic requirement policy rule anchored to a learning structure.
 */
export interface AcademicRequirement {
  id: string;
  name: string;
  requirementScope: AcademicRequirementScope | string;
  learningStructureId: string;
  learningStructureName?: string | null;
  learningStructureVersion?: string | null;
  learningStructureStatus?: string | null;
  learningUnitId?: string | null;
  learningUnitCode?: string | null;
  learningUnitName?: string | null;
  level?: string | null;
  requirementType: AcademicRequirementType | string;
  description: string;
  version: string;
  effectiveDate: string;
  endDate?: string | null;
  status: AcademicRequirementStatus | string;
  approvedBy?: string | null;
  approvedDate?: string | null;
  supersedesId?: string | null;
  notes?: string | null;
  createdAt: string;
  createdBy: string;
  updatedAt?: string | null;
  updatedBy?: string | null;
}

/**
 * Payload for creating a new academic requirement under a learning structure.
 */
export interface CreateAcademicRequirementRequest {
  name: string;
  requirementScope: AcademicRequirementScope | string;
  learningUnitId?: string | null;
  level?: string | null;
  requirementType: AcademicRequirementType | string;
  description: string;
  version: string;
  effectiveDate: string;
  endDate?: string | null;
  notes?: string | null;
}

/**
 * Payload for updating an existing academic requirement.
 */
export interface UpdateAcademicRequirementRequest {
  name: string;
  description: string;
  notes?: string | null | undefined;
  requirementScope?: AcademicRequirementScope | string | null | undefined;
  learningUnitId?: string | null | undefined;
  level?: string | null | undefined;
  requirementType?: AcademicRequirementType | string | null | undefined;
  version?: string | null | undefined;
  effectiveDate?: string | null | undefined;
  endDate?: string | null | undefined;
}

// ============================================================================
// Filter Query Parameters
// ============================================================================

export interface LearningStructureFilters {
  academicPeriodId?: string | null | undefined;
  status?: LearningStructureStatus | string | null | undefined;
  search?: string | null | undefined;
  page?: number | undefined;
  pageSize?: number | undefined;
}

export interface LearningUnitFilters {
  unitType?: LearningUnitType | string | null | undefined;
  parentUnitId?: string | null | undefined;
  level?: string | null | undefined;
  search?: string | null | undefined;
}

export interface AcademicRequirementFilters {
  scope?: AcademicRequirementScope | string | null | undefined;
  type?: AcademicRequirementType | string | null | undefined;
}
