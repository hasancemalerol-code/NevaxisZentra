"use client";

import { useCallback, useState } from "react";
import { useRouter } from "next/navigation";
import {
  AlertTriangle,
  CalendarClock,
  Clock3,
  Package,
  RefreshCw,
  ShieldCheck,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";

import {
  formatExportReadinessLabel,
  formatPlanningLogisticsIndicatorLabel,
  formatPlanningLogisticsRiskLabel,
  formatPlanningLogisticsSlackLabel,
  type PlanningLogisticsEvaluationView,
} from "../presentation/production-plan-logistics-formatters";
import type { evaluatePlanningLogisticsAction } from "../server/production-plan-logistics-actions";

type ProductionPlanLogisticsPanelProps = Readonly<{
  planId: string;
  evaluation: PlanningLogisticsEvaluationView;
  onEvaluate: typeof evaluatePlanningLogisticsAction;
}>;

function getRiskTone(riskLevel: string) {
  switch (riskLevel) {
    case "critical":
    case "high":
      return "danger" as const;
    case "medium":
      return "warning" as const;
    case "low":
      return "information" as const;
    default:
      return "neutral" as const;
  }
}

function formatDateTime(value?: string): string {
  if (!value) {
    return "—";
  }

  return new Intl.DateTimeFormat("tr-TR", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

export function ProductionPlanLogisticsPanel({
  planId,
  evaluation,
  onEvaluate,
}: ProductionPlanLogisticsPanelProps) {
  const router = useRouter();
  const [state, setState] = useState<
    "idle" | "pending" | "error"
  >("idle");
  const [errorMessage, setErrorMessage] = useState<string | null>(
    null,
  );

  const handleEvaluate = useCallback(async () => {
    setState("pending");
    setErrorMessage(null);

    const result = await onEvaluate(planId);
    if (result.status === "failure") {
      setState("error");
      setErrorMessage(result.message);
      return;
    }

    setState("idle");
    router.refresh();
  }, [onEvaluate, planId, router]);

  const linked = evaluation.linkageStatus === "linked";

  return (
    <section className="production-plan-detail-page__content">
      <Card className="production-plan-card">
        <div className="production-plan-card__heading">
          <div>
            <div className="production-plan-card__badges">
              <Badge tone="information">Lojistik Kısıtları</Badge>
              {linked ? (
                <Badge tone={getRiskTone(evaluation.riskLevel)}>
                  {formatPlanningLogisticsRiskLabel(evaluation.riskLevel)}
                </Badge>
              ) : null}
            </div>

            <h2>Fabrika sevkiyat termin kısıtları</h2>

            <p>
              Pickup ve hazırlık deadline&apos;ları Export/Shipment kaynağından
              salt okunur tüketilir. Planning dış lojistik veya broker
              operasyonlarını yönetmez.
            </p>
          </div>

          <button
            type="button"
            className="button button-secondary button-small"
            onClick={handleEvaluate}
            disabled={state === "pending"}
          >
            <RefreshCw aria-hidden="true" />
            {state === "pending" ? "Yeniden değerlendiriliyor…" : "Yeniden değerlendir"}
          </button>
        </div>

        {state === "error" && errorMessage ? (
          <p>{errorMessage}</p>
        ) : null}

        {!linked ? (
          <p>
            Bağlı ihracat/sevkiyat kaydı bulunamadı (
            {evaluation.linkageStatus}).
          </p>
        ) : (
          <>
            <div className="production-plan-card__summary">
              <div>
                <Package aria-hidden="true" />
                <div>
                  <span>Kaynak</span>
                  <strong>İhracat / Sevkiyat</strong>
                  <small>
                    {evaluation.exportCaseNumber ?? "EXP-…"} ·{" "}
                    {evaluation.shipmentReference ?? "SHP-…"}
                  </small>
                </div>
              </div>

              <div>
                <CalendarClock aria-hidden="true" />
                <div>
                  <span>Pickup deadline</span>
                  <strong>{formatDateTime(evaluation.pickupDeadlineUtc)}</strong>
                  <small>Fabrika dışı pickup tarihi (Export kaynağı)</small>
                </div>
              </div>

              <div>
                <Clock3 aria-hidden="true" />
                <div>
                  <span>En geç üretim bitişi</span>
                  <strong>
                    {formatDateTime(evaluation.latestProductionCompletionUtc)}
                  </strong>
                  <small>
                    Planlanan:{" "}
                    {formatDateTime(evaluation.plannedProductionCompletionUtc)}
                  </small>
                </div>
              </div>

              <div>
                <AlertTriangle aria-hidden="true" />
                <div>
                  <span>Slack / kalan süre</span>
                  <strong>
                    {formatPlanningLogisticsSlackLabel(evaluation.slackMinutes)}
                  </strong>
                  <small>
                    {formatPlanningLogisticsIndicatorLabel(evaluation)}
                  </small>
                </div>
              </div>

              <div>
                <ShieldCheck aria-hidden="true" />
                <div>
                  <span>Export readiness</span>
                  <strong>
                    {formatExportReadinessLabel(
                      evaluation.exportHandoffReady,
                      evaluation.exportHandoffBlocked,
                    )}
                  </strong>
                  <small>
                    Sevkiyata yetişme riski:{" "}
                    {formatPlanningLogisticsRiskLabel(evaluation.riskLevel)}
                  </small>
                </div>
              </div>
            </div>

            {evaluation.exportHandoffBlocked ? (
              <p>
                <strong>Export readiness blocked.</strong> Ayrıntılar Export
                modülünde görüntülenir.
              </p>
            ) : null}

            <p>
              Son değerlendirme: {formatDateTime(evaluation.evaluatedAtUtc)} ·
              kaynak sürüm {evaluation.sourceVersion}
            </p>
          </>
        )}
      </Card>
    </section>
  );
}
