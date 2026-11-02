<script setup lang="ts">
import { ref, computed, toRef } from 'vue';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Timeline from 'primevue/timeline';
import Button from 'primevue/button';
import Dialog from 'primevue/dialog';
import {
  ShieldAlert,
  History,
  FileEdit,
  PlusCircle,
  ArrowRight,
  Clock,
  UserCheck,
  Unlock,
  AlertTriangle,
} from 'lucide-vue-next';
import {
  ContentCard,
  StatusPill,
  EmptyState,
  LoadingSpinner,
} from '@/shared/components';
import { useAuthStore } from '@/app/stores/auth.store';
import { CALENDAR_PERMISSIONS } from '../../constants/permissions';
import {
  useSchoolClosuresQuery,
  useLiftSchoolClosureMutation,
  type SchoolClosureDto,
} from '../../composables/useSchoolClosuresQuery';
import {
  useCalendarAmendmentsQuery,
  useRecordCorrectionsQuery,
} from '../../composables/useAmendmentsQuery';
import DeclareClosureModal from '../modals/DeclareClosureModal.vue';

const props = defineProps<{
  academicYearId?: string | null;
  calendarId?: string | null;
}>();

// ---------------------------------------------------------------------------
// Authorization & Permissions
// ---------------------------------------------------------------------------
const authStore = useAuthStore();
const canDeclareClosure = computed(
  () =>
    authStore.hasPermission(CALENDAR_PERMISSIONS.DECLARE_CLOSURE) ||
    authStore.hasPermission(CALENDAR_PERMISSIONS.MANAGE),
);

// ---------------------------------------------------------------------------
// Modals State
// ---------------------------------------------------------------------------
const isDeclareModalOpen = ref(false);
const isLiftModalOpen = ref(false);
const selectedClosureForLift = ref<SchoolClosureDto | null>(null);

// ---------------------------------------------------------------------------
// Section 1: Campus Closures Data
// ---------------------------------------------------------------------------
const academicYearRef = toRef(() => props.academicYearId);
const calendarRef = toRef(() => props.calendarId);

const {
  data: closures,
  isLoading: isLoadingClosures,
  refetch: refetchClosures,
} = useSchoolClosuresQuery(academicYearRef);

const liftMutation = useLiftSchoolClosureMutation({
  onSuccess: () => {
    isLiftModalOpen.value = false;
    selectedClosureForLift.value = null;
  },
});

function openLiftModal(closure: SchoolClosureDto): void {
  selectedClosureForLift.value = closure;
  isLiftModalOpen.value = true;
}

function handleConfirmLift(): void {
  if (!selectedClosureForLift.value) return;
  liftMutation.mutate(selectedClosureForLift.value.id);
}

// ---------------------------------------------------------------------------
// Section 2: Calendar Amendments & Record Corrections Data
// ---------------------------------------------------------------------------
const {
  data: amendments,
  isLoading: isLoadingAmendments,
} = useCalendarAmendmentsQuery(calendarRef);

const {
  data: corrections,
  isLoading: isLoadingCorrections,
} = useRecordCorrectionsQuery(academicYearRef);

// ---------------------------------------------------------------------------
// Formatters & Presentation Helpers
// ---------------------------------------------------------------------------
function formatCategory(category: string): string {
  switch (category) {
    case 'weather_disaster':
      return 'Weather Disaster';
    case 'infrastructure_hazard':
      return 'Infrastructure Hazard';
    case 'health_safety':
      return 'Health & Safety';
    case 'government_mandate':
      return 'Government Mandate';
    case 'institutional_policy':
      return 'Institutional Policy';
    case 'emergency':
      return 'Emergency Incident';
    default:
      return category.replace(/_/g, ' ').replace(/\b\w/g, (c) => c.toUpperCase());
  }
}

function formatPopulation(pop: string): string {
  switch (pop) {
    case 'all':
      return 'All Students & Staff';
    case 'students_only':
      return 'Students Only';
    case 'staff_and_students':
      return 'Staff & Students';
    default:
      return pop;
  }
}

function getClosureStatusVariant(
  status: string,
): 'danger' | 'warning' | 'emerald' | 'slate' | 'info' {
  switch (status) {
    case 'in_effect':
      return 'danger';
    case 'authorized':
      return 'warning';
    case 'lifted':
      return 'emerald';
    default:
      return 'slate';
  }
}

function formatAmendmentType(type: string): string {
  switch (type) {
    case 'closure_declaration':
      return 'Closure Declared';
    case 'closure_lifted':
      return 'Closure Lifted';
    case 'entry_created':
      return 'Entry Added';
    case 'entry_deleted':
      return 'Entry Removed';
    case 'entry_updated':
      return 'Entry Modified';
    case 'initial_publication':
      return 'Initial Publication';
    default:
      return type.replace(/_/g, ' ').replace(/\b\w/g, (c) => c.toUpperCase());
  }
}

function formatDateTime(dateStr?: string | null): string {
  if (!dateStr) return '—';
  try {
    const d = new Date(dateStr);
    if (Number.isNaN(d.getTime())) return dateStr;
    return d.toLocaleDateString(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  } catch {
    return dateStr;
  }
}
</script>

<template>
  <div class="space-y-8" data-testid="closures-amendments-tab">
    <!-- ===================================================================== -->
    <!-- SECTION 1: Campus Closures (UC-OPS-12)                                 -->
    <!-- ===================================================================== -->
    <ContentCard
      title="Declared Campus Closures"
      subtitle="Authoritative emergency closures impacting instructional dates, school events, and staff schedules."
      data-testid="section-campus-closures"
    >
      <template #header-actions>
        <div class="flex items-center gap-2">
          <Button
            v-if="canDeclareClosure"
            type="button"
            size="small"
            severity="danger"
            class="bg-rose-600 hover:bg-rose-700 text-white flex items-center gap-1.5 cursor-pointer text-xs"
            data-testid="btn-declare-closure"
            :disabled="!academicYearId"
            @click="isDeclareModalOpen = true"
          >
            <ShieldAlert class="w-3.5 h-3.5" />
            <span>Declare Closure</span>
          </Button>
        </div>
      </template>

      <!-- Closures State: Loading / Empty / Data Table -->
      <div v-if="isLoadingClosures" class="py-8 flex justify-center" data-testid="closures-loading">
        <LoadingSpinner size="md" message="Loading declared closures..." />
      </div>

      <div
        v-else-if="!closures || closures.length === 0"
        class="py-6"
        data-testid="closures-empty-state"
      >
        <EmptyState
          title="No Campus Closures Declared"
          description="There are no emergency or administrative campus closures declared for this academic year. Academic schedules are running normally."
        >
          <template #actions>
            <Button
              v-if="canDeclareClosure"
              type="button"
              size="small"
              class="bg-rose-600 hover:bg-rose-700 text-white"
              data-testid="btn-declare-closure-empty"
              :disabled="!academicYearId"
              @click="isDeclareModalOpen = true"
            >
              <PlusCircle class="w-4 h-4 mr-1.5" />
              <span>Declare Campus Closure</span>
            </Button>
          </template>
        </EmptyState>
      </div>

      <div v-else class="overflow-x-auto" data-testid="closures-table-wrapper">
        <DataTable
          :value="closures"
          responsive-layout="scroll"
          class="text-xs w-full"
          data-testid="closures-data-table"
        >
          <!-- Closure Number -->
          <Column field="closureNumber" header="Closure #" sortable>
            <template #body="{ data }">
              <div class="font-mono font-semibold text-slate-800 flex items-center gap-1.5">
                <ShieldAlert class="w-3.5 h-3.5 text-rose-600 shrink-0" />
                <span>{{ data.closureNumber }}</span>
              </div>
            </template>
          </Column>

          <!-- Reason Category -->
          <Column field="reasonCategory" header="Category" sortable>
            <template #body="{ data }">
              <span
                class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-slate-100 text-slate-700"
              >
                {{ formatCategory(data.reasonCategory) }}
              </span>
            </template>
          </Column>

          <!-- Date Range -->
          <Column header="Date Span" sortable>
            <template #body="{ data }">
              <div class="flex items-center gap-1 text-slate-700 whitespace-nowrap">
                <span class="font-medium">{{ data.startDate }}</span>
                <span class="text-slate-400">→</span>
                <span class="font-medium">{{ data.endDate }}</span>
              </div>
            </template>
          </Column>

          <!-- Affected Campus -->
          <Column field="affectedCampus" header="Campus" sortable>
            <template #body="{ data }">
              <span class="text-slate-700 font-medium">{{ data.affectedCampus || 'All' }}</span>
            </template>
          </Column>

          <!-- Affected Population -->
          <Column field="affectedPopulation" header="Population">
            <template #body="{ data }">
              <span class="text-slate-600">{{ formatPopulation(data.affectedPopulation) }}</span>
            </template>
          </Column>

          <!-- Status -->
          <Column field="status" header="Status" sortable>
            <template #body="{ data }">
              <StatusPill
                :status="getClosureStatusVariant(data.status)"
                :label="data.status"
                size="sm"
                :data-testid="`closure-status-${data.id}`"
              />
            </template>
          </Column>

          <!-- Authorized By -->
          <Column field="authorizedBy" header="Authorized By">
            <template #body="{ data }">
              <div class="text-slate-700">
                <div class="font-medium">{{ data.authorizedBy }}</div>
                <div class="text-[11px] text-slate-400">{{ data.authorizedDate }}</div>
              </div>
            </template>
          </Column>

          <!-- Action: Lift Closure -->
          <Column header="Actions" align-frozen="right" frozen>
            <template #body="{ data }">
              <Button
                v-if="data.status !== 'lifted' && data.status !== 'cancelled' && canDeclareClosure"
                type="button"
                size="small"
                outlined
                severity="success"
                class="text-xs px-2 py-1 text-emerald-700 border-emerald-300 hover:bg-emerald-50 cursor-pointer flex items-center gap-1"
                :data-testid="`btn-lift-closure-${data.id}`"
                @click="openLiftModal(data)"
              >
                <Unlock class="w-3 h-3 text-emerald-600" />
                <span>Lift</span>
              </Button>
              <span v-else class="text-slate-400 text-[11px] italic">
                {{ data.status === 'lifted' ? 'Lifted' : '—' }}
              </span>
            </template>
          </Column>
        </DataTable>
      </div>
    </ContentCard>

    <!-- ===================================================================== -->
    <!-- SECTION 2: Calendar Amendments & Record Corrections                    -->
    <!-- ===================================================================== -->
    <div class="space-y-6" data-testid="section-amendments-corrections">
      <!-- Subsection 2A: Calendar Amendments Timeline -->
      <ContentCard
        title="Calendar Version Amendments"
        subtitle="Historical audit timeline of adjustments made to published calendars, version progressions, and authorizations."
        data-testid="card-calendar-amendments"
      >
        <template #header-actions>
          <div class="flex items-center gap-1.5 text-xs text-slate-500">
            <History class="w-4 h-4 text-teal-600" />
            <span>TD-05 Audit Trail</span>
          </div>
        </template>

        <!-- Loading State -->
        <div
          v-if="isLoadingAmendments"
          class="py-8 flex justify-center"
          data-testid="amendments-loading"
        >
          <LoadingSpinner size="md" message="Loading calendar amendment timeline..." />
        </div>

        <!-- No Calendar Selected -->
        <div
          v-else-if="!calendarId"
          class="py-6 text-center text-slate-500"
          data-testid="amendments-no-calendar"
        >
          <p class="text-xs">
            Select an academic year with a published school calendar to view the amendment audit
            timeline.
          </p>
        </div>

        <!-- Empty Amendments -->
        <div
          v-else-if="!amendments || amendments.length === 0"
          class="py-6"
          data-testid="amendments-empty-state"
        >
          <EmptyState
            title="No Calendar Amendments Recorded"
            description="The selected calendar is currently at its initial version. Any subsequent holiday additions, date shifts, or campus closures will generate versioned amendments here."
          />
        </div>

        <!-- Timeline Visualization -->
        <div v-else class="pt-2 pb-4" data-testid="amendments-timeline-wrapper">
          <Timeline
            :value="amendments"
            class="customized-timeline"
            data-testid="amendments-timeline"
          >
            <!-- Marker Slot -->
            <template #marker="{ item }">
              <span
                class="flex w-7 h-7 items-center justify-center rounded-full text-white shadow-sm ring-4 ring-white"
                :class="item.amendmentType.includes('closure') ? 'bg-amber-600' : 'bg-teal-600'"
              >
                <Clock class="w-3.5 h-3.5" />
              </span>
            </template>

            <!-- Content Slot -->
            <template #content="{ item }">
              <div
                class="p-4 bg-slate-50 border border-slate-200 rounded-lg shadow-xs mb-4 space-y-2 text-xs"
                :data-testid="`amendment-card-${item.id}`"
              >
                <!-- Top Row: Version Transition & Type -->
                <div class="flex items-center justify-between gap-2 flex-wrap">
                  <div class="flex items-center gap-2">
                    <span
                      class="inline-flex items-center gap-1 font-mono font-bold text-xs px-2 py-0.5 rounded bg-teal-100 text-teal-800"
                    >
                      <span>v{{ item.fromVersion }}</span>
                      <ArrowRight class="w-3 h-3 text-teal-600" />
                      <span>v{{ item.toVersion }}</span>
                    </span>
                    <span class="font-mono text-slate-400 text-[11px]">{{
                      item.amendmentNumber
                    }}</span>
                  </div>
                  <span
                    class="px-2 py-0.5 rounded-full text-[11px] font-medium bg-slate-200 text-slate-700"
                  >
                    {{ formatAmendmentType(item.amendmentType) }}
                  </span>
                </div>

                <!-- Change Summary -->
                <p class="font-medium text-slate-800 text-sm">
                  {{ item.changeSummary }}
                </p>

                <!-- Justification -->
                <div class="text-slate-600 bg-white p-2.5 rounded border border-slate-100">
                  <span class="font-medium text-slate-700">Justification: </span>
                  <span>{{ item.reason }}</span>
                </div>

                <!-- Meta footer -->
                <div
                  class="flex items-center justify-between text-[11px] text-slate-500 pt-1 border-t border-slate-200/60 flex-wrap gap-2"
                >
                  <div class="flex items-center gap-1">
                    <UserCheck class="w-3 h-3 text-slate-400" />
                    <span>Authorized by: <strong>{{ item.authorizedBy }}</strong></span>
                  </div>
                  <div class="flex items-center gap-3">
                    <span>Effective: {{ item.effectiveDate }}</span>
                    <span>Logged: {{ formatDateTime(item.createdAt) }}</span>
                  </div>
                </div>
              </div>
            </template>
          </Timeline>
        </div>
      </ContentCard>

      <!-- Subsection 2B: Operations Record Corrections -->
      <ContentCard
        title="Operations Record Corrections Log"
        subtitle="Authoritative audit trail of verified field corrections made to approved operational records (UC-OPS-13)."
        data-testid="card-record-corrections"
      >
        <template #header-actions>
          <div class="flex items-center gap-1.5 text-xs text-slate-500">
            <FileEdit class="w-4 h-4 text-indigo-600" />
            <span>UC-OPS-13 Audit</span>
          </div>
        </template>

        <!-- Loading State -->
        <div
          v-if="isLoadingCorrections"
          class="py-8 flex justify-center"
          data-testid="corrections-loading"
        >
          <LoadingSpinner size="md" message="Loading record corrections log..." />
        </div>

        <!-- Empty State -->
        <div
          v-else-if="!corrections || corrections.length === 0"
          class="py-6"
          data-testid="corrections-empty-state"
        >
          <EmptyState
            title="No Record Corrections Logged"
            description="No approved academic years, operational periods, or calendars have required administrative record corrections."
          />
        </div>

        <!-- Table View -->
        <div v-else class="overflow-x-auto" data-testid="corrections-table-wrapper">
          <DataTable
            :value="corrections"
            responsive-layout="scroll"
            class="text-xs w-full"
            data-testid="corrections-data-table"
          >
            <!-- Target Table -->
            <Column field="targetTableName" header="Target Table" sortable>
              <template #body="{ data }">
                <span class="font-mono text-slate-700 bg-slate-100 px-1.5 py-0.5 rounded text-[11px]">
                  {{ data.targetTableName }}
                </span>
              </template>
            </Column>

            <!-- Target Record ID -->
            <Column field="targetRecordId" header="Record ID">
              <template #body="{ data }">
                <span
                  class="font-mono text-slate-500 text-[11px]"
                  :title="data.targetRecordId"
                >
                  {{ data.targetRecordId.substring(0, 8) }}...
                </span>
              </template>
            </Column>

            <!-- Field Name -->
            <Column field="fieldName" header="Field" sortable>
              <template #body="{ data }">
                <span class="font-medium text-slate-800">{{ data.fieldName }}</span>
              </template>
            </Column>

            <!-- Old Value -->
            <Column header="Old Value">
              <template #body="{ data }">
                <span
                  v-if="data.oldValue"
                  class="line-through text-rose-700 bg-rose-50 px-1.5 py-0.5 rounded font-mono text-[11px]"
                >
                  {{ data.oldValue }}
                </span>
                <span v-else class="text-slate-400 italic">null</span>
              </template>
            </Column>

            <!-- New Value -->
            <Column header="New Value">
              <template #body="{ data }">
                <span
                  class="font-semibold text-emerald-800 bg-emerald-50 px-1.5 py-0.5 rounded font-mono text-[11px]"
                >
                  {{ data.newValue }}
                </span>
              </template>
            </Column>

            <!-- Justification Reason -->
            <Column field="justificationReason" header="Justification">
              <template #body="{ data }">
                <span class="text-slate-600">{{ data.justificationReason }}</span>
              </template>
            </Column>

            <!-- Authorized By & At -->
            <Column header="Authorized By">
              <template #body="{ data }">
                <div class="text-slate-700">
                  <div class="font-medium">{{ data.authorizedBy }}</div>
                  <div class="text-[11px] text-slate-400">
                    {{ formatDateTime(data.authorizedAt) }}
                  </div>
                </div>
              </template>
            </Column>
          </DataTable>
        </div>
      </ContentCard>
    </div>

    <!-- ===================================================================== -->
    <!-- MODALS & DIALOGS                                                      -->
    <!-- ===================================================================== -->

    <!-- Declare Campus Closure Modal -->
    <DeclareClosureModal
      v-if="isDeclareModalOpen"
      v-model:visible="isDeclareModalOpen"
      :academic-year-id="academicYearId"
      @declared="() => refetchClosures()"
    />

    <!-- Lift Campus Closure Confirmation Dialog -->
    <Dialog
      v-if="isLiftModalOpen"
      v-model:visible="isLiftModalOpen"
      header="Lift Campus Closure"
      modal
      append-to="self"
      class="max-w-md w-full"
      data-testid="lift-closure-modal"
    >
      <div v-if="selectedClosureForLift" class="space-y-4 pt-2 text-xs">
        <div class="p-3 bg-amber-50 border border-amber-200 rounded-md flex items-start gap-2 text-amber-800">
          <AlertTriangle class="w-4 h-4 text-amber-600 shrink-0 mt-0.5" />
          <div>
            Lifting closure <strong>{{ selectedClosureForLift.closureNumber }}</strong> will restore
            instructional status for affected calendar days and log a published calendar version
            amendment.
          </div>
        </div>

        <div class="bg-slate-50 p-3 rounded-md space-y-1.5 border border-slate-200">
          <div>
            <span class="text-slate-500">Closure:</span>
            <strong class="ml-1 text-slate-800">{{ selectedClosureForLift.closureNumber }}</strong>
          </div>
          <div>
            <span class="text-slate-500">Dates:</span>
            <span class="ml-1 font-medium text-slate-700">
              {{ selectedClosureForLift.startDate }} → {{ selectedClosureForLift.endDate }}
            </span>
          </div>
          <div>
            <span class="text-slate-500">Campus:</span>
            <span class="ml-1 text-slate-700">{{ selectedClosureForLift.affectedCampus }}</span>
          </div>
          <div>
            <span class="text-slate-500">Reason:</span>
            <span class="ml-1 text-slate-700">{{ selectedClosureForLift.reasonDetails }}</span>
          </div>
        </div>

        <div class="flex justify-end gap-2 pt-3 border-t border-slate-100">
          <Button
            type="button"
            text
            size="small"
            label="Cancel"
            data-testid="cancel-lift-btn"
            @click="isLiftModalOpen = false"
          />
          <Button
            type="button"
            size="small"
            label="Confirm Lift Closure"
            severity="success"
            class="bg-emerald-600 hover:bg-emerald-700 text-white cursor-pointer"
            :loading="liftMutation.isPending.value"
            data-testid="confirm-lift-btn"
            @click="handleConfirmLift"
          />
        </div>
      </div>
    </Dialog>
  </div>
</template>
