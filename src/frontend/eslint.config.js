import js from '@eslint/js';
import tseslint from 'typescript-eslint';
import pluginVue from 'eslint-plugin-vue';

// 6 Core domains and 20 functional modules defined in frontend architecture (ADR-FE-001)
const featureDomains = {
  operations: ['calendar', 'scheduling', 'events'],
  students: ['admission', 'enrollment', 'records', 'placement'],
  finance: ['obligations', 'billing', 'payments', 'clearance'],
  academics: ['structure', 'activities', 'assessment', 'progression'],
  teaching: ['profiles', 'workload', 'assignments'],
  development: ['behaviour', 'counselling', 'extracurricular'],
};

// Generate architectural boundary rules for features layer
const allDomains = Object.keys(featureDomains);
const featureBoundaryConfigs = [];

for (const [domain, submodules] of Object.entries(featureDomains)) {
  const otherDomains = allDomains.filter((d) => d !== domain);

  for (const submodule of submodules) {
    const otherSubmodules = submodules.filter((s) => s !== submodule);

    const restrictedPatterns = [
      // Cannot import upwards into app layer (except global stores and queryClient cache tiers)
      {
        group: [
          '@/app/router',
          '@/app/router/**',
          '@/app/layouts',
          '@/app/layouts/**',
          '@/app/config',
          '@/app/config/**',
        ],
        message: 'Features must not import application shell, router, or layouts (ADR-FE-001).',
      },
      // Cannot import from other domains
      {
        group: otherDomains.flatMap((d) => [
          `@/features/${d}`,
          `@/features/${d}/**`,
          `**/features/${d}/**`,
          `**/${d}/**`,
        ]),
        message: `Cross-domain imports are strictly prohibited (ADR-FE-001). Submodule '${domain}/${submodule}' cannot import from other domains.`,
      },
      // Cannot import from sibling submodules in the same domain
      {
        group: otherSubmodules.flatMap((s) => [
          `@/features/${domain}/${s}`,
          `@/features/${domain}/${s}/**`,
          `**/${domain}/${s}/**`,
          `**/${s}/**`,
        ]),
        message: `Cross-submodule imports are strictly prohibited (ADR-FE-001). Submodule '${domain}/${submodule}' cannot import from sibling submodule.`,
      },
    ];

    featureBoundaryConfigs.push({
      files: [
        `src/features/${domain}/${submodule}/**/*.ts`,
        `src/features/${domain}/${submodule}/**/*.js`,
        `src/features/${domain}/${submodule}/**/*.vue`,
      ],
      rules: {
        'no-restricted-imports': [
          'error',
          {
            patterns: restrictedPatterns,
          },
        ],
      },
    });
  }
}

const sharedBoundaryConfig = {
  files: ['src/shared/**/*.ts', 'src/shared/**/*.js', 'src/shared/**/*.vue'],
  rules: {
    'no-restricted-imports': [
      'error',
      {
        patterns: [
          {
            group: [
              '@/features',
              '@/features/**',
              '**/features',
              '**/features/**',
              '@/app',
              '@/app/**',
              '**/app',
              '**/app/**',
            ],
            message:
              'Shared layer must not import upwards into features/ or app/ layers (ADR-FE-001). Shared kernel must remain domain-agnostic.',
          },
        ],
      },
    ],
  },
};

const generalFeaturesBoundaryConfig = {
  files: ['src/features/**/*.ts', 'src/features/**/*.js', 'src/features/**/*.vue'],
  rules: {
    'no-restricted-imports': [
      'error',
      {
        patterns: [
          {
            group: [
              '@/app/router',
              '@/app/router/**',
              '@/app/layouts',
              '@/app/layouts/**',
              '@/app/config',
              '@/app/config/**',
            ],
            message: 'Features must not import application shell, router, or layouts (ADR-FE-001).',
          },
        ],
      },
    ],
  },
};

export default tseslint.config(
  {
    ignores: [
      'dist/**',
      'node_modules/**',
      'coverage/**',
      '**/*.d.ts',
      'src/shared/types/api/schema.d.ts',
    ],
  },
  js.configs.recommended,
  ...tseslint.configs.recommended,
  ...pluginVue.configs['flat/recommended'],
  {
    files: ['*.vue', '**/*.vue'],
    languageOptions: {
      parserOptions: {
        parser: tseslint.parser,
        extraFileExtensions: ['.vue'],
        sourceType: 'module',
      },
    },
    rules: {
      'vue/block-order': [
        'error',
        {
          order: ['script', 'template', 'style'],
        },
      ],
      'vue/component-api-style': ['error', ['script-setup']],
      'vue/component-name-in-template-casing': ['error', 'PascalCase'],
      'vue/define-macros-order': [
        'error',
        {
          order: ['defineProps', 'defineEmits', 'defineSlots'],
        },
      ],
      'vue/multi-word-component-names': 'off',
      'vue/no-v-html': 'error',
      'vue/require-default-prop': 'off',
      'vue/max-attributes-per-line': 'off',
      'vue/singleline-html-element-content-newline': 'off',
      'vue/html-closing-bracket-newline': 'off',
      'vue/html-indent': 'off',
      'vue/html-self-closing': 'off',
    },
  },
  {
    files: ['**/*.ts', '**/*.vue'],
    rules: {
      '@typescript-eslint/no-explicit-any': 'error',
      '@typescript-eslint/no-empty-object-type': [
        'error',
        {
          allowInterfaces: 'with-single-extends',
        },
      ],
      '@typescript-eslint/consistent-type-imports': [
        'error',
        {
          prefer: 'type-imports',
          fixStyle: 'inline-type-imports',
        },
      ],
      '@typescript-eslint/no-unused-vars': [
        'warn',
        {
          argsIgnorePattern: '^_',
          varsIgnorePattern: '^_',
        },
      ],
    },
  },
  sharedBoundaryConfig,
  generalFeaturesBoundaryConfig,
  ...featureBoundaryConfigs,
);
