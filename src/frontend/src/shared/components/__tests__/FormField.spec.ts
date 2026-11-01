// @vitest-environment jsdom
import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import FormField from '../forms/FormField.vue';

describe('FormField.vue', () => {
  it('renders label text when provided', () => {
    const wrapper = mount(FormField, {
      props: {
        label: 'Academic Year Name',
        id: 'academic-year-input',
      },
    });

    const label = wrapper.find('label');
    expect(label.exists()).toBe(true);
    expect(label.text()).toContain('Academic Year Name');
    expect(label.attributes('for')).toBe('academic-year-input');
  });

  it('renders required indicator when required prop is true', () => {
    const wrapper = mount(FormField, {
      props: {
        label: 'Start Date',
        required: true,
      },
    });

    const requiredIndicator = wrapper.find('[data-testid="required-indicator"]');
    expect(requiredIndicator.exists()).toBe(true);
    expect(requiredIndicator.text()).toBe('*');
    expect(requiredIndicator.classes()).toContain('text-rose-600');
  });

  it('renders optional indicator when optional prop is true', () => {
    const wrapper = mount(FormField, {
      props: {
        label: 'Description',
        optional: true,
      },
    });

    const optionalIndicator = wrapper.find('[data-testid="optional-indicator"]');
    expect(optionalIndicator.exists()).toBe(true);
    expect(optionalIndicator.text()).toContain('(Optional)');
  });

  it('renders help text when provided', () => {
    const wrapper = mount(FormField, {
      props: {
        label: 'Code',
        id: 'code-field',
        helpText: 'Unique 4-digit academic year code (e.g. 2026)',
      },
    });

    const help = wrapper.find('[data-testid="form-field-help"]');
    expect(help.exists()).toBe(true);
    expect(help.text()).toBe('Unique 4-digit academic year code (e.g. 2026)');
    expect(help.attributes('id')).toBe('code-field-help');
  });

  it('renders red validation error message text and alert attributes', () => {
    const wrapper = mount(FormField, {
      props: {
        label: 'Email',
        id: 'email-field',
        error: 'Invalid email address format',
      },
    });

    const errorEl = wrapper.find('[data-testid="form-field-error"]');
    expect(errorEl.exists()).toBe(true);
    expect(errorEl.text()).toContain('Invalid email address format');
    expect(errorEl.attributes('role')).toBe('alert');
    expect(errorEl.attributes('aria-live')).toBe('polite');
    expect(errorEl.attributes('id')).toBe('email-field-error');
    expect(errorEl.classes()).toContain('text-rose-600');
  });

  it('renders multiple error messages when error is an array of strings', () => {
    const wrapper = mount(FormField, {
      props: {
        label: 'Password',
        error: ['Must be at least 8 characters', 'Must contain a number'],
      },
    });

    const errorEl = wrapper.find('[data-testid="form-field-error"]');
    expect(errorEl.exists()).toBe(true);
    expect(errorEl.text()).toContain('Must be at least 8 characters');
    expect(errorEl.text()).toContain('Must contain a number');
  });

  it('passes accessibility attributes and slot props to default slot input', () => {
    const wrapper = mount(FormField, {
      props: {
        id: 'student-id-field',
        label: 'Student ID',
        helpText: 'Format: STU-XXXX',
        error: 'Student ID already exists',
      },
      slots: {
        default: `
          <template #default="{ id, ariaDescribedby, ariaInvalid }">
            <input :id="id" :aria-describedby="ariaDescribedby" :aria-invalid="ariaInvalid" />
          </template>
        `,
      },
    });

    const input = wrapper.find('input');
    expect(input.attributes('id')).toBe('student-id-field');
    expect(input.attributes('aria-invalid')).toBe('true');
    expect(input.attributes('aria-describedby')).toBe(
      'student-id-field-error student-id-field-help',
    );
  });

  it('supports custom slots for label, help, and error', () => {
    const wrapper = mount(FormField, {
      slots: {
        label: '<span class="custom-label">Custom Label</span>',
        help: '<span class="custom-help">Custom Help</span>',
        error: '<span class="custom-error">Custom Error</span>',
      },
      props: {
        error: 'Trigger error slot',
      },
    });

    expect(wrapper.find('.custom-label').exists()).toBe(true);
    expect(wrapper.find('.custom-help').exists()).toBe(true);
    expect(wrapper.find('.custom-error').exists()).toBe(true);
  });
});
