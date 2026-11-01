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
      // STAGE 1: STUDENTS DOMAIN
      {
        path: 'students',
        component: RouterView,
        children: [
          {
            path: 'admission',
            name: 'students.admission.queue',
            component: () => import('@/features/students/admission/views/AdmissionQueueView.vue'),
            meta: {
              permission: 'Permissions.Students.Admission.View',
              title: 'Admission Work Queue',
            },
          },
          {
            path: 'admission/:id',
            name: 'students.admission.detail',
            component: () =>
              import('@/features/students/admission/views/ApplicationDetailView.vue'),
            meta: {
              permission: 'Permissions.Students.Admission.Review',
              title: 'Application Review',
            },
          },
          {
            path: 'enrollment',
            name: 'students.enrollment.desk',
            component: () => import('@/features/students/enrollment/views/EnrollmentDeskView.vue'),
            meta: {
              permission: 'Permissions.Students.Enrollment.Execute',
              title: 'Student Enrollment Desk',
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
            path: 'obligations',
            name: 'finance.obligations.list',
            component: () => import('@/features/finance/obligations/views/ObligationsListView.vue'),
            meta: {
              permission: 'Permissions.Finance.Obligations.View',
              title: 'Student Obligations & Billing',
            },
          },
          {
            path: 'payments',
            name: 'finance.payments.desk',
            component: () => import('@/features/finance/payments/views/PaymentDeskView.vue'),
            meta: {
              permission: 'Permissions.Finance.Payments.Receive',
              title: 'Payment Receipt Desk',
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
