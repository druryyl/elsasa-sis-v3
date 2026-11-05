<script setup lang="ts">
import { computed, ref } from 'vue';
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
  BookOpen,
  Users,
  FileEdit,
  Clock,
  Compass,
  Zap,
} from 'lucide-vue-next';
 
import { useAuthStore } from '@/app/stores/auth.store';
 
import { useAppContextStore } from '@/app/stores/appContext.store';
import PageHeader from '@/shared/components/layout/PageHeader.vue';
import ContentCard from '@/shared/components/layout/ContentCard.vue';
import StatusPill from '@/shared/components/StatusPill.vue';

const authStore = useAuthStore();
const appContextStore = useAppContextStore();

// Interactive domain filter state
type DomainFilter = 'all' | 'operations' | 'academics' | 'students' | 'finance';
const selectedFilter = ref<DomainFilter>('all');

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
  badgeColor: string;
  accentBar: string;
  iconTheme: string;
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
    badgeColor: 'bg-amber-50 text-amber-800 border-amber-200/80',
    accentBar: 'from-amber-400 via-orange-400 to-amber-500',
    iconTheme: 'bg-gradient-to-br from-amber-50 to-orange-100 text-amber-700 border-amber-200/80 group-hover:from-amber-500 group-hover:to-orange-500 group-hover:text-white',
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
  accentBar: string;
  domainBadgeClass: string;
  iconThemeClass: string;
  hoverGlowClass: string;
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
    accentBar: 'from-teal-400 via-cyan-400 to-teal-500',
    domainBadgeClass: 'bg-teal-50 text-teal-800 border-teal-200/80',
    iconThemeClass: 'bg-gradient-to-br from-teal-50 to-cyan-100 text-teal-700 border-teal-200/80 group-hover:from-teal-600 group-hover:to-cyan-600 group-hover:text-white',
    hoverGlowClass: 'hover:border-teal-300 hover:shadow-teal-500/10',
    permission: 'Permissions.Students.Admission.View',
  },
  {
    id: 'student-enrollment',
    domain: 'Students',
    title: 'Student Enrollment Desk',
    description: 'Enroll accepted applicants and issue institutional identity records.',
    to: '/students/enrollment',
    icon: UserCheck,
    accentBar: 'from-cyan-400 via-blue-500 to-indigo-500',
    domainBadgeClass: 'bg-blue-50 text-blue-800 border-blue-200/80',
    iconThemeClass: 'bg-gradient-to-br from-blue-50 to-indigo-100 text-blue-700 border-blue-200/80 group-hover:from-blue-600 group-hover:to-indigo-600 group-hover:text-white',
    hoverGlowClass: 'hover:border-blue-300 hover:shadow-blue-500/10',
    permission: 'Permissions.Students.Enrollment.Execute',
  },
  {
    id: 'finance-obligations',
    domain: 'Finance',
    title: 'Obligations & Billing',
    description: 'Manage tuition fee schedules, student fee obligations, and account balances.',
    to: '/finance/obligations',
    icon: Receipt,
    accentBar: 'from-emerald-400 via-teal-400 to-emerald-500',
    domainBadgeClass: 'bg-emerald-50 text-emerald-800 border-emerald-200/80',
    iconThemeClass: 'bg-gradient-to-br from-emerald-50 to-teal-100 text-emerald-700 border-emerald-200/80 group-hover:from-emerald-600 group-hover:to-teal-600 group-hover:text-white',
    hoverGlowClass: 'hover:border-emerald-300 hover:shadow-emerald-500/10',
    permission: 'Permissions.Finance.Obligations.View',
  },
  {
    id: 'finance-payments',
    domain: 'Finance',
    title: 'Payment Receipt Desk',
    description: 'Receive tuition payments, generate receipts, and balance ledger entries.',
    to: '/finance/payments',
    icon: CreditCard,
    accentBar: 'from-emerald-400 via-green-400 to-teal-500',
    domainBadgeClass: 'bg-green-50 text-green-800 border-green-200/80',
    iconThemeClass: 'bg-gradient-to-br from-green-50 to-emerald-100 text-emerald-700 border-emerald-200/80 group-hover:from-emerald-600 group-hover:to-green-600 group-hover:text-white',
    hoverGlowClass: 'hover:border-green-300 hover:shadow-green-500/10',
    permission: 'Permissions.Finance.Payments.Receive',
  },
];

interface AcademicShortcut {
  id: string;
  domain: string;
  title: string;
  description: string;
  to: string;
  icon: typeof BookOpen;
  accentBar: string;
  domainBadgeClass: string;
  iconThemeClass: string;
  hoverGlowClass: string;
  permission?: string;
}

const academicWorkspaces: AcademicShortcut[] = [
  {
    id: 'acad-curriculum',
    domain: 'Academics',
    title: 'Curriculum & Learning Structures',
    description: 'Educational programs, learning units, credit rules, and graduation frameworks.',
    to: '/academics/curriculum',
    icon: BookOpen,
    accentBar: 'from-indigo-400 via-violet-400 to-purple-500',
    domainBadgeClass: 'bg-indigo-50 text-indigo-800 border-indigo-200/80',
    iconThemeClass: 'bg-gradient-to-br from-indigo-50 to-violet-100 text-indigo-700 border-indigo-200/80 group-hover:from-indigo-600 group-hover:to-violet-600 group-hover:text-white',
    hoverGlowClass: 'hover:border-indigo-300 hover:shadow-indigo-500/10',
    permission: 'academics.curriculum.view',
  },
  {
    id: 'acad-participation',
    domain: 'Academics',
    title: 'Participation & Rosters',
    description: 'Student enrollments, active cohort rosters, section allocations, and history.',
    to: '/academics/participation',
    icon: Users,
    accentBar: 'from-violet-400 via-purple-400 to-fuchsia-500',
    domainBadgeClass: 'bg-violet-50 text-violet-800 border-violet-200/80',
    iconThemeClass: 'bg-gradient-to-br from-violet-50 to-purple-100 text-violet-700 border-violet-200/80 group-hover:from-violet-600 group-hover:to-purple-600 group-hover:text-white',
    hoverGlowClass: 'hover:border-violet-300 hover:shadow-violet-500/10',
    permission: 'academics.participations.view',
  },
  {
    id: 'acad-sessions',
    domain: 'Academics',
    title: 'Class Sessions Workbench',
    description: 'Session scheduling, real-time attendance logging, instructor tracking, and notes.',
    to: '/academics/sessions',
    icon: Calendar,
    accentBar: 'from-purple-400 via-fuchsia-400 to-pink-500',
    domainBadgeClass: 'bg-purple-50 text-purple-800 border-purple-200/80',
    iconThemeClass: 'bg-gradient-to-br from-purple-50 to-fuchsia-100 text-purple-700 border-purple-200/80 group-hover:from-purple-600 group-hover:to-fuchsia-600 group-hover:text-white',
    hoverGlowClass: 'hover:border-purple-300 hover:shadow-purple-500/10',
    permission: 'academics.activities.view',
  },
  {
    id: 'acad-coursework',
    domain: 'Academics',
    title: 'Coursework & Grading',
    description: 'Assignments, student submissions, evaluation rubrics, and feedback dispatch.',
    to: '/academics/coursework',
    icon: FileEdit,
    accentBar: 'from-fuchsia-400 via-pink-400 to-rose-500',
    domainBadgeClass: 'bg-fuchsia-50 text-fuchsia-800 border-fuchsia-200/80',
    iconThemeClass: 'bg-gradient-to-br from-fuchsia-50 to-pink-100 text-fuchsia-700 border-fuchsia-200/80 group-hover:from-fuchsia-600 group-hover:to-pink-600 group-hover:text-white',
    hoverGlowClass: 'hover:border-fuchsia-300 hover:shadow-fuchsia-500/10',
    permission: 'academics.assignments.view',
  },
];
</script>

<template>
  <div data-testid="dashboard-view" class="space-y-6">
    <!-- View Header -->
    <PageHeader
      title="Administrative Dashboard"
      subtitle="Institutional overview, active academic session context, and operational workspaces."
    >
      <template #actions>
        <div class="flex items-center gap-2">
          <StatusPill status="active" label="Session Active" dot size="sm" />
          <div
            class="hidden sm:inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-gradient-to-r from-teal-50 to-emerald-50 text-teal-800 border border-teal-200/80 shadow-2xs"
          >
            <Sparkles class="w-3.5 h-3.5 text-teal-600" />
            <span>Elsasa SIS v3</span>
          </div>
        </div>
      </template>
    </PageHeader>

    <!-- Colorful Institutional Hero Banner -->
    <div
      class="relative overflow-hidden rounded-2xl bg-gradient-to-r from-slate-900 via-slate-800 to-teal-950 p-6 sm:p-7 text-white shadow-md border border-slate-700/60"
    >
      <!-- Ambient decorative gradient glows -->
      <div
        class="absolute -right-12 -top-12 w-64 h-64 rounded-full bg-teal-500/15 blur-3xl pointer-events-none"
        aria-hidden="true"
      />
      <div
        class="absolute right-48 -bottom-10 w-64 h-64 rounded-full bg-indigo-500/15 blur-3xl pointer-events-none"
        aria-hidden="true"
      />

      <div class="relative z-10 flex flex-col md:flex-row md:items-center justify-between gap-6">
        <div class="space-y-2">
          <div class="flex items-center gap-2">
            <span
              class="inline-flex items-center gap-1.5 px-3 py-0.5 rounded-full text-xs font-medium bg-teal-500/20 text-teal-300 border border-teal-500/30 backdrop-blur-xs"
            >
              <Sparkles class="w-3 h-3 text-teal-300" />
              <span>Campus Management Hub</span>
            </span>
            <span class="text-xs text-slate-400">|</span>
            <span class="text-xs text-slate-300 flex items-center gap-1">
              <Clock class="w-3 h-3 text-slate-400" />
              <span>Active Term Session</span>
            </span>
          </div>
          <h2 class="text-2xl sm:text-3xl font-bold tracking-tight text-white">
            Welcome to Elsasa Institutional Control
          </h2>
          <p class="text-sm text-slate-300 max-w-2xl leading-relaxed">
            Coordinated management of academic curricula, student enrollment pipelines, and
            institutional finances across all registered campus facilities.
          </p>
        </div>

        <!-- Quick Access Action Launchers -->
        <div class="flex flex-wrap items-center gap-2.5 shrink-0">
          <RouterLink
            to="/admission/apply"
            class="inline-flex items-center gap-2 px-3.5 py-2 rounded-xl text-xs font-semibold bg-gradient-to-r from-teal-500 to-emerald-500 text-white shadow-sm hover:from-teal-400 hover:to-emerald-400 hover:shadow-teal-500/20 hover:scale-[1.02] active:scale-[0.98] transition-all"
          >
            <UserPlus class="w-4 h-4" />
            <span>New Admission</span>
          </RouterLink>
          <RouterLink
            to="/operations/calendar"
            class="inline-flex items-center gap-2 px-3.5 py-2 rounded-xl text-xs font-semibold bg-white/10 text-white border border-white/20 hover:bg-white/20 hover:scale-[1.02] active:scale-[0.98] backdrop-blur-xs transition-all"
          >
            <Calendar class="w-4 h-4 text-amber-300" />
            <span>View Calendar</span>
          </RouterLink>
          <RouterLink
            to="/finance/payments"
            class="inline-flex items-center gap-2 px-3.5 py-2 rounded-xl text-xs font-semibold bg-white/10 text-white border border-white/20 hover:bg-white/20 hover:scale-[1.02] active:scale-[0.98] backdrop-blur-xs transition-all"
          >
            <CreditCard class="w-4 h-4 text-emerald-300" />
            <span>Receive Payment</span>
          </RouterLink>
        </div>
      </div>
    </div>

    <!-- Top KPI Grid: Greeting, Context, System Status -->
    <div class="grid grid-cols-1 md:grid-cols-3 gap-5" data-testid="dashboard-summary-grid">
      <!-- 1. Session Greeting Card -->
      <ContentCard
        data-testid="session-greeting-card"
        class="relative overflow-hidden border-teal-200/70 hover:border-teal-400 hover:shadow-md hover:shadow-teal-500/5 transition-all duration-200"
      >
        <!-- Top color accent stripe -->
        <div class="absolute top-0 left-0 right-0 h-1 bg-gradient-to-r from-teal-500 via-emerald-400 to-teal-600" />
        
        <div class="flex items-start justify-between">
          <div class="space-y-1.5">
            <div class="inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-xs font-semibold bg-teal-50 text-teal-700 border border-teal-200/60">
              <Sparkles class="w-3.5 h-3.5 text-teal-600" aria-hidden="true" />
              <span>Session Authenticated</span>
            </div>
            <h3 class="text-lg font-bold text-slate-900 tracking-tight" data-testid="session-greeting-title">
              {{ greetingMessage }}, {{ userDisplayName }}
            </h3>
            <p class="text-xs text-slate-500">
              Logged in as <span class="font-semibold text-teal-800 bg-teal-50/80 px-2 py-0.5 rounded-md border border-teal-200/50">{{ userRoleBadge }}</span>
            </p>
          </div>
          <div
            class="w-11 h-11 rounded-xl bg-gradient-to-br from-teal-500 to-emerald-600 text-white flex items-center justify-center shrink-0 shadow-sm shadow-teal-500/20"
          >
            <ShieldCheck class="w-5 h-5" aria-hidden="true" />
          </div>
        </div>
        <div
          class="mt-4 pt-3 border-t border-slate-100 flex items-center justify-between text-xs text-slate-500"
        >
          <span class="text-slate-400 font-medium">User ID:</span>
          <span class="font-mono text-slate-700 font-semibold bg-slate-50 px-2 py-0.5 rounded border border-slate-200/60">{{
            authStore.currentUser?.userId || 'usr-local'
          }}</span>
        </div>
      </ContentCard>

      <!-- 2. Active Campus & Academic Period Indicator -->
      <ContentCard
        data-testid="active-context-card"
        class="relative overflow-hidden border-sky-200/70 hover:border-sky-400 hover:shadow-md hover:shadow-sky-500/5 transition-all duration-200"
      >
        <!-- Top color accent stripe -->
        <div class="absolute top-0 left-0 right-0 h-1 bg-gradient-to-r from-sky-500 via-blue-500 to-indigo-500" />

        <div class="flex items-start justify-between">
          <div class="space-y-1.5">
            <div class="inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-xs font-semibold bg-sky-50 text-sky-700 border border-sky-200/60">
              <Building2 class="w-3.5 h-3.5 text-sky-600" aria-hidden="true" />
              <span>Active Operational Scope</span>
            </div>
            <h3 class="text-base font-bold text-slate-900 tracking-tight" data-testid="active-campus-label">
              {{ activeCampusDisplay }}
            </h3>
            <p
              class="text-xs text-slate-600 font-medium flex items-center gap-1.5 mt-0.5"
              data-testid="active-period-label"
            >
              <Calendar class="w-3.5 h-3.5 text-sky-500" aria-hidden="true" />
              <span>{{ activeAcademicPeriodDisplay }}</span>
            </p>
          </div>
          <div
            class="w-11 h-11 rounded-xl bg-gradient-to-br from-sky-500 to-indigo-600 text-white flex items-center justify-center shrink-0 shadow-sm shadow-sky-500/20"
          >
            <Layers class="w-5 h-5" aria-hidden="true" />
          </div>
        </div>
        <div class="mt-4 pt-3 border-t border-slate-100 flex items-center justify-between text-xs">
          <span class="text-slate-400 font-medium">Institutional Context:</span>
          <span class="inline-flex items-center gap-1 font-semibold text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-full border border-emerald-200/60">
            <CheckCircle2 class="w-3.5 h-3.5 text-emerald-600" />
            <span>Synchronized</span>
          </span>
        </div>
      </ContentCard>

      <!-- 3. System Status Tile -->
      <ContentCard
        data-testid="system-status-card"
        class="relative overflow-hidden border-emerald-200/70 hover:border-emerald-400 hover:shadow-md hover:shadow-emerald-500/5 transition-all duration-200"
      >
        <!-- Top color accent stripe -->
        <div class="absolute top-0 left-0 right-0 h-1 bg-gradient-to-r from-emerald-500 via-teal-500 to-emerald-600" />

        <div class="flex items-start justify-between">
          <div class="space-y-1.5">
            <div class="inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-xs font-semibold bg-emerald-50 text-emerald-700 border border-emerald-200/60">
              <Server class="w-3.5 h-3.5 text-emerald-600" aria-hidden="true" />
              <span>System Infrastructure</span>
            </div>
            <h3
              class="text-lg font-bold text-slate-900 flex items-center gap-2 tracking-tight"
              data-testid="system-health-status"
            >
              <span>Operational</span>
              <StatusPill status="active" label="Healthy" dot size="sm" />
            </h3>
            <p class="text-xs text-slate-500">Backend Minimal APIs &amp; Dapper connected</p>
          </div>
          <div
            class="w-11 h-11 rounded-xl bg-gradient-to-br from-emerald-500 to-teal-600 text-white flex items-center justify-center shrink-0 shadow-sm shadow-emerald-500/20"
          >
            <CheckCircle2 class="w-5 h-5" aria-hidden="true" />
          </div>
        </div>
        <div
          class="mt-4 pt-3 border-t border-slate-100 flex items-center justify-between text-xs text-slate-500"
        >
          <span class="text-slate-400 font-medium">Client Version:</span>
          <span class="font-mono text-slate-700 font-semibold bg-slate-50 px-2 py-0.5 rounded border border-slate-200/60">v0.1.0 (Stage 1 Core)</span>
        </div>
      </ContentCard>
    </div>

    <!-- Domain Navigator Pills / Filter Toolbar -->
    <div class="flex items-center justify-between flex-wrap gap-3 py-1">
      <div class="flex items-center gap-1.5 overflow-x-auto pb-1 sm:pb-0">
        <button
          type="button"
          :class="[
            'px-3 py-1.5 rounded-lg text-xs font-semibold transition-all flex items-center gap-1.5 cursor-pointer',
            selectedFilter === 'all'
              ? 'bg-slate-900 text-white shadow-xs'
              : 'bg-white text-slate-600 border border-slate-200 hover:bg-slate-100',
          ]"
          @click="selectedFilter = 'all'"
        >
          <Compass class="w-3.5 h-3.5" />
          <span>All Workspaces</span>
          <span
            :class="[
              'px-1.5 py-0.2 rounded-full text-2xs font-bold',
              selectedFilter === 'all' ? 'bg-slate-700 text-slate-200' : 'bg-slate-100 text-slate-600',
            ]"
          >
            9
          </span>
        </button>

        <button
          type="button"
          :class="[
            'px-3 py-1.5 rounded-lg text-xs font-semibold transition-all flex items-center gap-1.5 cursor-pointer',
            selectedFilter === 'operations'
              ? 'bg-amber-600 text-white shadow-xs'
              : 'bg-white text-amber-800 border border-amber-200 hover:bg-amber-50',
          ]"
          @click="selectedFilter = 'operations'"
        >
          <span class="w-2 h-2 rounded-full bg-amber-500" />
          <span>Operations</span>
        </button>

        <button
          type="button"
          :class="[
            'px-3 py-1.5 rounded-lg text-xs font-semibold transition-all flex items-center gap-1.5 cursor-pointer',
            selectedFilter === 'academics'
              ? 'bg-indigo-600 text-white shadow-xs'
              : 'bg-white text-indigo-800 border border-indigo-200 hover:bg-indigo-50',
          ]"
          @click="selectedFilter = 'academics'"
        >
          <span class="w-2 h-2 rounded-full bg-indigo-500" />
          <span>Academics</span>
        </button>

        <button
          type="button"
          :class="[
            'px-3 py-1.5 rounded-lg text-xs font-semibold transition-all flex items-center gap-1.5 cursor-pointer',
            selectedFilter === 'students'
              ? 'bg-teal-600 text-white shadow-xs'
              : 'bg-white text-teal-800 border border-teal-200 hover:bg-teal-50',
          ]"
          @click="selectedFilter = 'students'"
        >
          <span class="w-2 h-2 rounded-full bg-teal-500" />
          <span>Students</span>
        </button>

        <button
          type="button"
          :class="[
            'px-3 py-1.5 rounded-lg text-xs font-semibold transition-all flex items-center gap-1.5 cursor-pointer',
            selectedFilter === 'finance'
              ? 'bg-emerald-600 text-white shadow-xs'
              : 'bg-white text-emerald-800 border border-emerald-200 hover:bg-emerald-50',
          ]"
          @click="selectedFilter = 'finance'"
        >
          <span class="w-2 h-2 rounded-full bg-emerald-500" />
          <span>Finance</span>
        </button>
      </div>

      <div class="text-xs text-slate-500 flex items-center gap-1.5 font-medium">
        <Zap class="w-3.5 h-3.5 text-amber-500" />
        <span>Unified Institutional Desks</span>
      </div>
    </div>

    <!-- Operations Shortcuts Section -->
    <div
      v-show="selectedFilter === 'all' || selectedFilter === 'operations'"
      class="space-y-3"
      data-testid="operations-shortcuts-section"
    >
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-2">
          <div class="w-2.5 h-2.5 rounded-full bg-amber-500" />
          <div>
            <h3 class="text-base font-bold text-slate-900 tracking-tight">Operations Workspace</h3>
            <p class="text-xs text-slate-500">
              Direct navigation to academic calendar, scheduling cycles, and institutional milestones.
            </p>
          </div>
        </div>
        <span class="text-xs font-semibold text-amber-700 bg-amber-50 px-2.5 py-0.5 rounded-full border border-amber-200/70">
          Core Foundation
        </span>
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
          class="group relative block p-5 bg-white border border-slate-200 rounded-xl shadow-xs hover:border-amber-400 hover:shadow-md hover:shadow-amber-500/5 -translate-y-0 hover:-translate-y-0.5 transition-all duration-200 overflow-hidden"
        >
          <!-- Top subtle accent gradient stripe -->
          <div
            :class="['absolute top-0 left-0 right-0 h-1 bg-gradient-to-r', shortcut.accentBar]"
          />

          <div class="flex items-start justify-between">
            <div
              :class="[
                'w-11 h-11 rounded-xl flex items-center justify-center border shadow-2xs transition-all duration-200',
                shortcut.iconTheme,
              ]"
            >
              <component :is="shortcut.icon" class="w-5 h-5" aria-hidden="true" />
            </div>
            <span :class="['text-xs font-semibold px-2.5 py-0.5 rounded-full border', shortcut.badgeColor]">
              {{ shortcut.tag }}
            </span>
          </div>

          <div class="mt-3.5">
            <h4
              class="text-sm font-bold text-slate-900 group-hover:text-amber-800 transition-colors flex items-center justify-between"
            >
              <span>{{ shortcut.title }}</span>
              <ArrowRight
                class="w-4 h-4 text-slate-400 group-hover:text-amber-600 group-hover:translate-x-1 transition-transform"
              />
            </h4>
            <p class="mt-1.5 text-xs text-slate-500 leading-relaxed line-clamp-2">
              {{ shortcut.description }}
            </p>
          </div>
        </RouterLink>
      </div>
    </div>

    <!-- Active Stage 1 Domain Workspaces (Students & Finance) -->
    <div
      v-show="selectedFilter === 'all' || selectedFilter === 'students' || selectedFilter === 'finance'"
      class="space-y-3"
      data-testid="domain-workspaces-section"
    >
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-2">
          <div class="w-2.5 h-2.5 rounded-full bg-teal-500" />
          <div>
            <h3 class="text-base font-bold text-slate-900 tracking-tight">Stage 1 Core Workspaces</h3>
            <p class="text-xs text-slate-500">
              Active work queues and desks across Students and Finance domains.
            </p>
          </div>
        </div>
        <div class="flex items-center gap-1.5">
          <span class="text-xs font-semibold text-teal-800 bg-teal-50 px-2 py-0.5 rounded-md border border-teal-200/60">Students</span>
          <span class="text-xs font-semibold text-emerald-800 bg-emerald-50 px-2 py-0.5 rounded-md border border-emerald-200/60">Finance</span>
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
          :class="[
            'group relative block p-5 bg-white border border-slate-200 rounded-xl shadow-xs hover:shadow-md -translate-y-0 hover:-translate-y-0.5 transition-all duration-200 overflow-hidden',
            desk.hoverGlowClass,
          ]"
        >
          <!-- Top subtle accent gradient stripe -->
          <div :class="['absolute top-0 left-0 right-0 h-1 bg-gradient-to-r', desk.accentBar]" />

          <div class="flex items-center justify-between">
            <div
              :class="[
                'w-11 h-11 rounded-xl flex items-center justify-center border shadow-2xs transition-all duration-200',
                desk.iconThemeClass,
              ]"
            >
              <component :is="desk.icon" class="w-5 h-5" aria-hidden="true" />
            </div>
            <span
              :class="[
                'text-2xs font-bold uppercase tracking-wider px-2 py-0.5 rounded-md border',
                desk.domainBadgeClass,
              ]"
            >
              {{ desk.domain }}
            </span>
          </div>

          <div class="mt-3.5">
            <h4
              class="text-sm font-bold text-slate-900 group-hover:text-teal-800 transition-colors flex items-center justify-between"
            >
              <span>{{ desk.title }}</span>
              <ArrowRight
                class="w-4 h-4 text-slate-400 group-hover:text-teal-600 group-hover:translate-x-1 transition-transform"
              />
            </h4>
            <p class="mt-1.5 text-xs text-slate-500 leading-relaxed line-clamp-2">
              {{ desk.description }}
            </p>
          </div>
        </RouterLink>
      </div>
    </div>

    <!-- Stage 2 Academics Workspaces (Royal Violet & Indigo Palette) -->
    <div
      v-show="selectedFilter === 'all' || selectedFilter === 'academics'"
      class="space-y-3"
      data-testid="academics-workspaces-section"
    >
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-2">
          <div class="w-2.5 h-2.5 rounded-full bg-indigo-500" />
          <div>
            <h3 class="text-base font-bold text-slate-900 tracking-tight">Stage 2 Academic Workspaces</h3>
            <p class="text-xs text-slate-500">
              Learning programs, course rosters, classroom session execution, and grading workbenches.
            </p>
          </div>
        </div>
        <span class="text-xs font-semibold text-indigo-800 bg-indigo-50 px-2.5 py-0.5 rounded-full border border-indigo-200/70">
          Academics &amp; Pedagogy
        </span>
      </div>

      <div
        class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4"
        data-testid="academics-workspaces-grid"
      >
        <RouterLink
          v-for="desk in academicWorkspaces"
          :key="desk.id"
          :to="desk.to"
          :data-testid="desk.id"
          :class="[
            'group relative block p-5 bg-white border border-slate-200 rounded-xl shadow-xs hover:shadow-md -translate-y-0 hover:-translate-y-0.5 transition-all duration-200 overflow-hidden',
            desk.hoverGlowClass,
          ]"
        >
          <!-- Top subtle accent gradient stripe -->
          <div :class="['absolute top-0 left-0 right-0 h-1 bg-gradient-to-r', desk.accentBar]" />

          <div class="flex items-center justify-between">
            <div
              :class="[
                'w-11 h-11 rounded-xl flex items-center justify-center border shadow-2xs transition-all duration-200',
                desk.iconThemeClass,
              ]"
            >
              <component :is="desk.icon" class="w-5 h-5" aria-hidden="true" />
            </div>
            <span
              :class="[
                'text-2xs font-bold uppercase tracking-wider px-2 py-0.5 rounded-md border',
                desk.domainBadgeClass,
              ]"
            >
              {{ desk.domain }}
            </span>
          </div>

          <div class="mt-3.5">
            <h4
              class="text-sm font-bold text-slate-900 group-hover:text-indigo-800 transition-colors flex items-center justify-between"
            >
              <span>{{ desk.title }}</span>
              <ArrowRight
                class="w-4 h-4 text-slate-400 group-hover:text-indigo-600 group-hover:translate-x-1 transition-transform"
              />
            </h4>
            <p class="mt-1.5 text-xs text-slate-500 leading-relaxed line-clamp-2">
              {{ desk.description }}
            </p>
          </div>
        </RouterLink>
      </div>
    </div>
  </div>
</template>
