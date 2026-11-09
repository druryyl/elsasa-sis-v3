<script setup lang="ts">
import { computed } from 'vue';
import Button from 'primevue/button';
import Tag from 'primevue/tag';
import {
  Printer,
  Building2,
  BookOpen,
} from 'lucide-vue-next';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import type {
  ReportCard,
  ReportCardSnapshot,
  ReportCardAttendanceSummary,
} from '../types';

interface Props {
  reportCard?: ReportCard | null;
  snapshot?: ReportCardSnapshot | null;
  showPrintButton?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  reportCard: null,
  snapshot: null,
  showPrintButton: true,
});

const emit = defineEmits<{
  (e: 'print'): void;
}>();

// Resolve active snapshot: explicit snapshot prop > reportCard.snapshot > parsed contentSnapshot
const resolvedSnapshot = computed<ReportCardSnapshot | null>(() => {
  if (props.snapshot) return props.snapshot;
  if (props.reportCard?.snapshot) return props.reportCard.snapshot;
  if (props.reportCard?.contentSnapshot) {
    try {
      return JSON.parse(props.reportCard.contentSnapshot) as ReportCardSnapshot;
    } catch {
      return null;
    }
  }
  return null;
});

// Student profile fields
const studentName = computed(() => {
  return (
    resolvedSnapshot.value?.studentName ||
    props.reportCard?.studentName ||
    'Student Name'
  );
});

const studentNisn = computed(() => {
  return (
    resolvedSnapshot.value?.studentNisn ||
    props.reportCard?.studentNisn ||
    '-'
  );
});

const periodLabel = computed(() => {
  return (
    resolvedSnapshot.value?.periodLabel ||
    props.reportCard?.academicPeriodLabel ||
    props.reportCard?.periodLabel ||
    'Academic Period'
  );
});

const reportTypeLabel = computed(() => {
  const type = resolvedSnapshot.value?.reportType || props.reportCard?.reportType;
  if (type === 'end_of_term') return 'Laporan Hasil Belajar Akhir Semester (End of Term)';
  if (type === 'mid_term') return 'Laporan Kemajuan Belajar Tengah Semester (Mid-Term)';
  return 'Laporan Hasil Belajar Siswa';
});

const templateName = computed(() => {
  return resolvedSnapshot.value?.templateName || props.reportCard?.templateName || 'Buku Rapor Merdeka';
});

// Subjects entries
interface DisplaySubjectEntry {
  learningUnitId: string;
  subjectCode: string;
  subjectTitle: string;
  scoreNumeric?: number | null;
  gradeLetter?: string | null;
  gradeLabel?: string | null;
  teacherComment?: string | null;
  displayOrder: number;
}

const subjectEntries = computed<DisplaySubjectEntry[]>(() => {
  if (resolvedSnapshot.value?.subjectEntries && resolvedSnapshot.value.subjectEntries.length > 0) {
    return resolvedSnapshot.value.subjectEntries;
  }
  if (props.reportCard?.entries && props.reportCard.entries.length > 0) {
    return props.reportCard.entries.map((e, idx) => ({
      learningUnitId: e.learningUnitId,
      subjectCode: e.subjectCode || `SUBJ-${idx + 1}`,
      subjectTitle: e.subjectTitle || 'Mata Pelajaran',
      scoreNumeric: e.scoreNumeric,
      gradeLetter: e.gradeLetter,
      gradeLabel: e.gradeLabel,
      teacherComment: e.teacherComment,
      displayOrder: e.displayOrder ?? idx + 1,
    }));
  }
  return [];
});

// Attendance summary
const attendance = computed<ReportCardAttendanceSummary>(() => {
  if (resolvedSnapshot.value?.attendanceSummary) {
    return resolvedSnapshot.value.attendanceSummary;
  }
  if (props.reportCard?.attendanceSummary) {
    return props.reportCard.attendanceSummary;
  }
  return {
    presentDays: 0,
    absentDays: 0,
    lateDays: 0,
    excusedDays: 0,
  };
});

const narrativeComments = computed(() => {
  return (
    resolvedSnapshot.value?.narrativeComments ||
    props.reportCard?.narrativeComments ||
    'Peserta didik menunjukkan perkembangan akademik dan karakter yang baik sesuai profil Pelajar Pancasila.'
  );
});

const approvedBy = computed(() => {
  return resolvedSnapshot.value?.approvedBy || props.reportCard?.approvedBy || 'Academic Authority';
});

const approvedDateFormatted = computed(() => {
  const dateStr = resolvedSnapshot.value?.approvedDate || props.reportCard?.approvedDate;
  return dateStr ? formatDisplayDate(dateStr) : formatDisplayDate(new Date().toISOString());
});

function handlePrint() {
  emit('print');
  if (typeof window !== 'undefined') {
    // eslint-disable-next-line no-undef
    window.print();
  }
}
</script>

<template>
  <div class="report-card-print-container" data-testid="report-card-print-preview">
    <!-- Action Bar (Hidden during printing) -->
    <div
      v-if="showPrintButton"
      class="no-print flex items-center justify-between pb-4 mb-6 border-b border-slate-200"
    >
      <div class="flex items-center gap-2">
        <Tag
          value="Buku Rapor Merdeka"
          severity="info"
          class="!font-semibold"
        />
        <Tag
          v-if="resolvedSnapshot || reportCard?.status === 'published'"
          value="Certified &amp; Published"
          severity="success"
        />
      </div>

      <Button
        type="button"
        label="Print / Save PDF"
        size="small"
        class="!bg-teal-700 hover:!bg-teal-800 text-white font-medium cursor-pointer shadow-sm"
        data-testid="btn-execute-print"
        @click="handlePrint"
      >
        <template #icon>
          <Printer class="w-4 h-4 mr-1.5" />
        </template>
      </Button>
    </div>

    <!-- Official Statutory Printable Sheet -->
    <div
      class="print-sheet bg-white p-6 sm:p-10 border border-slate-300 rounded-lg shadow-sm text-slate-900 mx-auto max-w-4xl"
      data-testid="print-sheet-content"
    >
      <!-- School Header & Crest -->
      <div class="text-center pb-5 border-b-2 border-slate-800 mb-6">
        <div class="flex items-center justify-center gap-2 text-xs font-semibold tracking-wider text-teal-800 uppercase mb-1">
          <Building2 class="w-4 h-4 text-teal-700" />
          <span>Yayasan Pendidikan Elsasa Islamic School</span>
        </div>
        <h1 class="text-xl sm:text-2xl font-extrabold text-slate-950 uppercase tracking-wide">
          Sekolah Menengah Elsasa
        </h1>
        <p class="text-xs text-slate-600 mt-0.5">
          Jl. Cendekia Mandiri No. 10, Jakarta Selatan | Telp: (021) 7890-1234 | NPSN: 69987654
        </p>
        <div class="mt-3 inline-block px-4 py-1 bg-slate-100 rounded text-xs font-bold text-slate-800 border border-slate-300 uppercase">
          {{ reportTypeLabel }}
        </div>
      </div>

      <!-- Student & Period Metadata Grid -->
      <div class="grid grid-cols-2 gap-4 text-xs mb-6 p-3 bg-slate-50 border border-slate-200 rounded">
        <div class="space-y-1.5">
          <div class="flex">
            <span class="w-32 text-slate-500 font-medium">Nama Peserta Didik:</span>
            <span class="font-bold text-slate-900" data-testid="print-student-name">{{ studentName }}</span>
          </div>
          <div class="flex">
            <span class="w-32 text-slate-500 font-medium">NISN / No. Induk:</span>
            <span class="font-mono text-slate-800" data-testid="print-student-nisn">{{ studentNisn }}</span>
          </div>
          <div class="flex">
            <span class="w-32 text-slate-500 font-medium">Kurikulum:</span>
            <span class="font-medium text-slate-800">{{ templateName }}</span>
          </div>
        </div>
        <div class="space-y-1.5">
          <div class="flex">
            <span class="w-32 text-slate-500 font-medium">Tahun Ajaran / Term:</span>
            <span class="font-bold text-slate-900" data-testid="print-period-label">{{ periodLabel }}</span>
          </div>
          <div class="flex">
            <span class="w-32 text-slate-500 font-medium">Fase / Tingkat:</span>
            <span class="text-slate-800">Fase D / Kelas 7</span>
          </div>
          <div class="flex">
            <span class="w-32 text-slate-500 font-medium">Status Dokumen:</span>
            <span class="text-emerald-700 font-semibold uppercase">Resmi &amp; Terverifikasi</span>
          </div>
        </div>
      </div>

      <!-- Section A: Academic Achievements Table (Mata Pelajaran & Nilai) -->
      <div class="mb-6 space-y-2">
        <h2 class="text-xs font-bold text-slate-900 uppercase tracking-wider flex items-center gap-1.5">
          <BookOpen class="w-4 h-4 text-teal-700" />
          <span>A. Capaian Kompetensi Akademik (Academic Learning Outcomes)</span>
        </h2>

        <div class="overflow-x-auto border border-slate-300 rounded">
          <table class="w-full text-xs text-left border-collapse" data-testid="print-subjects-table">
            <thead>
              <tr class="bg-slate-100 border-b border-slate-300 text-slate-800 font-bold">
                <th class="py-2.5 px-3 w-10 text-center border-r border-slate-300">No.</th>
                <th class="py-2.5 px-3 border-r border-slate-300">Mata Pelajaran</th>
                <th class="py-2.5 px-3 w-20 text-center border-r border-slate-300">Nilai Akhir</th>
                <th class="py-2.5 px-3 w-16 text-center border-r border-slate-300">Predikat</th>
                <th class="py-2.5 px-3">Capaian Kompetensi &amp; Catatan Guru</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-200">
              <tr
                v-for="(subj, index) in subjectEntries"
                :key="subj.learningUnitId || index"
                class="hover:bg-slate-50"
                :data-testid="`print-subject-row-${index}`"
              >
                <td class="py-2 px-3 text-center border-r border-slate-200 font-medium text-slate-600">
                  {{ index + 1 }}
                </td>
                <td class="py-2 px-3 border-r border-slate-200 font-semibold text-slate-900">
                  <div>{{ subj.subjectTitle }}</div>
                  <div class="text-[10px] text-slate-400 font-mono">{{ subj.subjectCode }}</div>
                </td>
                <td class="py-2 px-3 text-center border-r border-slate-200 font-bold text-slate-900">
                  {{ subj.scoreNumeric !== null && subj.scoreNumeric !== undefined ? subj.scoreNumeric : '-' }}
                </td>
                <td class="py-2 px-3 text-center border-r border-slate-200 font-bold text-teal-800">
                  {{ subj.gradeLetter || '-' }}
                </td>
                <td class="py-2 px-3 text-slate-700 leading-relaxed text-[11px]">
                  <p v-if="subj.gradeLabel" class="font-medium text-slate-800">
                    [{{ subj.gradeLabel }}]
                  </p>
                  <p>{{ subj.teacherComment || 'Menunjukkan penguasaan materi yang baik dan mampu menyelesaikan tugas secara konsisten.' }}</p>
                </td>
              </tr>

              <tr v-if="subjectEntries.length === 0">
                <td colspan="5" class="py-6 text-center text-slate-500 italic">
                  Belum ada catatan nilai mata pelajaran yang diterbitkan.
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Section B: Attendance Summary & Homeroom Remarks -->
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4 mb-6">
        <!-- Attendance Table -->
        <div class="space-y-1.5">
          <h2 class="text-xs font-bold text-slate-900 uppercase tracking-wider">
            B. Rekapitulasi Kehadiran (Attendance)
          </h2>
          <div class="border border-slate-300 rounded overflow-hidden">
            <table class="w-full text-xs text-left border-collapse" data-testid="print-attendance-table">
              <tbody class="divide-y divide-slate-200">
                <tr>
                  <td class="py-1.5 px-3 text-slate-600 font-medium">Sakit (Excused Medical)</td>
                  <td class="py-1.5 px-3 text-right font-bold text-slate-900">{{ attendance.excusedDays }} hari</td>
                </tr>
                <tr>
                  <td class="py-1.5 px-3 text-slate-600 font-medium">Izin (Authorized Absence)</td>
                  <td class="py-1.5 px-3 text-right font-bold text-slate-900">{{ attendance.lateDays }} hari</td>
                </tr>
                <tr>
                  <td class="py-1.5 px-3 text-slate-600 font-medium">Tanpa Keterangan (Unexcused)</td>
                  <td class="py-1.5 px-3 text-right font-bold text-slate-900">{{ attendance.absentDays }} hari</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- Homeroom Teacher Remarks -->
        <div class="space-y-1.5">
          <h2 class="text-xs font-bold text-slate-900 uppercase tracking-wider">
            C. Catatan Wali Kelas (Homeroom Remarks)
          </h2>
          <div class="border border-slate-300 rounded p-3 bg-slate-50/80 min-h-[96px] text-xs text-slate-800 leading-relaxed italic" data-testid="print-homeroom-remarks">
            "{{ narrativeComments }}"
          </div>
        </div>
      </div>

      <!-- Section D: Signature Blocks -->
      <div class="pt-6 border-t border-slate-300 mt-8">
        <div class="text-right text-xs text-slate-600 mb-6">
          Jakarta Selatan, {{ approvedDateFormatted }}
        </div>

        <div class="grid grid-cols-3 gap-6 text-center text-xs">
          <!-- Parent Signature -->
          <div class="space-y-16">
            <div class="font-medium text-slate-600">Mengetahui,<br />Orang Tua / Wali Siswa</div>
            <div class="border-b border-slate-400 mx-4 font-semibold text-slate-800">
              ( .................................................. )
            </div>
          </div>

          <!-- Homeroom Teacher Signature -->
          <div class="space-y-16">
            <div class="font-medium text-slate-600">Wali Kelas / Pembimbing</div>
            <div class="border-b border-slate-400 mx-4 font-semibold text-slate-800">
              {{ approvedBy }}
            </div>
          </div>

          <!-- Principal Signature -->
          <div class="space-y-16">
            <div class="font-medium text-slate-600">Mengetahui,<br />Kepala Sekolah</div>
            <div class="border-b border-slate-400 mx-4 font-semibold text-slate-800">
              Dr. H. Muhammad Ramdhan, M.Pd.
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
@media print {
  /* Hide all surrounding non-printable layout chrome */
  :global(header),
  :global(nav),
  :global(aside),
  :global(.no-print) {
    display: none !important;
  }

  /* Target container to fill print page cleanly */
  .report-card-print-container {
    width: 100% !important;
    margin: 0 !important;
    padding: 0 !important;
  }

  .print-sheet {
    box-shadow: none !important;
    border: none !important;
    width: 100% !important;
    max-width: 100% !important;
    padding: 0 !important;
  }
}
</style>
