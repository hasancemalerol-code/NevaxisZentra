import { getProductionPlanFallbackLabel } from "../presentation/production-plan-fallback-labels";
import {
  ArrowRightLeft,
  CheckCircle2,
  CircleAlert,
  ShieldCheck,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";

import type {
  ProductionPlanApprovalTransferViewModel,
  ProductionPlanReadinessBlocker,
} from "../application/production-plan-approval-transfer-view-model";
import {
  getProductionPlanApprovalStatusLabel,
  getProductionPlanTargetSystemLabel,
  getProductionPlanTransferStatusLabel,
} from "../presentation/production-plan-labels";

type ProductionPlanApprovalTransferPanelProps =
  Readonly<{
    viewModel:
      ProductionPlanApprovalTransferViewModel;
  }>;

type ReadinessColumnProps =
  Readonly<{
    title: string;
    description: string;
    ready: boolean;
    blockers:
      readonly ProductionPlanReadinessBlocker[];
    readyLabel: string;
  }>;

function ReadinessColumn({
  title,
  description,
  ready,
  blockers,
  readyLabel,
}: ReadinessColumnProps) {
  return (
    <Card className="production-plan-readiness-column">
      <div className="production-plan-readiness-column__heading">
        <div
          className={[
            "production-plan-readiness-column__icon",
            ready
              ? "production-plan-readiness-column__icon--ready"
              : "production-plan-readiness-column__icon--blocked",
          ].join(" ")}
        >
          {ready ? (
            <CheckCircle2 aria-hidden="true" />
          ) : (
            <CircleAlert aria-hidden="true" />
          )}
        </div>

        <div>
          <p>{title}</p>
          <h3>{description}</h3>
        </div>
      </div>

      {ready ? (
        <div className="production-plan-readiness-column__ready">
          <CheckCircle2 aria-hidden="true" />

          <div>
            <strong>{readyLabel}</strong>

            <span>
              Nihai işlem yine yetkili kullanıcı
              incelemesi ve açık onayı gerektirir.
            </span>
          </div>
        </div>
      ) : (
        <div className="production-plan-readiness-column__blockers">
          {blockers.map((blocker) => (
            <article
              className="production-plan-readiness-blocker"
              key={blocker.type}
            >
              <CircleAlert aria-hidden="true" />

              <div>
                <strong>{blocker.title}</strong>
                <span>{blocker.description}</span>
              </div>
            </article>
          ))}
        </div>
      )}
    </Card>
  );
}

export function ProductionPlanApprovalTransferPanel({
  viewModel,
}: ProductionPlanApprovalTransferPanelProps) {
  return (
    <section className="production-plan-approval-transfer-panel">
      <div className="production-plan-approval-transfer-panel__heading">
        <div>
          <p>Onay ve aktarım hazırlığı</p>

          <h2>
            Human-in-the-Loop karar kapıları
          </h2>

          <span>
            Sistem yalnızca hazırlık durumunu ve
            engelleri gösterir. Planı otomatik
            onaylamaz veya bağlı sisteme otomatik
            aktarmaz.
          </span>
        </div>

        <div className="production-plan-approval-transfer-panel__badges">
          <Badge
            tone={
              viewModel.isReadyForApprovalReview
                ? "success"
                : "warning"
            }
          >
            <ShieldCheck aria-hidden="true" />
            {getProductionPlanApprovalStatusLabel(
              viewModel.approvalStatus,
            )}
          </Badge>

          <Badge
            tone={
              viewModel.isReadyForTransferReview
                ? "success"
                : "warning"
            }
          >
            <ArrowRightLeft aria-hidden="true" />
            {getProductionPlanTransferStatusLabel(
              viewModel.transferStatus,
            )}
          </Badge>
        </div>
      </div>

      <div className="production-plan-approval-transfer-panel__meta">
        <Card>
          <span>Onay durumu</span>

          <strong>
            {getProductionPlanApprovalStatusLabel(
              viewModel.approvalStatus,
            )}
          </strong>

          <small>
            {viewModel.approvalBlockers.length}
            {" "}hazırlık engeli
          </small>
        </Card>

        <Card>
          <span>Aktarım durumu</span>

          <strong>
            {getProductionPlanTransferStatusLabel(
              viewModel.transferStatus,
            )}
          </strong>

          <small>
            {viewModel.transferBlockers.length}
            {" "}hazırlık engeli
          </small>
        </Card>

        <Card>
          <span>Hedef sistem</span>

          <strong>
            {viewModel.targetSystem
              ? getProductionPlanTargetSystemLabel(
                  viewModel.targetSystem,
                )
              : getProductionPlanFallbackLabel("targetSystem")}
          </strong>

          <small>
            ERP veya MES bağlantısı
          </small>
        </Card>
      </div>

      <div className="production-plan-approval-transfer-panel__grid">
        <ReadinessColumn
          blockers={viewModel.approvalBlockers}
          description="Plan onay incelemesine hazır mı?"
          ready={
            viewModel.isReadyForApprovalReview
          }
          readyLabel="Onay incelemesine hazır"
          title="Onay kapısı"
        />

        <ReadinessColumn
          blockers={viewModel.transferBlockers}
          description="Bağlı sisteme aktarım incelemesine hazır mı?"
          ready={
            viewModel.isReadyForTransferReview
          }
          readyLabel="Aktarım incelemesine hazır"
          title="Aktarım kapısı"
        />
      </div>
    </section>
  );
}