"use client";

import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
  useTransition,
  type FormEvent,
  type KeyboardEvent as ReactKeyboardEvent,
} from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import {
  Cable,
  ClipboardCheck,
  LoaderCircle,
  Plus,
  RefreshCw,
  Search,
} from "lucide-react";

import type {
  IotPlcAssessmentDetail,
  IotPlcAssessmentSummary,
  IotPlcEndpointDetail,
  IotPlcEndpointSummary,
  IotPlcFinding,
  IotPlcHealthSegment,
  IotPlcQualityProfileSummary,
  IotPlcSignalDetail,
  IotPlcSignalSummary,
  IotPlcTabId,
  IotPlcWorkspacePayload,
} from "../domain/iot-plc-types";
import {
  IOT_PLC_CURRENT_EVIDENCE_SCOPE,
  IOT_PLC_CURRENT_STATUS_NOTE,
  IOT_PLC_DEVELOPMENT_BANNER,
  IOT_PLC_EVIDENCE_FILTER_OPTIONS,
  IOT_PLC_LAST_KNOWN_OBSERVATIONS_TITLE,
  IOT_PLC_OUTCOME_FILTER_OPTIONS,
  IOT_PLC_TAB_OPTIONS,
  getIotPlcAssessmentTitle,
  getIotPlcDirectionLabel,
  getIotPlcDriverLabel,
  getIotPlcEndpointStatusLabel,
  getIotPlcEvidenceLabel,
  getIotPlcHealthLabel,
  getIotPlcOutcomeLabel,
  getIotPlcProfileStatusLabel,
  getIotPlcProtocolLabel,
  getIotPlcQualityStateLabel,
  getIotPlcSemanticTypeLabel,
} from "../domain/iot-plc-labels";
import {
  formatIotPlcCompactPeriod,
  formatIotPlcCount,
  formatIotPlcDateTime,
  formatIotPlcRatioPercent,
  formatIotPlcSeconds,
} from "../domain/iot-plc-formatters";
import {
  buildHealthDonutArcs,
  buildQualityBarSegments,
  formatIotPlcQualitySliceLine,
  healthTimelineTone,
  isIotPlcHealthCompositionAllUnknown,
  reconcileIotPlcQualityDistribution,
  resolveKpiToneClass,
} from "./iot-plc-presentation";
import {
  createIotPlcEndpointAction,
  createIotPlcSignalAction,
  evaluateIotPlcConnectivityAction,
  evaluateIotPlcSignalQualityAction,
  loadIotPlcAssessmentDetailAction,
  loadIotPlcEndpointDetailAction,
  loadIotPlcSignalDetailAction,
  refreshIotPlcWorkspaceAction,
  updateIotPlcEndpointAction,
  updateIotPlcProfileAction,
  updateIotPlcSignalAction,
} from "../server/iot-plc-actions";
import "./iot-plc-workspace.css";

type IotPlcWorkspaceViewProps = Readonly<{
  workspace: IotPlcWorkspacePayload;
  canManage: boolean;
}>;

const VALID_TAB_IDS = new Set<IotPlcTabId>(
  IOT_PLC_TAB_OPTIONS.map((tab) => tab.id),
);

const KPI_ICONS: Record<string, typeof Cable> = {
  "registered-endpoints": Cable,
  "online-endpoints": Cable,
  "degraded-offline-endpoints": Cable,
  "active-signals": ClipboardCheck,
  "fresh-signals": ClipboardCheck,
  "attention-assessments": ClipboardCheck,
};

const PROTOCOL_OPTIONS = [
  "opc-ua",
  "modbus-tcp",
  "mqtt",
  "http-push",
  "edge-agent",
  "manual-development-simulator",
] as const;

const DIRECTION_OPTIONS = [
  "read-only-pull",
  "inbound-push",
  "edge-relay",
] as const;

const ENDPOINT_STATUS_OPTIONS = ["active", "inactive", "retired", "draft"] as const;

const SEMANTIC_TYPE_OPTIONS = ["state", "measurement", "counter", "event"] as const;

const DATA_TYPE_OPTIONS = ["boolean", "integer", "decimal", "text", "timestamp"] as const;

const ASSET_TYPE_OPTIONS = ["unmapped", "machine", "auxiliary-equipment"] as const;

const QUALITY_SLICE_COLORS: Record<string, string> = {
  good: "#16a34a",
  uncertain: "#ca8a04",
  bad: "#dc2626",
  stale: "#64748b",
  "source-conflict": "#7c3aed",
};

type EndpointFormState = Readonly<{
  mode: "create" | "edit";
  reference: string | null;
  endpointCode: string;
  displayName: string;
  protocolType: string;
  connectionDirection: string;
  endpointStatus: string;
  configurationProvenance: string;
  connectionProfileJson: string;
  expectedVersion: number;
}>;

type SignalFormState = Readonly<{
  mode: "create" | "edit";
  reference: string | null;
  endpointReference: string;
  signalCode: string;
  displayName: string;
  sourceAddress: string;
  dataType: string;
  engineeringUnit: string;
  semanticType: string;
  mappedAssetType: string;
  mappedCanonicalAssetId: string;
  isEnabled: boolean;
  samplingExpectation: string;
  sourceProvenance: string;
  expectedVersion: number;
}>;

type ProfileFormState = Readonly<{
  profile: IotPlcQualityProfileSummary;
  displayName: string;
  expectedObservationIntervalSeconds: string;
  freshnessToleranceSeconds: string;
  staleThresholdSeconds: string;
  maximumClockSkewToleranceSeconds: string;
  minimumExpectedObservationCount: string;
  minimumGoodQualityRatio: string;
  maximumBadQualityRatio: string;
  maximumUncertainQualityRatio: string;
  maximumSourceConflictRatio: string;
  sequenceGapEvaluationEnabled: boolean;
  status: string;
  sourceProvenance: string;
}>;

function parseTabId(value: string | null): IotPlcTabId {
  if (value && VALID_TAB_IDS.has(value as IotPlcTabId)) {
    return value as IotPlcTabId;
  }
  return "overview";
}

function outcomeBadgeClass(outcome: string): string {
  switch (outcome) {
    case "healthy":
      return "iot-plc-badge iot-plc-badge--healthy";
    case "attention":
    case "degraded":
      return "iot-plc-badge iot-plc-badge--attention";
    case "source-conflict":
      return "iot-plc-badge iot-plc-badge--conflict";
    default:
      return "iot-plc-badge iot-plc-badge--insufficient";
  }
}

function healthBadgeClass(bucket: string): string {
  switch (bucket) {
    case "online":
      return "iot-plc-badge iot-plc-badge--online";
    case "degraded":
      return "iot-plc-badge iot-plc-badge--degraded";
    case "offline":
      return "iot-plc-badge iot-plc-badge--offline";
    default:
      return "iot-plc-badge iot-plc-badge--unknown";
  }
}

function findingSeverityClass(severity: IotPlcFinding["severity"]): string {
  switch (severity) {
    case "danger":
      return "iot-plc-badge iot-plc-badge--offline";
    case "warning":
      return "iot-plc-badge iot-plc-badge--attention";
    case "info":
      return "iot-plc-badge iot-plc-badge--healthy";
    default:
      return "iot-plc-badge iot-plc-badge--unknown";
  }
}

function assessmentKey(assessment: IotPlcAssessmentSummary): string {
  return `${assessment.assessmentKind}:${assessment.id}`;
}

function defaultEvaluationWindow(): Readonly<{
  evaluationWindowStartUtc: string;
  evaluationWindowEndUtc: string;
  evaluationTimeUtc: string;
}> {
  const end = new Date();
  const start = new Date(end.getTime() - 60 * 60 * 1000);
  return {
    evaluationWindowStartUtc: start.toISOString(),
    evaluationWindowEndUtc: end.toISOString(),
    evaluationTimeUtc: end.toISOString(),
  };
}

function IotPlcHealthDonut({
  segments,
  total,
}: {
  segments: readonly IotPlcHealthSegment[];
  total: number;
}) {
  const arcs = buildHealthDonutArcs(segments, total);
  const allUnknown = isIotPlcHealthCompositionAllUnknown(segments);
  const centerLabel = allUnknown ? "Güncel kanıt yok" : "Toplam bağlantı";

  return (
    <div className="iot-plc-health-donut-layout" data-testid="iot-plc-health-donut-layout">
      <div className="iot-plc-health-donut" data-testid="iot-plc-health-donut">
        <svg viewBox="0 0 120 120" role="img" aria-label="Bağlantı sağlık dağılımı halka grafiği">
          {arcs.map((arc) => (
            <path key={arc.id} d={arc.path} fill={arc.color} data-testid="iot-plc-health-donut-segment" />
          ))}
          <circle cx={60} cy={60} r={32} fill="#ffffff" />
        </svg>
        <div className="iot-plc-health-donut__center" aria-hidden="true">
          <span className="iot-plc-health-donut__label">{centerLabel}</span>
          <span className="iot-plc-health-donut__total">{formatIotPlcCount(total)}</span>
        </div>
      </div>
      <ul className="iot-plc-quality-bar__legend iot-plc-health-donut__legend">
        {segments.map((segment) => (
          <li
            key={segment.bucket}
            className="iot-plc-quality-bar__row"
            data-testid={`iot-plc-health-legend-${segment.bucket}`}
          >
            <span className="iot-plc-quality-bar__row-label">
              <span
                className="iot-plc-quality-bar__swatch"
                style={{ background: segment.color }}
                aria-hidden="true"
              />
              {segment.label}
            </span>
            <span className="iot-plc-quality-bar__row-value">
              {formatIotPlcCount(segment.count)} · {formatIotPlcRatioPercent(segment.percent)}
            </span>
          </li>
        ))}
      </ul>
    </div>
  );
}

function IotPlcQualityDistribution({
  slices,
  denominator,
  assessmentLabel,
  contractViolationCount,
}: {
  slices: IotPlcWorkspacePayload["qualityDistribution"];
  denominator: number | null;
  assessmentLabel: string | null;
  contractViolationCount: number;
}) {
  const barSegments = buildQualityBarSegments(slices, denominator);

  if (!denominator || denominator <= 0) {
    return (
      <p className="iot-plc-empty" data-testid="iot-plc-quality-distribution-empty">
        Uyumlu değerlendirme penceresi veya gözlem paydası bulunamadı; kalite dağılımı
        gösterilemiyor.
      </p>
    );
  }

  return (
    <div className="iot-plc-quality-bar" data-testid="iot-plc-quality-distribution">
      {contractViolationCount > 0 ? (
        <p className="iot-plc-contract-note" data-testid="iot-plc-quality-contract-note" role="status">
          {contractViolationCount} kayıtlı değerlendirme 0–1 oran sözleşmesini ihlal ediyor;
          en güncel uyumlu kayıt gösteriliyor.
        </p>
      ) : null}
      {assessmentLabel ? (
        <p className="iot-plc-panel__meta">{assessmentLabel}</p>
      ) : null}
      <div
        className="iot-plc-quality-bar__track"
        role="img"
        aria-label="Sinyal kalite dağılımı yığılmış çubuk grafiği"
      >
        {barSegments.map((segment) => (
          <span
            key={segment.id}
            className="iot-plc-quality-bar__segment"
            style={{ width: `${segment.widthPercent}%`, background: segment.color }}
            data-testid="iot-plc-quality-bar-segment"
          />
        ))}
      </div>
      <ul className="iot-plc-quality-bar__legend">
        {slices.map((slice) => (
          <li key={slice.key} className="iot-plc-quality-bar__row">
            <span className="iot-plc-quality-bar__row-label">
              <span
                className="iot-plc-quality-bar__swatch"
                style={{ background: slice.color }}
                aria-hidden="true"
              />
              {slice.label}
            </span>
            <span className="iot-plc-quality-bar__row-value" data-testid={`iot-plc-quality-row-${slice.key}`}>
              {formatIotPlcQualitySliceLine(
                slice.count,
                slice.ratio,
                formatIotPlcCount,
                formatIotPlcRatioPercent,
              )}
            </span>
          </li>
        ))}
      </ul>
      <p className="iot-plc-panel__meta" data-testid="iot-plc-quality-reconciliation">
        Payda: {formatIotPlcCount(denominator)} gözlem
      </p>
    </div>
  );
}

function buildHeroDistributionFromDetail(detail: IotPlcAssessmentDetail) {
  const denominator = detail.actualObservationCount ?? 0;
  if (denominator <= 0) {
    return { slices: [], denominator: null as number | null };
  }

  const slices = reconcileIotPlcQualityDistribution(
    denominator,
    [
      { key: "good", label: getIotPlcQualityStateLabel("good"), rawRatio: detail.goodRatio, color: QUALITY_SLICE_COLORS.good },
      { key: "uncertain", label: getIotPlcQualityStateLabel("uncertain"), rawRatio: detail.uncertainRatio, color: QUALITY_SLICE_COLORS.uncertain },
      { key: "bad", label: getIotPlcQualityStateLabel("bad"), rawRatio: detail.badRatio, color: QUALITY_SLICE_COLORS.bad },
      { key: "stale", label: getIotPlcQualityStateLabel("stale"), rawRatio: detail.staleRatio, color: QUALITY_SLICE_COLORS.stale },
      {
        key: "source-conflict",
        label: getIotPlcQualityStateLabel("source-conflict"),
        rawRatio: detail.sourceConflictRatio,
        color: QUALITY_SLICE_COLORS["source-conflict"],
      },
    ],
  );

  return { slices, denominator };
}

function QualityProfileIntelligenceSurface({
  profile,
  assessment,
  detail,
  isLoading,
}: {
  profile: IotPlcQualityProfileSummary | null;
  assessment: IotPlcAssessmentSummary | null;
  detail: IotPlcAssessmentDetail | undefined;
  isLoading: boolean;
}) {
  if (!profile) {
    return (
      <p className="iot-plc-empty" data-testid="iot-plc-quality-hero-empty">
        Kalite profili bulunmuyor.
      </p>
    );
  }

  const distribution = detail ? buildHeroDistributionFromDetail(detail) : { slices: [], denominator: null };
  const barSegments = buildQualityBarSegments(distribution.slices, distribution.denominator);
  const leadingDriver = detail?.drivers.find((driver) => driver.isLeadingDriver) ?? detail?.drivers[0] ?? null;

  return (
    <div className="iot-plc-quality-hero__body" data-testid="iot-plc-quality-hero-intelligence">
      <div className="iot-plc-quality-hero__header">
        <div className="iot-plc-quality-hero__intro">
          <h2 className="iot-plc-quality-hero__title">{profile.displayName}</h2>
          <p className="iot-plc-quality-hero__meta">
            {profile.profileCode} · {profile.statusLabel}
          </p>
        </div>
        {assessment ? (
          <span className={outcomeBadgeClass(assessment.outcome)}>
            {assessment.outcomeLabel}
          </span>
        ) : (
          <span className="iot-plc-badge iot-plc-badge--unknown">Değerlendirme yok</span>
        )}
      </div>

      {isLoading ? (
        <div className="iot-plc-quality-hero__loading" aria-busy="true">
          <LoaderCircle aria-hidden="true" className="animate-spin" size={16} />
          <span>Profil analizi yükleniyor…</span>
        </div>
      ) : !assessment || !detail || !distribution.denominator ? (
        <p className="iot-plc-empty" data-testid="iot-plc-quality-hero-unavailable">
          Bu profil için uyumlu sinyal kalitesi değerlendirmesi veya gözlem paydası bulunamadı.
        </p>
      ) : (
        <>
          <div className="iot-plc-quality-hero__facts">
            <div>
              <span className="iot-plc-quality-hero__fact-label">Kanıt dönemi</span>
              <strong>{assessment.evaluationPeriodLabel}</strong>
            </div>
            <div>
              <span className="iot-plc-quality-hero__fact-label">Gözlem paydası</span>
              <strong data-testid="iot-plc-quality-hero-denominator">
                {formatIotPlcCount(distribution.denominator)}
              </strong>
            </div>
            <div>
              <span className="iot-plc-quality-hero__fact-label">Öncü sürücü</span>
              <strong>{leadingDriver?.reasonLabel ?? assessment.leadingDriverLabel ?? "—"}</strong>
            </div>
            <div>
              <span className="iot-plc-quality-hero__fact-label">Kanıt / kaynak</span>
              <strong>{detail.provenance || assessment.evidenceLabel}</strong>
            </div>
          </div>

          <div className="iot-plc-quality-hero__analytics">
            <div className="iot-plc-quality-hero__thresholds" aria-label="Profil eşik karşılaştırması">
              <p className="iot-plc-quality-hero__thresholds-title">Profil eşikleri</p>
              <ul className="iot-plc-quality-hero__threshold-list">
                <li>
                  İyi min: {formatIotPlcRatioPercent(profile.minimumGoodQualityRatio)}
                  {detail.goodRatio !== null ? (
                    <span className="iot-plc-quality-hero__threshold-marker">
                      · Gerçek: {formatIotPlcRatioPercent(detail.goodRatio)}
                    </span>
                  ) : null}
                </li>
                <li>
                  Kötü max: {formatIotPlcRatioPercent(profile.maximumBadQualityRatio)}
                  {detail.badRatio !== null ? (
                    <span className="iot-plc-quality-hero__threshold-marker">
                      · Gerçek: {formatIotPlcRatioPercent(detail.badRatio)}
                    </span>
                  ) : null}
                </li>
                <li>Bayat eşiği: {formatIotPlcSeconds(profile.staleThresholdSeconds)}</li>
              </ul>
            </div>

            <div
              className="iot-plc-quality-bar iot-plc-quality-bar--hero"
              data-testid="iot-plc-quality-hero-distribution"
            >
              <div
                className="iot-plc-quality-bar__track"
                role="img"
                aria-label="Seçili profil kalite dağılımı yığılmış çubuk grafiği"
              >
                {barSegments.map((segment) => (
                  <span
                    key={segment.id}
                    className="iot-plc-quality-bar__segment"
                    style={{ width: `${segment.widthPercent}%`, background: segment.color }}
                    data-testid="iot-plc-quality-hero-bar-segment"
                  />
                ))}
              </div>
              <ul className="iot-plc-quality-bar__legend">
                {distribution.slices.map((slice) => (
                  <li key={slice.key} className="iot-plc-quality-bar__row">
                    <span className="iot-plc-quality-bar__row-label">
                      <span
                        className="iot-plc-quality-bar__swatch"
                        style={{ background: slice.color }}
                        aria-hidden="true"
                      />
                      {slice.label}
                    </span>
                    <span
                      className="iot-plc-quality-bar__row-value"
                      data-testid={`iot-plc-quality-hero-row-${slice.key}`}
                    >
                      {formatIotPlcQualitySliceLine(
                        slice.count,
                        slice.ratio,
                        formatIotPlcCount,
                        formatIotPlcRatioPercent,
                      )}
                    </span>
                  </li>
                ))}
              </ul>
              <p className="iot-plc-panel__meta" data-testid="iot-plc-quality-hero-reconciliation">
                Payda: {formatIotPlcCount(distribution.denominator)} gözlem
              </p>
            </div>
          </div>
        </>
      )}
    </div>
  );
}

function WorkspaceSkeleton() {
  return (
    <div className="iot-plc-skeleton-grid iot-plc-skeleton-grid--kpi" aria-busy="true" aria-label="Yükleniyor">
      {Array.from({ length: 6 }, (_, index) => (
        <div key={index} className="iot-plc-skeleton iot-plc-skeleton--kpi" />
      ))}
    </div>
  );
}

export function IotPlcWorkspaceView({
  workspace: initialWorkspace,
  canManage: initialCanManage,
}: IotPlcWorkspaceViewProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const [workspace, setWorkspace] = useState(initialWorkspace);
  const [canManage, setCanManage] = useState(initialCanManage);
  const [error, setError] = useState<string | null>(null);
  const [isPending, startTransition] = useTransition();
  const [isWorkspaceRefreshing, setIsWorkspaceRefreshing] = useState(false);

  const activeTab = parseTabId(searchParams.get("tab"));

  const [endpointSearch, setEndpointSearch] = useState("");
  const [selectedEndpointRef, setSelectedEndpointRef] = useState("");
  const [endpointDetail, setEndpointDetail] = useState<IotPlcEndpointDetail | undefined>();

  const [signalSearch, setSignalSearch] = useState("");
  const [selectedSignalRef, setSelectedSignalRef] = useState("");
  const [signalDetail, setSignalDetail] = useState<IotPlcSignalDetail | undefined>();

  const [assessmentOutcomeFilter, setAssessmentOutcomeFilter] = useState("");
  const [assessmentEvidenceFilter, setAssessmentEvidenceFilter] = useState("");
  const [assessmentDriverFilter, setAssessmentDriverFilter] = useState("");
  const [selectedAssessmentKey, setSelectedAssessmentKey] = useState("");
  const [assessmentDetail, setAssessmentDetail] = useState<IotPlcAssessmentDetail | undefined>();

  const [evaluateEndpointRef, setEvaluateEndpointRef] = useState("");
  const [evaluateSignalRef, setEvaluateSignalRef] = useState("");
  const [evaluateProfileRef, setEvaluateProfileRef] = useState("QP-IOT-STANDARD");
  const resolvedEvaluateEndpointRef = evaluateEndpointRef || workspace.endpoints[0]?.reference || "";
  const resolvedEvaluateSignalRef = evaluateSignalRef || workspace.signals[0]?.reference || "";
  const resolvedEvaluateProfileRef = evaluateProfileRef
    || workspace.profiles[0]?.profileCode
    || "QP-IOT-STANDARD";
  const [selectedQualityProfileCode, setSelectedQualityProfileCode] = useState(resolvedEvaluateProfileRef);
  const [qualityHeroDetail, setQualityHeroDetail] = useState<IotPlcAssessmentDetail | undefined>();
  const [actionMessage, setActionMessage] = useState<string | null>(null);

  const [endpointForm, setEndpointForm] = useState<EndpointFormState | null>(null);
  const [signalForm, setSignalForm] = useState<SignalFormState | null>(null);
  const [profileForm, setProfileForm] = useState<ProfileFormState | null>(null);
  const [dialogError, setDialogError] = useState<string | null>(null);

  const tabListRef = useRef<HTMLDivElement | null>(null);
  const endpointRowRefs = useRef<Map<string, HTMLTableRowElement>>(new Map());
  const signalRowRefs = useRef<Map<string, HTMLTableRowElement>>(new Map());
  const assessmentRowRefs = useRef<Map<string, HTMLTableRowElement>>(new Map());

  const { meta } = workspace;

  const setActiveTab = useCallback(
    (tabId: IotPlcTabId) => {
      const params = new URLSearchParams(searchParams.toString());
      params.set("tab", tabId);
      const query = params.toString();
      router.replace(query ? `${pathname}?${query}` : pathname, { scroll: false });
    },
    [pathname, router, searchParams],
  );

  const driverFilterOptions = useMemo(() => {
    const unique = new Map<string, string>();
    for (const assessment of [...workspace.connectivityAssessments, ...workspace.signalAssessments]) {
      if (assessment.leadingDriverCode) {
        unique.set(assessment.leadingDriverCode, assessment.leadingDriverLabel);
      }
    }
    return [
      { value: "", label: "Tüm sürücüler" },
      ...[...unique.entries()]
        .sort((a, b) => a[1].localeCompare(b[1], "tr"))
        .map(([value, label]) => ({ value, label })),
    ];
  }, [workspace.connectivityAssessments, workspace.signalAssessments]);

  const selectedQualityProfile = useMemo(
    () =>
      workspace.profiles.find((profile) => profile.profileCode === selectedQualityProfileCode)
      ?? workspace.profiles[0]
      ?? null,
    [selectedQualityProfileCode, workspace.profiles],
  );

  const qualityHeroAssessment = useMemo(() => {
    const profileCode = selectedQualityProfile?.profileCode ?? selectedQualityProfileCode;
    return workspace.signalAssessments
      .filter((assessment) => assessment.profileCode === profileCode)
      .sort(
        (left, right) =>
          new Date(right.evaluatedAtUtc).getTime() - new Date(left.evaluatedAtUtc).getTime(),
      )[0] ?? null;
  }, [selectedQualityProfile, selectedQualityProfileCode, workspace.signalAssessments]);

  function selectQualityProfile(profileCode: string) {
    setSelectedQualityProfileCode(profileCode);
    setEvaluateProfileRef(profileCode);
  }

  useEffect(() => {
    if (activeTab !== "quality" || !qualityHeroAssessment) {
      return;
    }

    if (qualityHeroDetail?.summary.id === qualityHeroAssessment.id) {
      return;
    }

    let cancelled = false;

    void loadIotPlcAssessmentDetailAction(
      qualityHeroAssessment.id,
      qualityHeroAssessment.assessmentKind,
    ).then((result) => {
      if (cancelled) return;
      if (result.status === "success") {
        setQualityHeroDetail(result.detail);
      }
    });

    return () => {
      cancelled = true;
    };
  }, [activeTab, qualityHeroAssessment, qualityHeroDetail?.summary.id]);

  const filteredEndpoints = useMemo(() => {
    const query = endpointSearch.trim().toLowerCase();
    return workspace.endpoints.filter((endpoint) => {
      if (!query) return true;
      return (
        endpoint.displayName.toLowerCase().includes(query)
        || endpoint.endpointCode.toLowerCase().includes(query)
      );
    });
  }, [endpointSearch, workspace.endpoints]);

  const filteredSignals = useMemo(() => {
    const query = signalSearch.trim().toLowerCase();
    return workspace.signals.filter((signal) => {
      if (!query) return true;
      return (
        signal.displayName.toLowerCase().includes(query)
        || signal.signalCode.toLowerCase().includes(query)
        || signal.endpointLabel.toLowerCase().includes(query)
      );
    });
  }, [signalSearch, workspace.signals]);

  const filterAssessment = useCallback(
    (assessment: IotPlcAssessmentSummary) => {
      const matchesOutcome =
        !assessmentOutcomeFilter || assessment.outcome === assessmentOutcomeFilter;
      const matchesEvidence =
        !assessmentEvidenceFilter
        || assessment.evidenceLabel === getIotPlcEvidenceLabel(assessmentEvidenceFilter);
      const matchesDriver =
        !assessmentDriverFilter || assessment.leadingDriverCode === assessmentDriverFilter;
      return matchesOutcome && matchesEvidence && matchesDriver;
    },
    [assessmentDriverFilter, assessmentEvidenceFilter, assessmentOutcomeFilter],
  );

  const filteredConnectivityAssessments = useMemo(
    () => workspace.connectivityAssessments.filter(filterAssessment),
    [filterAssessment, workspace.connectivityAssessments],
  );

  const filteredSignalAssessments = useMemo(
    () => workspace.signalAssessments.filter(filterAssessment),
    [filterAssessment, workspace.signalAssessments],
  );

  const filteredAssessmentCount =
    filteredConnectivityAssessments.length + filteredSignalAssessments.length;

  const selectedEndpoint = useMemo(
    () =>
      workspace.endpoints.find((endpoint) => endpoint.reference === selectedEndpointRef)
      ?? filteredEndpoints.find((endpoint) => endpoint.reference === selectedEndpointRef),
    [filteredEndpoints, selectedEndpointRef, workspace.endpoints],
  );

  const selectedSignal = useMemo(
    () =>
      workspace.signals.find((signal) => signal.reference === selectedSignalRef)
      ?? filteredSignals.find((signal) => signal.reference === selectedSignalRef),
    [filteredSignals, selectedSignalRef, workspace.signals],
  );

  const selectedAssessment = useMemo(() => {
    const all = [...workspace.connectivityAssessments, ...workspace.signalAssessments];
    return all.find((item) => assessmentKey(item) === selectedAssessmentKey);
  }, [selectedAssessmentKey, workspace.connectivityAssessments, workspace.signalAssessments]);

  const qualityHeroDetailForView = useMemo(() => {
    if (!qualityHeroAssessment || !qualityHeroDetail) return undefined;
    return qualityHeroDetail.summary.id === qualityHeroAssessment.id ? qualityHeroDetail : undefined;
  }, [qualityHeroAssessment, qualityHeroDetail]);

  const isQualityHeroLoading =
    activeTab === "quality" && !!qualityHeroAssessment && !qualityHeroDetailForView;

  const handleRefresh = useCallback(() => {
    startTransition(async () => {
      setIsWorkspaceRefreshing(true);
      setError(null);
      const result = await refreshIotPlcWorkspaceAction();
      setIsWorkspaceRefreshing(false);
      if (result.status === "failure") {
        setError(result.message);
        return;
      }
      setWorkspace(result.workspace);
      setCanManage(result.canManage);
      setEndpointDetail(undefined);
      setSignalDetail(undefined);
      setAssessmentDetail(undefined);
      setQualityHeroDetail(undefined);
      setSelectedEndpointRef("");
      setSelectedSignalRef("");
      setSelectedAssessmentKey("");
    });
  }, []);

  const handleTabKeyDown = useCallback(
    (event: ReactKeyboardEvent<HTMLButtonElement>, index: number) => {
      if (event.key !== "ArrowRight" && event.key !== "ArrowLeft") return;
      event.preventDefault();
      const delta = event.key === "ArrowRight" ? 1 : -1;
      const nextIndex = (index + delta + IOT_PLC_TAB_OPTIONS.length) % IOT_PLC_TAB_OPTIONS.length;
      const buttons = tabListRef.current?.querySelectorAll<HTMLButtonElement>('[role="tab"]');
      buttons?.[nextIndex]?.focus();
      setActiveTab(IOT_PLC_TAB_OPTIONS[nextIndex].id);
    },
    [setActiveTab],
  );

  function selectEndpoint(endpoint: IotPlcEndpointSummary) {
    setSelectedEndpointRef(endpoint.reference);
    setEndpointDetail(undefined);
    setError(null);
    startTransition(async () => {
      const result = await loadIotPlcEndpointDetailAction(endpoint.reference);
      if (result.status === "success") {
        setEndpointDetail(result.detail);
      } else {
        setError(result.message);
      }
    });
  }

  function closeEndpointDetail() {
    const restore = selectedEndpointRef;
    setSelectedEndpointRef("");
    setEndpointDetail(undefined);
    requestAnimationFrame(() => {
      endpointRowRefs.current.get(restore)?.focus({ preventScroll: true });
    });
  }

  function selectSignal(signal: IotPlcSignalSummary) {
    setSelectedSignalRef(signal.reference);
    setSignalDetail(undefined);
    setError(null);
    startTransition(async () => {
      const result = await loadIotPlcSignalDetailAction(signal.reference);
      if (result.status === "success") {
        setSignalDetail(result.detail);
      } else {
        setError(result.message);
      }
    });
  }

  function closeSignalDetail() {
    const restore = selectedSignalRef;
    setSelectedSignalRef("");
    setSignalDetail(undefined);
    requestAnimationFrame(() => {
      signalRowRefs.current.get(restore)?.focus({ preventScroll: true });
    });
  }

  function selectAssessment(assessment: IotPlcAssessmentSummary) {
    setSelectedAssessmentKey(assessmentKey(assessment));
    setAssessmentDetail(undefined);
    setError(null);
    startTransition(async () => {
      const result = await loadIotPlcAssessmentDetailAction(
        assessment.id,
        assessment.assessmentKind,
      );
      if (result.status === "success") {
        setAssessmentDetail(result.detail);
      } else {
        setError(result.message);
      }
    });
  }

  function closeAssessmentDetail() {
    const restore = selectedAssessmentKey;
    setSelectedAssessmentKey("");
    setAssessmentDetail(undefined);
    requestAnimationFrame(() => {
      assessmentRowRefs.current.get(restore)?.focus({ preventScroll: true });
    });
  }

  function openCreateEndpoint() {
    setDialogError(null);
    setEndpointForm({
      mode: "create",
      reference: null,
      endpointCode: "",
      displayName: "",
      protocolType: "manual-development-simulator",
      connectionDirection: "read-only-pull",
      endpointStatus: "draft",
      configurationProvenance: meta.provenanceDisclaimer || "ui-create",
      connectionProfileJson: "{}",
      expectedVersion: 1,
    });
  }

  function openEditEndpoint(endpoint: IotPlcEndpointSummary) {
    setDialogError(null);
    setEndpointForm({
      mode: "edit",
      reference: endpoint.reference,
      endpointCode: endpoint.endpointCode,
      displayName: endpoint.displayName,
      protocolType: endpoint.protocolType,
      connectionDirection: endpoint.connectionDirection,
      endpointStatus: endpoint.endpointStatus,
      configurationProvenance: endpoint.configurationProvenance,
      connectionProfileJson: endpointDetail?.connectionProfileJson ?? "{}",
      expectedVersion: endpoint.version,
    });
  }

  function submitEndpointForm(event: FormEvent) {
    event.preventDefault();
    if (!endpointForm) return;

    startTransition(async () => {
      setDialogError(null);
      const payload = {
        endpointCode: endpointForm.endpointCode.trim(),
        displayName: endpointForm.displayName.trim(),
        protocolType: endpointForm.protocolType,
        connectionDirection: endpointForm.connectionDirection,
        endpointStatus: endpointForm.endpointStatus,
        configurationProvenance: endpointForm.configurationProvenance.trim(),
        connectionProfileJson: endpointForm.connectionProfileJson.trim() || "{}",
      };

      const result =
        endpointForm.mode === "create"
          ? await createIotPlcEndpointAction(payload)
          : await updateIotPlcEndpointAction(endpointForm.reference!, {
              ...payload,
              expectedVersion: endpointForm.expectedVersion,
            });

      if (result.status === "failure") {
        setDialogError(result.message);
        return;
      }

      setEndpointForm(null);
      setActionMessage("Bağlantı kaydı güncellendi.");
      handleRefresh();
    });
  }

  function openCreateSignal() {
    setDialogError(null);
    setSignalForm({
      mode: "create",
      reference: null,
      endpointReference: workspace.endpoints[0]?.reference ?? "",
      signalCode: "",
      displayName: "",
      sourceAddress: "",
      dataType: "decimal",
      engineeringUnit: "",
      semanticType: "measurement",
      mappedAssetType: "unmapped",
      mappedCanonicalAssetId: "",
      isEnabled: true,
      samplingExpectation: "",
      sourceProvenance: meta.provenanceDisclaimer || "ui-create",
      expectedVersion: 1,
    });
  }

  function openEditSignal(signal: IotPlcSignalSummary) {
    setDialogError(null);
    setSignalForm({
      mode: "edit",
      reference: signal.reference,
      endpointReference: signal.endpointCode,
      signalCode: signal.signalCode,
      displayName: signal.displayName,
      sourceAddress: signal.sourceAddress,
      dataType: signal.dataType,
      engineeringUnit: signal.engineeringUnit ?? "",
      semanticType: signal.semanticType,
      mappedAssetType: signal.mappedAssetLabel === "Eşleşmemiş" ? "unmapped" : "machine",
      mappedCanonicalAssetId: "",
      isEnabled: signal.isEnabled,
      samplingExpectation: signal.samplingExpectation ?? "",
      sourceProvenance: signal.sourceProvenance,
      expectedVersion: signal.version,
    });
  }

  function submitSignalForm(event: FormEvent) {
    event.preventDefault();
    if (!signalForm) return;

    startTransition(async () => {
      setDialogError(null);
      const mappedCanonicalAssetId =
        signalForm.mappedAssetType === "unmapped" || !signalForm.mappedCanonicalAssetId.trim()
          ? null
          : signalForm.mappedCanonicalAssetId.trim();

      const basePayload = {
        displayName: signalForm.displayName.trim(),
        sourceAddress: signalForm.sourceAddress.trim(),
        dataType: signalForm.dataType,
        engineeringUnit: signalForm.engineeringUnit.trim() || null,
        semanticType: signalForm.semanticType,
        mappedAssetType: signalForm.mappedAssetType,
        mappedCanonicalAssetId,
        isEnabled: signalForm.isEnabled,
        samplingExpectation: signalForm.samplingExpectation.trim() || null,
        sourceProvenance: signalForm.sourceProvenance.trim(),
      };

      const result =
        signalForm.mode === "create"
          ? await createIotPlcSignalAction({
              endpointReference: signalForm.endpointReference,
              signalCode: signalForm.signalCode.trim(),
              ...basePayload,
            })
          : await updateIotPlcSignalAction(signalForm.reference!, {
              ...basePayload,
              expectedVersion: signalForm.expectedVersion,
            });

      if (result.status === "failure") {
        setDialogError(result.message);
        return;
      }

      setSignalForm(null);
      setActionMessage("Sinyal kaydı güncellendi.");
      handleRefresh();
    });
  }

  function openEditProfile(profile: IotPlcQualityProfileSummary) {
    setDialogError(null);
    setProfileForm({
      profile,
      displayName: profile.displayName,
      expectedObservationIntervalSeconds: String(
        meta.standardProfileFreshnessSeconds ?? 60,
      ),
      freshnessToleranceSeconds: String(profile.freshnessToleranceSeconds),
      staleThresholdSeconds: String(profile.staleThresholdSeconds),
      maximumClockSkewToleranceSeconds: "30",
      minimumExpectedObservationCount: "1",
      minimumGoodQualityRatio: String(profile.minimumGoodQualityRatio),
      maximumBadQualityRatio: String(profile.maximumBadQualityRatio),
      maximumUncertainQualityRatio: String(profile.maximumUncertainQualityRatio),
      maximumSourceConflictRatio: String(profile.maximumSourceConflictRatio),
      sequenceGapEvaluationEnabled: profile.sequenceGapEvaluationEnabled,
      status: profile.status,
      sourceProvenance: profile.sourceProvenance,
    });
  }

  function submitProfileForm(event: FormEvent) {
    event.preventDefault();
    if (!profileForm) return;

    startTransition(async () => {
      setDialogError(null);
      const result = await updateIotPlcProfileAction(profileForm.profile.reference, {
        displayName: profileForm.displayName.trim(),
        expectedObservationIntervalSeconds: Number(profileForm.expectedObservationIntervalSeconds),
        freshnessToleranceSeconds: Number(profileForm.freshnessToleranceSeconds),
        staleThresholdSeconds: Number(profileForm.staleThresholdSeconds),
        maximumClockSkewToleranceSeconds: Number(profileForm.maximumClockSkewToleranceSeconds),
        minimumExpectedObservationCount: Number(profileForm.minimumExpectedObservationCount),
        minimumGoodQualityRatio: Number(profileForm.minimumGoodQualityRatio),
        maximumBadQualityRatio: Number(profileForm.maximumBadQualityRatio),
        maximumUncertainQualityRatio: Number(profileForm.maximumUncertainQualityRatio),
        maximumSourceConflictRatio: Number(profileForm.maximumSourceConflictRatio),
        sequenceGapEvaluationEnabled: profileForm.sequenceGapEvaluationEnabled,
        status: profileForm.status,
        sourceProvenance: profileForm.sourceProvenance.trim(),
        expectedVersion: profileForm.profile.version,
      });

      if (result.status === "failure") {
        setDialogError(result.message);
        return;
      }

      setProfileForm(null);
      setActionMessage("Kalite profili güncellendi.");
      handleRefresh();
    });
  }

  function runConnectivityEvaluation() {
    if (!resolvedEvaluateEndpointRef) return;
    const window = defaultEvaluationWindow();
    startTransition(async () => {
      setActionMessage(null);
      setError(null);
      const result = await evaluateIotPlcConnectivityAction({
        endpointReference: resolvedEvaluateEndpointRef,
        evaluationWindowStartUtc: window.evaluationWindowStartUtc,
        evaluationWindowEndUtc: window.evaluationWindowEndUtc,
        evaluationTimeUtc: window.evaluationTimeUtc,
        profileReference: resolvedEvaluateProfileRef,
        evidenceMode: meta.hasDevelopmentPilotData ? "simulated" : "measured",
        sourceProvenance: meta.provenanceDisclaimer || "ui-evaluation",
        idempotencyKey: `ui-connectivity-${Date.now()}`,
      });
      if (result.status === "failure") {
        setError(result.message);
        return;
      }
      setActionMessage("Bağlantı değerlendirmesi tamamlandı.");
      handleRefresh();
    });
  }

  function runSignalQualityEvaluation() {
    if (!resolvedEvaluateSignalRef) return;
    const window = defaultEvaluationWindow();
    startTransition(async () => {
      setActionMessage(null);
      setError(null);
      const result = await evaluateIotPlcSignalQualityAction({
        signalReference: resolvedEvaluateSignalRef,
        evaluationWindowStartUtc: window.evaluationWindowStartUtc,
        evaluationWindowEndUtc: window.evaluationWindowEndUtc,
        evaluationTimeUtc: window.evaluationTimeUtc,
        profileReference: resolvedEvaluateProfileRef,
        evidenceMode: meta.hasDevelopmentPilotData ? "simulated" : "measured",
        sourceProvenance: meta.provenanceDisclaimer || "ui-evaluation",
        idempotencyKey: `ui-signal-quality-${Date.now()}`,
      });
      if (result.status === "failure") {
        setError(result.message);
        return;
      }
      setActionMessage("Sinyal kalitesi değerlendirmesi tamamlandı.");
      handleRefresh();
    });
  }

  function renderAssessmentTable(
    assessments: readonly IotPlcAssessmentSummary[],
    emptyLabel: string,
  ) {
    if (assessments.length === 0) {
      return <p className="iot-plc-empty">{emptyLabel}</p>;
    }

    return (
      <table className="iot-plc-registry-table">
        <thead>
          <tr>
            <th>Değerlendirme</th>
            <th>Dönem</th>
            <th>Sonuç</th>
            <th>Sürücü</th>
            <th>Kanıt</th>
          </tr>
        </thead>
        <tbody>
          {assessments.map((assessment) => {
            const key = assessmentKey(assessment);
            return (
              <tr
                key={key}
                ref={(node) => {
                  if (node) assessmentRowRefs.current.set(key, node);
                  else assessmentRowRefs.current.delete(key);
                }}
                tabIndex={0}
                className={key === selectedAssessmentKey ? "is-selected" : ""}
                onClick={() => selectAssessment(assessment)}
                onKeyDown={(event) => {
                  if (event.key === "Enter" || event.key === " ") {
                    event.preventDefault();
                    selectAssessment(assessment);
                  }
                }}
              >
                <td>
                  {getIotPlcAssessmentTitle(
                    assessment.assessmentKind,
                    assessment.targetLabel,
                    assessment.outcome,
                  )}
                </td>
                <td>{assessment.evaluationPeriodLabel}</td>
                <td>
                  <span className={outcomeBadgeClass(assessment.outcome)}>
                    {assessment.outcomeLabel}
                  </span>
                </td>
                <td>{assessment.leadingDriverLabel}</td>
                <td>{assessment.evidenceLabel}</td>
              </tr>
            );
          })}
        </tbody>
      </table>
    );
  }

  return (
    <main className="iot-plc-page" data-testid="iot-plc-workspace">
      <section className="iot-plc-executive" aria-label="IoT ve PLC komuta yüzeyi">
        <div className="iot-plc-executive__top">
          <div className="iot-plc-executive__intro">
            <p className="iot-plc-executive__eyebrow">ENDÜSTRİYEL BAĞLANTI</p>
            <h1>IoT & PLC</h1>
            <p className="iot-plc-executive__subtitle">
              Endüstriyel bağlantıları, sinyal akışını ve veri kalitesi kanıtlarını tek çalışma
              alanında izleyin.
            </p>
            <ul className="iot-plc-executive__meta" aria-label="Çalışma alanı meta verisi">
              <li>{meta.facilityLabel}</li>
              <li>Yenileme: {meta.refreshedLabel}</li>
              <li>Kanıt türü: {meta.hasDevelopmentPilotData ? "Simülasyon" : "Ölçülmüş"}</li>
            </ul>
          </div>
          <div className="iot-plc-executive__actions">
            <button
              type="button"
              className="iot-plc-button"
              disabled={isPending}
              onClick={handleRefresh}
              aria-label={isPending ? "Çalışma alanı yenileniyor" : "Çalışma alanını yenile"}
            >
              {isPending && isWorkspaceRefreshing ? (
                <LoaderCircle aria-hidden="true" className="animate-spin" size={14} />
              ) : (
                <RefreshCw aria-hidden="true" size={14} />
              )}
              Yenile
            </button>
          </div>
        </div>

        {meta.hasDevelopmentPilotData ? (
          <p
            className="iot-plc-provenance-strip"
            role="status"
            data-testid="iot-plc-provenance-strip"
          >
            <span className="iot-plc-provenance-strip__badge">Simülasyon</span>
            <span>{IOT_PLC_DEVELOPMENT_BANNER}</span>
          </p>
        ) : null}
      </section>

      {workspace.partialErrors.length > 0 ? (
        <div className="iot-plc-partial-error" role="status">
          <strong>Kısmi yükleme uyarısı:</strong>
          <ul style={{ margin: "0.35rem 0 0", paddingLeft: "1.1rem" }}>
            {workspace.partialErrors.map((item) => (
              <li key={item}>{item}</li>
            ))}
          </ul>
        </div>
      ) : null}

      {error ? (
        <div className="iot-plc-error-banner" role="alert">{error}</div>
      ) : null}

      {actionMessage ? (
        <div className="iot-plc-readonly-note" role="status">{actionMessage}</div>
      ) : null}

      {!canManage ? (
        <div className="iot-plc-readonly-note" role="status" data-testid="iot-plc-readonly-banner">
          Salt okunur erişim: kayıt oluşturma, güncelleme ve değerlendirme işlemleri devre dışıdır.
        </div>
      ) : null}

      {isWorkspaceRefreshing ? (
        <WorkspaceSkeleton />
      ) : (
        <div className="iot-plc-kpi-grid" data-testid="iot-plc-kpi-grid" aria-label="IoT göstergeleri">
          {workspace.kpis.map((kpi) => {
            const Icon = KPI_ICONS[kpi.id] ?? Cable;
            return (
              <article
                key={kpi.id}
                className={`iot-plc-kpi-card ${resolveKpiToneClass(kpi.tone)}`}
                data-testid="iot-plc-kpi-card"
                data-kpi-id={kpi.id}
                aria-label={`${kpi.label}: ${kpi.value}`}
              >
                <div className="iot-plc-kpi-card__top">
                  <span className="iot-plc-kpi-card__icon" aria-hidden="true">
                    <Icon size={16} strokeWidth={2} />
                  </span>
                  {kpi.badgeLabel ? (
                    <span className="iot-plc-kpi-card__tone">{kpi.badgeLabel}</span>
                  ) : null}
                </div>
                <div className="iot-plc-kpi-card__body">
                  <p className="iot-plc-kpi-card__label">{kpi.label}</p>
                  <div className="iot-plc-kpi-card__value-row">
                    <p className="iot-plc-kpi-card__value">{kpi.value}</p>
                  </div>
                  <p className="iot-plc-kpi-card__meta">{kpi.comparisonLabel}</p>
                </div>
              </article>
            );
          })}
        </div>
      )}

      <div
        ref={tabListRef}
        className="iot-plc-tab-rail"
        data-testid="iot-plc-tab-rail"
        role="tablist"
        aria-label="IoT çalışma alanı sekmeleri"
      >
        {IOT_PLC_TAB_OPTIONS.map((tab, index) => (
          <button
            key={tab.id}
            type="button"
            role="tab"
            id={`iot-plc-tab-${tab.id}`}
            aria-selected={activeTab === tab.id}
            aria-controls={`iot-plc-tab-panel-${tab.id}`}
            className="iot-plc-tab"
            onClick={() => setActiveTab(tab.id)}
            onKeyDown={(event) => handleTabKeyDown(event, index)}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {activeTab === "overview" ? (
        <div
          id="iot-plc-tab-panel-overview"
          role="tabpanel"
          aria-labelledby="iot-plc-tab-overview"
          className="iot-plc-panel-grid iot-plc-panel-grid--overview"
          data-testid="iot-plc-overview-grid"
        >
          <section className="iot-plc-panel iot-plc-panel--analytics" aria-label="Bağlantı durumu">
            <div className="iot-plc-panel__header">
              <h2 className="iot-plc-panel__title">Bağlantı durumu</h2>
              <p className="iot-plc-panel__scope" data-testid="iot-plc-current-evidence-scope">
                {IOT_PLC_CURRENT_EVIDENCE_SCOPE}
              </p>
            </div>
            <p className="iot-plc-panel__note" data-testid="iot-plc-current-status-note">
              {IOT_PLC_CURRENT_STATUS_NOTE}
            </p>
            {workspace.healthTotal === 0 ? (
              <p className="iot-plc-empty">Kayıtlı bağlantı bulunmuyor.</p>
            ) : (
              <IotPlcHealthDonut segments={workspace.healthComposition} total={workspace.healthTotal} />
            )}
          </section>

          <section className="iot-plc-panel iot-plc-panel--analytics" aria-label="Sinyal kalitesi">
            <div className="iot-plc-panel__header">
              <h2 className="iot-plc-panel__title">Sinyal kalitesi</h2>
              {workspace.qualityAssessmentLabel ? (
                <p className="iot-plc-panel__scope">{workspace.qualityAssessmentLabel}</p>
              ) : null}
            </div>
            <IotPlcQualityDistribution
              slices={workspace.qualityDistribution}
              denominator={workspace.qualityDenominator}
              assessmentLabel={null}
              contractViolationCount={workspace.qualityContractViolationCount}
            />
          </section>

          <section className="iot-plc-panel iot-plc-panel--findings" aria-label="Bağlantı ve veri kalitesi özeti">
            <h2 className="iot-plc-panel__title">Bağlantı ve veri kalitesi özeti</h2>
            {workspace.findings.length === 0 ? (
              <p className="iot-plc-empty">Öncelikli bulgu bulunmuyor.</p>
            ) : (
              <ul className="iot-plc-findings-list">
                {workspace.findings.map((finding) => (
                  <li
                    key={finding.id}
                    className={`iot-plc-finding-card iot-plc-finding-card--${finding.severity}`}
                    data-testid="iot-plc-finding-card"
                  >
                    <div className="iot-plc-finding-card__header">
                      <span className={`iot-plc-finding-card__badge ${findingSeverityClass(finding.severity)}`}>
                        {finding.outcomeLabel ?? finding.title}
                      </span>
                      {finding.evidenceLabel ? (
                        <span className="iot-plc-finding-card__evidence">{finding.evidenceLabel}</span>
                      ) : null}
                    </div>
                    <h3 className="iot-plc-finding-card__title">{finding.title}</h3>
                    {finding.periodLabel ? (
                      <p className="iot-plc-finding-card__scope">{finding.periodLabel}</p>
                    ) : null}
                    {finding.leadingDriverLabel ? (
                      <p className="iot-plc-finding-card__driver">
                        Öncü sürücü: {finding.leadingDriverLabel}
                      </p>
                    ) : null}
                    <p className="iot-plc-finding-card__detail">{finding.detail}</p>
                    {finding.id.startsWith("assessment-") ? (
                      <button
                        type="button"
                        className="iot-plc-button iot-plc-finding-card__action"
                        onClick={() => setActiveTab("quality")}
                        aria-label={`${finding.title} için veri kalitesi sekmesini aç`}
                      >
                        Detayı gör
                      </button>
                    ) : null}
                  </li>
                ))}
              </ul>
            )}
          </section>

          <section
            className="iot-plc-panel"
            aria-label="Son bilinen sağlık gözlemleri"
            data-testid="iot-plc-health-timeline"
          >
            <h2 className="iot-plc-panel__title">{IOT_PLC_LAST_KNOWN_OBSERVATIONS_TITLE}</h2>
            <p className="iot-plc-panel__meta" data-testid="iot-plc-last-known-evidence-note">
              Geçmiş sağlık gözlemleri — mevcut durumdan ayrıdır.
            </p>
            {workspace.healthTimeline.length === 0 ? (
              <p className="iot-plc-empty">Uyumlu sağlık gözlemi bulunmuyor.</p>
            ) : (
              <div className="iot-plc-timeline">
                {workspace.healthTimeline.map((point, index) => (
                  <div key={`${point.endpointCode}-${point.observedAtUtc}-${index}`} className="iot-plc-timeline__item">
                    <div className="iot-plc-timeline__rail">
                      <span
                        className={`iot-plc-timeline__point ${healthTimelineTone(point.healthState)}`}
                        aria-hidden="true"
                      />
                      <span className="iot-plc-timeline__line" aria-hidden="true" />
                    </div>
                    <div className="iot-plc-timeline__body">
                      <p className="iot-plc-timeline__title">
                        {point.endpointLabel} · {point.healthLabel}
                      </p>
                      <p className="iot-plc-timeline__summary">
                        {formatIotPlcDateTime(point.observedAtUtc)} · Kanıt:{" "}
                        {getIotPlcEvidenceLabel(point.evidenceState)}
                      </p>
                      <p className="iot-plc-timeline__meta">{point.endpointCode}</p>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </section>
        </div>
      ) : null}

      {activeTab === "connections" ? (
        <section
          id="iot-plc-tab-panel-connections"
          role="tabpanel"
          aria-labelledby="iot-plc-tab-connections"
          data-testid="iot-plc-connections-panel"
        >
          <div
            className={`iot-plc-registry-layout${selectedEndpoint ? " iot-plc-registry-layout--split" : ""}`}
          >
            <div className="iot-plc-registry">
              <div className="iot-plc-panel__header">
                <h2 className="iot-plc-panel__title">Bağlantı kayıtları</h2>
                {canManage ? (
                  <button
                    type="button"
                    className="iot-plc-button iot-plc-button--primary"
                    disabled={isPending}
                    onClick={openCreateEndpoint}
                    aria-label="Yeni bağlantı kaydı oluştur"
                  >
                    <Plus aria-hidden="true" size={14} />
                    Bağlantı ekle
                  </button>
                ) : null}
              </div>
              <div className="iot-plc-registry-toolbar">
                <label style={{ display: "inline-flex", alignItems: "center", gap: "0.35rem" }}>
                  <Search aria-hidden="true" size={14} />
                  <span className="iot-plc-visually-hidden">Bağlantı ara</span>
                  <input
                    type="search"
                    placeholder="Ad veya kod ara"
                    value={endpointSearch}
                    onChange={(event) => setEndpointSearch(event.target.value)}
                    aria-label="Bağlantı ara"
                  />
                </label>
              </div>
              <div className="iot-plc-registry__scroll">
                {filteredEndpoints.length === 0 ? (
                  <div className="iot-plc-empty">
                    <Cable aria-hidden="true" size={22} />
                    <p>Kayıtlı bağlantı bulunmuyor.</p>
                  </div>
                ) : (
                  <table className="iot-plc-registry-table">
                    <thead>
                      <tr>
                        <th>Ad</th>
                        <th>Kod</th>
                        <th>Protokol</th>
                        <th>Yön</th>
                        <th>Durum</th>
                        <th>Sağlık</th>
                        <th>Son kanıt</th>
                        <th>Sinyaller</th>
                        <th>Kanıt</th>
                      </tr>
                    </thead>
                    <tbody>
                      {filteredEndpoints.map((endpoint) => (
                        <tr
                          key={endpoint.reference}
                          ref={(node) => {
                            if (node) endpointRowRefs.current.set(endpoint.reference, node);
                            else endpointRowRefs.current.delete(endpoint.reference);
                          }}
                          tabIndex={0}
                          className={endpoint.reference === selectedEndpointRef ? "is-selected" : ""}
                          onClick={() => selectEndpoint(endpoint)}
                          onKeyDown={(event) => {
                            if (event.key === "Enter" || event.key === " ") {
                              event.preventDefault();
                              selectEndpoint(endpoint);
                            }
                          }}
                        >
                          <td>{endpoint.displayName}</td>
                          <td>{endpoint.endpointCode}</td>
                          <td>{endpoint.protocolLabel}</td>
                          <td>{endpoint.connectionDirectionLabel}</td>
                          <td>{endpoint.endpointStatusLabel}</td>
                          <td>
                            <span className={healthBadgeClass(endpoint.healthBucket)}>
                              {endpoint.healthLabel}
                            </span>
                          </td>
                          <td>{endpoint.lastEvidenceLabel}</td>
                          <td>{formatIotPlcCount(endpoint.signalCount)}</td>
                          <td>{endpoint.evidenceModeLabel}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </div>
            </div>

            {selectedEndpoint ? (
              <aside className="iot-plc-detail-panel" aria-label="Bağlantı detayı">
                <div className="iot-plc-detail-panel__header">
                  <div>
                    <h3 className="iot-plc-detail-panel__title">{selectedEndpoint.displayName}</h3>
                    <p className="iot-plc-panel__meta">{selectedEndpoint.endpointCode}</p>
                  </div>
                  <button
                    type="button"
                    className="iot-plc-button"
                    aria-label="Bağlantı detayını kapat"
                    onClick={closeEndpointDetail}
                  >
                    Kapat
                  </button>
                </div>
                {isPending && !endpointDetail ? (
                  <div className="iot-plc-empty">
                    <LoaderCircle aria-hidden="true" className="animate-spin" size={16} />
                    Detay yükleniyor…
                  </div>
                ) : endpointDetail ? (
                  <>
                    <dl>
                      <dt>Protokol</dt>
                      <dd>{endpointDetail.summary.protocolLabel}</dd>
                      <dt>Yön</dt>
                      <dd>{endpointDetail.summary.connectionDirectionLabel}</dd>
                      <dt>Durum</dt>
                      <dd>{endpointDetail.summary.endpointStatusLabel}</dd>
                      <dt>Sağlık</dt>
                      <dd>
                        <span className={healthBadgeClass(endpointDetail.summary.healthBucket)}>
                          {endpointDetail.summary.healthLabel}
                        </span>
                      </dd>
                      <dt>Son kanıt</dt>
                      <dd>{endpointDetail.summary.lastEvidenceLabel}</dd>
                      <dt>Kanıt modu</dt>
                      <dd>{endpointDetail.summary.evidenceModeLabel}</dd>
                      <dt>Kaynak</dt>
                      <dd>{endpointDetail.summary.configurationProvenance}</dd>
                      <dt>Sürüm</dt>
                      <dd>{endpointDetail.summary.version}</dd>
                    </dl>
                    <section style={{ marginTop: "0.65rem" }} aria-label="Eşlenen sinyaller">
                      <h4 className="iot-plc-panel__title">Eşlenen sinyaller</h4>
                      {endpointDetail.mappedSignals.length === 0 ? (
                        <p className="iot-plc-empty">Eşlenen sinyal yok.</p>
                      ) : (
                        <ul style={{ margin: 0, paddingLeft: "1rem" }}>
                          {endpointDetail.mappedSignals.map((signal) => (
                            <li key={signal.reference}>{signal.displayName} · {signal.signalCode}</li>
                          ))}
                        </ul>
                      )}
                    </section>
                    {canManage ? (
                      <button
                        type="button"
                        className="iot-plc-button iot-plc-button--primary"
                        style={{ marginTop: "0.55rem" }}
                        disabled={isPending}
                        onClick={() => openEditEndpoint(endpointDetail.summary)}
                        aria-label="Bağlantı kaydını düzenle"
                      >
                        Bağlantıyı düzenle
                      </button>
                    ) : null}
                  </>
                ) : (
                  <p className="iot-plc-empty">Detay yüklenemedi.</p>
                )}
              </aside>
            ) : null}
          </div>
        </section>
      ) : null}

      {activeTab === "signals" ? (
        <section
          id="iot-plc-tab-panel-signals"
          role="tabpanel"
          aria-labelledby="iot-plc-tab-signals"
          data-testid="iot-plc-signals-panel"
        >
          <div
            className={`iot-plc-registry-layout${selectedSignal ? " iot-plc-registry-layout--split" : ""}`}
          >
            <div className="iot-plc-registry">
              <div className="iot-plc-panel__header">
                <h2 className="iot-plc-panel__title">Sinyal kayıtları</h2>
                {canManage ? (
                  <button
                    type="button"
                    className="iot-plc-button iot-plc-button--primary"
                    disabled={isPending || workspace.endpoints.length === 0}
                    onClick={openCreateSignal}
                    aria-label="Yeni sinyal kaydı oluştur"
                  >
                    <Plus aria-hidden="true" size={14} />
                    Sinyal ekle
                  </button>
                ) : null}
              </div>
              <div className="iot-plc-registry-toolbar">
                <label style={{ display: "inline-flex", alignItems: "center", gap: "0.35rem" }}>
                  <Search aria-hidden="true" size={14} />
                  <span className="iot-plc-visually-hidden">Sinyal ara</span>
                  <input
                    type="search"
                    placeholder="Ad, kod veya bağlantı ara"
                    value={signalSearch}
                    onChange={(event) => setSignalSearch(event.target.value)}
                    aria-label="Sinyal ara"
                  />
                </label>
              </div>
              <div className="iot-plc-registry__scroll">
                {filteredSignals.length === 0 ? (
                  <div className="iot-plc-empty">
                    <ClipboardCheck aria-hidden="true" size={22} />
                    <p>Kayıtlı sinyal bulunmuyor.</p>
                  </div>
                ) : (
                  <table className="iot-plc-registry-table">
                    <thead>
                      <tr>
                        <th>Ad</th>
                        <th>Kod</th>
                        <th>Bağlantı</th>
                        <th>Semantik</th>
                        <th>Veri tipi</th>
                        <th>Birim</th>
                        <th>Varlık</th>
                        <th>Etkin</th>
                        <th>Son gözlem</th>
                        <th>Kalite / kanıt</th>
                      </tr>
                    </thead>
                    <tbody>
                      {filteredSignals.map((signal) => (
                        <tr
                          key={signal.reference}
                          ref={(node) => {
                            if (node) signalRowRefs.current.set(signal.reference, node);
                            else signalRowRefs.current.delete(signal.reference);
                          }}
                          tabIndex={0}
                          className={signal.reference === selectedSignalRef ? "is-selected" : ""}
                          onClick={() => selectSignal(signal)}
                          onKeyDown={(event) => {
                            if (event.key === "Enter" || event.key === " ") {
                              event.preventDefault();
                              selectSignal(signal);
                            }
                          }}
                        >
                          <td>{signal.displayName}</td>
                          <td>{signal.signalCode}</td>
                          <td>{signal.endpointLabel}</td>
                          <td>{signal.semanticTypeLabel}</td>
                          <td>{signal.dataType}</td>
                          <td>{signal.engineeringUnit ?? "—"}</td>
                          <td>{signal.mappedAssetLabel}</td>
                          <td>{signal.enabledLabel}</td>
                          <td>{signal.latestObservationLabel}</td>
                          <td>
                            {signal.latestQualityLabel} · {signal.latestEvidenceLabel}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </div>
            </div>

            {selectedSignal ? (
              <aside className="iot-plc-detail-panel" aria-label="Sinyal detayı">
                <div className="iot-plc-detail-panel__header">
                  <div>
                    <h3 className="iot-plc-detail-panel__title">{selectedSignal.displayName}</h3>
                    <p className="iot-plc-panel__meta">{selectedSignal.signalCode}</p>
                  </div>
                  <button
                    type="button"
                    className="iot-plc-button"
                    aria-label="Sinyal detayını kapat"
                    onClick={closeSignalDetail}
                  >
                    Kapat
                  </button>
                </div>
                {isPending && !signalDetail ? (
                  <div className="iot-plc-empty">
                    <LoaderCircle aria-hidden="true" className="animate-spin" size={16} />
                    Detay yükleniyor…
                  </div>
                ) : signalDetail ? (
                  <>
                    <dl>
                      <dt>Bağlantı</dt>
                      <dd>{signalDetail.summary.endpointLabel}</dd>
                      <dt>Semantik tip</dt>
                      <dd>{signalDetail.summary.semanticTypeLabel}</dd>
                      <dt>Veri tipi</dt>
                      <dd>{signalDetail.summary.dataType}</dd>
                      <dt>Birim</dt>
                      <dd>{signalDetail.summary.engineeringUnit ?? "—"}</dd>
                      <dt>Varlık eşlemesi</dt>
                      <dd>{signalDetail.summary.mappedAssetLabel}</dd>
                      <dt>Beklenen kadans</dt>
                      <dd>{signalDetail.summary.samplingExpectation ?? "—"}</dd>
                      <dt>Kaynak adresi</dt>
                      <dd>{signalDetail.summary.sourceAddress}</dd>
                      <dt>Son gözlem</dt>
                      <dd>{signalDetail.summary.latestObservationLabel}</dd>
                      <dt>Kalite / kanıt</dt>
                      <dd>
                        {signalDetail.summary.latestQualityLabel} ·{" "}
                        {signalDetail.summary.latestEvidenceLabel}
                      </dd>
                      <dt>Kaynak</dt>
                      <dd>{signalDetail.summary.sourceProvenance}</dd>
                      <dt>Sürüm</dt>
                      <dd>{signalDetail.summary.version}</dd>
                    </dl>
                    <section style={{ marginTop: "0.65rem" }} aria-label="Son gözlemler">
                      <h4 className="iot-plc-panel__title">Son gözlemler</h4>
                      {signalDetail.recentObservations.length === 0 ? (
                        <p className="iot-plc-empty">Gözlem kaydı yok.</p>
                      ) : (
                        <ul style={{ margin: 0, paddingLeft: "1rem" }}>
                          {signalDetail.recentObservations.map((observation) => (
                            <li key={`${observation.observedAtUtc}-${observation.receivedAtUtc}`}>
                              {formatIotPlcDateTime(observation.observedAtUtc)} ·{" "}
                              {observation.qualityLabel} · {observation.evidenceLabel} ·{" "}
                              {observation.valueLabel}
                            </li>
                          ))}
                        </ul>
                      )}
                    </section>
                    {signalDetail.qualityDistributionAvailable ? (
                      <section style={{ marginTop: "0.65rem" }} aria-label="Kalite dağılımı">
                        <h4 className="iot-plc-panel__title">Kalite dağılımı</h4>
                        <ul style={{ margin: 0, paddingLeft: "1rem" }}>
                          {signalDetail.qualityDistribution.map((slice) => (
                            <li key={slice.key}>
                              {slice.label}: {formatIotPlcCount(slice.count)} ·{" "}
                              {formatIotPlcRatioPercent(slice.ratio)}
                            </li>
                          ))}
                        </ul>
                      </section>
                    ) : null}
                    {canManage ? (
                      <button
                        type="button"
                        className="iot-plc-button iot-plc-button--primary"
                        style={{ marginTop: "0.55rem" }}
                        disabled={isPending}
                        onClick={() => openEditSignal(signalDetail.summary)}
                        aria-label="Sinyal kaydını düzenle"
                      >
                        Sinyali düzenle
                      </button>
                    ) : null}
                  </>
                ) : (
                  <p className="iot-plc-empty">Detay yüklenemedi.</p>
                )}
              </aside>
            ) : null}
          </div>
        </section>
      ) : null}

      {activeTab === "quality" ? (
        <section
          id="iot-plc-tab-panel-quality"
          role="tabpanel"
          aria-labelledby="iot-plc-tab-quality"
          data-testid="iot-plc-quality-panel"
        >
          <div
            className={`iot-plc-registry-layout${selectedAssessment ? " iot-plc-registry-layout--split" : ""}`}
          >
            <div className="iot-plc-registry iot-plc-quality-workspace">
              <div className="iot-plc-quality-profile-rail" data-testid="iot-plc-quality-profile-rail">
                <h2 className="iot-plc-panel__title iot-plc-quality-section-title">Kalite profilleri</h2>
                {workspace.profiles.length === 0 ? (
                  <p className="iot-plc-empty">Kalite profili bulunmuyor.</p>
                ) : (
                  <div className="iot-plc-quality-profile-rail__grid">
                    {workspace.profiles.map((profile) => {
                      const isSelected = profile.profileCode === selectedQualityProfile?.profileCode;
                      return (
                        <article
                          key={profile.reference}
                          className={`iot-plc-profile-rail-card${isSelected ? " is-selected" : ""}`}
                          data-testid={`iot-plc-quality-profile-${profile.profileCode}`}
                          data-selected={isSelected ? "true" : "false"}
                        >
                          <button
                            type="button"
                            className="iot-plc-profile-rail-card__select"
                            onClick={() => selectQualityProfile(profile.profileCode)}
                            aria-label={`${profile.displayName} profilini seç`}
                            aria-pressed={isSelected}
                          >
                            <div className="iot-plc-profile-rail-card__head">
                              <strong className="iot-plc-profile-rail-card__name">{profile.displayName}</strong>
                              <span className="iot-plc-profile-rail-card__status">{profile.statusLabel}</span>
                            </div>
                            <p className="iot-plc-profile-rail-card__code">{profile.profileCode}</p>
                            <p className="iot-plc-profile-rail-card__threshold">
                              İyi min: {formatIotPlcRatioPercent(profile.minimumGoodQualityRatio)} · Bayat:{" "}
                              {formatIotPlcSeconds(profile.staleThresholdSeconds)}
                            </p>
                          </button>
                          {canManage ? (
                            <button
                              type="button"
                              className="iot-plc-button iot-plc-profile-rail-card__edit"
                              disabled={isPending}
                              onClick={() => openEditProfile(profile)}
                              aria-label={`${profile.displayName} profilini düzenle`}
                            >
                              Profili düzenle
                            </button>
                          ) : null}
                        </article>
                      );
                    })}
                  </div>
                )}
              </div>

              <section className="iot-plc-quality-hero" aria-label="Seçili profil kalite analizi">
                <QualityProfileIntelligenceSurface
                  profile={selectedQualityProfile}
                  assessment={activeTab === "quality" ? qualityHeroAssessment : null}
                  detail={activeTab === "quality" ? qualityHeroDetailForView : undefined}
                  isLoading={isQualityHeroLoading}
                />
              </section>

              {canManage ? (
                <div className="iot-plc-quality-eval-grid" data-testid="iot-plc-quality-controls">
                  <div className="iot-plc-quality-eval-group" data-testid="iot-plc-quality-eval-connection">
                    <h3 className="iot-plc-quality-eval-group__title">Bağlantı değerlendirmesi</h3>
                    <label className="iot-plc-quality-eval-group__field">
                      <span className="iot-plc-quality-eval-group__label">Değerlendirme profili</span>
                      <select
                        aria-label="Bağlantı değerlendirmesi için profil seç"
                        value={resolvedEvaluateProfileRef}
                        onChange={(event) => selectQualityProfile(event.target.value)}
                      >
                        {workspace.profiles.map((profile) => (
                          <option key={profile.reference} value={profile.profileCode}>
                            {profile.displayName}
                          </option>
                        ))}
                      </select>
                    </label>
                    <label className="iot-plc-quality-eval-group__field">
                      <span className="iot-plc-quality-eval-group__label">Bağlantı</span>
                      <select
                        aria-label="Bağlantı değerlendirmesi için bağlantı seç"
                        value={resolvedEvaluateEndpointRef}
                        onChange={(event) => setEvaluateEndpointRef(event.target.value)}
                      >
                        {workspace.endpoints.map((endpoint) => (
                          <option key={endpoint.reference} value={endpoint.reference}>
                            {endpoint.displayName}
                          </option>
                        ))}
                      </select>
                    </label>
                    <div className="iot-plc-quality-eval-group__action">
                      <button
                        type="button"
                        className="iot-plc-button iot-plc-button--secondary iot-plc-quality-eval-group__button"
                        disabled={isPending || !resolvedEvaluateEndpointRef}
                        onClick={runConnectivityEvaluation}
                        aria-label="Bağlantı değerlendirmesi çalıştır"
                      >
                        Bağlantı değerlendirmesi çalıştır
                      </button>
                    </div>
                  </div>

                  <div className="iot-plc-quality-eval-group" data-testid="iot-plc-quality-eval-signal">
                    <h3 className="iot-plc-quality-eval-group__title">Sinyal kalitesi değerlendirmesi</h3>
                    <label className="iot-plc-quality-eval-group__field">
                      <span className="iot-plc-quality-eval-group__label">Değerlendirme profili</span>
                      <select
                        aria-label="Sinyal kalitesi değerlendirmesi için profil seç"
                        value={resolvedEvaluateProfileRef}
                        onChange={(event) => selectQualityProfile(event.target.value)}
                      >
                        {workspace.profiles.map((profile) => (
                          <option key={profile.reference} value={profile.profileCode}>
                            {profile.displayName}
                          </option>
                        ))}
                      </select>
                    </label>
                    <label className="iot-plc-quality-eval-group__field">
                      <span className="iot-plc-quality-eval-group__label">Sinyal</span>
                      <select
                        aria-label="Sinyal kalitesi değerlendirmesi için sinyal seç"
                        value={resolvedEvaluateSignalRef}
                        onChange={(event) => setEvaluateSignalRef(event.target.value)}
                      >
                        {workspace.signals.map((signal) => (
                          <option key={signal.reference} value={signal.reference}>
                            {signal.displayName}
                          </option>
                        ))}
                      </select>
                    </label>
                    <div className="iot-plc-quality-eval-group__action">
                      <button
                        type="button"
                        className="iot-plc-button iot-plc-button--secondary iot-plc-quality-eval-group__button"
                        disabled={isPending || !resolvedEvaluateSignalRef}
                        onClick={runSignalQualityEvaluation}
                        aria-label="Sinyal kalitesi değerlendirmesi çalıştır"
                      >
                        Sinyal kalitesi değerlendirmesi çalıştır
                      </button>
                    </div>
                  </div>
                </div>
              ) : null}

              <div className="iot-plc-quality-filters" data-testid="iot-plc-quality-filters">
                <p className="iot-plc-quality-filters__scope" data-testid="iot-plc-quality-filter-scope">
                  {formatIotPlcCount(filteredAssessmentCount)} sonuç
                </p>
                <select
                  aria-label="Sonuç filtresi"
                  value={assessmentOutcomeFilter}
                  onChange={(event) => setAssessmentOutcomeFilter(event.target.value)}
                >
                  {IOT_PLC_OUTCOME_FILTER_OPTIONS.map((option) => (
                    <option key={option.value || "all"} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
                <select
                  aria-label="Sürücü filtresi"
                  value={assessmentDriverFilter}
                  onChange={(event) => setAssessmentDriverFilter(event.target.value)}
                >
                  {driverFilterOptions.map((option) => (
                    <option key={option.value || "all"} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
                <select
                  aria-label="Kanıt filtresi"
                  value={assessmentEvidenceFilter}
                  onChange={(event) => setAssessmentEvidenceFilter(event.target.value)}
                >
                  {IOT_PLC_EVIDENCE_FILTER_OPTIONS.map((option) => (
                    <option key={option.value || "all"} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </div>

              <div className="iot-plc-quality-results" data-testid="iot-plc-quality-results">
                <h3 className="iot-plc-panel__title">Bağlantı değerlendirmeleri</h3>
                {renderAssessmentTable(
                  filteredConnectivityAssessments,
                  assessmentOutcomeFilter || assessmentEvidenceFilter || assessmentDriverFilter
                    ? "Seçili filtrelere uygun bağlantı değerlendirmesi yok."
                    : "Bağlantı değerlendirmesi bulunmuyor.",
                )}

                <h3 className="iot-plc-panel__title iot-plc-quality-results__subtitle">
                  Sinyal değerlendirmeleri
                </h3>
                {renderAssessmentTable(
                  filteredSignalAssessments,
                  assessmentOutcomeFilter || assessmentEvidenceFilter || assessmentDriverFilter
                    ? "Seçili filtrelere uygun sinyal değerlendirmesi yok."
                    : "Sinyal değerlendirmesi bulunmuyor.",
                )}
              </div>
            </div>

            {selectedAssessment ? (
              <aside className="iot-plc-detail-panel" aria-label="Değerlendirme detayı">
                <div className="iot-plc-detail-panel__header">
                  <div>
                    <h3 className="iot-plc-detail-panel__title">
                      {getIotPlcAssessmentTitle(
                        selectedAssessment.assessmentKind,
                        selectedAssessment.targetLabel,
                        selectedAssessment.outcome,
                      )}
                    </h3>
                    <p className="iot-plc-panel__meta">
                      {selectedAssessment.evaluationPeriodLabel} · {selectedAssessment.evidenceLabel}
                    </p>
                  </div>
                  <button
                    type="button"
                    className="iot-plc-button"
                    aria-label="Değerlendirme detayını kapat"
                    onClick={closeAssessmentDetail}
                  >
                    Kapat
                  </button>
                </div>
                {isPending && !assessmentDetail ? (
                  <div className="iot-plc-empty">
                    <LoaderCircle aria-hidden="true" className="animate-spin" size={16} />
                    Detay yükleniyor…
                  </div>
                ) : assessmentDetail ? (
                  <>
                    <dl>
                      <dt>Hedef</dt>
                      <dd>{assessmentDetail.summary.targetLabel}</dd>
                      <dt>Dönem</dt>
                      <dd>
                        {formatIotPlcCompactPeriod(
                          assessmentDetail.summary.evaluationWindowStartUtc,
                          assessmentDetail.summary.evaluationWindowEndUtc,
                        )}
                      </dd>
                      <dt>Sonuç</dt>
                      <dd>
                        <span className={outcomeBadgeClass(assessmentDetail.summary.outcome)}>
                          {getIotPlcOutcomeLabel(assessmentDetail.summary.outcome)}
                        </span>
                      </dd>
                      <dt>Öncelikli sürücü</dt>
                      <dd>{assessmentDetail.summary.leadingDriverLabel}</dd>
                      <dt>Profil</dt>
                      <dd>{assessmentDetail.profileLabel}</dd>
                      <dt>Gözlem paydası</dt>
                      <dd>
                        {assessmentDetail.actualObservationCount !== null
                          ? formatIotPlcCount(assessmentDetail.actualObservationCount)
                          : "—"}
                        {assessmentDetail.expectedObservationCount !== null
                          ? ` / ${formatIotPlcCount(assessmentDetail.expectedObservationCount)}`
                          : ""}
                      </dd>
                      <dt>Tamlık</dt>
                      <dd>{formatIotPlcRatioPercent(assessmentDetail.completenessRatio)}</dd>
                      <dt>İyi oranı</dt>
                      <dd>{formatIotPlcRatioPercent(assessmentDetail.goodRatio)}</dd>
                      <dt>Belirsiz oranı</dt>
                      <dd>{formatIotPlcRatioPercent(assessmentDetail.uncertainRatio)}</dd>
                      <dt>Kötü oranı</dt>
                      <dd>{formatIotPlcRatioPercent(assessmentDetail.badRatio)}</dd>
                      <dt>Bayat oranı</dt>
                      <dd>{formatIotPlcRatioPercent(assessmentDetail.staleRatio)}</dd>
                      <dt>Kaynak uyuşmazlığı</dt>
                      <dd>{formatIotPlcRatioPercent(assessmentDetail.sourceConflictRatio)}</dd>
                      <dt>Güncellik</dt>
                      <dd>{formatIotPlcSeconds(assessmentDetail.freshnessSeconds)}</dd>
                      <dt>Saat kayması</dt>
                      <dd>{formatIotPlcSeconds(assessmentDetail.maxClockSkewSeconds)}</dd>
                      <dt>Son sağlık</dt>
                      <dd>
                        {assessmentDetail.latestHealthState
                          ? getIotPlcHealthLabel(assessmentDetail.latestHealthState)
                          : "—"}
                      </dd>
                      <dt>Sağlık gözlem kapsamı</dt>
                      <dd>
                        {formatIotPlcRatioPercent(assessmentDetail.healthObservationAvailabilityRatio)}
                      </dd>
                      <dt>Değerlendirme zamanı</dt>
                      <dd>{formatIotPlcDateTime(assessmentDetail.summary.evaluatedAtUtc)}</dd>
                      <dt>Kaynak</dt>
                      <dd>{assessmentDetail.provenance}</dd>
                    </dl>
                    <section style={{ marginTop: "0.65rem" }} aria-label="Sürücü kanıtları">
                      <h4 className="iot-plc-panel__title">Sürücü kanıtları</h4>
                      {assessmentDetail.drivers.length === 0 ? (
                        <p className="iot-plc-empty">Sürücü kanıtı bulunmuyor.</p>
                      ) : (
                        <ul style={{ margin: 0, paddingLeft: "1rem" }}>
                          {assessmentDetail.drivers.map((driver) => (
                            <li key={driver.reasonCode} style={{ marginBottom: "0.45rem" }}>
                              <strong>{getIotPlcDriverLabel(driver.reasonCode)}</strong>
                              {driver.isLeadingDriver ? " · Öncelikli" : ""}
                              {driver.explanation ? ` · ${driver.explanation}` : ""}
                              {driver.affectedObservationCount > 0
                                ? ` · ${formatIotPlcCount(driver.affectedObservationCount)} gözlem`
                                : ""}
                            </li>
                          ))}
                        </ul>
                      )}
                    </section>
                  </>
                ) : (
                  <p className="iot-plc-empty">Detay yüklenemedi.</p>
                )}
              </aside>
            ) : null}
          </div>
        </section>
      ) : null}

      {endpointForm ? (
        <div
          className="iot-plc-dialog-backdrop"
          role="presentation"
          style={{
            position: "fixed",
            inset: 0,
            background: "rgb(15 23 42 / 0.45)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            padding: "1rem",
            zIndex: 40,
          }}
        >
          <form
            className="iot-plc-panel"
            role="dialog"
            aria-modal="true"
            aria-labelledby="iot-plc-endpoint-dialog-title"
            onSubmit={submitEndpointForm}
            style={{ width: "min(100%, 32rem)" }}
          >
            <h3 id="iot-plc-endpoint-dialog-title">
              {endpointForm.mode === "create" ? "Bağlantı ekle" : "Bağlantıyı düzenle"}
            </h3>
            {dialogError ? (
              <div className="iot-plc-error-banner" role="alert">{dialogError}</div>
            ) : null}
            <label>
              Kod
              <input
                value={endpointForm.endpointCode}
                disabled={endpointForm.mode === "edit"}
                onChange={(event) =>
                  setEndpointForm((current) =>
                    current ? { ...current, endpointCode: event.target.value } : current,
                  )
                }
                required
              />
            </label>
            <label>
              Görünen ad
              <input
                value={endpointForm.displayName}
                onChange={(event) =>
                  setEndpointForm((current) =>
                    current ? { ...current, displayName: event.target.value } : current,
                  )
                }
                required
              />
            </label>
            <label>
              Protokol
              <select
                value={endpointForm.protocolType}
                onChange={(event) =>
                  setEndpointForm((current) =>
                    current ? { ...current, protocolType: event.target.value } : current,
                  )
                }
              >
                {PROTOCOL_OPTIONS.map((value) => (
                  <option key={value} value={value}>{getIotPlcProtocolLabel(value)}</option>
                ))}
              </select>
            </label>
            <label>
              Bağlantı yönü
              <select
                value={endpointForm.connectionDirection}
                onChange={(event) =>
                  setEndpointForm((current) =>
                    current ? { ...current, connectionDirection: event.target.value } : current,
                  )
                }
              >
                {DIRECTION_OPTIONS.map((value) => (
                  <option key={value} value={value}>{getIotPlcDirectionLabel(value)}</option>
                ))}
              </select>
            </label>
            <label>
              Durum
              <select
                value={endpointForm.endpointStatus}
                onChange={(event) =>
                  setEndpointForm((current) =>
                    current ? { ...current, endpointStatus: event.target.value } : current,
                  )
                }
              >
                {ENDPOINT_STATUS_OPTIONS.map((value) => (
                  <option key={value} value={value}>
                    {getIotPlcEndpointStatusLabel(value)}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Kaynak açıklaması
              <textarea
                rows={2}
                value={endpointForm.configurationProvenance}
                onChange={(event) =>
                  setEndpointForm((current) =>
                    current ? { ...current, configurationProvenance: event.target.value } : current,
                  )
                }
              />
            </label>
            <label>
              Bağlantı profili (JSON)
              <textarea
                rows={3}
                value={endpointForm.connectionProfileJson}
                onChange={(event) =>
                  setEndpointForm((current) =>
                    current ? { ...current, connectionProfileJson: event.target.value } : current,
                  )
                }
              />
            </label>
            <div style={{ display: "flex", gap: "0.5rem", justifyContent: "flex-end" }}>
              <button
                type="button"
                className="iot-plc-button"
                disabled={isPending}
                onClick={() => setEndpointForm(null)}
                aria-label="Bağlantı formunu iptal et"
              >
                İptal
              </button>
              <button
                type="submit"
                className="iot-plc-button iot-plc-button--primary"
                disabled={isPending}
                aria-label="Bağlantı kaydını kaydet"
              >
                Kaydet
              </button>
            </div>
          </form>
        </div>
      ) : null}

      {signalForm ? (
        <div
          className="iot-plc-dialog-backdrop"
          role="presentation"
          style={{
            position: "fixed",
            inset: 0,
            background: "rgb(15 23 42 / 0.45)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            padding: "1rem",
            zIndex: 40,
          }}
        >
          <form
            className="iot-plc-panel"
            role="dialog"
            aria-modal="true"
            aria-labelledby="iot-plc-signal-dialog-title"
            onSubmit={submitSignalForm}
            style={{ width: "min(100%, 32rem)" }}
          >
            <h3 id="iot-plc-signal-dialog-title">
              {signalForm.mode === "create" ? "Sinyal ekle" : "Sinyali düzenle"}
            </h3>
            {dialogError ? (
              <div className="iot-plc-error-banner" role="alert">{dialogError}</div>
            ) : null}
            {signalForm.mode === "create" ? (
              <>
                <label>
                  Bağlantı
                  <select
                    value={signalForm.endpointReference}
                    onChange={(event) =>
                      setSignalForm((current) =>
                        current ? { ...current, endpointReference: event.target.value } : current,
                      )
                    }
                  >
                    {workspace.endpoints.map((endpoint) => (
                      <option key={endpoint.reference} value={endpoint.reference}>
                        {endpoint.displayName}
                      </option>
                    ))}
                  </select>
                </label>
                <label>
                  Sinyal kodu
                  <input
                    value={signalForm.signalCode}
                    onChange={(event) =>
                      setSignalForm((current) =>
                        current ? { ...current, signalCode: event.target.value } : current,
                      )
                    }
                    required
                  />
                </label>
              </>
            ) : null}
            <label>
              Görünen ad
              <input
                value={signalForm.displayName}
                onChange={(event) =>
                  setSignalForm((current) =>
                    current ? { ...current, displayName: event.target.value } : current,
                  )
                }
                required
              />
            </label>
            <label>
              Kaynak adresi
              <input
                value={signalForm.sourceAddress}
                onChange={(event) =>
                  setSignalForm((current) =>
                    current ? { ...current, sourceAddress: event.target.value } : current,
                  )
                }
                required
              />
            </label>
            <label>
              Veri tipi
              <select
                value={signalForm.dataType}
                onChange={(event) =>
                  setSignalForm((current) =>
                    current ? { ...current, dataType: event.target.value } : current,
                  )
                }
              >
                {DATA_TYPE_OPTIONS.map((value) => (
                  <option key={value} value={value}>{value}</option>
                ))}
              </select>
            </label>
            <label>
              Mühendislik birimi
              <input
                value={signalForm.engineeringUnit}
                onChange={(event) =>
                  setSignalForm((current) =>
                    current ? { ...current, engineeringUnit: event.target.value } : current,
                  )
                }
              />
            </label>
            <label>
              Semantik tip
              <select
                value={signalForm.semanticType}
                onChange={(event) =>
                  setSignalForm((current) =>
                    current ? { ...current, semanticType: event.target.value } : current,
                  )
                }
              >
                {SEMANTIC_TYPE_OPTIONS.map((value) => (
                  <option key={value} value={value}>{getIotPlcSemanticTypeLabel(value)}</option>
                ))}
              </select>
            </label>
            <label>
              Varlık eşlemesi
              <select
                value={signalForm.mappedAssetType}
                onChange={(event) =>
                  setSignalForm((current) =>
                    current ? { ...current, mappedAssetType: event.target.value } : current,
                  )
                }
              >
                {ASSET_TYPE_OPTIONS.map((value) => (
                  <option key={value} value={value}>{value}</option>
                ))}
              </select>
            </label>
            <label>
              Etkin
              <input
                type="checkbox"
                checked={signalForm.isEnabled}
                onChange={(event) =>
                  setSignalForm((current) =>
                    current ? { ...current, isEnabled: event.target.checked } : current,
                  )
                }
              />
            </label>
            <label>
              Beklenen kadans
              <input
                value={signalForm.samplingExpectation}
                onChange={(event) =>
                  setSignalForm((current) =>
                    current ? { ...current, samplingExpectation: event.target.value } : current,
                  )
                }
              />
            </label>
            <label>
              Kaynak açıklaması
              <textarea
                rows={2}
                value={signalForm.sourceProvenance}
                onChange={(event) =>
                  setSignalForm((current) =>
                    current ? { ...current, sourceProvenance: event.target.value } : current,
                  )
                }
              />
            </label>
            <div style={{ display: "flex", gap: "0.5rem", justifyContent: "flex-end" }}>
              <button
                type="button"
                className="iot-plc-button"
                disabled={isPending}
                onClick={() => setSignalForm(null)}
                aria-label="Sinyal formunu iptal et"
              >
                İptal
              </button>
              <button
                type="submit"
                className="iot-plc-button iot-plc-button--primary"
                disabled={isPending}
                aria-label="Sinyal kaydını kaydet"
              >
                Kaydet
              </button>
            </div>
          </form>
        </div>
      ) : null}

      {profileForm ? (
        <div
          className="iot-plc-dialog-backdrop"
          role="presentation"
          style={{
            position: "fixed",
            inset: 0,
            background: "rgb(15 23 42 / 0.45)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            padding: "1rem",
            zIndex: 40,
          }}
        >
          <form
            className="iot-plc-panel"
            role="dialog"
            aria-modal="true"
            aria-labelledby="iot-plc-profile-dialog-title"
            onSubmit={submitProfileForm}
            style={{ width: "min(100%, 32rem)" }}
          >
            <h3 id="iot-plc-profile-dialog-title">Kalite profilini düzenle</h3>
            <p className="iot-plc-panel__meta">{profileForm.profile.profileCode}</p>
            {dialogError ? (
              <div className="iot-plc-error-banner" role="alert">{dialogError}</div>
            ) : null}
            <label>
              Görünen ad
              <input
                value={profileForm.displayName}
                onChange={(event) =>
                  setProfileForm((current) =>
                    current ? { ...current, displayName: event.target.value } : current,
                  )
                }
                required
              />
            </label>
            <label>
              Beklenen gözlem aralığı (sn)
              <input
                type="number"
                value={profileForm.expectedObservationIntervalSeconds}
                onChange={(event) =>
                  setProfileForm((current) =>
                    current
                      ? { ...current, expectedObservationIntervalSeconds: event.target.value }
                      : current,
                  )
                }
              />
            </label>
            <label>
              Güncellik toleransı (sn)
              <input
                type="number"
                value={profileForm.freshnessToleranceSeconds}
                onChange={(event) =>
                  setProfileForm((current) =>
                    current ? { ...current, freshnessToleranceSeconds: event.target.value } : current,
                  )
                }
              />
            </label>
            <label>
              Bayat eşiği (sn)
              <input
                type="number"
                value={profileForm.staleThresholdSeconds}
                onChange={(event) =>
                  setProfileForm((current) =>
                    current ? { ...current, staleThresholdSeconds: event.target.value } : current,
                  )
                }
              />
            </label>
            <label>
              Minimum iyi oranı
              <input
                type="number"
                step="any"
                value={profileForm.minimumGoodQualityRatio}
                onChange={(event) =>
                  setProfileForm((current) =>
                    current ? { ...current, minimumGoodQualityRatio: event.target.value } : current,
                  )
                }
              />
            </label>
            <label>
              Durum
              <select
                value={profileForm.status}
                onChange={(event) =>
                  setProfileForm((current) =>
                    current ? { ...current, status: event.target.value } : current,
                  )
                }
              >
                {["draft", "approved", "retired"].map((value) => (
                  <option key={value} value={value}>
                    {getIotPlcProfileStatusLabel(value)}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Kaynak açıklaması
              <textarea
                rows={2}
                value={profileForm.sourceProvenance}
                onChange={(event) =>
                  setProfileForm((current) =>
                    current ? { ...current, sourceProvenance: event.target.value } : current,
                  )
                }
              />
            </label>
            <div style={{ display: "flex", gap: "0.5rem", justifyContent: "flex-end" }}>
              <button
                type="button"
                className="iot-plc-button"
                disabled={isPending}
                onClick={() => setProfileForm(null)}
                aria-label="Profil formunu iptal et"
              >
                İptal
              </button>
              <button
                type="submit"
                className="iot-plc-button iot-plc-button--primary"
                disabled={isPending}
                aria-label="Kalite profilini kaydet"
              >
                Kaydet
              </button>
            </div>
          </form>
        </div>
      ) : null}
    </main>
  );
}
