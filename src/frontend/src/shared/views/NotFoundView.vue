<script setup lang="ts">
import { useRouter } from 'vue-router';
import { FileQuestion, ArrowLeft, LayoutDashboard } from 'lucide-vue-next';
import Button from 'primevue/button';
import StatusPill from '@/shared/components/StatusPill.vue';

const router = useRouter();

function navigateToDashboard() {
  router.push('/dashboard');
}

function navigateBack() {
  router.back();
}
</script>

<template>
  <div
    data-testid="not-found-view"
    class="min-h-[70vh] flex flex-col items-center justify-center p-6 text-center"
  >
    <div class="max-w-md mx-auto space-y-6">
      <!-- 404 Icon Badge -->
      <div
        class="inline-flex items-center justify-center w-20 h-20 rounded-2xl bg-amber-50 border border-amber-100 text-amber-600 shadow-xs mb-2"
      >
        <FileQuestion class="w-10 h-10" aria-hidden="true" />
      </div>

      <!-- Status Header -->
      <div class="space-y-2">
        <div class="flex items-center justify-center gap-2">
          <StatusPill status="pending" label="HTTP 404" dot size="sm" />
          <span class="text-xs uppercase font-semibold tracking-wider text-slate-400">
            Resource Missing
          </span>
        </div>
        <h1 class="text-2xl font-bold tracking-tight text-slate-900 sm:text-3xl">Page Not Found</h1>
        <p class="text-sm text-slate-600 leading-relaxed">
          The requested URL path could not be located in Elsasa SIS. Please check the web address
          for errors or return to the main dashboard.
        </p>
      </div>

      <!-- Action Buttons -->
      <div class="flex flex-col sm:flex-row items-center justify-center gap-3 pt-2">
        <Button
          data-testid="return-dashboard-btn"
          class="w-full sm:w-auto inline-flex items-center justify-center gap-2 px-4 py-2.5 rounded-lg bg-teal-600 hover:bg-teal-700 text-white font-medium text-sm transition-colors cursor-pointer shadow-xs"
          @click="navigateToDashboard"
        >
          <LayoutDashboard class="w-4 h-4" aria-hidden="true" />
          <span>Return to Dashboard</span>
        </Button>

        <Button
          data-testid="go-back-btn"
          class="w-full sm:w-auto inline-flex items-center justify-center gap-2 px-4 py-2.5 rounded-lg border border-slate-300 bg-white hover:bg-slate-50 text-slate-700 font-medium text-sm transition-colors cursor-pointer"
          @click="navigateBack"
        >
          <ArrowLeft class="w-4 h-4" aria-hidden="true" />
          <span>Go Back</span>
        </Button>
      </div>

      <!-- Institutional Support Footnote -->
      <div class="pt-6 border-t border-slate-200 text-xs text-slate-400">
        <span>Elsasa SIS V3 &bull; Single Page Application Router</span>
      </div>
    </div>
  </div>
</template>
