import 'vue-router';

/**
 * Represents a single breadcrumb segment in layout navigation headers.
 */
export interface BreadcrumbItem {
  label: string;
  to?: string | undefined;
  icon?: string | unknown;
  disabled?: boolean | undefined;
  current?: boolean | undefined;
}

/**
 * Represents an item or section header in the application sidebar or menu.
 */
export interface NavigationItem {
  id?: string | undefined;
  label: string;
  to?: string | undefined;
  icon?: string | unknown;
  permission?: string | undefined;
  roles?: string[] | undefined;
  badge?: string | number | undefined;
  badgeSeverity?: 'info' | 'success' | 'warn' | 'danger' | 'secondary' | 'contrast' | undefined;
  children?: NavigationItem[] | undefined;
  disabled?: boolean | undefined;
  external?: boolean | undefined;
}

/**
 * Extended metadata attached to Vue Router route records.
 * Enforces permission and layout attributes used by navigation guards and shells.
 */
export interface AppRouteMeta {
  requiresAuth?: boolean | undefined;
  permission?: string | undefined;
  roles?: string[] | undefined;
  title?: string | undefined;
  layout?: 'app' | 'auth' | 'empty' | string | undefined;
  breadcrumb?: string | undefined;
  activeMenu?: string | undefined;
  [key: string]: unknown;
}

declare module 'vue-router' {
  interface RouteMeta extends AppRouteMeta {}
}
