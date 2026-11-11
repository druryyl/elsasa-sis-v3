<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import Button from 'primevue/button';
import Tabs from 'primevue/tabs';
import TabList from 'primevue/tablist';
import Tab from 'primevue/tab';
import TabPanels from 'primevue/tabpanels';
import TabPanel from 'primevue/tabpanel';
import {
  GraduationCap,
  PanelLeftClose,
  PanelLeftOpen,
  Mail,
  Phone,
  Building2,
  Calendar,
  AlertTriangle,
  RotateCcw,
  User,
  ShieldCheck,
  Award,
  BookOpen,
  History,
  FileCheck2,
} from 'lucide-vue-next';
import {
  PageHeader,
  StatusPill,
  EmptyState,
  LoadingSpinner,
} from '@/shared/components';
import type { BreadcrumbItem } from '@/shared/types/navigation';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import { getQueryString, useRouteQueryHydration } from '@/shared/composables/useRouteQueryHydration';
import { useAuthStore } from '@/app/stores/auth.store';
import type { FacultyQueueFilter, RegisterTeacherProfileResponse } from '../api/teachingTypes';
import { TEACHING_PERMISSIONS } from '../constants/permissions';
import { useTeacherProfileQuery } from '../composables/useFacultyQueries';
import FacultyQueueSidebar from '../components/FacultyQueueSidebar.vue';
import TeacherHeaderBanner from '../components/TeacherHeaderBanner.vue';
import RegisterTeacherModal from '../components/RegisterTeacherModal.vue';
import ProfileAffiliationTab from '../components/ProfileAffiliationTab.vue';
import TeachingCapabilitiesTab from '../components/TeachingCapabilitiesTab.vue';
import QualificationEvidenceTab from '../components/QualificationEvidenceTab.vue';
import AuthorizationsSupervisionTab from '../components/AuthorizationsSupervisionTab.vue';
import CorrectionsAuditTab from '../components/CorrectionsAuditTab.vue';
import ChangeStandingModal from '../components/ChangeStandingModal.vue';
import RecordCorrectionModal from '../components/RecordCorrectionModal.vue';

// ---------------------------------------------------------------------------
// 1. Navigation & Breadcrumbs
// ---------------------------------------------------------------------------
const route = useRoute();
const router = useRouter();

const breadcrumbs: BreadcrumbItem[] = [
  { label: 'Dashboard', to: '/dashboard' },
  { label: 'Teaching', to: '/teaching/faculty' },
  { label: 'Faculty Resources', current: true },
];

// ---------------------------------------------------------------------------
// 2. Authorization & Permissions
// ---------------------------------------------------------------------------
const authStore = useAuthStore();
const canManage = computed(() =>
  authStore.hasPermission(TEACHING_PERMISSIONS.MANAGE),
);
const canAuthorize = computed(() =>
  authStore.hasPermission(TEACHING_PERMISSIONS.AUTHORIZE),
);
const canVerify = computed(() =>
  authStore.hasPermission(TEACHING_PERMISSIONS.VERIFY) ||
  authStore.hasPermission(TEACHING_PERMISSIONS.AUTHORIZE),
);

// ---------------------------------------------------------------------------
// 3. Selection & Route Query Sync
// ---------------------------------------------------------------------------
const selectedTeacherId = ref<string | null>(null);

watch(
  () => route.query.teacherId,
  (queryId) => {
    if (typeof queryId === 'string' && queryId.trim()) {
      selectedTeacherId.value = queryId.trim();
    }
  },
  { immediate: true },
);

function handleSelectTeacher(teacherId: string) {
  selectedTeacherId.value = teacherId;
  router.replace({
    query: {
      ...route.query,
      teacherId,
    },
  });

  // On smaller viewports, auto-collapse sidebar upon selection so user sees dossier
  if (window.innerWidth < 1024) {
    isSidebarOpenOnMobile.value = false;
  }
}

// ---------------------------------------------------------------------------
// 4. Responsive Sidebar Collapsibility State
// ---------------------------------------------------------------------------
const isSidebarCollapsed = ref(false);
const isSidebarOpenOnMobile = ref(false);

function toggleSidebar() {
  if (window.innerWidth < 1024) {
    isSidebarOpenOnMobile.value = !isSidebarOpenOnMobile.value;
  } else {
    isSidebarCollapsed.value = !isSidebarCollapsed.value;
  }
}

// ---------------------------------------------------------------------------
// 5. Teacher Dossier TanStack Query
// ---------------------------------------------------------------------------
const {
  data: dossier,
  isLoading: isLoadingDossier,
  isError: isDossierError,
  refetch: refetchDossier,
} = useTeacherProfileQuery(selectedTeacherId);

const teacher = computed(() => dossier.value ?? null);

// Active Tab & Verification State Filter
const activeTab = ref('profile');
const sidebarQueueFilter = ref<FacultyQueueFilter>('all');
const verificationStateFilter = ref<string>('');

useRouteQueryHydration((query) => {
  const teacherId = getQueryString(query, 'teacherId');
  if (teacherId) {
    selectedTeacherId.value = teacherId;
  }

  const verificationState = getQueryString(query, 'verificationState');
  if (verificationState) {
    verificationStateFilter.value = verificationState;
    if (verificationState.toLowerCase() === 'submitted' || verificationState.toLowerCase() === 'pending') {
      sidebarQueueFilter.value = 'pending_verification';
    }
    activeTab.value = 'qualifications';
  }

  const tab = getQueryString(query, 'tab');
  if (tab) {
    activeTab.value = tab;
  }
});

// Registration Modal open flag (P8-S24)
const isRegisterModalOpen = ref(false);

function handleOpenRegister() {
  isRegisterModalOpen.value = true;
}

function handleTeacherRegistered(response: RegisterTeacherProfileResponse) {
  handleSelectTeacher(response.id);
  isRegisterModalOpen.value = false;
}

// Standing & Record Correction modal states (slotted in P8-S25)
const isChangeStandingModalOpen = ref(false);
const isRecordCorrectionModalOpen = ref(false);

function handleOpenChangeStanding() {
  isChangeStandingModalOpen.value = true;
}

function handleOpenRecordCorrection() {
  isRecordCorrectionModalOpen.value = true;
}
</script>

<template>
  <div
    class="space-y-4 max-w-full"
    data-testid="faculty-resource-workspace-view"
  >
    <!-- Page Header with Responsive Sidebar Toggle -->
    <PageHeader
      title="Faculty Resource Workspace"
      description="Authoritative faculty identity, credential verification, teaching authorizations, and compliance governance."
      :breadcrumbs="breadcrumbs"
    >
      <template #actions>
        <Button
          severity="secondary"
          variant="outlined"
          size="small"
          class="!text-xs !py-1.5 !px-3 !gap-1.5 font-medium"
          data-testid="toggle-sidebar-button"
          :title="isSidebarCollapsed ? 'Expand Work Queue' : 'Collapse Work Queue'"
          @click="toggleSidebar"
        >
          <component
            :is="isSidebarCollapsed ? PanelLeftOpen : PanelLeftClose"
            class="w-4 h-4 text-surface-600 dark:text-surface-300"
          />
          <span class="hidden sm:inline">
            {{ isSidebarCollapsed ? 'Show Queue' : 'Hide Queue' }}
          </span>
        </Button>
      </template>
    </PageHeader>

    <!-- Master-Detail Workbench Shell (SCR-TCH-01) -->
    <div class="flex flex-col lg:flex-row gap-4 items-start relative min-h-[720px]">
      <!-- ================================================================= -->
      <!-- LEFT PANE: Filterable Work Queue Sidebar                          -->
      <!-- ================================================================= -->
      <div
        :class="[
          'transition-all duration-200 shrink-0 z-20',
          // Mobile visibility
          isSidebarOpenOnMobile ? 'block fixed inset-x-4 top-20 z-30 max-h-[85vh]' : 'hidden lg:block',
          // Desktop collapsed vs expanded
          isSidebarCollapsed ? 'lg:hidden' : 'w-full lg:w-80 xl:w-96',
        ]"
        data-testid="workspace-sidebar-pane"
      >
        <FacultyQueueSidebar
          :selected-teacher-id="selectedTeacherId"
          :can-register="canManage"
          :initial-queue-filter="sidebarQueueFilter"
          @select="handleSelectTeacher"
          @register="handleOpenRegister"
        />
      </div>

      <!-- Backdrop for mobile drawer -->
      <div
        v-if="isSidebarOpenOnMobile"
        class="fixed inset-0 bg-surface-900/40 backdrop-blur-xs z-10 lg:hidden"
        data-testid="mobile-sidebar-backdrop"
        @click="isSidebarOpenOnMobile = false"
      />

      <!-- ================================================================= -->
      <!-- RIGHT PANE: Detail Workbench Area                                 -->
      <!-- ================================================================= -->
      <main
        class="flex-1 w-full min-w-0 space-y-4"
        data-testid="workspace-detail-pane"
      >
        <!-- State 1: No Faculty Selected Empty State -->
        <div
          v-if="!selectedTeacherId"
          class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl p-12 text-center shadow-xs"
          data-testid="no-faculty-selected-state"
        >
          <EmptyState
            title="No Faculty Selected"
            description="Select a teacher profile from the work queue on the left to inspect credentials, teaching capabilities, formal authorizations, and auditable record modifications."
          >
            <template #icon>
              <div class="w-16 h-16 rounded-full bg-primary-50 dark:bg-primary-950/60 border border-primary-200 dark:border-primary-800 flex items-center justify-center mx-auto mb-4 text-primary-600 dark:text-primary-400">
                <GraduationCap class="w-8 h-8" />
              </div>
            </template>
            <template #actions>
              <Button
                v-if="isSidebarCollapsed"
                severity="primary"
                size="small"
                class="!text-xs !py-1.5 !px-3 !gap-1.5 font-medium"
                data-testid="expand-queue-empty-button"
                @click="isSidebarCollapsed = false"
              >
                <PanelLeftOpen class="w-4 h-4" />
                <span>Open Work Queue</span>
              </Button>
            </template>
          </EmptyState>
        </div>

        <!-- State 2: Loading Faculty Dossier -->
        <div
          v-else-if="isLoadingDossier"
          class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl p-16 flex flex-col items-center justify-center space-y-3 shadow-xs min-h-[400px]"
          data-testid="detail-loading"
        >
          <LoadingSpinner size="lg" color="teal" />
          <p class="text-sm font-medium text-surface-600 dark:text-surface-300">
            Loading faculty dossier...
          </p>
        </div>

        <!-- State 3: Error Loading Faculty Profile -->
        <div
          v-else-if="isDossierError"
          class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl p-12 text-center shadow-xs"
          data-testid="detail-error"
        >
          <EmptyState
            title="Unable to load faculty dossier"
            description="An error occurred while retrieving the faculty profile. Please verify your connection or try again."
          >
            <template #icon>
              <div class="w-16 h-16 rounded-full bg-rose-50 dark:bg-rose-950 border border-rose-200 dark:border-rose-800 flex items-center justify-center mx-auto mb-4 text-rose-500">
                <AlertTriangle class="w-8 h-8" />
              </div>
            </template>
            <template #actions>
              <Button
                severity="danger"
                size="small"
                class="!text-xs !py-1.5 !px-3 !gap-1.5 font-medium"
                data-testid="retry-detail-button"
                @click="refetchDossier()"
              >
                <RotateCcw class="w-4 h-4" />
                <span>Retry</span>
              </Button>
            </template>
          </EmptyState>
        </div>

        <!-- State 4: Loaded Faculty Dossier Workbench -->
        <div
          v-else-if="teacher"
          class="space-y-4"
          data-testid="faculty-detail-container"
        >
          <!-- Teacher Header Summary Banner (TD-07, P8-S24) -->
          <TeacherHeaderBanner
            :teacher="teacher"
            :can-authorize="canAuthorize"
            @approve="refetchDossier()"
            @change-standing="handleOpenChangeStanding"
            @audit-correction="handleOpenRecordCorrection"
          />

          <!-- 5 Cohesive Tabs Workbench (TD-07) -->
          <section
            class="bg-surface-0 dark:bg-surface-900 border border-surface-200 dark:border-surface-700 rounded-xl shadow-xs overflow-hidden"
            data-testid="faculty-workbench-tabs-card"
          >
            <Tabs v-model:value="activeTab">
              <TabList class="border-b border-surface-200 dark:border-surface-700 bg-surface-50/40 dark:bg-surface-800/20 px-2">
                <Tab value="profile" class="!text-xs sm:!text-sm !py-3 !px-4" data-testid="tab-profile">
                  <div class="flex items-center gap-1.5">
                    <User class="w-4 h-4" />
                    <span>Profile & Affiliation</span>
                  </div>
                </Tab>

                <Tab value="capabilities" class="!text-xs sm:!text-sm !py-3 !px-4" data-testid="tab-capabilities">
                  <div class="flex items-center gap-1.5">
                    <BookOpen class="w-4 h-4" />
                    <span>Teaching Capabilities</span>
                    <span
                      v-if="teacher.capabilities?.length"
                      class="text-[10px] px-1.5 py-0.2 rounded-full bg-surface-200 dark:bg-surface-700"
                    >
                      {{ teacher.capabilities.length }}
                    </span>
                  </div>
                </Tab>

                <Tab value="qualifications" class="!text-xs sm:!text-sm !py-3 !px-4" data-testid="tab-qualifications">
                  <div class="flex items-center gap-1.5">
                    <Award class="w-4 h-4" />
                    <span>Qualification Evidence</span>
                    <span
                      v-if="teacher.qualifications?.length"
                      class="text-[10px] px-1.5 py-0.2 rounded-full bg-surface-200 dark:bg-surface-700"
                    >
                      {{ teacher.qualifications.length }}
                    </span>
                  </div>
                </Tab>

                <Tab value="authorizations" class="!text-xs sm:!text-sm !py-3 !px-4" data-testid="tab-authorizations">
                  <div class="flex items-center gap-1.5">
                    <ShieldCheck class="w-4 h-4" />
                    <span>Authorizations & Supervision</span>
                    <span
                      v-if="teacher.authorizations?.length"
                      class="text-[10px] px-1.5 py-0.2 rounded-full bg-surface-200 dark:bg-surface-700"
                    >
                      {{ teacher.authorizations.length }}
                    </span>
                  </div>
                </Tab>

                <Tab value="audit" class="!text-xs sm:!text-sm !py-3 !px-4" data-testid="tab-audit">
                  <div class="flex items-center gap-1.5">
                    <History class="w-4 h-4" />
                    <span>Corrections Audit</span>
                    <span
                      v-if="teacher.recentCorrections?.length"
                      class="text-[10px] px-1.5 py-0.2 rounded-full bg-surface-200 dark:bg-surface-700"
                    >
                      {{ teacher.recentCorrections.length }}
                    </span>
                  </div>
                </Tab>
              </TabList>

              <TabPanels class="p-5">
                <!-- Tab Panel 1: Profile & Affiliation (TD-04, TD-07, P8-S25) -->
                <TabPanel value="profile" data-testid="panel-profile">
                  <ProfileAffiliationTab
                    :teacher="teacher"
                    :can-edit="canManage"
                    @updated="refetchDossier()"
                  />
                </TabPanel>

                <!-- Tab Panel 2: Teaching Capabilities (TD-02, P8-S26) -->
                <TabPanel value="capabilities" data-testid="panel-capabilities">
                  <TeachingCapabilitiesTab
                    :teacher="teacher"
                    :can-manage="canManage"
                    @refresh="refetchDossier()"
                  />
                </TabPanel>

                <!-- Tab Panel 3: Qualification Evidence (TD-05, P8-S26) -->
                <TabPanel value="qualifications" data-testid="panel-qualifications">
                  <QualificationEvidenceTab
                    :teacher="teacher"
                    :can-manage="canManage"
                    :can-verify="canVerify"
                    :initial-verification-state="verificationStateFilter"
                    @refresh="refetchDossier()"
                  />
                </TabPanel>

                <!-- Tab Panel 4: Authorizations & Supervision (TD-05, P8-S27) -->
                <TabPanel value="authorizations" data-testid="panel-authorizations">
                  <AuthorizationsSupervisionTab
                    :teacher="teacher"
                    :can-authorize="canAuthorize"
                    :can-manage="canManage"
                    @refresh="refetchDossier()"
                  />
                </TabPanel>

                <!-- Tab Panel 5: Corrections Audit (TD-06, P8-S27) -->
                <TabPanel value="audit" data-testid="panel-audit">
                  <CorrectionsAuditTab
                    :teacher="teacher"
                    :can-authorize="canAuthorize"
                    @refresh="refetchDossier()"
                  />
                </TabPanel>
              </TabPanels>
            </Tabs>
          </section>
        </div>
      </main>
    </div>

    <!-- Candidate Intake Registration Modal (P8-S24) -->
    <RegisterTeacherModal
      v-model:visible="isRegisterModalOpen"
      @registered="handleTeacherRegistered"
    />

    <!-- Change Standing Modal (TD-03, P8-S25) -->
    <ChangeStandingModal
      v-model:visible="isChangeStandingModalOpen"
      :teacher="teacher"
      @changed="refetchDossier()"
    />

    <!-- Auditable Record Correction Modal (TD-06, P8-S25) -->
    <RecordCorrectionModal
      v-model:visible="isRecordCorrectionModalOpen"
      :teacher="teacher"
      :can-authorize="canAuthorize"
      @corrected="refetchDossier()"
    />
  </div>
</template>

