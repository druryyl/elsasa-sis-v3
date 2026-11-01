<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import InputText from 'primevue/inputtext';
import Button from 'primevue/button';
import { useToast } from 'primevue/usetoast';
import {
  Calendar,
  PlusCircle,
  RefreshCw,
  CheckCircle2,
  AlertTriangle,
  RotateCcw,
  Sparkles,
} from 'lucide-vue-next';
import { PageHeader, ContentCard, StatusPill, FormField, EmptyState } from '@/shared/components';
import { ApiError } from '@/shared/types/common';
import type { BreadcrumbItem } from '@/shared/types/navigation';
import {
  checkBackendHealth,
  createAcademicYear,
  type AcademicYearDto,
} from '../api/academicYear.api';
import {
  createAcademicYearSchema,
  type CreateAcademicYearFormData,
} from '../schemas/createAcademicYear.schema';

const toast = useToast();

const breadcrumbs: BreadcrumbItem[] = [
  { label: 'Dashboard', to: '/dashboard' },
  { label: 'Operations', to: '/operations' },
  { label: 'Calendar & Periods', current: true },
];

// --------------------------------------------------------------------------
// 1. Live Backend Connectivity Probe (/health)
// --------------------------------------------------------------------------
type HealthState = 'idle' | 'loading' | 'healthy' | 'unhealthy';

const healthState = ref<HealthState>('idle');
const healthStatusMessage = ref<string>('Not checked');
const lastCheckedAt = ref<string | null>(null);

async function refreshHealth(): Promise<void> {
  healthState.value = 'loading';
  try {
    const res = await checkBackendHealth();
    healthStatusMessage.value = res.status || 'Healthy';
    healthState.value = 'healthy';
    lastCheckedAt.value = new Date().toLocaleTimeString();
  } catch (err: unknown) {
    healthState.value = 'unhealthy';
    healthStatusMessage.value =
      err instanceof Error ? err.message : 'Backend unreachable or unhealthy';
    lastCheckedAt.value = new Date().toLocaleTimeString();
  }
}

onMounted(() => {
  void refreshHealth();
});

// --------------------------------------------------------------------------
// 2. Interactive Academic Year Form & Validation
// --------------------------------------------------------------------------
const isSubmitting = ref(false);
const lastCreatedRecord = ref<AcademicYearDto | null>(null);
const createdHistory = ref<AcademicYearDto[]>([]);
const serverErrorMessage = ref<string | null>(null);
const serverValidationErrors = ref<string[]>([]);

const { handleSubmit, errors, setErrors, resetForm, setValues, defineField } =
  useForm<CreateAcademicYearFormData>({
    validationSchema: toTypedSchema(createAcademicYearSchema),
    initialValues: {
      code: '',
      name: '',
      startDate: '',
      endDate: '',
    },
  });

const [code, codeProps] = defineField('code');
const [name, nameProps] = defineField('name');
const [startDate, startDateProps] = defineField('startDate');
const [endDate, endDateProps] = defineField('endDate');

function quickFillValid(): void {
  serverErrorMessage.value = null;
  serverValidationErrors.value = [];
  setValues({
    code: 'AY-2026-2027',
    name: 'Academic Year 2026/2027',
    startDate: '2026-07-01',
    endDate: '2027-06-30',
  });
}

function quickFillInvalidRange(): void {
  serverErrorMessage.value = null;
  serverValidationErrors.value = [];
  // Invalid range where end date is before start date to exercise 422 ProblemDetails
  setValues({
    code: 'AY-INV-RANGE',
    name: 'Invalid Date Range Year',
    startDate: '2027-07-01',
    endDate: '2026-06-30',
  });
}

function handleReset(): void {
  resetForm();
  serverErrorMessage.value = null;
  serverValidationErrors.value = [];
}

const onSubmit = handleSubmit(async (values) => {
  isSubmitting.value = true;
  serverErrorMessage.value = null;
  serverValidationErrors.value = [];
  lastCreatedRecord.value = null;

  try {
    const result = await createAcademicYear(values);
    lastCreatedRecord.value = result;
    createdHistory.value.unshift(result);

    toast.add({
      severity: 'success',
      summary: 'Academic Year Created',
      detail: `Created '${result.code}' with ID: ${result.id}`,
      life: 5000,
    });

    // Reset form after successful submission
    resetForm({
      values: {
        code: '',
        name: '',
        startDate: '',
        endDate: '',
      },
    });
  } catch (err: unknown) {
    if (err instanceof ApiError) {
      serverErrorMessage.value =
        err.detail || err.title || 'An error occurred during academic year creation.';

      if (err.hasValidationErrors() && err.errors) {
        const fieldErrors: Record<string, string> = {};
        const messages: string[] = [];

        for (const [key, msgs] of Object.entries(err.errors)) {
          // Normalize PascalCase (e.g. Code, EndDate) from backend to camelCase (code, endDate)
          const normalizedKey = key.charAt(0).toLowerCase() + key.slice(1);
          const list = Array.isArray(msgs) ? msgs : [String(msgs)];

          if (list.length > 0 && list[0]) {
            fieldErrors[normalizedKey] = list[0];
            messages.push(...list);
          }
        }

        setErrors(fieldErrors);
        serverValidationErrors.value = messages;
      }

      toast.add({
        severity: 'error',
        summary: err.title || 'Validation Error',
        detail: err.detail || 'Validation failed on the backend. Please check highlighted fields.',
        life: 5000,
      });
    } else {
      const msg = err instanceof Error ? err.message : 'An unexpected error occurred.';
      serverErrorMessage.value = msg;
      toast.add({
        severity: 'error',
        summary: 'Request Failed',
        detail: msg,
        life: 5000,
      });
    }
  } finally {
    isSubmitting.value = false;
  }
});
</script>

<template>
  <div class="space-y-6" data-testid="calendar-workspace-view">
    <!-- Page Header -->
    <PageHeader
      title="Academic Calendar & Periods"
      subtitle="Operational catalogue for institutional years, calendar terms, and live ASP.NET Core integration verification."
      :breadcrumbs="breadcrumbs"
    >
      <template #title-extra>
        <span
          class="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-medium bg-teal-50 text-teal-700 border border-teal-200"
        >
          <Calendar class="w-3.5 h-3.5 text-teal-600" aria-hidden="true" />
          <span>Operations Catalogue</span>
        </span>
      </template>

      <template #actions>
        <Button
          type="button"
          outlined
          size="small"
          data-testid="refresh-health-btn"
          :loading="healthState === 'loading'"
          class="flex items-center gap-1.5 cursor-pointer text-slate-700 hover:text-slate-900 border-slate-300"
          @click="refreshHealth"
        >
          <RefreshCw class="w-3.5 h-3.5" :class="{ 'animate-spin': healthState === 'loading' }" />
          <span>Check Backend Health</span>
        </Button>
      </template>
    </PageHeader>

    <!-- Top Grid: Live Connectivity Probe & Quick Info -->
    <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
      <!-- Backend Health Connectivity Card -->
      <ContentCard
        title="Backend Health Probe"
        subtitle="Verifies HTTP proxying and SQL Server readiness"
        data-testid="backend-health-card"
        class="md:col-span-1"
      >
        <div class="space-y-4">
          <div class="flex items-center justify-between">
            <span class="text-xs font-medium text-slate-500 uppercase tracking-wider">Status</span>
            <StatusPill
              v-if="healthState === 'healthy'"
              status="success"
              label="Healthy (HTTP 200)"
              size="sm"
              data-testid="backend-health-status"
            />
            <StatusPill
              v-else-if="healthState === 'unhealthy'"
              status="danger"
              label="Unreachable / Error"
              size="sm"
              data-testid="backend-health-status"
            />
            <StatusPill
              v-else
              status="pending"
              label="Checking..."
              size="sm"
              data-testid="backend-health-status"
            />
          </div>

          <div class="rounded-lg bg-slate-50 p-3 border border-slate-200/80 space-y-1.5 text-xs">
            <div class="flex justify-between text-slate-600">
              <span class="font-medium">Endpoint:</span>
              <code class="font-mono text-slate-800">GET /health</code>
            </div>
            <div class="flex justify-between text-slate-600">
              <span class="font-medium">Message:</span>
              <span class="text-slate-800 truncate max-w-[160px]" :title="healthStatusMessage">
                {{ healthStatusMessage }}
              </span>
            </div>
            <div v-if="lastCheckedAt" class="flex justify-between text-slate-600">
              <span class="font-medium">Last Checked:</span>
              <span class="text-slate-500">{{ lastCheckedAt }}</span>
            </div>
          </div>

          <p class="text-xs text-slate-400">
            Requests forward through Vite dev proxy (<code>http://localhost:5000/health</code>) and
            include correlation tracing headers.
          </p>
        </div>
      </ContentCard>

      <!-- Architecture Integration Overview -->
      <ContentCard
        title="Integration Verification Slice"
        subtitle="End-to-end frontend-to-backend contract validation"
        class="md:col-span-2"
      >
        <div class="space-y-3 text-xs text-slate-600">
          <div class="flex items-start gap-2.5">
            <CheckCircle2 class="w-4 h-4 text-teal-600 shrink-0 mt-0.5" />
            <div>
              <span class="font-semibold text-slate-800">HTTP Proxying & Correlation ID:</span>
              Axios requests to <code>/api/*</code> and <code>/health</code> automatically attach a
              unique <code>X-Correlation-ID</code> UUID v4 header matching ASP.NET Core
              <code>CorrelationIdMiddleware</code>.
            </div>
          </div>
          <div class="flex items-start gap-2.5">
            <CheckCircle2 class="w-4 h-4 text-teal-600 shrink-0 mt-0.5" />
            <div>
              <span class="font-semibold text-slate-800">Schema-First Dual Validation:</span>
              Client validation executes via <code>createAcademicYearSchema</code> (Zod), mirroring
              backend FluentValidation rules in <code>CreateAcademicYearValidator.cs</code>.
            </div>
          </div>
          <div class="flex items-start gap-2.5">
            <CheckCircle2 class="w-4 h-4 text-teal-600 shrink-0 mt-0.5" />
            <div>
              <span class="font-semibold text-slate-800">RFC 7807 ProblemDetails Mapping:</span>
              HTTP 422 responses are mapped into typed <code>ApiError</code> instances, unbundling
              field-level validation errors directly onto VeeValidate form inputs.
            </div>
          </div>
        </div>
      </ContentCard>
    </div>

    <!-- Main Section: Form & Created History -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <!-- Create Academic Year Form -->
      <ContentCard
        title="Create Academic Year"
        subtitle="Submits payload to POST /api/operations/academic-years"
        class="lg:col-span-2"
        data-testid="create-academic-year-card"
      >
        <template #header-actions>
          <div class="flex items-center gap-1.5">
            <Button
              type="button"
              text
              size="small"
              data-testid="quick-fill-valid-btn"
              class="text-xs text-teal-700 hover:text-teal-900 cursor-pointer flex items-center gap-1"
              @click="quickFillValid"
            >
              <Sparkles class="w-3.5 h-3.5" />
              <span>Fill Valid</span>
            </Button>
            <Button
              type="button"
              text
              size="small"
              data-testid="quick-fill-invalid-btn"
              class="text-xs text-amber-700 hover:text-amber-900 cursor-pointer flex items-center gap-1"
              @click="quickFillInvalidRange"
            >
              <AlertTriangle class="w-3.5 h-3.5" />
              <span>Fill Invalid Range</span>
            </Button>
          </div>
        </template>

        <!-- Success Alert Banner -->
        <div
          v-if="lastCreatedRecord"
          data-testid="creation-success-alert"
          class="mb-5 rounded-lg bg-emerald-50 p-4 border border-emerald-200 text-emerald-800 text-sm flex items-start gap-3"
        >
          <CheckCircle2 class="w-5 h-5 text-emerald-600 shrink-0 mt-0.5" />
          <div class="space-y-1">
            <div class="font-semibold">Academic Year Created Successfully (HTTP 201 Created)</div>
            <div class="text-xs text-emerald-700">
              Code: <span class="font-mono font-medium">{{ lastCreatedRecord.code }}</span> | ID:
              <span class="font-mono font-medium">{{ lastCreatedRecord.id }}</span>
            </div>
          </div>
        </div>

        <!-- ProblemDetails Validation Error Banner -->
        <div
          v-if="serverErrorMessage"
          data-testid="validation-error-banner"
          class="mb-5 rounded-lg bg-rose-50 p-4 border border-rose-200 text-rose-800 text-sm flex items-start gap-3"
        >
          <AlertTriangle class="w-5 h-5 text-rose-600 shrink-0 mt-0.5" />
          <div class="space-y-1.5 flex-1">
            <div class="font-semibold text-rose-900">Server Response: {{ serverErrorMessage }}</div>
            <ul
              v-if="serverValidationErrors.length > 0"
              class="list-disc list-inside text-xs text-rose-700 space-y-0.5"
            >
              <li v-for="(msg, idx) in serverValidationErrors" :key="idx">
                {{ msg }}
              </li>
            </ul>
          </div>
        </div>

        <!-- Form -->
        <form class="space-y-4" data-testid="create-academic-year-form" @submit.prevent="onSubmit">
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <!-- Code Field -->
            <FormField
              id="academic-year-code"
              label="Academic Year Code"
              :error="errors.code"
              hint="Unique identifier (e.g. AY-2026-2027)"
              required
            >
              <template #default="{ id, ariaDescribedby, ariaInvalid }">
                <InputText
                  :id="id"
                  v-model="code"
                  v-bind="codeProps"
                  type="text"
                  placeholder="e.g. AY-2026-2027"
                  :aria-describedby="ariaDescribedby"
                  :aria-invalid="ariaInvalid"
                  class="w-full px-3 py-2 text-sm border rounded-lg focus:outline-none focus:ring-2 focus:ring-teal-500 transition-colors"
                  :class="errors.code ? 'border-rose-400 focus:ring-rose-500' : 'border-slate-300'"
                  data-testid="code-input"
                />
              </template>
            </FormField>

            <!-- Name Field -->
            <FormField
              id="academic-year-name"
              label="Descriptive Name"
              :error="errors.name"
              hint="Institutional title (e.g. Academic Year 2026/2027)"
              required
            >
              <template #default="{ id, ariaDescribedby, ariaInvalid }">
                <InputText
                  :id="id"
                  v-model="name"
                  v-bind="nameProps"
                  type="text"
                  placeholder="e.g. Academic Year 2026/2027"
                  :aria-describedby="ariaDescribedby"
                  :aria-invalid="ariaInvalid"
                  class="w-full px-3 py-2 text-sm border rounded-lg focus:outline-none focus:ring-2 focus:ring-teal-500 transition-colors"
                  :class="errors.name ? 'border-rose-400 focus:ring-rose-500' : 'border-slate-300'"
                  data-testid="name-input"
                />
              </template>
            </FormField>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <!-- Start Date Field -->
            <FormField
              id="academic-year-start-date"
              label="Start Date"
              :error="errors.startDate"
              hint="Format: YYYY-MM-DD"
              required
            >
              <template #default="{ id, ariaDescribedby, ariaInvalid }">
                <InputText
                  :id="id"
                  v-model="startDate"
                  v-bind="startDateProps"
                  type="date"
                  placeholder="YYYY-MM-DD"
                  :aria-describedby="ariaDescribedby"
                  :aria-invalid="ariaInvalid"
                  class="w-full px-3 py-2 text-sm border rounded-lg focus:outline-none focus:ring-2 focus:ring-teal-500 transition-colors"
                  :class="
                    errors.startDate ? 'border-rose-400 focus:ring-rose-500' : 'border-slate-300'
                  "
                  data-testid="start-date-input"
                />
              </template>
            </FormField>

            <!-- End Date Field -->
            <FormField
              id="academic-year-end-date"
              label="End Date"
              :error="errors.endDate"
              hint="Must be strictly after start date"
              required
            >
              <template #default="{ id, ariaDescribedby, ariaInvalid }">
                <InputText
                  :id="id"
                  v-model="endDate"
                  v-bind="endDateProps"
                  type="date"
                  placeholder="YYYY-MM-DD"
                  :aria-describedby="ariaDescribedby"
                  :aria-invalid="ariaInvalid"
                  class="w-full px-3 py-2 text-sm border rounded-lg focus:outline-none focus:ring-2 focus:ring-teal-500 transition-colors"
                  :class="
                    errors.endDate ? 'border-rose-400 focus:ring-rose-500' : 'border-slate-300'
                  "
                  data-testid="end-date-input"
                />
              </template>
            </FormField>
          </div>

          <div class="pt-4 flex items-center justify-between border-t border-slate-100">
            <Button
              type="button"
              text
              size="small"
              data-testid="reset-form-btn"
              class="text-slate-600 hover:text-slate-800 cursor-pointer flex items-center gap-1.5"
              @click="handleReset"
            >
              <RotateCcw class="w-3.5 h-3.5" />
              <span>Reset Fields</span>
            </Button>

            <Button
              type="submit"
              :loading="isSubmitting"
              data-testid="create-academic-year-submit"
              class="bg-teal-600 hover:bg-teal-700 text-white font-medium py-2.5 px-5 rounded-lg transition-colors flex items-center gap-2 cursor-pointer shadow-xs disabled:opacity-50"
            >
              <PlusCircle class="w-4 h-4" aria-hidden="true" />
              <span>Create Academic Year</span>
            </Button>
          </div>
        </form>
      </ContentCard>

      <!-- Created Entities List / History -->
      <ContentCard
        title="Created Records"
        subtitle="Entities created during this browser session"
        class="lg:col-span-1"
        data-testid="created-records-card"
      >
        <div v-if="createdHistory.length === 0" class="py-6">
          <EmptyState
            title="No Academic Years Created"
            description="Submit the form to verify live backend persistence and response handling."
            data-testid="empty-records-state"
          />
        </div>

        <ul v-else class="divide-y divide-slate-100 space-y-3" data-testid="created-records-list">
          <li
            v-for="ay in createdHistory"
            :key="ay.id"
            class="pt-3 first:pt-0 space-y-1.5"
            data-testid="created-record-item"
          >
            <div class="flex items-center justify-between">
              <span class="font-semibold text-sm text-slate-800 font-mono">{{ ay.code }}</span>
              <StatusPill
                :status="ay.isActive ? 'active' : 'inactive'"
                :label="ay.isActive ? 'Active' : 'Inactive'"
                size="sm"
              />
            </div>
            <p class="text-xs text-slate-600">
              {{ ay.name }}
            </p>
            <div class="flex items-center justify-between text-[11px] text-slate-400">
              <span>{{ ay.startDate }} → {{ ay.endDate }}</span>
              <span class="font-mono truncate max-w-[100px]" :title="ay.id">
                {{ ay.id.slice(0, 8) }}...
              </span>
            </div>
          </li>
        </ul>
      </ContentCard>
    </div>
  </div>
</template>
