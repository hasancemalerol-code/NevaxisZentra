import {
  getOperationalRiskAssessmentLabel,
  getOperationalRiskCategoryLabel,
  getOperationalRiskDetectionOutcomeLabel,
  getOperationalRiskDriverLabel,
  getOperationalRiskEvidenceLabel,
  getOperationalRiskSourceModuleLabel,
} from "../domain/operational-risk-labels";
import type {
  OperationalRiskDistributionSlice,
  OperationalRiskEvidenceItem,
  OperationalRiskKpiCard,
  OperationalRiskKpiTone,
} from "../types/operational-risk-types";

const DONUT_OUTER_RADIUS = 68;
const DONUT_INNER_RADIUS = 42;
const DONUT_CENTER = { x: 80, y: 80 } as const;

const UNKNOWN_TECHNICAL_REASON = "Tanımlanmamış teknik neden";

const VISIBLE_TOKEN_LABELS: Record<string, string> = {
  "connectivity-risk": "Bağlantı riski",
  "maintenance-risk": "Bakım riski",
  "energy-risk": "Enerji riski",
  "material-shortfall": "Malzeme yetersizliği",
  "supplier-delay": "Tedarik gecikmesi",
  "order-delivery-delay": "Termin gecikmesi",
  "cost-deviation-exceeded": "Maliyet sapması aşıldı",
  "production-continuity-risk": "Üretim sürekliliği riski",
  "quality-risk": "Kalite riski",
  "source-stale": "Kaynak bayat",
  "missing-threshold": "Eşik eksik",
  "missing-denominator": "Payda eksik",
  "insufficient-evidence": "Yetersiz kanıt",
  "source-conflict": "Kaynak uyuşmazlığı",
  "within-threshold": "Eşik içinde",
  "risk-detected": "Risk tespit edildi",
  "insufficient-data": "Veri yetersiz",
  "endpoint-offline": "Uç nokta çevrimdışı",
  "observation-stale": "Gözlem güncelliğini yitirmiş",
  "source-sequence-gap": "Kaynak sıra boşluğu",
  "clock-skew-exceeded": "Saat sapması sınırı aşılmış",
  "within-quality-profile": "Kalite profili içinde",
  "iot-unavailable": "IoT kaynağı kullanılamıyor",
  "energy-unavailable": "Enerji kaynağı kullanılamıyor",
  "maintenance-unavailable": "Bakım kaynağı kullanılamıyor",
  simulated: "Simülasyon",
  calculated: "Hesaplanmış",
  measured: "Ölçülmüş",
  estimated: "Tahmini",
  "development-simulator": "Geliştirme simülatörü",
  "development-pilot": "Geliştirme pilot kaynağı",
};

export const KNOWN_RAW_VISIBLE_CODES = Object.keys(VISIBLE_TOKEN_LABELS);

const TECHNICAL_TOKEN_PATTERN = /^[a-z0-9]+(?:[-_][a-z0-9]+)+$/i;

const CROSS_MODULE_TITLE_PATTERN = /^Cross-module risk detected \(([^)]+)\)$/i;

function humanizeToken(value: string): string {
  return value
    .split(/[-_.]/)
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ");
}

export function localizeOperationalRiskVisibleToken(
  value: string | null | undefined,
): string {
  if (!value?.trim()) return "Belirtilmedi";

  const trimmed = value.trim();
  if (VISIBLE_TOKEN_LABELS[trimmed]) return VISIBLE_TOKEN_LABELS[trimmed];

  const driverLabel = getOperationalRiskDriverLabel(trimmed);
  if (driverLabel !== humanizeToken(trimmed)) return driverLabel;

  const evidenceLabel = getOperationalRiskEvidenceLabel(trimmed);
  if (evidenceLabel !== humanizeToken(trimmed)) return evidenceLabel;

  const outcomeLabel = getOperationalRiskDetectionOutcomeLabel(trimmed);
  if (outcomeLabel !== humanizeToken(trimmed)) return outcomeLabel;

  const assessmentLabel = getOperationalRiskAssessmentLabel(trimmed);
  if (assessmentLabel !== humanizeToken(trimmed)) return assessmentLabel;

  const sourceLabel = getOperationalRiskSourceModuleLabel(trimmed);
  if (sourceLabel !== "Belirtilmedi" && sourceLabel !== humanizeToken(trimmed)) return sourceLabel;

  if (TECHNICAL_TOKEN_PATTERN.test(trimmed)) return UNKNOWN_TECHNICAL_REASON;

  return trimmed;
}

export function localizeOperationalRiskEvidenceSummary(summary: string): string {
  return localizeOperationalRiskVisibleToken(summary);
}

export function localizeOperationalRiskVisibleText(text: string | null | undefined): string {
  if (!text?.trim()) return "";
  const trimmed = text.trim();
  if (TECHNICAL_TOKEN_PATTERN.test(trimmed) || VISIBLE_TOKEN_LABELS[trimmed]) {
    return localizeOperationalRiskVisibleToken(trimmed);
  }
  return trimmed;
}

export function collectOperationalRiskTechnicalAuditTokens(input: Readonly<{
  evidenceTypeCode?: string | null;
  comparisonReference?: string | null;
  sourceReference?: string | null;
  summary?: string | null;
}>): readonly string[] {
  const tokens = [
    input.evidenceTypeCode,
    input.comparisonReference,
    input.sourceReference,
    input.summary,
  ]
    .filter((value): value is string => Boolean(value?.trim()))
    .map((value) => value.trim());
  return [...new Set(tokens)];
}

const KNOWN_ENGLISH_CASE_TITLES: Record<string, string> = {
  "Terminal energy detection case for recurrence proof":
    "Tekrar kanıtı için terminal enerji tespit vakası",
};

const RAW_ENGLISH_PRIMARY_TITLE_PATTERNS = [
  /^Cross-module risk detected \(/i,
  /^Terminal energy detection case/i,
];

export function isRawTechnicalTokenVisible(value: string): boolean {
  const trimmed = value.trim();
  if (!trimmed) return false;
  if (RAW_ENGLISH_PRIMARY_TITLE_PATTERNS.some((pattern) => pattern.test(trimmed))) return true;
  if (TECHNICAL_TOKEN_PATTERN.test(trimmed)) {
    const localized = localizeOperationalRiskVisibleToken(trimmed);
    return localized === UNKNOWN_TECHNICAL_REASON || VISIBLE_TOKEN_LABELS[trimmed] !== undefined;
  }
  return false;
}

export type OperationalRiskDonutArc = Readonly<{
  key: string;
  label: string;
  count: number;
  ratio: number | null;
  color: string;
  path: string;
}>;

export function resolveOperationalRiskKpiToneClass(tone: OperationalRiskKpiTone): string {
  return `oprisk-kpi-card__tone--${tone}`;
}

export function buildOperationalRiskDonutArcs(
  slices: readonly OperationalRiskDistributionSlice[],
): readonly OperationalRiskDonutArc[] {
  const total = slices.reduce((sum, slice) => sum + slice.count, 0);
  if (total <= 0) return [];

  let cursor = -Math.PI / 2;
  return slices.map((slice) => {
    const angle = (slice.count / total) * Math.PI * 2;
    const start = cursor;
    const end = cursor + angle;
    cursor = end;

    const outerStart = polar(DONUT_CENTER, DONUT_OUTER_RADIUS, start);
    const outerEnd = polar(DONUT_CENTER, DONUT_OUTER_RADIUS, end);
    const innerEnd = polar(DONUT_CENTER, DONUT_INNER_RADIUS, end);
    const innerStart = polar(DONUT_CENTER, DONUT_INNER_RADIUS, start);
    const largeArc = angle > Math.PI ? 1 : 0;

    const path = [
      `M ${outerStart.x} ${outerStart.y}`,
      `A ${DONUT_OUTER_RADIUS} ${DONUT_OUTER_RADIUS} 0 ${largeArc} 1 ${outerEnd.x} ${outerEnd.y}`,
      `L ${innerEnd.x} ${innerEnd.y}`,
      `A ${DONUT_INNER_RADIUS} ${DONUT_INNER_RADIUS} 0 ${largeArc} 0 ${innerStart.x} ${innerStart.y}`,
      "Z",
    ].join(" ");

    return {
      key: slice.key,
      label: slice.label,
      count: slice.count,
      ratio: slice.ratio,
      color: slice.color,
      path,
    };
  });
}

function polar(
  center: Readonly<{ x: number; y: number }>,
  radius: number,
  angle: number,
): Readonly<{ x: number; y: number }> {
  return {
    x: center.x + radius * Math.cos(angle),
    y: center.y + radius * Math.sin(angle),
  };
}

export function reconcileOperationalRiskDistribution(
  entries: ReadonlyArray<Readonly<{ key: string; label: string; count: number; color: string }>>,
): Readonly<{ slices: readonly OperationalRiskDistributionSlice[]; total: number }> {
  const total = entries.reduce((sum, entry) => sum + entry.count, 0);
  if (total <= 0) {
    return { slices: [], total: 0 };
  }

  const slices = entries
    .filter((entry) => entry.count > 0)
    .map((entry) => ({
      key: entry.key,
      label: entry.label,
      count: entry.count,
      ratio: entry.count / total,
      color: entry.color,
    }));

  return { slices, total };
}

export function validateOperationalRiskKpiReconciliation(
  kpis: readonly OperationalRiskKpiCard[],
  openCaseCount: number,
): Readonly<{ compliant: boolean; reason: string | null }> {
  const openKpi = kpis.find((kpi) => kpi.id === "open-cases");
  if (!openKpi || openKpi.value === "—") {
    return { compliant: openCaseCount === 0, reason: openCaseCount > 0 ? "open-kpi-missing" : null };
  }

  const parsed = Number.parseInt(openKpi.value.replace(/\./g, ""), 10);
  if (!Number.isFinite(parsed)) {
    return { compliant: false, reason: "open-kpi-unparseable" };
  }

  return parsed === openCaseCount
    ? { compliant: true, reason: null }
    : { compliant: false, reason: "open-kpi-mismatch" };
}

export const OPERATIONAL_RISK_SEVERITY_COLORS: Record<string, string> = {
  critical: "#dc2626",
  high: "#d97706",
  moderate: "#2563eb",
  low: "#16a34a",
  unknown: "#94a3b8",
};

export const OPERATIONAL_RISK_OUTCOME_COLORS: Record<string, string> = {
  "risk-detected": "#dc2626",
  "within-threshold": "#16a34a",
  "insufficient-data": "#64748b",
  "source-conflict": "#7c3aed",
};

export function localizeOperationalRiskCaseTitle(
  title: string,
  category: string,
): string {
  if (KNOWN_ENGLISH_CASE_TITLES[title]) {
    return KNOWN_ENGLISH_CASE_TITLES[title];
  }

  const crossMatch = title.match(CROSS_MODULE_TITLE_PATTERN);
  if (crossMatch) {
    const driverLabel = getOperationalRiskDriverLabel(crossMatch[1]).toLocaleLowerCase("tr-TR");
    return `Modüller arası ${driverLabel} tespit edildi`;
  }

  if (!RAW_ENGLISH_PRIMARY_TITLE_PATTERNS.some((pattern) => pattern.test(title))) {
    return title;
  }

  return `${getOperationalRiskCategoryLabel(category)} riski`;
}

export function isRawEnglishOperationalRiskPrimaryTitle(title: string): boolean {
  return RAW_ENGLISH_PRIMARY_TITLE_PATTERNS.some((pattern) => pattern.test(title));
}

export function extractOperationalRiskCaseDrivers(
  title: string,
  category: string,
  evidence: readonly OperationalRiskEvidenceItem[],
): Readonly<{ leadingDriverLabel: string | null; supportingDriverLabels: readonly string[] }> {
  const crossMatch = title.match(CROSS_MODULE_TITLE_PATTERN);
  if (crossMatch) {
    const leadingDriverLabel = localizeOperationalRiskVisibleToken(crossMatch[1]);
    const supportingDriverLabels = evidence
      .flatMap((entry) => entry.technicalAuditTokens)
      .map((value) => localizeOperationalRiskVisibleToken(value))
      .filter((label) => label !== leadingDriverLabel && label !== "Belirtilmedi");
    return {
      leadingDriverLabel,
      supportingDriverLabels: [...new Set(supportingDriverLabels)],
    };
  }

  const evidenceDrivers = evidence
    .flatMap((entry) => [entry.comparisonReference, ...entry.technicalAuditTokens])
    .filter((value): value is string => Boolean(value))
    .map((value) => localizeOperationalRiskVisibleToken(value))
    .filter((label) => label !== "Belirtilmedi");

  if (evidenceDrivers.length > 0) {
    return {
      leadingDriverLabel: evidenceDrivers[0],
      supportingDriverLabels: evidenceDrivers.slice(1),
    };
  }

  return {
    leadingDriverLabel: getOperationalRiskCategoryLabel(category),
    supportingDriverLabels: [],
  };
}
