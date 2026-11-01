<script setup lang="ts">
import { ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import InputText from 'primevue/inputtext';
import Button from 'primevue/button';
import { LogIn, ShieldCheck, Lock, User as UserIcon } from 'lucide-vue-next';
import FormField from '@/shared/components/forms/FormField.vue';
// eslint-disable-next-line no-restricted-imports -- ADR-FE-002: Pinia session store in @/app/stores
import { useAuthStore, type UserSession } from '@/app/stores/auth.store';
import { loginSchema, type LoginFormValues } from '../schemas/login.schema';

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();

const isSubmitting = ref(false);
const authError = ref<string | null>(null);

const { handleSubmit, errors, defineField } = useForm<LoginFormValues>({
  validationSchema: toTypedSchema(loginSchema),
  initialValues: {
    username: '',
    password: '',
  },
});

const [username, usernameProps] = defineField('username');
const [password, passwordProps] = defineField('password');

const onSubmit = handleSubmit(async (values) => {
  isSubmitting.value = true;
  authError.value = null;

  try {
    const trimmedUsername = values.username.trim();
    const isSuperAdmin =
      trimmedUsername.toLowerCase() === 'admin' || trimmedUsername.toLowerCase() === 'superadmin';

    // Build authenticated user session context
    const userSession: UserSession = {
      userId: `usr-${Date.now().toString(36)}`,
      username: trimmedUsername,
      fullName: isSuperAdmin
        ? 'Super Administrator'
        : `${trimmedUsername.charAt(0).toUpperCase() + trimmedUsername.slice(1)} Staff`,
      roles: isSuperAdmin ? ['SuperAdmin', 'AcademicAdmin'] : ['AcademicStaff', 'FinanceStaff'],
      permissions: isSuperAdmin
        ? ['*']
        : [
            'Permissions.Operations.Calendar.Manage',
            'Permissions.Students.Admission.View',
            'Permissions.Students.Admission.Review',
            'Permissions.Students.Enrollment.Execute',
            'Permissions.Finance.Obligations.View',
            'Permissions.Finance.Payments.Receive',
          ],
      campusId: 'campus-main',
    };

    const token = `token-${Date.now()}`;
    authStore.setSession(token, userSession);

    // Navigate to original destination or default dashboard
    const redirectTarget = (route.query.redirect as string) || '/dashboard';
    await router.push(redirectTarget);
  } catch (err: unknown) {
    authError.value =
      err instanceof Error ? err.message : 'Authentication failed. Please verify credentials.';
  } finally {
    isSubmitting.value = false;
  }
});
</script>

<template>
  <div data-testid="login-view" class="w-full">
    <div class="mb-6 text-center sm:text-left">
      <div
        class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-teal-50 text-teal-800 text-xs font-medium mb-3"
      >
        <ShieldCheck class="w-3.5 h-3.5 text-teal-600" aria-hidden="true" />
        <span>Administrative Authentication</span>
      </div>
      <h2 class="text-xl font-bold tracking-tight text-slate-900 sm:text-2xl">
        Sign in to Elsasa SIS
      </h2>
      <p class="mt-1 text-sm text-slate-500">
        Enter your institutional credentials to access operational workspaces.
      </p>
    </div>

    <!-- Alert error banner -->
    <div
      v-if="authError"
      data-testid="auth-error-banner"
      class="mb-5 rounded-lg bg-rose-50 p-3.5 text-sm text-rose-700 border border-rose-200"
      role="alert"
    >
      {{ authError }}
    </div>

    <form class="space-y-4" data-testid="login-form" @submit.prevent="onSubmit">
      <FormField id="login-username" label="Username or Staff ID" :error="errors.username" required>
        <template #default="{ id, ariaDescribedby, ariaInvalid }">
          <div class="relative">
            <div
              class="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400"
            >
              <UserIcon class="w-4 h-4" aria-hidden="true" />
            </div>
            <InputText
              :id="id"
              v-model="username"
              v-bind="usernameProps"
              type="text"
              autocomplete="username"
              placeholder="e.g. admin or officer"
              :aria-describedby="ariaDescribedby"
              :aria-invalid="ariaInvalid"
              class="w-full pl-9 pr-3 py-2 text-sm border rounded-lg focus:outline-none focus:ring-2 focus:ring-teal-500 transition-colors"
              :class="errors.username ? 'border-rose-400 focus:ring-rose-500' : 'border-slate-300'"
              data-testid="username-input"
            />
          </div>
        </template>
      </FormField>

      <FormField id="login-password" label="Password" :error="errors.password" required>
        <template #default="{ id, ariaDescribedby, ariaInvalid }">
          <div class="relative">
            <div
              class="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400"
            >
              <Lock class="w-4 h-4" aria-hidden="true" />
            </div>
            <InputText
              :id="id"
              v-model="password"
              v-bind="passwordProps"
              type="password"
              autocomplete="current-password"
              placeholder="••••••••"
              :aria-describedby="ariaDescribedby"
              :aria-invalid="ariaInvalid"
              class="w-full pl-9 pr-3 py-2 text-sm border rounded-lg focus:outline-none focus:ring-2 focus:ring-teal-500 transition-colors"
              :class="errors.password ? 'border-rose-400 focus:ring-rose-500' : 'border-slate-300'"
              data-testid="password-input"
            />
          </div>
        </template>
      </FormField>

      <div class="pt-2">
        <Button
          type="submit"
          :loading="isSubmitting"
          data-testid="login-submit-btn"
          class="w-full justify-center bg-teal-600 hover:bg-teal-700 text-white font-medium py-2.5 px-4 rounded-lg transition-colors flex items-center gap-2 cursor-pointer shadow-xs disabled:opacity-50"
        >
          <LogIn class="w-4 h-4" aria-hidden="true" />
          <span>Sign In</span>
        </Button>
      </div>

      <div class="pt-3 border-t border-slate-100 text-center">
        <p class="text-xs text-slate-400">
          Development environment: use
          <code class="px-1 py-0.5 rounded bg-slate-100 text-slate-700 font-mono">admin</code> or
          any username to test.
        </p>
      </div>
    </form>
  </div>
</template>
