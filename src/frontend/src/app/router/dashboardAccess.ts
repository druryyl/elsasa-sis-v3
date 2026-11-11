export interface AnyAccessRule {
  permission?: string | undefined;
  permissions?: string[] | undefined;
  roles?: string[] | undefined;
  excludedRoles?: string[] | undefined;
}

export interface AccessActor {
  roles: string[];
  permissions: string[];
  hasPermission: (permission: string) => boolean;
  hasAnyRole: (roles: string[]) => boolean;
}

export type Level2DashboardDomain =
  | 'students'
  | 'academics'
  | 'finance'
  | 'teaching'
  | 'operations';

const SELF_SERVICE_ROLES: string[] = ['Student', 'Parent'];

/**
 * Authoritative Level 2 domain dashboard access rules aligned with Architecture TD-05
 * and backend `GetExecutiveDashboardHandler` / Level 2 endpoint authorization policies.
 */
export const LEVEL2_DASHBOARD_ACCESS: Record<
  Level2DashboardDomain,
  Required<AnyAccessRule>
> = {
  students: {
    permission: 'Permissions.Students.Admission.View',
    permissions: [
      'Permissions.Students.Admission.View',
      'Permissions.Students.Records.View',
    ],
    roles: [
      'SuperAdmin',
      'Admin',
      'AdmissionOfficer',
      'Admission Officer',
      'StudentAdminOfficer',
      'Student Administration Officer',
      'SchoolLeadership',
      'School Leadership',
    ],
    excludedRoles: SELF_SERVICE_ROLES,
  },
  academics: {
    permission: 'academics.participations.view',
    permissions: [
      'academics.participations.view',
      'Permissions.Academics.Participations.View',
      'Permissions.Academics.Coursework.View',
    ],
    roles: [
      'SuperAdmin',
      'Admin',
      'AcademicAdmin',
      'AcademicCoordinator',
      'Academic Coordinator',
      'AcademicAuthority',
      'Academic Authority',
      'Teacher',
    ],
    excludedRoles: SELF_SERVICE_ROLES,
  },
  finance: {
    permission: 'Permissions.Finance.Obligations.View',
    permissions: ['Permissions.Finance.Obligations.View'],
    roles: [
      'SuperAdmin',
      'Admin',
      'FinanceAuthority',
      'Finance Authority',
      'FinanceOfficer',
      'Finance Officer',
      'FinanceStaff',
      'Finance Staff',
    ],
    excludedRoles: SELF_SERVICE_ROLES,
  },
  teaching: {
    permission: 'Permissions.Teaching.Faculty.View',
    permissions: [
      'Permissions.Teaching.Faculty.View',
      'teaching.faculty.view',
      'Permissions.Teaching.Workload.View',
      'teaching.workload.view',
    ],
    roles: [
      'SuperAdmin',
      'Admin',
      'AcademicCoordinator',
      'Academic Coordinator',
      'AcademicAuthority',
      'Academic Authority',
      'TeachingAuthority',
      'Teaching Authority',
      'TeachingCoordinator',
      'Teaching Coordinator',
      'Teacher',
    ],
    excludedRoles: SELF_SERVICE_ROLES,
  },
  operations: {
    permission: 'Permissions.Operations.Calendar.Manage',
    permissions: [
      'Permissions.Operations.Calendar.Manage',
      'Permissions.Operations.Calendar.View',
    ],
    roles: [
      'SuperAdmin',
      'Admin',
      'OperationsAuthority',
      'Operations Authority',
      'AcademicCoordinator',
      'Academic Coordinator',
      'AcademicAdmin',
    ],
    excludedRoles: SELF_SERVICE_ROLES,
  },
};

/**
 * Evaluates OR-based authorization for a route or navigation item (`authMode: 'any'`).
 * Grants access when the actor holds SuperAdmin/'*', an allowed role, or an allowed
 * permission while blocking self-service roles (Student/Parent) from staff dashboards.
 */
export function isAuthorizedForAnyRule(
  rule: AnyAccessRule,
  actor: AccessActor,
  checkPermissionFn?: (permission: string) => boolean,
): boolean {
  if (actor.roles.includes('SuperAdmin') || actor.permissions.includes('*')) {
    return true;
  }

  const allPermissions = Array.from(
    new Set(
      [
        ...(rule.permission ? [rule.permission] : []),
        ...(rule.permissions ?? []),
      ].filter(Boolean),
    ),
  );
  const allowedRoles = rule.roles ?? [];

  if (allPermissions.length === 0 && allowedRoles.length === 0) {
    if (rule.excludedRoles?.some((role) => actor.roles.includes(role))) {
      return false;
    }
    return true;
  }

  if (allowedRoles.length > 0 && actor.hasAnyRole(allowedRoles)) {
    return true;
  }

  // Self-service roles (e.g. Student/Parent) may hold view permissions for their own portal
  // data and must not gain access to staff Level 2 dashboards via permission claims alone.
  if (rule.excludedRoles?.some((role) => actor.roles.includes(role))) {
    return false;
  }

  const checkPerm = checkPermissionFn ?? ((perm: string) => actor.hasPermission(perm));
  return allPermissions.some((perm) => checkPerm(perm));
}
