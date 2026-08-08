"use client";

import { useCallback, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import {
  AlertTriangle,
  Bot,
  CheckCircle2,
  Coins,
  ShieldAlert,
  Sparkles,
  UserRoundCheck,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";

import type {
  AiUsageSummary,
  ExportAiFinding,
  ExportAiRecommendation,
  ExportAiRunDetail,
} from "../domain/export-4l";
import {
  EXPORT_AI_DEVELOPMENT_PROVIDER,
  formatAiConfidence,
  getCrossModuleRiskSeverityLabel,
  getExportAiFindingSeverityLabel,
  getExportAiProviderLabel,
  getExportAiRecommendationPriorityLabel,
  getExportAiRecommendationStatusLabel,
  getExportAiRunStatusLabel,
  getExportAiRunTypeLabel,
  getExportAiSourceTypeLabel,
} from "../domain/export-4l";
import type {
  acceptExportAiRecommendationAction,
  analyzeExportCaseAction,
  dismissExportAiRecommendationAction,
  explainExportReadinessAction,
  rejectExportAiRecommendationAction,
  suggestExportNextActionsAction,
} from "../server/export-4l-writer";

type BadgeTone =
  | "neutral"
  | "information"
  | "success"
  | "warning"
  | "danger"
  | "ai";

type PanelState =
  | { kind: "idle" }
  | { kind: "pending"; action: string }
  | { kind: "error"; message: string }
  | { kind: "success"; message: string };

function severityTone(severity: string): BadgeTone {
  switch (severity) {
    case "critical":
      return "danger";
    case "warning":
      return "warning";
    case "info":
    default:
      return "information";
  }
}

function recommendationTone(priority: string): BadgeTone {
  switch (priority) {
    case "critical":
      return "danger";
    case "high":
      return "warning";
    case "medium":
      return "information";
    default:
      return "neutral";
  }
}

function formatUtcDate(iso: string | undefined): string {
  if (!iso) return "—";

  try {
    return new Date(iso).toLocaleDateString("tr-TR", {
      day: "2-digit",
      month: "short",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  } catch {
    return iso;
  }
}

type ExportAiCopilotPanelProps = Readonly<{
  exportCaseId: string;
  latestRunDetail: ExportAiRunDetail | null;
  recommendations: readonly ExportAiRecommendation[];
  usage: AiUsageSummary | null;
  onAnalyze: typeof analyzeExportCaseAction;
  onExplainReadiness: typeof explainExportReadinessAction;
  onSuggestNextActions: typeof suggestExportNextActionsAction;
  onAcceptRecommendation: typeof acceptExportAiRecommendationAction;
  onRejectRecommendation: typeof rejectExportAiRecommendationAction;
  onDismissRecommendation: typeof dismissExportAiRecommendationAction;
}>;

export function ExportAiCopilotPanel({
  exportCaseId,
  latestRunDetail,
  recommendations,
  usage,
  onAnalyze,
  onExplainReadiness,
  onSuggestNextActions,
  onAcceptRecommendation,
  onRejectRecommendation,
  onDismissRecommendation,
}: ExportAiCopilotPanelProps) {
  const router = useRouter();
  const [state, setState] = useState<PanelState>({ kind: "idle" });

  const isPending = state.kind === "pending";
  const run = latestRunDetail?.run ?? null;
  const contextSnapshot = latestRunDetail?.contextSnapshot;
  const provider = run?.provider ?? EXPORT_AI_DEVELOPMENT_PROVIDER;

  const blockers = useMemo(
    () =>
      (latestRunDetail?.findings ?? []).filter(
        (finding: ExportAiFinding) =>
          finding.severity === "critical" || finding.humanReviewRequired,
      ),
    [latestRunDetail?.findings],
  );

  const crossModuleRisks = contextSnapshot?.crossModuleRisks ?? [];
  const readinessCase = contextSnapshot?.exportCase;

  const proposedRecommendations = useMemo(
    () => recommendations.filter((item) => item.status === "proposed"),
    [recommendations],
  );

  const runRecommendations = latestRunDetail?.recommendations ?? [];

  const displayRecommendations =
    runRecommendations.length > 0 ? runRecommendations : proposedRecommendations;

  const creditsConsumed =
    usage?.creditsConsumedThisPeriod ?? run?.aiCreditsConsumed ?? 0;

  const runAnalysis = useCallback(
    async (action: string, handler: () => Promise<{ status: string; message?: string }>) => {
      setState({ kind: "pending", action });
      const result = await handler();
      if (result.status === "failure") {
        setState({
          kind: "error",
          message: result.message ?? "AI analizi başarısız oldu.",
        });
        return;
      }
      setState({ kind: "success", message: "AI analizi tamamlandı." });
      router.refresh();
    },
    [router],
  );

  const handleAnalyze = useCallback(async () => {
    await runAnalysis("analyze", () => onAnalyze(exportCaseId));
  }, [exportCaseId, onAnalyze, runAnalysis]);

  const handleExplainReadiness = useCallback(async () => {
    await runAnalysis("readiness", () => onExplainReadiness(exportCaseId));
  }, [exportCaseId, onExplainReadiness, runAnalysis]);

  const handleSuggestNextActions = useCallback(async () => {
    await runAnalysis("next-actions", () => onSuggestNextActions(exportCaseId));
  }, [exportCaseId, onSuggestNextActions, runAnalysis]);

  const handleReview = useCallback(
    async (
      recommendation: ExportAiRecommendation,
      decision: "accept" | "reject" | "dismiss",
    ) => {
      setState({ kind: "pending", action: `review-${decision}` });
      const input = { expectedVersion: recommendation.version };

      const result =
        decision === "accept"
          ? await onAcceptRecommendation(
              exportCaseId,
              recommendation.id,
              input,
            )
          : decision === "reject"
            ? await onRejectRecommendation(
                exportCaseId,
                recommendation.id,
                input,
              )
            : await onDismissRecommendation(
                exportCaseId,
                recommendation.id,
                input,
              );

      if (result.status === "failure") {
        setState({ kind: "error", message: result.message });
        return;
      }

      setState({ kind: "success", message: "Öneri incelendi." });
      router.refresh();
    },
    [
      exportCaseId,
      onAcceptRecommendation,
      onDismissRecommendation,
      onRejectRecommendation,
      router,
    ],
  );

  return (
    <section className="export-ai-copilot-panel">
      <header className="export-ai-copilot-panel__header">
        <div>
          <p>AI EXPORT COPILOT</p>
          <h2>
            <Sparkles aria-hidden="true" />
            İhracat AI asistanı
          </h2>
          <span>Kaynak tabanlı analiz, hazırlık açıklaması ve kontrollü öneriler</span>
        </div>
        <div className="export-ai-copilot-panel__badges">
          <Badge tone="ai">
            <Bot aria-hidden="true" />
            {getExportAiProviderLabel(provider)}
          </Badge>
          {run ? (
            <Badge tone={run.status === "completed" ? "success" : "warning"}>
              {getExportAiRunStatusLabel(run.status)}
            </Badge>
          ) : null}
        </div>
      </header>

      {state.kind === "error" ? (
        <div
          className="export-ai-copilot-panel__feedback export-ai-copilot-panel__feedback--error"
          role="alert"
        >
          {state.message}
        </div>
      ) : null}
      {state.kind === "success" ? (
        <div
          className="export-ai-copilot-panel__feedback export-ai-copilot-panel__feedback--success"
          role="status"
        >
          <CheckCircle2 aria-hidden="true" />
          {state.message}
        </div>
      ) : null}

      <div className="export-ai-copilot-panel__usage">
        <Coins aria-hidden="true" />
        <div>
          <span>Tüketilen AI kredisi</span>
          <strong>{creditsConsumed.toFixed(2)}</strong>
          {usage?.monthlyQuota ? (
            <small>
              Kota: {usage.monthlyQuota.toFixed(0)} · %{usage.usagePercent.toFixed(0)}
            </small>
          ) : null}
        </div>
      </div>

      <div className="export-ai-copilot-panel__actions">
        <button type="button" disabled={isPending} onClick={() => void handleAnalyze()}>
          İhracat dosyasını analiz et
        </button>
        <button
          type="button"
          className="export-ai-copilot-panel__secondary"
          disabled={isPending}
          onClick={() => void handleExplainReadiness()}
        >
          Hazırlığı açıkla
        </button>
        <button
          type="button"
          className="export-ai-copilot-panel__secondary"
          disabled={isPending}
          onClick={() => void handleSuggestNextActions()}
        >
          Sonraki adımları öner
        </button>
      </div>

      <div className="export-ai-copilot-panel__summary">
        <article>
          <ShieldAlert aria-hidden="true" />
          <div>
            <span>Son analiz</span>
            <strong>
              {run
                ? getExportAiRunTypeLabel(run.runType)
                : "Henüz analiz yok"}
            </strong>
            <small>{formatUtcDate(run?.completedAtUtc ?? run?.startedAtUtc)}</small>
          </div>
        </article>
        <article>
          <Sparkles aria-hidden="true" />
          <div>
            <span>Hazırlık özeti</span>
            <strong>
              {readinessCase
                ? `%${readinessCase.readinessPercent} · ${readinessCase.readinessStatus}`
                : run?.reasoningSummary
                  ? "Analiz özeti mevcut"
                  : "—"}
            </strong>
            <small>
              {run?.reasoningSummary ??
                "Analiz başlatıldığında hazırlık özeti burada görünür."}
            </small>
          </div>
        </article>
      </div>

      {blockers.length > 0 ? (
        <div className="export-ai-copilot-panel__blockers">
          <h3>
            <AlertTriangle aria-hidden="true" />
            Blokajlar ve inceleme gerektiren bulgular
          </h3>
          <ul>
            {blockers.map((finding) => (
              <li key={finding.id}>
                <div>
                  <strong>{finding.title}</strong>
                  <p>{finding.summary}</p>
                  <small>
                    {getExportAiSourceTypeLabel(finding.sourceType)}
                    {finding.sourceReferences[0]
                      ? ` · ${finding.sourceReferences[0].label}`
                      : ""}
                  </small>
                </div>
                <Badge tone={severityTone(finding.severity)}>
                  {getExportAiFindingSeverityLabel(finding.severity)}
                </Badge>
              </li>
            ))}
          </ul>
        </div>
      ) : null}

      {crossModuleRisks.length > 0 ? (
        <div className="export-ai-copilot-panel__risks">
          <h3>Modüller arası riskler</h3>
          <ul>
            {crossModuleRisks.map((risk) => (
              <li key={`${risk.type}-${risk.sourceRecordReference}`}>
                <div>
                  <strong>{risk.description}</strong>
                  <small>
                    {risk.sourceModule} · {risk.sourceRecordReference}
                    {risk.blocking ? " · Bloke edici" : ""}
                  </small>
                </div>
                <Badge tone={severityTone(risk.severity)}>
                  {getCrossModuleRiskSeverityLabel(risk.severity)}
                </Badge>
              </li>
            ))}
          </ul>
        </div>
      ) : null}

      <div className="export-ai-copilot-panel__recommendations">
        <h3>Önerilen aksiyonlar</h3>
        {displayRecommendations.length === 0 ? (
          <p className="export-ai-copilot-panel__empty">
            Henüz öneri yok. Analiz veya sonraki adım önerisi başlatın.
          </p>
        ) : (
          <ul>
            {displayRecommendations.map((recommendation) => (
              <li key={recommendation.id}>
                <div>
                  <strong>{recommendation.title}</strong>
                  <p>{recommendation.description}</p>
                  <div className="export-ai-copilot-panel__recommendation-meta">
                    <Badge tone={recommendationTone(recommendation.priority)}>
                      {getExportAiRecommendationPriorityLabel(recommendation.priority)}
                    </Badge>
                    <span>{formatAiConfidence(recommendation.confidence)} güven</span>
                    <span>{recommendation.reasoningSummary}</span>
                    {recommendation.status === "proposed" ? (
                      <span className="export-ai-copilot-panel__human-review">
                        <UserRoundCheck aria-hidden="true" />
                        İnsan onayı gerekli
                      </span>
                    ) : (
                      <Badge tone="neutral">
                        {getExportAiRecommendationStatusLabel(recommendation.status)}
                      </Badge>
                    )}
                  </div>
                </div>
                {recommendation.status === "proposed" ? (
                  <div className="export-ai-copilot-panel__review-actions">
                    <button
                      type="button"
                      disabled={isPending}
                      onClick={() => void handleReview(recommendation, "accept")}
                    >
                      Kabul
                    </button>
                    <button
                      type="button"
                      className="export-ai-copilot-panel__secondary"
                      disabled={isPending}
                      onClick={() => void handleReview(recommendation, "reject")}
                    >
                      Reddet
                    </button>
                    <button
                      type="button"
                      className="export-ai-copilot-panel__secondary"
                      disabled={isPending}
                      onClick={() => void handleReview(recommendation, "dismiss")}
                    >
                      Kapat
                    </button>
                  </div>
                ) : null}
              </li>
            ))}
          </ul>
        )}
      </div>
    </section>
  );
}
