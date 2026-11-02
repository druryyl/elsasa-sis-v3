<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import Select from 'primevue/select';
import Button from 'primevue/button';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import Textarea from 'primevue/textarea';
import Tabs from 'primevue/tabs';
import TabList from 'primevue/tablist';
import Tab from 'primevue/tab';
import TabPanels from 'primevue/tabpanels';
import TabPanel from 'primevue/tabpanel';
import {
  CalendarDays,
  Layers,
  History,
  CheckCircle2,
  AlertTriangle,
  PlusCircle,
  Sparkles,
  ShieldAlert,
  Send,
  Calendar,
} from 'lucide-vue-next';
import {
  PageHeader,
  ContentCard,
  StatusPill,
  EmptyState,
  LoadingSpinner,
  FormField,
} from '@/shared/components';
import type { BreadcrumbItem } from '@/shared/types/navigation';
import { useAuthStore } from '@/app/stores/auth.store';
import { CALENDAR_PERMISSIONS } from '../constants/permissions';
import {
  useAcademicYearsQuery,
  useApproveAcademicYearMutation,
  useActivateAcademicYearMutation,
  useApplyPresetMutation,
  useCreateAcademicYearMutation,
} from '../composables/useAcademicYearsQuery';
import {
  useCalendarsQuery,
  useActiveCalendarQuery,
  usePublishCalendarMutation,
} from '../composables/useCalendarMatrixQuery';
import { useDeclareSchoolClosureMutation } from '../composables/useSchoolClosuresQuery';
import CalendarMatrixTab from '../components/tabs/CalendarMatrixTab.vue';
import PeriodsTreeTab from '../components/tabs/PeriodsTreeTab.vue';
import ClosuresAmendmentsTab from '../components/tabs/ClosuresAmendmentsTab.vue';

// --------------------------------------------------------------------------
// 1. Navigation & Breadcrumbs
// --------------------------------------------------------------------------
const breadcrumbs: BreadcrumbItem[] = [
  { label: 'Dashboard', to: '/dashboard' },
  { label: 'Operations', to: '/operations' },
  { label: 'Calendar & Periods Workspace', current: true },
];

// --------------------------------------------------------------------------
// 2. Authorization & Permissions
// --------------------------------------------------------------------------
const authStore = useAuthStore();

const canManage = computed(() => authStore.hasPermission(CALENDAR_PERMISSIONS.MANAGE));
const canApprove = computed(
  () => authStore.hasPermission(CALENDAR_PERMISSIONS.APPROVE) || canManage.value,
);
const canPublish = computed(
  () => authStore.hasPermission(CALENDAR_PERMISSIONS.PUBLISH) || canManage.value,
);
const canDeclareClosure = computed(
  () => authStore.hasPermission(CALENDAR_PERMISSIONS.DECLARE_CLOSURE) || canManage.value,
);

// --------------------------------------------------------------------------
// 3. Ephemeral UI State (Tabs & Modals)
// --------------------------------------------------------------------------
const activeTab = ref<string>('matrix');
const isCreateYearModalOpen = ref(false);
const isApproveModalOpen = ref(false);
const isActivateModalOpen = ref(false);
const isPublishModalOpen = ref(false);
const isPresetModalOpen = ref(false);
const isClosureModalOpen = ref(false);

// --------------------------------------------------------------------------
// 4. Reactive TanStack Queries
// --------------------------------------------------------------------------
const { data: academicYears, isLoading: isLoadingYears } = useAcademicYearsQuery();

const selectedAcademicYearId = ref<string | null>(null);

// Automatically set selected academic year when list loads or changes
watch(
  () => academicYears.value,
  (years) => {
    if (!years || years.length === 0) {
      selectedAcademicYearId.value = null;
      return;
    }
    const currentStillExists = years.find((y) => y.id === selectedAcademicYearId.value);
    if (!currentStillExists) {
      const activeYear = years.find((y) => y.status === 'active');
      selectedAcademicYearId.value = activeYear ? activeYear.id : (years[0]?.id ?? null);
    }
  },
  { immediate: true },
);

const selectedAcademicYear = computed(() => {
  if (!selectedAcademicYearId.value || !academicYears.value) return null;
  return academicYears.value.find((y) => y.id === selectedAcademicYearId.value) ?? null;
});

const academicYearOptions = computed(() => {
  return (academicYears.value ?? []).map((ay) => ({
    id: ay.id,
    code: ay.code,
    name: ay.name,
    status: ay.status,
    label: `${ay.code} — ${ay.name}`,
  }));
});

// Calendar queries for selected academic year
const { data: calendars } = useCalendarsQuery(selectedAcademicYearId);
const { data: activeCalendar } = useActiveCalendarQuery(selectedAcademicYearId);

const selectedCalendar = computed(() => {
  return activeCalendar.value ?? calendars.value?.[0] ?? null;
});

// Badges & Labels
const academicYearStatusLabel = computed(() => {
  if (!selectedAcademicYear.value) return 'No Year Selected';
  const s = selectedAcademicYear.value.status;
  if (!s) return 'Draft';
  return s.charAt(0).toUpperCase() + s.slice(1);
});

const academicYearStatusVariant = computed(() => {
  if (!selectedAcademicYear.value) return 'slate';
  const status = selectedAcademicYear.value.status;
  if (status === 'active' || status === 'approved' || status === 'completed') return 'emerald';
  if (status === 'draft') return 'slate';
  return 'slate';
});

const calendarStatusLabel = computed(() => {
  if (!selectedCalendar.value) return 'Draft';
  if (selectedCalendar.value.publicationStatus === 'published') {
    const v = selectedCalendar.value.version || '1.0';
    return v.startsWith('v') ? `Published ${v}` : `Published v${v}`;
  }
  return 'Draft';
});

const calendarStatusVariant = computed(() => {
  if (!selectedCalendar.value) return 'slate';
  return selectedCalendar.value.publicationStatus === 'published' ? 'emerald' : 'slate';
});

// --------------------------------------------------------------------------
// 5. Action Mutations & Handlers
// --------------------------------------------------------------------------
const approveMutation = useApproveAcademicYearMutation({
  onSuccess: () => {
    isApproveModalOpen.value = false;
  },
});

const activateMutation = useActivateAcademicYearMutation({
  onSuccess: () => {
    isActivateModalOpen.value = false;
  },
});

const publishMutation = usePublishCalendarMutation({
  onSuccess: () => {
    isPublishModalOpen.value = false;
  },
});

const presetMutation = useApplyPresetMutation({
  onSuccess: () => {
    isPresetModalOpen.value = false;
  },
});

const closureMutation = useDeclareSchoolClosureMutation({
  onSuccess: () => {
    isClosureModalOpen.value = false;
  },
});

const createYearMutation = useCreateAcademicYearMutation({
  onSuccess: (data) => {
    isCreateYearModalOpen.value = false;
    selectedAcademicYearId.value = data.id;
  },
});

// Approve Form State
const approverNotes = ref('');
function handleApproveYear(): void {
  if (!selectedAcademicYearId.value) return;
  approveMutation.mutate({
    id: selectedAcademicYearId.value,
    payload: {
      notes: approverNotes.value || undefined,
      approvedBy: authStore.user?.username || undefined,
    },
  });
}

// Activate Form State
function handleActivateYear(): void {
  if (!selectedAcademicYearId.value) return;
  activateMutation.mutate(selectedAcademicYearId.value);
}

// Publish Form State
function handlePublishCalendar(): void {
  if (!selectedCalendar.value) return;
  publishMutation.mutate(selectedCalendar.value.id);
}

// Preset Form State
function handleApplyPreset(): void {
  if (!selectedAcademicYearId.value) return;
  presetMutation.mutate(selectedAcademicYearId.value);
}

// Closure Form State
const closureForm = ref({
  reasonCategory: 'weather_emergency' as const,
  reasonDetails: '',
  startDate: '',
  endDate: '',
  affectedCampus: 'All',
  affectedPopulation: 'All Students & Staff',
});

function handleDeclareClosure(): void {
  if (!selectedAcademicYearId.value) return;
  closureMutation.mutate({
    academicYearId: selectedAcademicYearId.value,
    reasonCategory: closureForm.value.reasonCategory,
    reasonDetails: closureForm.value.reasonDetails,
    startDate: closureForm.value.startDate,
    endDate: closureForm.value.endDate,
    affectedCampus: closureForm.value.affectedCampus,
    affectedPopulation: closureForm.value.affectedPopulation,
  });
}

// Create Year Form State
const newYearForm = ref({
  code: '',
  name: '',
  startDate: '',
  endDate: '',
  notes: '',
});

function handleCreateYear(): void {
  createYearMutation.mutate({
    code: newYearForm.value.code,
    name: newYearForm.value.name,
    startDate: newYearForm.value.startDate,
    endDate: newYearForm.value.endDate,
    notes: newYearForm.value.notes || undefined,
  });
}
</script>

<template>
  <div class="space-y-6" data-testid="calendar-workspace-view">
    <!-- Institutional Header & Context Controls -->
    <PageHeader
      title="Calendar & Periods Workspace"
      subtitle="Institutional schedule coordination, operational periods hierarchy, visual calendar matrix, and campus closures."
      :breadcrumbs="breadcrumbs"
    >
      <template #title-extra>
        <div class="flex items-center gap-2 flex-wrap" data-testid="header-status-badges">
          <!-- Academic Year Status Badge -->
          <StatusPill
            :status="academicYearStatusVariant"
            :label="academicYearStatusLabel"
            size="sm"
            data-testid="academic-year-status-pill"
          />

          <!-- Calendar Publication Status Badge -->
          <StatusPill
            :status="calendarStatusVariant"
            :label="calendarStatusLabel"
            size="sm"
            data-testid="calendar-status-pill"
          />
        </div>
      </template>

      <template #actions>
        <div class="flex items-center gap-3 flex-wrap">
          <!-- Academic Year Selector Dropdown -->
          <div class="flex items-center gap-2">
            <Calendar class="w-4 h-4 text-slate-500" aria-hidden="true" />
            <Select
              v-model="selectedAcademicYearId"
              :options="academicYearOptions"
              option-label="label"
              option-value="id"
              placeholder="Select Academic Year"
              data-testid="academic-year-select"
              class="w-64 text-sm"
              :loading="isLoadingYears"
            />
          </div>

          <!-- New Year Action Button -->
          <Button
            v-if="canManage"
            type="button"
            size="small"
            data-testid="action-new-year-btn"
            class="bg-teal-600 hover:bg-teal-700 text-white flex items-center gap-1.5 cursor-pointer"
            @click="isCreateYearModalOpen = true"
          >
            <PlusCircle class="w-3.5 h-3.5" />
            <span>New Year</span>
          </Button>
        </div>
      </template>
    </PageHeader>

    <!-- Primary Action Toolbar -->
    <ContentCard
      no-padding
      class="bg-slate-50/70 border border-slate-200"
      data-testid="workspace-toolbar-card"
    >
      <div
        class="px-5 py-3 flex flex-col md:flex-row md:items-center justify-between gap-4 text-xs"
      >
        <!-- Active Year Context Summary -->
        <div class="flex items-center gap-3">
          <div v-if="selectedAcademicYear" class="space-y-0.5">
            <div class="flex items-center gap-2">
              <span class="font-bold text-slate-800 text-sm font-mono">
                {{ selectedAcademicYear.code }}
              </span>
              <span class="text-slate-600 font-medium">— {{ selectedAcademicYear.name }}</span>
            </div>
            <div class="text-slate-500 text-[11px]">
              Effective: {{ selectedAcademicYear.startDate }} to {{ selectedAcademicYear.endDate }}
            </div>
          </div>
          <div v-else class="text-slate-500 italic">
            No academic year selected. Choose or create an academic year above.
          </div>
        </div>

        <!-- Action Toolbar Buttons (with permission checks) -->
        <div class="flex items-center gap-2 flex-wrap" data-testid="workspace-action-toolbar">
          <!-- Approve Year Button -->
          <Button
            v-if="canApprove"
            type="button"
            size="small"
            outlined
            data-testid="action-approve-year-btn"
            :disabled="!selectedAcademicYear || selectedAcademicYear.status !== 'draft'"
            class="flex items-center gap-1 cursor-pointer text-slate-700 hover:text-emerald-700 disabled:opacity-50"
            @click="isApproveModalOpen = true"
          >
            <CheckCircle2 class="w-3.5 h-3.5 text-emerald-600" />
            <span>Approve Year</span>
          </Button>

          <!-- Activate Year Button -->
          <Button
            v-if="canManage"
            type="button"
            size="small"
            outlined
            data-testid="action-activate-year-btn"
            :disabled="!selectedAcademicYear || selectedAcademicYear.status !== 'approved'"
            class="flex items-center gap-1 cursor-pointer text-slate-700 hover:text-teal-700 disabled:opacity-50"
            @click="isActivateModalOpen = true"
          >
            <Sparkles class="w-3.5 h-3.5 text-teal-600" />
            <span>Activate Year</span>
          </Button>

          <!-- Publish Calendar Button -->
          <Button
            v-if="canPublish"
            type="button"
            size="small"
            outlined
            data-testid="action-publish-calendar-btn"
            :disabled="!selectedCalendar || selectedCalendar.publicationStatus === 'published'"
            class="flex items-center gap-1 cursor-pointer text-slate-700 hover:text-sky-700 disabled:opacity-50"
            @click="isPublishModalOpen = true"
          >
            <Send class="w-3.5 h-3.5 text-sky-600" />
            <span>Publish Calendar</span>
          </Button>

          <!-- Declare Closure Button -->
          <Button
            v-if="canDeclareClosure"
            type="button"
            size="small"
            outlined
            severity="danger"
            data-testid="action-declare-closure-btn"
            :disabled="!selectedAcademicYear"
            class="flex items-center gap-1 cursor-pointer text-rose-700 hover:bg-rose-50 disabled:opacity-50"
            @click="isClosureModalOpen = true"
          >
            <ShieldAlert class="w-3.5 h-3.5 text-rose-600" />
            <span>Declare Closure</span>
          </Button>

          <!-- Apply Preset Button -->
          <Button
            v-if="canManage"
            type="button"
            size="small"
            outlined
            data-testid="action-apply-preset-btn"
            :disabled="!selectedAcademicYear"
            class="flex items-center gap-1 cursor-pointer text-slate-700 hover:text-indigo-700 disabled:opacity-50"
            @click="isPresetModalOpen = true"
          >
            <Layers class="w-3.5 h-3.5 text-indigo-600" />
            <span>Apply Preset</span>
          </Button>
        </div>
      </div>
    </ContentCard>

    <!-- Workspace State: Loading / Empty / Tabs -->
    <div v-if="isLoadingYears" class="py-12 flex justify-center" data-testid="workspace-loading">
      <LoadingSpinner size="lg" message="Loading academic years and calendar configuration..." />
    </div>

    <div
      v-else-if="!academicYears || academicYears.length === 0"
      class="py-8"
      data-testid="workspace-empty-state"
    >
      <EmptyState
        title="No Academic Years Configured"
        description="There are currently no institutional academic years in the system. Create your first academic year to begin managing terms, calendars, and closures."
      >
        <template #actions>
          <Button
            v-if="canManage"
            type="button"
            size="small"
            class="bg-teal-600 hover:bg-teal-700 text-white"
            @click="isCreateYearModalOpen = true"
          >
            <PlusCircle class="w-4 h-4 mr-1.5" />
            <span>Create Academic Year</span>
          </Button>
        </template>
      </EmptyState>
    </div>

    <!-- 3-Tab Workspace Navigation Container -->
    <div v-else class="space-y-4" data-testid="workspace-tabs-container">
      <Tabs v-model:value="activeTab" class="w-full" data-testid="workspace-tabs">
        <TabList class="border-b border-slate-200">
          <Tab value="matrix" data-testid="tab-matrix">
            <div class="flex items-center gap-2 py-1">
              <CalendarDays class="w-4 h-4 text-teal-600" />
              <span class="font-medium text-sm">Visual Calendar Matrix</span>
            </div>
          </Tab>
          <Tab value="periods" data-testid="tab-periods">
            <div class="flex items-center gap-2 py-1">
              <Layers class="w-4 h-4 text-teal-600" />
              <span class="font-medium text-sm">Periods Hierarchy</span>
            </div>
          </Tab>
          <Tab value="closures" data-testid="tab-closures">
            <div class="flex items-center gap-2 py-1">
              <History class="w-4 h-4 text-teal-600" />
              <span class="font-medium text-sm">Closures & Amendments</span>
            </div>
          </Tab>
        </TabList>

        <TabPanels class="pt-4">
          <!-- Tab 1: Visual Calendar Matrix & Exception Entries -->
          <TabPanel value="matrix" data-testid="panel-matrix">
            <CalendarMatrixTab
              :academic-year-id="selectedAcademicYearId"
              :calendar-id="selectedCalendar?.id ?? null"
            />
          </TabPanel>

          <!-- Tab 2: Academic Years & Hierarchical Operational Periods -->
          <TabPanel value="periods" data-testid="panel-periods">
            <PeriodsTreeTab :academic-year-id="selectedAcademicYearId" />
          </TabPanel>

          <!-- Tab 3: Campus Closures & Amendment History -->
          <TabPanel value="closures" data-testid="panel-closures">
            <ClosuresAmendmentsTab
              :academic-year-id="selectedAcademicYearId"
              :calendar-id="selectedCalendar?.id ?? null"
            />
          </TabPanel>
        </TabPanels>
      </Tabs>
    </div>

    <!-- ---------------------------------------------------------------------- -->
    <!-- Modals & Dialogs                                                       -->
    <!-- ---------------------------------------------------------------------- -->

    <!-- 1. Create Academic Year Modal -->
    <Dialog
      v-model:visible="isCreateYearModalOpen"
      header="Create Academic Year"
      modal
      class="max-w-md w-full"
      data-testid="create-year-modal"
    >
      <form class="space-y-4 pt-2" @submit.prevent="handleCreateYear">
        <FormField id="new-ay-code" label="Academic Year Code" required hint="e.g. AY-2026-2027">
          <InputText
            id="new-ay-code"
            v-model="newYearForm.code"
            required
            class="w-full text-sm"
            data-testid="modal-code-input"
          />
        </FormField>
        <FormField
          id="new-ay-name"
          label="Descriptive Name"
          required
          hint="e.g. Academic Year 2026/2027"
        >
          <InputText
            id="new-ay-name"
            v-model="newYearForm.name"
            required
            class="w-full text-sm"
            data-testid="modal-name-input"
          />
        </FormField>
        <div class="grid grid-cols-2 gap-3">
          <FormField id="new-ay-start" label="Start Date" required>
            <InputText
              id="new-ay-start"
              v-model="newYearForm.startDate"
              type="date"
              required
              class="w-full text-sm"
              data-testid="modal-start-date-input"
            />
          </FormField>
          <FormField id="new-ay-end" label="End Date" required>
            <InputText
              id="new-ay-end"
              v-model="newYearForm.endDate"
              type="date"
              required
              class="w-full text-sm"
              data-testid="modal-end-date-input"
            />
          </FormField>
        </div>
        <FormField id="new-ay-notes" label="Notes" hint="Optional institutional context">
          <Textarea
            id="new-ay-notes"
            v-model="newYearForm.notes"
            rows="2"
            class="w-full text-sm"
            data-testid="modal-notes-input"
          />
        </FormField>
        <div class="flex justify-end gap-2 pt-2 border-t border-slate-100">
          <Button
            type="button"
            text
            size="small"
            label="Cancel"
            @click="isCreateYearModalOpen = false"
          />
          <Button
            type="submit"
            size="small"
            label="Create Year"
            class="bg-teal-600 hover:bg-teal-700 text-white"
            :loading="createYearMutation.isPending.value"
            data-testid="modal-submit-create-year-btn"
          />
        </div>
      </form>
    </Dialog>

    <!-- 2. Approve Year Modal -->
    <Dialog
      v-model:visible="isApproveModalOpen"
      header="Approve Academic Year"
      modal
      class="max-w-md w-full"
      data-testid="approve-year-modal"
    >
      <div class="space-y-4 pt-2 text-sm text-slate-700">
        <p>
          You are about to formally approve
          <strong class="font-mono text-slate-900">{{ selectedAcademicYear?.code }}</strong>
          ({{ selectedAcademicYear?.name }}). Once approved, the academic year structure is locked
          against ad-hoc modification without formal record correction.
        </p>
        <FormField id="approve-notes" label="Approver Policy Notes" hint="Optional reference notes">
          <Textarea
            id="approve-notes"
            v-model="approverNotes"
            rows="3"
            placeholder="e.g. Approved per Academic Council Decision 2026/04."
            class="w-full text-sm"
            data-testid="modal-approve-notes-input"
          />
        </FormField>
        <div class="flex justify-end gap-2 pt-2 border-t border-slate-100">
          <Button
            type="button"
            text
            size="small"
            label="Cancel"
            @click="isApproveModalOpen = false"
          />
          <Button
            type="button"
            size="small"
            label="Confirm Approval"
            class="bg-emerald-600 hover:bg-emerald-700 text-white"
            :loading="approveMutation.isPending.value"
            data-testid="modal-confirm-approve-btn"
            @click="handleApproveYear"
          />
        </div>
      </div>
    </Dialog>

    <!-- 3. Activate Year Modal -->
    <Dialog
      v-model:visible="isActivateModalOpen"
      header="Activate Academic Year"
      modal
      class="max-w-md w-full"
      data-testid="activate-year-modal"
    >
      <div class="space-y-4 pt-2 text-sm text-slate-700">
        <div
          class="p-3 bg-amber-50 border border-amber-200 rounded text-amber-800 text-xs flex gap-2"
        >
          <AlertTriangle class="w-4 h-4 text-amber-600 shrink-0 mt-0.5" />
          <span>
            Activating <strong>{{ selectedAcademicYear?.code }}</strong> will mark it as the current
            operational year across all SIS modules (Students, Finance, Operations). Any previously
            active year will transition to completed status.
          </span>
        </div>
        <p>Confirm activation of this academic year for institutional operations?</p>
        <div class="flex justify-end gap-2 pt-2 border-t border-slate-100">
          <Button
            type="button"
            text
            size="small"
            label="Cancel"
            @click="isActivateModalOpen = false"
          />
          <Button
            type="button"
            size="small"
            label="Confirm Activation"
            class="bg-teal-600 hover:bg-teal-700 text-white"
            :loading="activateMutation.isPending.value"
            data-testid="modal-confirm-activate-btn"
            @click="handleActivateYear"
          />
        </div>
      </div>
    </Dialog>

    <!-- 4. Publish Calendar Modal -->
    <Dialog
      v-model:visible="isPublishModalOpen"
      header="Publish Calendar"
      modal
      class="max-w-md w-full"
      data-testid="publish-calendar-modal"
    >
      <div class="space-y-4 pt-2 text-sm text-slate-700">
        <p>
          Publishing the school calendar locks Version
          <strong>{{
            selectedCalendar?.version ? `v${selectedCalendar.version}` : 'v1.0'
          }}</strong> for operational use. All subsequent date shifts or milestone additions will produce an auditable Calendar
          Amendment record.
        </p>
        <div class="flex justify-end gap-2 pt-2 border-t border-slate-100">
          <Button
            type="button"
            text
            size="small"
            label="Cancel"
            @click="isPublishModalOpen = false"
          />
          <Button
            type="button"
            size="small"
            label="Publish Calendar"
            class="bg-sky-600 hover:bg-sky-700 text-white"
            :loading="publishMutation.isPending.value"
            data-testid="modal-confirm-publish-btn"
            @click="handlePublishCalendar"
          />
        </div>
      </div>
    </Dialog>

    <!-- 5. Apply Preset Modal -->
    <Dialog
      v-model:visible="isPresetModalOpen"
      header="Apply Standard Institutional Preset"
      modal
      class="max-w-md w-full"
      data-testid="apply-preset-modal"
    >
      <div class="space-y-4 pt-2 text-sm text-slate-700">
        <p>
          This action applies the <strong>Standard Indonesian 2-Semester Structure</strong> to
          academic year <strong class="font-mono">{{ selectedAcademicYear?.code }}</strong
          >:
        </p>
        <ul class="list-disc list-inside text-xs text-slate-600 space-y-1">
          <li><strong>Semester 1 (Ganjil):</strong> July 15 – December 20</li>
          <li><strong>Semester 2 (Genap):</strong> January 5 – June 25</li>
        </ul>
        <p class="text-xs text-slate-500">
          Periods will be created with status <em>draft</em>. You can modify or add additional
          nested periods afterwards.
        </p>
        <div class="flex justify-end gap-2 pt-2 border-t border-slate-100">
          <Button
            type="button"
            text
            size="small"
            label="Cancel"
            @click="isPresetModalOpen = false"
          />
          <Button
            type="button"
            size="small"
            label="Apply Preset"
            class="bg-indigo-600 hover:bg-indigo-700 text-white"
            :loading="presetMutation.isPending.value"
            data-testid="modal-confirm-preset-btn"
            @click="handleApplyPreset"
          />
        </div>
      </div>
    </Dialog>

    <!-- 6. Declare Closure Modal -->
    <Dialog
      v-model:visible="isClosureModalOpen"
      header="Declare Campus Closure"
      modal
      class="max-w-lg w-full"
      data-testid="declare-closure-modal"
    >
      <form class="space-y-4 pt-2" @submit.prevent="handleDeclareClosure">
        <FormField id="closure-category" label="Reason Category" required>
          <select
            id="closure-category"
            v-model="closureForm.reasonCategory"
            required
            class="w-full h-9 px-3 py-1.5 text-sm border border-slate-300 rounded-md focus:outline-none focus:ring-2 focus:ring-teal-500"
            data-testid="modal-closure-category-select"
          >
            <option value="weather_emergency">Weather Emergency</option>
            <option value="public_health">Public Health</option>
            <option value="infrastructure_failure">Infrastructure Failure</option>
            <option value="administrative_directive">Administrative Directive</option>
            <option value="security_incident">Security Incident</option>
            <option value="other">Other</option>
          </select>
        </FormField>

        <div class="grid grid-cols-2 gap-3">
          <FormField id="closure-start" label="Start Date" required>
            <InputText
              id="closure-start"
              v-model="closureForm.startDate"
              type="date"
              required
              class="w-full text-sm"
              data-testid="modal-closure-start-input"
            />
          </FormField>
          <FormField id="closure-end" label="End Date" required>
            <InputText
              id="closure-end"
              v-model="closureForm.endDate"
              type="date"
              required
              class="w-full text-sm"
              data-testid="modal-closure-end-input"
            />
          </FormField>
        </div>

        <FormField
          id="closure-reason-details"
          label="Detailed Justification Reason"
          required
          hint="Administrative rationale and background for campus closure"
        >
          <Textarea
            id="closure-reason-details"
            v-model="closureForm.reasonDetails"
            required
            rows="3"
            class="w-full text-sm"
            data-testid="modal-closure-justification-input"
          />
        </FormField>

        <div class="flex justify-end gap-2 pt-2 border-t border-slate-100">
          <Button
            type="button"
            text
            size="small"
            label="Cancel"
            @click="isClosureModalOpen = false"
          />
          <Button
            type="submit"
            size="small"
            label="Declare Closure"
            severity="danger"
            class="bg-rose-600 hover:bg-rose-700 text-white"
            :loading="closureMutation.isPending.value"
            data-testid="modal-submit-closure-btn"
          />
        </div>
      </form>
    </Dialog>
  </div>
</template>
