<script setup lang="ts">
import { computed } from 'vue';
import {
  Users,
  CheckCircle2,
  AlertTriangle,
  ShieldCheck,
  AlertOctagon,
} from 'lucide-vue-next';
import type { TeacherWorkloadCommitmentDto, WorkloadCommitmentStatus } from '../types';

export interface WorkloadKpiCardsProps {
  commitments?: TeacherWorkloadCommitmentDto[];
  selectedStatus?: string | null;
  isLoading?: boolean;
}

const props = withDefaults(defineProps<WorkloadKpiCardsProps>(), {
  commitments: () => [],
  selectedStatus: null,
  isLoading: false,
});

const emit = defineEmits<{
  (e: 'selectStatus', status: WorkloadCommitmentStatus | 'all'): void;
}>();

const totalFaculty = computed(() => props.commitments.length);

const normalCount = computed(
  () =>
    props.commitments.filter(
      (c) => c.status === 'normal' || c.status === 'under_allocated',
    ).length,
);

const approachingLimitCount = computed(
  () => props.commitments.filter((c) => c.status === 'approaching_limit').length,
);

const overloadedWithExceptionCount = computed(
  () =>
    props.commitments.filter((c) => c.status === 'overloaded_with_exception').length,
);

const breachedCount = computed(
  () => props.commitments.filter((c) => c.status === 'breached').length,
);

function handleCardClick(status: WorkloadCommitmentStatus | 'all') {
  emit('selectStatus', status);
}
</script>

<template>
  <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-4">
    <!-- 1. Total Faculty -->
    <div
      data-testid="kpi-card-total"
      class="group relative overflow-hidden rounded-xl border p-4 transition-all duration-200 cursor-pointer hover:shadow-md bg-white"
      :class="[
        selectedStatus === 'all' || !selectedStatus
          ? 'border-slate-400 ring-2 ring-slate-400/20 shadow-xs'
          : 'border-slate-200 hover:border-slate-300',
      ]"
      role="button"
      tabindex="0"
      @click="handleCardClick('all')"
      @keydown.enter="handleCardClick('all')"
      @keydown.space.prevent="handleCardClick('all')"
    >
      <div class="flex items-center justify-between">
        <span class="text-xs font-semibold tracking-wider uppercase text-slate-500">
          Total Faculty
        </span>
        <div class="p-2 rounded-lg bg-slate-100 text-slate-600 group-hover:scale-105 transition-transform">
          <Users class="w-5 h-5" />
        </div>
      </div>
      <div class="mt-2 flex items-baseline gap-2">
        <span class="text-3xl font-bold tracking-tight text-slate-900" data-testid="kpi-count-total">
          <template v-if="isLoading">--</template>
          <template v-else>{{ totalFaculty }}</template>
        </span>
        <span class="text-xs text-slate-500 font-medium">monitored</span>
      </div>
      <div class="mt-3 text-xs text-slate-500 flex items-center gap-1">
        <span>Active academic period</span>
      </div>
    </div>

    <!-- 2. Normal Capacity -->
    <div
      data-testid="kpi-card-normal"
      class="group relative overflow-hidden rounded-xl border p-4 transition-all duration-200 cursor-pointer hover:shadow-md bg-white"
      :class="[
        selectedStatus === 'normal'
          ? 'border-emerald-500 ring-2 ring-emerald-500/20 shadow-xs bg-emerald-50/20'
          : 'border-slate-200 hover:border-emerald-300',
      ]"
      role="button"
      tabindex="0"
      @click="handleCardClick('normal')"
      @keydown.enter="handleCardClick('normal')"
      @keydown.space.prevent="handleCardClick('normal')"
    >
      <div class="flex items-center justify-between">
        <span class="text-xs font-semibold tracking-wider uppercase text-emerald-700">
          Normal Load
        </span>
        <div class="p-2 rounded-lg bg-emerald-50 text-emerald-600 group-hover:scale-105 transition-transform">
          <CheckCircle2 class="w-5 h-5" />
        </div>
      </div>
      <div class="mt-2 flex items-baseline gap-2">
        <span class="text-3xl font-bold tracking-tight text-emerald-700" data-testid="kpi-count-normal">
          <template v-if="isLoading">--</template>
          <template v-else>{{ normalCount }}</template>
        </span>
        <span v-if="totalFaculty > 0 && !isLoading" class="text-xs text-emerald-600 font-medium">
          ({{ Math.round((normalCount / totalFaculty) * 100) }}%)
        </span>
      </div>
      <div class="mt-3 text-xs text-emerald-600/90 font-medium">
        Within standard policy limits
      </div>
    </div>

    <!-- 3. Approaching Limit -->
    <div
      data-testid="kpi-card-approaching"
      class="group relative overflow-hidden rounded-xl border p-4 transition-all duration-200 cursor-pointer hover:shadow-md bg-white"
      :class="[
        selectedStatus === 'approaching_limit'
          ? 'border-amber-500 ring-2 ring-amber-500/20 shadow-xs bg-amber-50/20'
          : 'border-slate-200 hover:border-amber-300',
      ]"
      role="button"
      tabindex="0"
      @click="handleCardClick('approaching_limit')"
      @keydown.enter="handleCardClick('approaching_limit')"
      @keydown.space.prevent="handleCardClick('approaching_limit')"
    >
      <div class="flex items-center justify-between">
        <span class="text-xs font-semibold tracking-wider uppercase text-amber-700">
          Approaching Limit
        </span>
        <div class="p-2 rounded-lg bg-amber-50 text-amber-600 group-hover:scale-105 transition-transform">
          <AlertTriangle class="w-5 h-5" />
        </div>
      </div>
      <div class="mt-2 flex items-baseline gap-2">
        <span class="text-3xl font-bold tracking-tight text-amber-700" data-testid="kpi-count-approaching">
          <template v-if="isLoading">--</template>
          <template v-else>{{ approachingLimitCount }}</template>
        </span>
        <span v-if="totalFaculty > 0 && !isLoading" class="text-xs text-amber-600 font-medium">
          ({{ Math.round((approachingLimitCount / totalFaculty) * 100) }}%)
        </span>
      </div>
      <div class="mt-3 text-xs text-amber-600/90 font-medium">
        In warning threshold zone
      </div>
    </div>

    <!-- 4. Overloaded with Exception -->
    <div
      data-testid="kpi-card-exception"
      class="group relative overflow-hidden rounded-xl border p-4 transition-all duration-200 cursor-pointer hover:shadow-md bg-white"
      :class="[
        selectedStatus === 'overloaded_with_exception'
          ? 'border-purple-500 ring-2 ring-purple-500/20 shadow-xs bg-purple-50/20'
          : 'border-slate-200 hover:border-purple-300',
      ]"
      role="button"
      tabindex="0"
      @click="handleCardClick('overloaded_with_exception')"
      @keydown.enter="handleCardClick('overloaded_with_exception')"
      @keydown.space.prevent="handleCardClick('overloaded_with_exception')"
    >
      <div class="flex items-center justify-between">
        <span class="text-xs font-semibold tracking-wider uppercase text-purple-700">
          Overloaded w/ Exception
        </span>
        <div class="p-2 rounded-lg bg-purple-50 text-purple-600 group-hover:scale-105 transition-transform">
          <ShieldCheck class="w-5 h-5" />
        </div>
      </div>
      <div class="mt-2 flex items-baseline gap-2">
        <span class="text-3xl font-bold tracking-tight text-purple-700" data-testid="kpi-count-exception">
          <template v-if="isLoading">--</template>
          <template v-else>{{ overloadedWithExceptionCount }}</template>
        </span>
        <span v-if="totalFaculty > 0 && !isLoading" class="text-xs text-purple-600 font-medium">
          ({{ Math.round((overloadedWithExceptionCount / totalFaculty) * 100) }}%)
        </span>
      </div>
      <div class="mt-3 text-xs text-purple-600/90 font-medium">
        Authorized by Teaching Authority
      </div>
    </div>

    <!-- 5. Breached -->
    <div
      data-testid="kpi-card-breached"
      class="group relative overflow-hidden rounded-xl border p-4 transition-all duration-200 cursor-pointer hover:shadow-md bg-white"
      :class="[
        selectedStatus === 'breached'
          ? 'border-rose-500 ring-2 ring-rose-500/20 shadow-xs bg-rose-50/20'
          : 'border-slate-200 hover:border-rose-300',
      ]"
      role="button"
      tabindex="0"
      @click="handleCardClick('breached')"
      @keydown.enter="handleCardClick('breached')"
      @keydown.space.prevent="handleCardClick('breached')"
    >
      <div class="flex items-center justify-between">
        <span class="text-xs font-semibold tracking-wider uppercase text-rose-700">
          Limit Breached
        </span>
        <div class="p-2 rounded-lg bg-rose-50 text-rose-600 group-hover:scale-105 transition-transform">
          <AlertOctagon class="w-5 h-5" />
        </div>
      </div>
      <div class="mt-2 flex items-baseline gap-2">
        <span class="text-3xl font-bold tracking-tight text-rose-700" data-testid="kpi-count-breached">
          <template v-if="isLoading">--</template>
          <template v-else>{{ breachedCount }}</template>
        </span>
        <span v-if="totalFaculty > 0 && !isLoading" class="text-xs text-rose-600 font-medium">
          ({{ Math.round((breachedCount / totalFaculty) * 100) }}%)
        </span>
      </div>
      <div class="mt-3 text-xs text-rose-600/90 font-medium">
        Requires immediate exception or relief
      </div>
    </div>
  </div>
</template>
