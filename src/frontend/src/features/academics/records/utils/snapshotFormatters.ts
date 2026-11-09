import type {
  ReportCardSnapshot,
  ReportCardAttendanceSummary,
  ReportCardSubjectEntrySnapshot,
} from '@/features/academics/assessment/types';

/**
 * Snapshot Data Formatters & Parsers
 * Pure utility functions for validating, parsing, and formatting frozen JSON snapshots
 * for Report Cards (Buku Rapor Merdeka) and Cumulative Transcripts (TD-05, GAP-006).
 */

/**
 * Safely parse a JSON report card snapshot string into a structured ReportCardSnapshot object.
 * Returns null if snapshot is null, undefined, empty, or unparseable.
 */
export function parseReportCardSnapshot(
  snapshotJson?: string | null,
): ReportCardSnapshot | null {
  if (!snapshotJson || typeof snapshotJson !== 'string' || !snapshotJson.trim()) {
    return null;
  }

  try {
    const parsed = JSON.parse(snapshotJson) as unknown;
    if (!parsed || typeof parsed !== 'object') {
      return null;
    }

    const candidate = parsed as Partial<ReportCardSnapshot>;
    if (!candidate.reportCardId || !candidate.studentId || !Array.isArray(candidate.subjectEntries)) {
      return null;
    }

    return parsed as ReportCardSnapshot;
  } catch {
    return null;
  }
}

/**
 * Formats attendance summary tallies into aggregate counts and percentage rate.
 */
export function formatAttendanceSummary(
  summary?: ReportCardAttendanceSummary | null,
): {
  presentDays: number;
  absentDays: number;
  lateDays: number;
  excusedDays: number;
  totalDays: number;
  attendanceRateFormatted: string;
} {
  if (!summary) {
    return {
      presentDays: 0,
      absentDays: 0,
      lateDays: 0,
      excusedDays: 0,
      totalDays: 0,
      attendanceRateFormatted: '0.0%',
    };
  }

  const present = Number(summary.presentDays ?? 0);
  const absent = Number(summary.absentDays ?? 0);
  const late = Number(summary.lateDays ?? 0);
  const excused = Number(summary.excusedDays ?? 0);
  const total = present + absent + late + excused;

  const rate = total > 0 ? (present / total) * 100 : 0;
  return {
    presentDays: present,
    absentDays: absent,
    lateDays: late,
    excusedDays: excused,
    totalDays: total,
    attendanceRateFormatted: `${rate.toFixed(1)}%`,
  };
}

/**
 * Formats score display string (e.g. "85.0 / 100" or "-" when null).
 */
export function formatScoreDisplay(
  scoreNumeric?: number | null,
  maxScore?: number | null,
): string {
  if (scoreNumeric === null || scoreNumeric === undefined || Number.isNaN(Number(scoreNumeric))) {
    return '-';
  }

  const formattedScore = Number(scoreNumeric).toFixed(1).replace(/\.0$/, '');
  if (maxScore !== null && maxScore !== undefined && !Number.isNaN(Number(maxScore))) {
    const formattedMax = Number(maxScore).toFixed(1).replace(/\.0$/, '');
    return `${formattedScore} / ${formattedMax}`;
  }

  return formattedScore;
}

/**
 * Formats grade presentation combining letter grade and competency label.
 */
export function formatGradeDisplay(
  gradeLetter?: string | null,
  gradeLabel?: string | null,
): string {
  const parts: string[] = [];
  if (gradeLetter && gradeLetter.trim()) {
    parts.push(gradeLetter.trim());
  }
  if (gradeLabel && gradeLabel.trim()) {
    parts.push(`(${gradeLabel.trim()})`);
  }
  return parts.length > 0 ? parts.join(' ') : '-';
}

/**
 * Calculates arithmetic average across all valid numeric scores in subject entries.
 */
export function calculateSnapshotAverage(
  entries?: ReportCardSubjectEntrySnapshot[] | null,
): number | null {
  if (!entries || entries.length === 0) {
    return null;
  }

  const validScores = entries
    .map((e) => e.scoreNumeric)
    .filter((s): s is number => typeof s === 'number' && !Number.isNaN(s));

  if (validScores.length === 0) {
    return null;
  }

  const sum = validScores.reduce((acc, curr) => acc + curr, 0);
  const avg = sum / validScores.length;
  return Math.round(avg * 100) / 100;
}

/**
 * Sorts subject entries deterministically by displayOrder ascending, then subjectTitle ascending.
 */
export function sortSubjectEntries(
  entries?: ReportCardSubjectEntrySnapshot[] | null,
): ReportCardSubjectEntrySnapshot[] {
  if (!entries) {
    return [];
  }

  return [...entries].sort((a, b) => {
    const orderA = a.displayOrder ?? 0;
    const orderB = b.displayOrder ?? 0;
    if (orderA !== orderB) {
      return orderA - orderB;
    }
    return (a.subjectTitle || '').localeCompare(b.subjectTitle || '');
  });
}
