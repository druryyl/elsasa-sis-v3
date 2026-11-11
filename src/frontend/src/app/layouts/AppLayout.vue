<script setup lang="ts">
import { computed, ref, type Component as VueComponent } from 'vue';
import { useRoute, useRouter, RouterLink, RouterView } from 'vue-router';
import { useToast } from 'primevue/usetoast';
import { useQueryClient } from '@tanstack/vue-query';
import { useAuthStore } from '@/app/stores/auth.store';
import { useAppContextStore } from '@/app/stores/appContext.store';
import { useUIStore } from '@/app/stores/ui.store';
import {
  LEVEL2_DASHBOARD_ACCESS,
  isAuthorizedForAnyRule,
} from '@/app/router/dashboardAccess';
import ResetDemoDataDialog from '@/app/layouts/components/ResetDemoDataDialog.vue';
import {
  Menu,
  ChevronLeft,
  ChevronRight,
  ChevronDown,
  LayoutDashboard,
  Calendar,
  CalendarDays,
  GraduationCap,
  ClipboardList,
  UserPlus,
  UserCheck,
  DollarSign,
  Receipt,
  CreditCard,
  Building2,
  User,
  LogOut,
  Shield,
  Layers,
  Wallet,
  BookOpen,
  RotateCcw,
  Sparkles,
} from 'lucide-vue-next';

interface NavChildItem {
  id: string;
  label: string;
  to: string;
  icon?: VueComponent | string;
  permission?: string;
  permissions?: string[];
  roles?: string[];
  excludedRoles?: string[];
  authMode?: 'all' | 'any';
}

interface NavParentItem {
  id: string;
  label: string;
  to?: string;
  icon: VueComponent | string;
  permission?: string;
  permissions?: string[];
  roles?: string[];
  excludedRoles?: string[];
  authMode?: 'all' | 'any';
  children?: NavChildItem[];
}

const authStore = useAuthStore();
const appContextStore = useAppContextStore();
const uiStore = useUIStore();
const route = useRoute();
const router = useRouter();

// PrimeVue toast instance for reactive notifications
const toastInstance = useToast();

// TanStack Query client instance for cache invalidation on reset
const queryClientInstance = useQueryClient();

// User profile dropdown state
const isProfileMenuOpen = ref(false);

// Campus selector dropdown state
const isCampusSelectorOpen = ref(false);

// Expanded menu groups in expanded sidebar mode
const openGroups = ref<Record<string, boolean>>({
  dashboard: true,
  operations: true,
  academics: true,
  teaching: true,
  students: true,
  finance: true,
  portal: true,
});

function isGroupOpen(groupId: string): boolean {
  if (uiStore.isSidebarCollapsed) {
    return true;
  }
  return openGroups.value[groupId] ?? true;
}

function toggleGroup(groupId: string) {
  openGroups.value = {
    ...openGroups.value,
    [groupId]: !isGroupOpen(groupId),
  };
}

interface CampusOption {
  id: string | null;
  name: string;
}

// Available campus list for selector
const campusOptions: CampusOption[] = [
  { id: null, name: 'All Campuses (Semua Kampus)' },
  { id: 'campus-main', name: 'Main Campus (Kampus A)' },
  { id: 'campus-east', name: 'East Campus (Kampus B)' },
];

const activeCampusName = computed(() => {
  const match = campusOptions.find((c) => c.id === appContextStore.activeCampusId);
  return match ? match.name : 'All Campuses (Semua Kampus)';
});

const activeAcademicPeriodLabel = computed(() => {
  return appContextStore.activeAcademicPeriodId ?? 'AY 2026/2027 (Active)';
});

// Master navigation tree definition
const masterNavItems: NavParentItem[] = [
  {
    id: 'dashboard',
    label: 'Dashboard',
    icon: LayoutDashboard,
    children: [
      {
        id: 'dashboard',
        label: 'Executive Overview',
        to: '/dashboard',
        icon: LayoutDashboard,
      },
      {
        id: 'dashboard-students',
        label: 'Students KPIs',
        to: '/dashboard/students',
        icon: GraduationCap,
        authMode: 'any',
        ...LEVEL2_DASHBOARD_ACCESS.students,
      },
      {
        id: 'dashboard-academics',
        label: 'Academics KPIs',
        to: '/dashboard/academics',
        icon: BookOpen,
        authMode: 'any',
        ...LEVEL2_DASHBOARD_ACCESS.academics,
      },
      {
        id: 'dashboard-finance',
        label: 'Finance KPIs',
        to: '/dashboard/finance',
        icon: DollarSign,
        authMode: 'any',
        ...LEVEL2_DASHBOARD_ACCESS.finance,
      },
      {
        id: 'dashboard-teaching',
        label: 'Teaching KPIs',
        to: '/dashboard/teaching',
        icon: 'pi pi-graduation-cap',
        authMode: 'any',
        ...LEVEL2_DASHBOARD_ACCESS.teaching,
      },
      {
        id: 'dashboard-operations',
        label: 'Operations KPIs',
        to: '/dashboard/operations',
        icon: Calendar,
        authMode: 'any',
        ...LEVEL2_DASHBOARD_ACCESS.operations,
      },
      {
        id: 'dashboard-development',
        label: 'Development (Stage 3)',
        to: '/dashboard/development',
        icon: Sparkles,
      },
    ],
  },
  {
    id: 'operations',
    label: 'Operations',
    icon: Calendar,
    children: [
      {
        id: 'operations-calendar',
        label: 'Calendar & Periods',
        to: '/operations/calendar',
        icon: CalendarDays,
        permission: 'Permissions.Operations.Calendar.Manage',
      },
    ],
  },
  {
    id: 'academics',
    label: 'Academics',
    icon: BookOpen,
    children: [
      {
        id: 'academics-curriculum',
        label: 'Curriculum & Requirements',
        to: '/academics/curriculum',
        icon: 'pi pi-book',
        permission: 'academics.curriculum.view',
        roles: ['AcademicCoordinator', 'AcademicAuthority', 'Admin'],
      },
      {
        id: 'academics-learning-plans',
        label: 'Learning Plans',
        to: '/academics/learning-plans',
        icon: 'pi pi-id-card',
        permission: 'academics:learning-plans:view',
        roles: ['AcademicCoordinator', 'AcademicAuthority', 'Teacher', 'StudentDevelopmentOfficer', 'Admin'],
      },
      {
        id: 'academics-participation',
        label: 'Participation & Rosters',
        to: '/academics/participation',
        icon: 'pi pi-users',
        permission: 'academics.participations.view',
        roles: ['AcademicCoordinator', 'AcademicAuthority', 'Admin'],
      },
      {
        id: 'academics-sessions',
        label: 'Class Sessions',
        to: '/academics/sessions',
        icon: 'pi pi-calendar',
        permission: 'academics.activities.view',
        roles: ['Teacher', 'AcademicCoordinator', 'AcademicAuthority', 'Admin'],
      },
      {
        id: 'academics-coursework',
        label: 'Coursework',
        to: '/academics/coursework',
        icon: 'pi pi-file-edit',
        permission: 'academics.assignments.view',
        roles: ['Teacher', 'AcademicCoordinator', 'AcademicAuthority', 'Admin'],
      },
      {
        id: 'academics-interventions',
        label: 'Academic Interventions',
        to: '/academics/interventions',
        icon: 'pi pi-bolt',
        permission: 'academics.interventions.view',
        roles: ['AcademicCoordinator', 'Teacher', 'AcademicAuthority', 'Admin'],
      },
      {
        id: 'academics-cohort-review',
        label: 'Cohort Academic Review',
        to: '/academics/cohort-review',
        icon: 'pi pi-check-square',
        permission: 'academics.standings.view',
        roles: ['AcademicAuthority', 'Admin'],
      },
    ],
  },
  {
    id: 'teaching',
    label: 'Teaching',
    icon: 'pi pi-graduation-cap',
    children: [
      {
        id: 'teaching-faculty',
        label: 'Faculty Resources',
        to: '/teaching/faculty',
        icon: 'pi pi-users',
        permission: 'Permissions.Teaching.Faculty.View',
        roles: ['TeachingAuthority', 'TeachingCoordinator', 'Admin'],
      },
      {
        id: 'teaching-assignments',
        label: 'Assignments & Coverage',
        to: '/teaching/assignments',
        icon: 'pi pi-briefcase',
        permission: 'Permissions.Teaching.Assignments.View',
        roles: ['TeachingCoordinator', 'TeachingAuthority', 'Admin'],
      },
      {
        id: 'teaching-workload',
        label: 'Workload Monitoring',
        to: '/teaching/workload',
        icon: 'pi pi-chart-bar',
        permission: 'Permissions.Teaching.Workload.View',
        roles: ['TeachingCoordinator', 'TeachingAuthority', 'Admin'],
      },
      {
        id: 'teaching-my-availability',
        label: 'My Availability',
        to: '/teaching/my-availability',
        icon: 'pi pi-clock',
        permission: 'Permissions.Teaching.Availability.Declare',
        roles: ['Teacher', 'TeachingCoordinator', 'Admin'],
      },
    ],
  },
  {
    id: 'students',
    label: 'Students',
    icon: GraduationCap,
    children: [
      {
        id: 'students-admission',
        label: 'Admission Queue',
        to: '/students/admission',
        icon: ClipboardList,
        permission: 'Permissions.Students.Admission.View',
      },
      {
        id: 'students-admission-apply',
        label: 'New Registration',
        to: '/admission/apply',
        icon: UserPlus,
        permission: 'Permissions.Students.Admission.Register',
      },
      {
        id: 'students-enrollment',
        label: 'Enrollment Desk',
        to: '/students/enrollment',
        icon: UserCheck,
        permission: 'Permissions.Students.Enrollment.Execute',
      },
    ],
  },
  {
    id: 'finance',
    label: 'Finance',
    icon: DollarSign,
    children: [
      {
        id: 'finance-policies',
        label: 'Financial Policies',
        to: '/finance/policies',
        icon: Shield,
        permission: 'Permissions.Finance.Policies.View',
      },
      {
        id: 'finance-billing-cycles',
        label: 'Billing Cycles',
        to: '/finance/billing-cycles',
        icon: Layers,
        permission: 'Permissions.Finance.Obligations.Assess',
      },
      {
        id: 'finance-accounts',
        label: 'Student Accounts',
        to: '/finance/accounts',
        icon: Wallet,
        permission: 'Permissions.Finance.Accounts.View',
      },
      {
        id: 'finance-obligations',
        label: 'Obligations & Billing',
        to: '/finance/obligations',
        icon: Receipt,
        permission: 'Permissions.Finance.Obligations.View',
      },
      {
        id: 'finance-payments',
        label: 'Payment Receipt Desk',
        to: '/finance/payments',
        icon: CreditCard,
        permission: 'Permissions.Finance.Payments.Receive',
      },
    ],
  },
  {
    id: 'portal',
    label: 'Student Portal',
    icon: GraduationCap,
    roles: ['Student'],
    children: [
      {
        id: 'portal-learning',
        label: 'My Learning & Coursework',
        to: '/portal/learning',
        icon: 'pi pi-book',
        roles: ['Student'],
      },
    ],
  },
];

function checkPermission(permission: string): boolean {
  if (authStore.hasPermission(permission)) return true;

  const permissionAliases: Record<string, string[]> = {
    'academics.curriculum.view': ['Permissions.Academics.Curriculum.View'],
    'Permissions.Academics.Curriculum.View': ['academics.curriculum.view'],
    'academics:learning-plans:view': ['Permissions.Academics.LearningPlans.View', 'academics.learning_plans.view'],
    'Permissions.Academics.LearningPlans.View': ['academics:learning-plans:view', 'academics.learning_plans.view'],
    'academics.participations.view': ['Permissions.Academics.Participations.View'],
    'Permissions.Academics.Participations.View': ['academics.participations.view'],
    'academics.activities.view': ['Permissions.Academics.Activities.View'],
    'Permissions.Academics.Activities.View': ['academics.activities.view'],
    'academics.assignments.view': ['Permissions.Academics.Assignments.View'],
    'Permissions.Academics.Assignments.View': ['academics.assignments.view'],
    'academics.interventions.view': ['Permissions.Academics.Interventions.View'],
    'Permissions.Academics.Interventions.View': ['academics.interventions.view'],
    'academics.standings.view': ['Permissions.Academics.Standings.View'],
    'Permissions.Academics.Standings.View': ['academics.standings.view'],
    'teaching.faculty.view': ['Permissions.Teaching.Faculty.View'],
    'Permissions.Teaching.Faculty.View': ['teaching.faculty.view'],
    'teaching.assignments.view': ['Permissions.Teaching.Assignments.View'],
    'Permissions.Teaching.Assignments.View': ['teaching.assignments.view'],
    'teaching.workload.view': ['Permissions.Teaching.Workload.View'],
    'Permissions.Teaching.Workload.View': ['teaching.workload.view'],
    'teaching.availability.declare': ['Permissions.Teaching.Availability.Declare'],
    'Permissions.Teaching.Availability.Declare': ['teaching.availability.declare'],
  };

  const aliases = permissionAliases[permission];
  if (aliases) {
    return aliases.some((alias) => authStore.hasPermission(alias));
  }
  return false;
}

function isNavAuthorized(item: {
  permission?: string;
  permissions?: string[];
  roles?: string[];
  excludedRoles?: string[];
  authMode?: 'all' | 'any';
}): boolean {
  if (
    item.authMode === 'any' ||
    (item.permissions && item.permissions.length > 0) ||
    (item.excludedRoles && item.excludedRoles.length > 0)
  ) {
    return isAuthorizedForAnyRule(item, authStore, checkPermission);
  }
  if (!item.permission && (!item.roles || item.roles.length === 0)) {
    return true;
  }
  if (item.permission && checkPermission(item.permission)) {
    return true;
  }
  if (item.roles && item.roles.length > 0 && authStore.hasAnyRole(item.roles)) {
    return true;
  }
  return false;
}

// Hierarchical navigation links filtered dynamically via authStore permissions & roles
const filteredNavItems = computed<NavParentItem[]>(() => {
  return masterNavItems
    .map((item) => {
      // If parent item requires a direct permission or role
      if ((item.permission || item.roles) && !isNavAuthorized(item)) {
        return null;
      }

      // If parent item has children, filter children by permission or role
      if (item.children) {
        const visibleChildren = item.children.filter((child) => isNavAuthorized(child));

        // Hide parent group if it has no link itself and 0 visible children
        if (!item.to && visibleChildren.length === 0) {
          return null;
        }

        return {
          ...item,
          children: visibleChildren,
        };
      }

      return item;
    })
    .filter((item): item is NavParentItem => item !== null);
});

// Checks if a route path is currently active
function isItemActive(path?: string): boolean {
  if (!path) return false;
  if (path === '/dashboard') {
    return route.path === '/dashboard';
  }
  return route.path === path || route.path.startsWith(`${path}/`);
}

function isGroupActive(group: NavParentItem): boolean {
  if (group.to && isItemActive(group.to)) return true;
  return Boolean(group.children?.some((child) => isItemActive(child.to)));
}

// User logout handler
function handleLogout() {
  authStore.logout();
  isProfileMenuOpen.value = false;
  router.push('/auth/login');
}

function selectCampus(campusId: string | null) {
  appContextStore.setActiveCampus(campusId);
  isCampusSelectorOpen.value = false;
}

/**
 * Display condition for the "Reset Demo Data" menu item.
 * Hard-guarded to SuperAdmin role and non-production environments only
 * (ADR-010, TD-08, TD-10, GAP-008).
 */
const canResetDemoData = computed(() => {
  return authStore.hasRole('SuperAdmin') && !import.meta.env.PROD;
});

// Dialog visibility state for the destructive reset confirmation modal
const isResetDemoDataDialogOpen = ref(false);

function openResetDemoDataDialog() {
  isResetDemoDataDialogOpen.value = true;
  isProfileMenuOpen.value = false;
}

/**
 * Reactive reset workflow executed after a successful demo data reset.
 *
 * - Shows a success toast notification.
 * - Clears Pinia caches (appContextStore + cached TanStack Query keys).
 * - Performs a clean page reload so the application re-hydrates from the
 *   newly pristine database state.
 */
function handleResetDemoDataSuccess() {
  if (toastInstance && typeof toastInstance.add === 'function') {
    toastInstance.add({
      severity: 'success',
      summary: 'Demo Data Reset',
      detail: 'Demo sample data reset successfully.',
      life: 4000,
    });
  }

  // Clear Pinia application context cache
  appContextStore.clear();

  // Clear cached TanStack Query keys so all views re-fetch from the pristine database
  if (queryClientInstance) {
    queryClientInstance.clear();
  }

  // Clean page reload to refresh application state from the new pristine database
  if (typeof globalThis.window !== 'undefined' && typeof globalThis.window.location.reload === 'function') {
    globalThis.window.location.reload();
  }
}

function getDomainBadgeColor(groupId: string): string {
  switch (groupId) {
    case 'dashboard':
      return 'bg-teal-600';
    case 'operations':
      return 'bg-amber-500';
    case 'academics':
      return 'bg-indigo-500';
    case 'teaching':
      return 'bg-purple-500';
    case 'students':
      return 'bg-teal-500';
    case 'finance':
      return 'bg-emerald-500';
    case 'portal':
      return 'bg-rose-500';
    default:
      return 'bg-slate-400';
  }
}
</script>

<template>
  <div class="flex h-screen w-full bg-slate-50 overflow-hidden" data-testid="app-layout">
    <!-- Collapsible Sidebar -->
    <aside
      :class="[
        'flex flex-col bg-white border-r border-slate-200 transition-all duration-300 ease-in-out shrink-0 select-none z-20',
        uiStore.isSidebarCollapsed ? 'w-20' : 'w-64',
      ]"
      data-testid="app-sidebar"
      :aria-expanded="!uiStore.isSidebarCollapsed"
    >
      <!-- Sidebar Header / Branding -->
      <div
        class="h-14 flex items-center border-b border-slate-200 px-4"
        :class="uiStore.isSidebarCollapsed ? 'justify-center' : 'justify-between'"
      >
        <RouterLink
          to="/dashboard"
          class="flex items-center gap-3 overflow-hidden focus:outline-none"
          data-testid="institution-branding"
        >
          <div
            class="flex items-center justify-center w-9 h-9 rounded-lg bg-elsasa-navy-900 text-white shrink-0 shadow-xs"
          >
            <GraduationCap class="w-5 h-5 text-elsasa-teal-500" aria-hidden="true" />
          </div>
          <div v-if="!uiStore.isSidebarCollapsed" class="flex flex-col leading-tight min-w-0">
            <span class="font-bold text-base text-elsasa-navy-900 truncate">Elsasa SIS</span>
            <span class="text-[10px] uppercase font-semibold text-elsasa-teal-600 tracking-wider">
              Institutional
            </span>
          </div>
        </RouterLink>
      </div>

      <!-- Navigation Tree -->
      <nav
        class="flex-1 overflow-y-auto px-3 py-4 space-y-1.5 focus:outline-none"
        aria-label="Sidebar Navigation"
        data-testid="sidebar-nav"
      >
        <div v-for="item in filteredNavItems" :key="item.id" class="space-y-1">
          <!-- Single Link Item (e.g. Dashboard) -->
          <RouterLink
            v-if="item.to && !item.children"
            :to="item.to"
            :class="[
              'flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-all duration-150',
              isItemActive(item.to)
                ? 'bg-gradient-to-r from-teal-50 to-teal-100/50 text-teal-700 font-semibold border-l-2 border-teal-600 shadow-2xs'
                : 'text-slate-600 hover:bg-slate-100/80 hover:text-slate-900',
              uiStore.isSidebarCollapsed ? 'justify-center' : '',
            ]"
            :title="uiStore.isSidebarCollapsed ? item.label : undefined"
            :data-testid="`nav-item-${item.id}`"
          >
            <i
              v-if="typeof item.icon === 'string'"
              :class="[item.icon, 'text-base shrink-0']"
              aria-hidden="true"
            />
            <component :is="item.icon" v-else class="w-5 h-5 shrink-0" aria-hidden="true" />
            <span v-if="!uiStore.isSidebarCollapsed" class="truncate">{{ item.label }}</span>
          </RouterLink>

          <!-- Group Item with Submenu (e.g. Operations, Students, Finance) -->
          <div v-else class="space-y-1">
            <!-- Group Header (Expanded mode) -->
            <button
              v-if="!uiStore.isSidebarCollapsed"
              type="button"
              class="w-full flex items-center justify-between px-3 py-2 rounded-lg text-xs font-semibold text-slate-500 uppercase tracking-wider hover:text-slate-800 hover:bg-slate-50 transition-colors focus:outline-none"
              :data-testid="`nav-group-${item.id}`"
              @click="toggleGroup(item.id)"
            >
              <span class="flex items-center gap-2 truncate">
                <span :class="['w-1.5 h-1.5 rounded-full shrink-0', getDomainBadgeColor(item.id)]" aria-hidden="true" />
                <i
                  v-if="typeof item.icon === 'string'"
                  :class="[item.icon, 'text-base shrink-0']"
                  aria-hidden="true"
                />
                <component :is="item.icon" v-else class="w-4 h-4 shrink-0 text-slate-400" aria-hidden="true" />
                <span class="truncate">{{ item.label }}</span>
              </span>
              <ChevronDown
                :class="[
                  'w-4 h-4 text-slate-400 transition-transform duration-200',
                  isGroupOpen(item.id) ? 'rotate-0' : '-rotate-90',
                ]"
                aria-hidden="true"
              />
            </button>

            <!-- Group Header Icon Only (Collapsed mode) -->
            <div
              v-else
              class="flex justify-center py-2 text-slate-500"
              :title="item.label"
              :data-testid="`nav-group-icon-${item.id}`"
            >
              <i
                v-if="typeof item.icon === 'string'"
                :class="[
                  item.icon,
                  'text-lg',
                  isGroupActive(item) ? 'text-elsasa-teal-600' : 'text-slate-400',
                ]"
                aria-hidden="true"
              />
              <component
                :is="item.icon"
                v-else
                :class="[
                  'w-5 h-5',
                  isGroupActive(item) ? 'text-elsasa-teal-600' : 'text-slate-400',
                ]"
                aria-hidden="true"
              />
            </div>

            <!-- Group Children Links -->
            <div
              v-if="item.children && isGroupOpen(item.id)"
              class="space-y-1"
              :class="!uiStore.isSidebarCollapsed ? 'pl-4' : ''"
            >
              <RouterLink
                v-for="child in item.children"
                :key="child.id"
                :to="child.to"
                :class="[
                  'flex items-center gap-3 px-3 py-2 rounded-lg text-sm transition-all duration-150',
                  isItemActive(child.to)
                    ? 'bg-gradient-to-r from-teal-50 to-teal-100/50 text-teal-700 font-semibold border-l-2 border-teal-600 shadow-2xs'
                    : 'text-slate-600 hover:bg-slate-100/80 hover:text-slate-900',
                  uiStore.isSidebarCollapsed ? 'justify-center' : '',
                ]"
                :title="uiStore.isSidebarCollapsed ? child.label : undefined"
                :data-testid="`nav-item-${child.id}`"
              >
                <i
                  v-if="typeof child.icon === 'string'"
                  :class="[child.icon, 'text-base shrink-0']"
                  aria-hidden="true"
                />
                <component :is="child.icon ?? Layers" v-else class="w-4 h-4 shrink-0" aria-hidden="true" />
                <span v-if="!uiStore.isSidebarCollapsed" class="truncate">{{ child.label }}</span>
              </RouterLink>
            </div>
          </div>
        </div>
      </nav>

      <!-- Sidebar Footer / Collapse Toggle -->
      <div class="p-3 border-t border-slate-200 flex items-center justify-center">
        <button
          type="button"
          class="flex items-center gap-2 p-2 rounded-lg text-slate-500 hover:bg-slate-100 hover:text-slate-800 transition-colors focus:outline-none"
          :title="uiStore.isSidebarCollapsed ? 'Expand sidebar' : 'Collapse sidebar'"
          aria-label="Toggle sidebar width"
          data-testid="sidebar-collapse-btn"
          @click="uiStore.toggleSidebar()"
        >
          <ChevronRight v-if="uiStore.isSidebarCollapsed" class="w-5 h-5" aria-hidden="true" />
          <ChevronLeft v-else class="w-5 h-5" aria-hidden="true" />
          <span v-if="!uiStore.isSidebarCollapsed" class="text-xs font-medium">Collapse</span>
        </button>
      </div>
    </aside>

    <!-- Main Application Column -->
    <div class="flex-1 flex flex-col min-w-0 overflow-hidden">
      <!-- Top Navigation Bar -->
      <header
        class="h-14 bg-white border-b border-slate-200 flex items-center justify-between px-4 sm:px-5 z-10 shrink-0"
        data-testid="top-navigation-bar"
      >
        <!-- Left: Sidebar Toggle Button & Branding Placeholder -->
        <div class="flex items-center gap-3">
          <button
            type="button"
            class="p-2 rounded-lg text-slate-600 hover:bg-slate-100 hover:text-slate-900 transition-colors focus:outline-none"
            aria-label="Toggle navigation sidebar"
            data-testid="sidebar-toggle-btn"
            @click="uiStore.toggleSidebar()"
          >
            <Menu class="w-5 h-5" aria-hidden="true" />
          </button>

          <!-- Topbar Institution Title (visible when sidebar is collapsed) -->
          <span
            v-if="uiStore.isSidebarCollapsed"
            class="font-semibold text-slate-800 text-sm hidden sm:inline-block"
          >
            Elsasa SIS
          </span>
        </div>

        <!-- Center / Right: Indicators & Menus -->
        <div class="flex items-center gap-2 sm:gap-4">
          <!-- Active Academic Period Indicator -->
          <div
            class="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-medium bg-emerald-50 text-emerald-700 border border-emerald-200"
            data-testid="academic-period-indicator"
            title="Active Academic Period"
          >
            <span class="inline-block w-2 h-2 rounded-full bg-emerald-500 animate-pulse" />
            <Calendar class="w-3.5 h-3.5 text-emerald-600 shrink-0" aria-hidden="true" />
            <span class="truncate max-w-[140px] sm:max-w-xs">{{ activeAcademicPeriodLabel }}</span>
          </div>

          <!-- Campus Selector Placeholder -->
          <div class="relative">
            <button
              type="button"
              class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium text-slate-700 bg-slate-100 hover:bg-slate-200 border border-slate-200 transition-colors focus:outline-none"
              data-testid="campus-selector"
              aria-haspopup="true"
              :aria-expanded="isCampusSelectorOpen"
              @click="isCampusSelectorOpen = !isCampusSelectorOpen"
            >
              <Building2 class="w-3.5 h-3.5 text-slate-500 shrink-0" aria-hidden="true" />
              <span class="truncate max-w-[120px] sm:max-w-[160px]">{{ activeCampusName }}</span>
              <ChevronDown class="w-3.5 h-3.5 text-slate-400 shrink-0" aria-hidden="true" />
            </button>

            <!-- Campus Dropdown Menu -->
            <div
              v-if="isCampusSelectorOpen"
              class="absolute right-0 mt-2 w-56 bg-white rounded-lg shadow-lg border border-slate-200 py-1 z-30"
              data-testid="campus-selector-dropdown"
            >
              <div
                class="px-3 py-1.5 text-[11px] font-semibold text-slate-400 uppercase tracking-wider"
              >
                Select Campus
              </div>
              <button
                v-for="campus in campusOptions"
                :key="campus.id ?? 'all'"
                type="button"
                :data-testid="`campus-option-${campus.id ?? 'all'}`"
                :class="[
                  'w-full text-left px-3 py-2 text-xs transition-colors flex items-center justify-between',
                  appContextStore.activeCampusId === campus.id
                    ? 'bg-elsasa-teal-50 text-elsasa-teal-600 font-semibold'
                    : 'text-slate-700 hover:bg-slate-50',
                ]"
                @click="selectCampus(campus.id)"
              >
                <span>{{ campus.name }}</span>
                <span
                  v-if="appContextStore.activeCampusId === campus.id"
                  class="w-1.5 h-1.5 rounded-full bg-elsasa-teal-600"
                />
              </button>
            </div>
          </div>

          <!-- User Profile Menu -->
          <div class="relative">
            <button
              type="button"
              class="flex items-center gap-2 p-1.5 rounded-lg hover:bg-slate-100 transition-colors focus:outline-none"
              data-testid="user-profile-menu-button"
              aria-haspopup="true"
              :aria-expanded="isProfileMenuOpen"
              @click="isProfileMenuOpen = !isProfileMenuOpen"
            >
              <div
                class="w-8 h-8 rounded-full bg-elsasa-navy-900 text-white flex items-center justify-center text-xs font-semibold"
              >
                <User class="w-4 h-4 text-elsasa-teal-500" aria-hidden="true" />
              </div>
              <div class="hidden md:flex flex-col text-left leading-tight">
                <span class="text-xs font-semibold text-slate-800">
                  {{ authStore.user?.fullName || authStore.user?.username || 'Administrator' }}
                </span>
                <span class="text-[10px] text-slate-500">
                  {{ authStore.roles[0] || 'Staff' }}
                </span>
              </div>
              <ChevronDown class="w-3.5 h-3.5 text-slate-400 hidden sm:block" aria-hidden="true" />
            </button>

            <!-- User Profile Dropdown Menu -->
            <div
              v-if="isProfileMenuOpen"
              class="absolute right-0 mt-2 w-64 bg-white rounded-xl shadow-lg border border-slate-200 py-2 z-30"
              data-testid="user-profile-menu"
            >
              <div class="px-4 py-2 border-b border-slate-100">
                <p class="text-xs font-semibold text-slate-800">
                  {{ authStore.user?.fullName || 'Administrator' }}
                </p>
                <p class="text-[11px] text-slate-500">@{{ authStore.user?.username || 'admin' }}</p>
                <div class="mt-1 flex items-center gap-1.5">
                  <Shield class="w-3 h-3 text-elsasa-teal-600" aria-hidden="true" />
                  <span class="text-[10px] font-medium text-elsasa-teal-600">
                    {{ authStore.roles[0] || 'Staff Member' }}
                  </span>
                </div>
              </div>

              <div class="px-2 py-1">
                <button
                  v-if="canResetDemoData"
                  type="button"
                  class="w-full text-left flex items-center gap-2 px-3 py-2 rounded-lg text-xs font-medium text-amber-700 hover:bg-amber-50 transition-colors"
                  data-testid="reset-demo-data-button"
                  @click="openResetDemoDataDialog"
                >
                  <RotateCcw class="w-4 h-4 text-amber-600 shrink-0" aria-hidden="true" />
                  <span>Reset Demo Data</span>
                </button>
                <button
                  type="button"
                  class="w-full text-left flex items-center gap-2 px-3 py-2 rounded-lg text-xs font-medium text-rose-600 hover:bg-rose-50 transition-colors"
                  data-testid="logout-button"
                  @click="handleLogout"
                >
                  <LogOut class="w-4 h-4 text-rose-500 shrink-0" aria-hidden="true" />
                  <span>Sign Out</span>
                </button>
              </div>
            </div>
          </div>
        </div>
      </header>

      <!-- Main Routed Content (Wrapped in Transition) -->
      <main class="flex-1 overflow-y-auto p-4 sm:p-5 bg-slate-50" data-testid="main-content">
        <RouterView v-slot="{ Component, route: currentRoute }">
          <Transition name="page" mode="out-in">
            <component :is="Component" :key="currentRoute.fullPath" />
          </Transition>
        </RouterView>
      </main>
    </div>
  </div>

  <!-- Destructive Reset Demo Data Confirmation Dialog (mounted at root level to preserve state) -->
  <ResetDemoDataDialog
    v-if="canResetDemoData"
    :visible="isResetDemoDataDialogOpen"
    @update:visible="isResetDemoDataDialogOpen = $event"
    @success="handleResetDemoDataSuccess"
  />
</template>

<style scoped>
.page-enter-active,
.page-leave-active {
  transition:
    opacity 0.15s ease,
    transform 0.15s ease;
}

.page-enter-from {
  opacity: 0;
  transform: translateY(2px);
}

.page-leave-to {
  opacity: 0;
  transform: translateY(-2px);
}
</style>
