<script setup lang="ts">
import { computed } from 'vue';
import { RouterLink } from 'vue-router';
import {
  CalendarDays,
  UserPlus,
  UserCheck,
  Receipt,
  CreditCard,
  Building2,
  Calendar,
  ShieldCheck,
  CheckCircle2,
  ArrowRight,
  Sparkles,
  Server,
  Layers,
} from 'lucide-vue-next';
// eslint-disable-next-line no-restricted-imports -- ADR-FE-002: Pinia session store in @/app/stores
import { useAuthStore } from '@/app/stores/auth.store';
// eslint-disable-next-line no-restricted-imports -- ADR-FE-002: Pinia session store in @/app/stores
import { useAppContextStore } from '@/app/stores/appContext.store';
import PageHeader from '@/shared/components/layout/PageHeader.vue';
import ContentCard from '@/shared/components/layout/ContentCard.vue';
import StatusPill from '@/shared/components/StatusPill.vue';

const authStore = useAuthStore();
const appContextStore = useAppContextStore();

const greetingMessage = computed(() => {
  const hour = new Date().getHours();
  if (hour < 12) return 'Good morning';
  if (hour < 18) return 'Good afternoon';
  return 'Good evening';
});

const userDisplayName = computed(() => {
  return authStore.currentUser?.fullName || authStore.currentUser?.username || 'Administrator';
});

const userRoleBadge = computed(() => {
  if (authStore.currentUser?.roles && authStore.currentUser.roles.length > 0) {
    return authStore.currentUser.roles.join(', ');
  }
  return 'Administrator';
});

const activeCampusDisplay = computed(() => {
  if (appContextStore.activeCampusId === 'campus-east') {
    return 'East Campus (Kampus B)';
  }
  return 'Main Campus (Kampus A)';
});

const activeAcademicPeriodDisplay = computed(() => {
  return appContextStore.activeAcademicPeriodId || 'AY 2026/2027 (Active)';
});

interface OperationShortcut {
  id: string;
  title: string;
  description: string;
  to: string;
  icon: typeof CalendarDays;
  tag: string;
  permission?: string;
}

const operationsShortcuts: OperationShortcut[] = [
  {
    id: 'op-calendar',
    title: 'Academic Calendar & Periods',
    description: 'Configure academic years, semester terms, holiday exceptions, and census dates.',
    to: '/operations/calendar',
    icon: CalendarDays,
    tag: 'Operations Core',
    permission: 'Permissions.Operations.Calendar.Manage',
  },
];

interface DomainShortcut {
  id: string;
  domain: string;
  title: string;
  description: string;
  to: string;
  icon: typeof UserPlus;
  colorClass: string;
  permission?: string;
}

const domainWorkspaces: DomainShortcut[] = [
  {
    id: 'student-admission',
    domain: 'Students',
    title: 'Admission Work Queue',
    description: 'Review incoming applications, verify documents, and record admission decisions.',
    to: '/students/admission',
    icon: UserPlus,
    colorClass: 'text-teal-600 bg-teal-50',
    permission: 'Permissions.Students.Admission.View',
  },
  {
    id: 'student-enrollment',
    domain: 'Students',
    title: 'Student Enrollment Desk',
    description: 'Enroll accepted applicants and issue institutional identity records.',
    to: '/students/enrollment',
    icon: UserCheck,
    colorClass: 'text-teal-600 bg-teal-50',
    permission: 'Permissions.Students.Enrollment.Execute',
  },
  {
    id: 'finance-obligations',
    domain: 'Finance',
    title: 'Obligations & Billing',
    description: 'Manage tuition fee schedules, student fee obligations, and account balances.',
    to: '/finance/obligations',
    icon: Receipt,
    colorClass: 'text-emerald-600 bg-emerald-50',
    permission: 'Permissions.Finance.Obligations.View',
  },
  {
    id: 'finance-payments',
    domain: 'Finance',
    title: 'Payment Receipt Desk',
    description: 'Receive tuition payments, generate receipts, and balance ledger entries.',
    to: '/finance/payments',
    icon: CreditCard,
    colorClass: 'text-emerald-600 bg-emerald-50',
    permission: 'Permissions.Finance.Payments.Receive',
  },
];
</script>

<template>
  <div data-testid="dashboard-view" class="space-y-6">
    <!-- View Header -->
    <PageHeader
      title="Administrative Dashboard"
      subtitle="Institutional overview, active academic session context, and Stage 1 operational workspaces."
    >
      <template #actions>
        <div class="flex items-center gap-2">
          <StatusPill status="active" label="Session Active" dot size="sm" />
        </div>
      </template>
    </PageHeader>

    <!-- Top KPI Grid: Greeting, Context, System Status -->
    <div class="grid grid-cols-1 md:grid-cols-3 gap-5" data-testid="dashboard-summary-grid">
      <!-- 1. Session Greeting Card -->
      <ContentCard data-testid="session-greeting-card">
        <div class="flex items-start justify-between">
          <div class="space-y-1">
            <div class="flex items-center gap-1.5 text-xs font-medium text-teal-600">
              <Sparkles class="w-3.5 h-3.5" aria-hidden="true" />
              <span>Session Authenticated</span>
            </div>
            <h3 class="text-lg font-semibold text-slate-900" data-testid="session-greeting-title">
              {{ greetingMessage }}, {{ userDisplayName }}
            </h3>
            <p class="text-xs text-slate-500">
              Logged in as <span class="font-medium text-slate-700">{{ userRoleBadge }}</span>
            </p>
          </div>
          <div
            class="w-10 h-10 rounded-xl bg-teal-50 text-teal-700 flex items-center justify-center shrink-0"
          >
            <ShieldCheck class="w-5 h-5" aria-hidden="true" />
          </div>
        </div>
        <div
          class="mt-4 pt-3 border-t border-slate-100 flex items-center justify-between text-xs text-slate-500"
        >
          <span>User ID:</span>
          <span class="font-mono text-slate-700">{{
            authStore.currentUser?.userId || 'usr-local'
          }}</span>
        </div>
      </ContentCard>

      <!-- 2. Active Campus & Academic Period Indicator -->
      <ContentCard data-testid="active-context-card">
        <div class="flex items-start justify-between">
          <div class="space-y-1">
            <div class="flex items-center gap-1.5 text-xs font-medium text-sky-600">
              <Building2 class="w-3.5 h-3.5" aria-hidden="true" />
              <span>Active Operational Scope</span>
            </div>
            <h3 class="text-base font-semibold text-slate-900" data-testid="active-campus-label">
              {{ activeCampusDisplay }}
            </h3>
            <p
              class="text-xs text-slate-500 flex items-center gap-1.5 mt-0.5"
              data-testid="active-period-label"
            >
              <Calendar class="w-3.5 h-3.5 text-slate-400" aria-hidden="true" />
              <span>{{ activeAcademicPeriodDisplay }}</span>
            </p>
          </div>
          <div
            class="w-10 h-10 rounded-xl bg-sky-50 text-sky-700 flex items-center justify-center shrink-0"
          >
            <Layers class="w-5 h-5" aria-hidden="true" />
          </div>
        </div>
        <div class="mt-4 pt-3 border-t border-slate-100 flex items-center justify-between text-xs">
          <span class="text-slate-500">Institutional Context:</span>
          <span class="inline-flex items-center gap-1 font-medium text-emerald-600">
            <CheckCircle2 class="w-3.5 h-3.5" />
            <span>Synchronized</span>
          </span>
        </div>
      </ContentCard>

      <!-- 3. System Status Tile -->
      <ContentCard data-testid="system-status-card">
        <div class="flex items-start justify-between">
          <div class="space-y-1">
            <div class="flex items-center gap-1.5 text-xs font-medium text-emerald-600">
              <Server class="w-3.5 h-3.5" aria-hidden="true" />
              <span>System Infrastructure</span>
            </div>
            <h3
              class="text-lg font-semibold text-slate-900 flex items-center gap-2"
              data-testid="system-health-status"
            >
              <span>Operational</span>
              <StatusPill status="active" label="Healthy" dot size="sm" />
            </h3>
            <p class="text-xs text-slate-500">Backend Minimal APIs &amp; Dapper connected</p>
          </div>
          <div
            class="w-10 h-10 rounded-xl bg-emerald-50 text-emerald-700 flex items-center justify-center shrink-0"
          >
            <CheckCircle2 class="w-5 h-5" aria-hidden="true" />
          </div>
        </div>
        <div
          class="mt-4 pt-3 border-t border-slate-100 flex items-center justify-between text-xs text-slate-500"
        >
          <span>Client Version:</span>
          <span class="font-mono text-slate-700">v0.1.0 (Stage 1 Core)</span>
        </div>
      </ContentCard>
    </div>

    <!-- Operations Shortcuts Section -->
    <div class="space-y-3" data-testid="operations-shortcuts-section">
      <div class="flex items-center justify-between">
        <div>
          <h3 class="text-base font-semibold text-slate-900">Operations Workspace</h3>
          <p class="text-xs text-slate-500">
            Direct navigation to academic calendar, scheduling cycles, and institutional milestones.
          </p>
        </div>
      </div>

      <div
        class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4"
        data-testid="operations-shortcuts-grid"
      >
        <RouterLink
          v-for="shortcut in operationsShortcuts"
          :key="shortcut.id"
          :to="shortcut.to"
          :data-testid="shortcut.id"
          class="group block p-4 bg-white border border-slate-200 rounded-xl shadow-xs hover:border-teal-500 hover:shadow-sm transition-all duration-150"
        >
          <div class="flex items-start justify-between">
            <div
              class="w-10 h-10 rounded-lg bg-teal-50 text-teal-700 flex items-center justify-center group-hover:bg-teal-600 group-hover:text-white transition-colors"
            >
              <component :is="shortcut.icon" class="w-5 h-5" aria-hidden="true" />
            </div>
            <span class="text-xs font-medium px-2 py-0.5 rounded-full bg-slate-100 text-slate-600">
              {{ shortcut.tag }}
            </span>
          </div>

          <div class="mt-3">
            <h4
              class="text-sm font-semibold text-slate-900 group-hover:text-teal-700 transition-colors flex items-center gap-1.5"
            >
              <span>{{ shortcut.title }}</span>
              <ArrowRight
                class="w-3.5 h-3.5 text-slate-400 group-hover:text-teal-600 group-hover:translate-x-0.5 transition-transform"
              />
            </h4>
            <p class="mt-1 text-xs text-slate-500 line-clamp-2">
              {{ shortcut.description }}
            </p>
          </div>
        </RouterLink>
      </div>
    </div>

    <!-- Active Stage 1 Domain Workspaces -->
    <div class="space-y-3" data-testid="domain-workspaces-section">
      <div class="flex items-center justify-between">
        <div>
          <h3 class="text-base font-semibold text-slate-900">Stage 1 Core Workspaces</h3>
          <p class="text-xs text-slate-500">
            Active work queues and desks across Students and Finance domains.
          </p>
        </div>
      </div>

      <div
        class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4"
        data-testid="domain-workspaces-grid"
      >
        <RouterLink
          v-for="desk in domainWorkspaces"
          :key="desk.id"
          :to="desk.to"
          :data-testid="desk.id"
          class="group block p-4 bg-white border border-slate-200 rounded-xl shadow-xs hover:border-slate-300 hover:shadow-sm transition-all duration-150"
        >
          <div class="flex items-center justify-between">
            <div :class="['w-9 h-9 rounded-lg flex items-center justify-center', desk.colorClass]">
              <component :is="desk.icon" class="w-4 h-4" aria-hidden="true" />
            </div>
            <span class="text-2xs font-semibold uppercase tracking-wider text-slate-400">
              {{ desk.domain }}
            </span>
          </div>

          <div class="mt-3">
            <h4
              class="text-sm font-semibold text-slate-900 group-hover:text-teal-700 transition-colors flex items-center gap-1"
            >
              <span>{{ desk.title }}</span>
              <ArrowRight
                class="w-3.5 h-3.5 text-slate-400 group-hover:text-teal-600 group-hover:translate-x-0.5 transition-transform"
              />
            </h4>
            <p class="mt-1 text-xs text-slate-500 line-clamp-2">
              {{ desk.description }}
            </p>
          </div>
        </RouterLink>
      </div>
    </div>
  </div>
</template>
