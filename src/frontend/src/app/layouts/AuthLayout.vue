<script setup lang="ts">
import { computed } from 'vue';
import { GraduationCap } from 'lucide-vue-next';
import { RouterView, useRoute } from 'vue-router';

const route = useRoute();
const maxWidthClass = computed(() => (route.meta?.wide ? 'sm:max-w-4xl' : 'sm:max-w-md'));
</script>

<template>
  <div
    class="min-h-screen bg-slate-50 flex flex-col justify-center items-center px-4 py-12 sm:px-6 lg:px-8"
    data-testid="auth-layout"
  >
    <div class="sm:mx-auto sm:w-full" :class="maxWidthClass">
      <!-- Institutional Branding Header -->
      <div class="flex flex-col items-center text-center">
        <div
          class="inline-flex items-center justify-center w-12 h-12 rounded-xl bg-elsasa-navy-900 text-white shadow-md mb-4"
        >
          <GraduationCap class="w-7 h-7 text-elsasa-teal-500" aria-hidden="true" />
        </div>
        <h1 class="text-2xl font-bold tracking-tight text-elsasa-navy-900 sm:text-3xl">
          Elsasa SIS
        </h1>
        <p class="mt-1 text-sm text-slate-500">Integrated School Information System</p>
      </div>
    </div>

    <!-- Centered Distraction-Free Card Container -->
    <div class="mt-8 sm:mx-auto sm:w-full" :class="maxWidthClass">
      <div
        class="bg-white py-8 px-6 shadow-sm border border-slate-200 rounded-xl sm:px-10"
        data-testid="auth-card-container"
      >
        <RouterView />
      </div>

      <!-- Institutional Footer Note -->
      <p class="mt-8 text-center text-xs text-slate-400">
        &copy; 2026 Elsasa SIS. All rights reserved. &bull; Secure Institutional Portal
      </p>
    </div>
  </div>
</template>
