import type { RouteRecordRaw } from 'vue-router';
import { RouterView } from 'vue-router';

/**
 * Authoritative hierarchical route configuration for Elsasa SIS V3.
 * Organized by Stage 1 active domains (Operations, Students, Finance) wrapped in
 * persistent layout shells and guarded by client permission policies.
 */
export const routes: RouteRecordRaw[] = [
  {
    path: '/',
    redirect: '/dashboard',
  },
  {
    path: '/auth',
    component: () => import('@/app/layouts/AuthLayout.vue'),
    children: [
      {
        path: 'login',
        name: 'auth.login',
        component: () => import('@/features/auth/views/LoginView.vue'),
        meta: { requiresAuth: false, title: 'Sign In — Elsasa SIS' },
      },
    ],
  },
  // PUBLIC SELF-SERVICE ADMISSION PORTAL (SCR-SELF-01)
  {
    path: '/admission',
    component: () => import('@/app/layouts/AuthLayout.vue'),
    children: [
      {
        path: 'apply',
        name: 'admission.apply',
        component: () =>
          import('@/features/students/admission/views/AdmissionPortalView.vue'),
        meta: { requiresAuth: false, title: 'Admission Application Portal', wide: true },
      },
      {
        path: 'status',
        name: 'admission.status',
        component: () =>
          import('@/features/students/admission/views/ApplicationTrackerView.vue'),
        meta: { requiresAuth: false, title: 'Application Status Tracker' },
      },
    ],
  },
  {
    path: '/',
    component: () => import('@/app/layouts/AppLayout.vue'),
    meta: { requiresAuth: true },
    children: [
      {
        path: 'dashboard',
        name: 'dashboard',
        component: () => import('@/features/dashboard/views/DashboardView.vue'),
        meta: { title: 'Dashboard' },
      },
      // STAGE 2: ACADEMICS DOMAIN (SCR-ACD-03, SCR-ACD-05, SCR-ACD-01, SCR-ACD-02, SCR-ACD-06, SCR-ACD-09)
      {
        path: 'academics',
        component: RouterView,
        children: [
          {
            path: 'curriculum',
            name: 'curriculum-workspace',
            component: () =>
              import('@/features/academics/curriculum/views/CurriculumWorkspaceView.vue'),
            meta: {
              requiresAuth: true,
              permission: 'academics.curriculum.view',
              title: 'Curriculum & Learning Structures',
            },
          },
          {
            path: 'learning-plans',
            name: 'academics.learning-plans',
            component: () =>
              import(
                '@/features/academics/learning-plans/views/LearningPlanWorkspaceView.vue'
              ),
            meta: {
              requiresAuth: true,
              permission: 'academics:learning-plans:view',
              title: 'Learning Plans',
            },
          },
          {
            path: 'participation',
            name: 'academics.participation',
            component: () =>
              import(
                '@/features/academics/participation/views/ParticipationWorkbenchView.vue'
              ),
            meta: {
              requiresAuth: true,
              permission: 'academics.participations.view',
              title: 'Participation & Rosters',
            },
          },
          {
            path: 'sessions',
            name: 'academics.sessions',
            component: () =>
              import('@/features/academics/sessions/views/ClassSessionWorkbenchView.vue'),
            meta: {
              requiresAuth: true,
              permission: 'academics.activities.view',
              title: 'Class Sessions',
            },
          },
          {
            path: 'coursework',
            name: 'academics.coursework',
            component: () =>
              import('@/features/academics/coursework/views/CourseworkWorkbenchView.vue'),
            meta: {
              requiresAuth: true,
              permission: 'academics.assignments.view',
              title: 'Coursework',
            },
          },
          {
            path: 'assessment',
            name: 'academics.assessment',
            component: () =>
              import('@/features/academics/assessment/views/AssessmentWorkbenchView.vue'),
            meta: {
              requiresAuth: true,
              permission: 'academics.assessments.view',
              title: 'Assessment & Scoring',
            },
          },
          // STAGE 2: SCR-ACD-06 Academic Intervention Workbench (ACD-WF-10, UC-ACD-11, P8-S21, P8-S25)
          {
            path: 'interventions',
            name: 'academics.interventions',
            component: () =>
              import(
                '@/features/academics/interventions/views/AcademicInterventionWorkbenchView.vue'
              ),
            meta: {
              requiresAuth: true,
              permission: 'academics.interventions.view',
              title: 'Academic Interventions',
            },
          },
          // STAGE 2: SCR-ACD-09 Cohort Academic Review Workbench (ACD-WF-13, UC-ACD-17, UC-ACD-21, P8-S22, P8-S25)
          {
            path: 'cohort-review',
            name: 'academics.cohort-review',
            component: () =>
              import(
                '@/features/academics/cohort-review/views/CohortAcademicReviewView.vue'
              ),
            meta: {
              requiresAuth: true,
              permission: 'academics.standings.view',
              title: 'Cohort Academic Review',
            },
          },
        ],
      },
      // STAGE 2: TEACHING DOMAIN (SCR-TCH-01, P8-S28)
      {
        path: 'teaching',
        component: RouterView,
        redirect: '/teaching/faculty',
        children: [
          {
            path: 'faculty',
            name: 'teaching-faculty',
            component: () =>
              import('@/features/teaching/faculty/views/FacultyResourceWorkspaceView.vue'),
            meta: {
              requiresAuth: true,
              permission: 'Permissions.Teaching.Faculty.View',
              title: 'Faculty Resources — Elsasa SIS',
              breadcrumb: 'Faculty Resources',
            },
          },
        ],
      },
      // STAGE 1: STUDENTS DOMAIN
      {
        path: 'students',
        component: RouterView,
        children: [
          {
            path: 'admission',
            name: 'students.admission.queue',
            component: () =>
              import('@/features/students/admission/views/StudentIntakeWorkbenchView.vue'),
            meta: {
              permission: 'Permissions.Students.Admission.View',
              title: 'Admission Work Queue',
            },
          },
          {
            path: 'admission/apply',
            redirect: '/admission/apply',
          },
          {
            path: 'admission/:id',
            name: 'students.admission.detail',
            component: () =>
              import('@/features/students/admission/views/StudentIntakeWorkbenchView.vue'),
            meta: {
              permission: 'Permissions.Students.Admission.Review',
              title: 'Application Review',
            },
          },
          {
            path: 'enrollment',
            redirect: '/students/records',
          },
          {
            path: 'records',
            name: 'students.records.desk',
            component: () =>
              import('@/features/students/records/views/StudentRecordsDeskView.vue'),
            meta: {
              permission: 'Permissions.Students.Records.View',
              title: 'Student Records Desk',
            },
          },
          {
            path: 'records/:id',
            name: 'students.records.detail',
            component: () =>
              import('@/features/students/records/views/StudentRecordsDeskView.vue'),
            meta: {
              permission: 'Permissions.Students.Records.View',
              title: 'Student Records Desk',
            },
          },
          // STAGE 2: SCR-STU-03 Student Lifecycle Workbench (STD-WF-08, P8-S24)
          {
            path: 'lifecycle',
            name: 'students.lifecycle.workbench',
            component: () =>
              import('@/features/students/lifecycle/views/StudentLifecycleWorkbenchView.vue'),
            meta: {
              permission: 'Permissions.Students.Records.View',
              title: 'Student Lifecycle Workbench',
            },
          },
        ],
      },
      // STAGE 1: FINANCE DOMAIN
      {
        path: 'finance',
        component: RouterView,
        children: [
          {
            path: 'policies',
            name: 'finance.policies.workspace',
            component: () =>
              import('@/features/finance/policies/views/FinancialPolicyWorkspaceView.vue'),
            meta: {
              permission: 'Permissions.Finance.Policies.View',
              title: 'Financial Policy Workspace',
            },
          },
          {
            path: 'billing-cycles',
            name: 'finance.billing-cycles.workspace',
            component: () =>
              import('@/features/finance/billing/views/BillingCycleWorkspaceView.vue'),
            meta: {
              permission: 'Permissions.Finance.Obligations.Assess',
              title: 'Billing Cycle Workspace',
            },
          },
          {
            path: 'obligations',
            name: 'finance.obligations.list',
            component: () => import('@/features/finance/obligations/views/ObligationsListView.vue'),
            meta: {
              permission: 'Permissions.Finance.Obligations.View',
              title: 'Student Obligations & Billing',
            },
          },
          {
            path: 'accounts',
            name: 'finance.accounts.workbench',
            component: () =>
              import('@/features/finance/accounts/views/StudentAccountsWorkbenchView.vue'),
            meta: {
              permission: 'Permissions.Finance.Accounts.View',
              title: 'Student Accounts Workbench',
            },
          },
          {
            path: 'accounts/:studentId',
            name: 'finance.accounts.detail',
            component: () =>
              import('@/features/finance/accounts/views/StudentAccountsWorkbenchView.vue'),
            meta: {
              permission: 'Permissions.Finance.Accounts.View',
              title: 'Student Accounts Workbench',
            },
          },
          {
            path: 'payments',
            name: 'finance.payments.queue',
            component: () =>
              import('@/features/finance/payments/views/PaymentVerificationQueueView.vue'),
            meta: {
              permission: 'Permissions.Finance.Payments.View',
              title: 'Payment Verification Queue',
            },
          },
        ],
      },
      // STAGE 1 & 2: PORTAL DOMAIN (SCR-SELF-02, SCR-SELF-03)
      {
        path: 'portal',
        component: RouterView,
        children: [
          {
            path: 'family',
            name: 'portal.family',
            component: () => import('@/features/portal/FamilyTasksView.vue'),
            meta: {
              title: 'Family Portal',
            },
          },
          {
            path: 'learning',
            name: 'portal.learning',
            component: () =>
              import('@/features/portal/learning/views/StudentLearningWorkspaceView.vue'),
            meta: {
              requiresAuth: true,
              roles: ['Student'],
              title: 'My Learning & Coursework',
            },
          },
        ],
      },
      // STAGE 1: OPERATIONS DOMAIN
      {
        path: 'operations',
        component: RouterView,
        children: [
          {
            path: 'calendar',
            name: 'operations.calendar.workspace',
            component: () =>
              import('@/features/operations/calendar/views/CalendarWorkspaceView.vue'),
            meta: {
              permission: 'Permissions.Operations.Calendar.Manage',
              title: 'Calendar & Periods Workspace',
            },
          },
        ],
      },
      // Error Routes
      {
        path: '403',
        name: 'forbidden',
        component: () => import('@/shared/views/ForbiddenView.vue'),
        meta: { title: 'Access Forbidden' },
      },
      {
        path: ':pathMatch(.*)*',
        name: 'not-found',
        component: () => import('@/shared/views/NotFoundView.vue'),
        meta: { title: 'Page Not Found' },
      },
    ],
  },
];
