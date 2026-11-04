<script setup lang="ts">
import { computed, ref, type Component as VueComponent } from 'vue';
import { useRoute, useRouter, RouterLink, RouterView } from 'vue-router';
import { useAuthStore } from '@/app/stores/auth.store';
import { useAppContextStore } from '@/app/stores/appContext.store';
import { useUIStore } from '@/app/stores/ui.store';
import type { BreadcrumbItem } from '@/shared/types/navigation';
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
} from 'lucide-vue-next';

interface NavChildItem {
  id: string;
  label: string;
  to: string;
  icon?: VueComponent;
  permission?: string;
}

interface NavParentItem {
  id: string;
  label: string;
  to?: string;
  icon: VueComponent;
  permission?: string;
  children?: NavChildItem[];
}

const authStore = useAuthStore();
const appContextStore = useAppContextStore();
const uiStore = useUIStore();
const route = useRoute();
const router = useRouter();

// User profile dropdown state
const isProfileMenuOpen = ref(false);

// Campus selector dropdown state
const isCampusSelectorOpen = ref(false);

// Expanded menu groups in expanded sidebar mode
const openGroups = ref<Record<string, boolean>>({
  operations: true,
  students: true,
  finance: true,
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

// Available campus list for selector
const campusOptions = [
  { id: 'campus-main', name: 'Main Campus (Kampus A)' },
  { id: 'campus-east', name: 'East Campus (Kampus B)' },
];

const activeCampusName = computed(() => {
  const match = campusOptions.find((c) => c.id === appContextStore.activeCampusId);
  return match ? match.name : 'Main Campus (Kampus A)';
});

const activeAcademicPeriodLabel = computed(() => {
  return appContextStore.activeAcademicPeriodId ?? 'AY 2026/2027 (Active)';
});

// Master navigation tree definition
const masterNavItems: NavParentItem[] = [
  {
    id: 'dashboard',
    label: 'Dashboard',
    to: '/dashboard',
    icon: LayoutDashboard,
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
];

// Hierarchical navigation links filtered dynamically via authStore.hasPermission()
const filteredNavItems = computed<NavParentItem[]>(() => {
  return masterNavItems
    .map((item) => {
      // If parent item requires a direct permission
      if (item.permission && !authStore.hasPermission(item.permission)) {
        return null;
      }

      // If parent item has children, filter children by permission
      if (item.children) {
        const visibleChildren = item.children.filter((child) => {
          if (!child.permission) return true;
          return authStore.hasPermission(child.permission);
        });

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
  return route.path === path || route.path.startsWith(`${path}/`);
}

function isGroupActive(group: NavParentItem): boolean {
  if (group.to && isItemActive(group.to)) return true;
  return Boolean(group.children?.some((child) => isItemActive(child.to)));
}

// Dynamic Breadcrumbs resolution
const resolvedBreadcrumbs = computed<BreadcrumbItem[]>(() => {
  if (uiStore.breadcrumbs.length > 0) {
    return uiStore.breadcrumbs;
  }

  const crumbs: BreadcrumbItem[] = [{ label: 'Home', to: '/dashboard' }];

  if (route.path === '/dashboard') {
    return [{ label: 'Dashboard', current: true }];
  }

  // Derive domain and page crumbs from route path
  const pathSegments = route.path.split('/').filter(Boolean);
  if (pathSegments.length > 0) {
    const domainSegment = pathSegments[0];
    const matchedParent = masterNavItems.find(
      (item) => item.id.toLowerCase() === domainSegment.toLowerCase(),
    );
    if (matchedParent) {
      crumbs.push({ label: matchedParent.label });
    }

    const title = (route.meta.title as string | undefined) ?? pathSegments[pathSegments.length - 1];
    crumbs.push({ label: title, current: true });
  }

  return crumbs;
});

const currentPageTitle = computed(() => {
  return (route.meta.title as string | undefined) ?? 'Elsasa SIS';
});

// User logout handler
function handleLogout() {
  authStore.logout();
  isProfileMenuOpen.value = false;
  router.push('/auth/login');
}

function selectCampus(campusId: string) {
  appContextStore.setActiveCampus(campusId);
  isCampusSelectorOpen.value = false;
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
        class="h-16 flex items-center border-b border-slate-200 px-4"
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
              'flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors',
              isItemActive(item.to)
                ? 'bg-elsasa-teal-50 text-elsasa-teal-600 font-semibold'
                : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900',
              uiStore.isSidebarCollapsed ? 'justify-center' : '',
            ]"
            :title="uiStore.isSidebarCollapsed ? item.label : undefined"
            :data-testid="`nav-item-${item.id}`"
          >
            <component :is="item.icon" class="w-5 h-5 shrink-0" aria-hidden="true" />
            <span v-if="!uiStore.isSidebarCollapsed" class="truncate">{{ item.label }}</span>
          </RouterLink>

          <!-- Group Item with Submenu (e.g. Operations, Students, Finance) -->
          <div v-else class="space-y-1">
            <!-- Group Header (Expanded mode) -->
            <button
              v-if="!uiStore.isSidebarCollapsed"
              type="button"
              class="w-full flex items-center justify-between px-3 py-2 rounded-lg text-xs font-semibold text-slate-500 uppercase tracking-wider hover:text-slate-800 transition-colors focus:outline-none"
              :data-testid="`nav-group-${item.id}`"
              @click="toggleGroup(item.id)"
            >
              <span class="flex items-center gap-2 truncate">
                <component :is="item.icon" class="w-4 h-4 shrink-0" aria-hidden="true" />
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
              <component
                :is="item.icon"
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
                  'flex items-center gap-3 px-3 py-2 rounded-lg text-sm transition-colors',
                  isItemActive(child.to)
                    ? 'bg-elsasa-teal-50 text-elsasa-teal-600 font-semibold'
                    : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900',
                  uiStore.isSidebarCollapsed ? 'justify-center' : '',
                ]"
                :title="uiStore.isSidebarCollapsed ? child.label : undefined"
                :data-testid="`nav-item-${child.id}`"
              >
                <component :is="child.icon ?? Layers" class="w-4 h-4 shrink-0" aria-hidden="true" />
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
        class="h-16 bg-white border-b border-slate-200 flex items-center justify-between px-4 sm:px-6 z-10 shrink-0"
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

          <!-- Topbar Institution Title (visible on mobile / tablet) -->
          <span class="font-semibold text-slate-800 text-sm hidden sm:inline-block">
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
                :key="campus.id"
                type="button"
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

      <!-- Dynamic Breadcrumbs & Page Header Region -->
      <section
        class="bg-white border-b border-slate-200 px-4 sm:px-6 py-3.5 shrink-0 shadow-xs"
        data-testid="layout-page-header"
      >
        <!-- Breadcrumb Navigation -->
        <nav
          v-if="resolvedBreadcrumbs.length > 0"
          aria-label="Breadcrumb"
          class="mb-1"
          data-testid="layout-breadcrumbs"
        >
          <ol class="flex items-center flex-wrap gap-1.5 text-xs text-slate-500">
            <li
              v-for="(crumb, idx) in resolvedBreadcrumbs"
              :key="idx"
              class="inline-flex items-center gap-1.5"
            >
              <ChevronRight
                v-if="idx > 0"
                class="w-3.5 h-3.5 text-slate-400 shrink-0"
                aria-hidden="true"
              />
              <RouterLink
                v-if="crumb.to && !crumb.disabled && !crumb.current"
                :to="crumb.to"
                class="text-slate-600 hover:text-elsasa-teal-600 font-medium transition-colors"
              >
                {{ crumb.label }}
              </RouterLink>
              <span
                v-else
                :class="[crumb.current ? 'font-semibold text-slate-800' : 'text-slate-500']"
                :aria-current="crumb.current ? 'page' : undefined"
              >
                {{ crumb.label }}
              </span>
            </li>
          </ol>
        </nav>

        <!-- Dynamic Page Title -->
        <div class="flex items-center justify-between">
          <h1
            class="text-lg sm:text-xl font-bold text-elsasa-navy-900 tracking-tight"
            data-testid="layout-page-title"
          >
            {{ currentPageTitle }}
          </h1>
        </div>
      </section>

      <!-- Main Routed Content (Wrapped in Transition) -->
      <main class="flex-1 overflow-y-auto p-4 sm:p-6 bg-slate-50" data-testid="main-content">
        <RouterView v-slot="{ Component, route: currentRoute }">
          <Transition name="page" mode="out-in">
            <component :is="Component" :key="currentRoute.fullPath" />
          </Transition>
        </RouterView>
      </main>
    </div>
  </div>
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
