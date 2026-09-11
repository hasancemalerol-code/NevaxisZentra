import Link from "next/link";

import {
  getProductionManagerConfidenceLabel,
  getProductionManagerStatusLabel,
  PRODUCTION_MANAGER_TITLE,
} from "@/features/production-manager/domain/production-manager-labels";
import { readProductionManagerDashboardPanel } from "@/features/production-manager/server/production-manager-reader";

export async function AiProductionManagerDashboardPanel() {
  const panel = await readProductionManagerDashboardPanel();
  if (panel.status === "failure") {
    return null;
  }

  return (
    <section className="dashboard-panel" data-testid="dashboard-ai-production-manager-panel">
      <p className="shift-ops-hero__eyebrow">{PRODUCTION_MANAGER_TITLE}</p>
      {panel.topPrediction ? (
        <p>
          Tahmin: {panel.topPrediction.title} ·{" "}
          {getProductionManagerConfidenceLabel(panel.topPrediction.confidenceBand)}
        </p>
      ) : (
        <p>Tahmin: stabil</p>
      )}
      {panel.topRecommendation ? (
        <p>
          Öneri: {panel.topRecommendation.title} ·{" "}
          {getProductionManagerStatusLabel(panel.topRecommendation.status)}
        </p>
      ) : (
        <p>Öneri: bekleyen karar yok</p>
      )}
      <p>Onay bekleyen: {panel.pendingApprovalCount}</p>
      <p>Otonom aksiyon: {panel.autonomousActionCount}</p>
      <p>Bugün doğrulanan: {panel.verifiedTodayCount}</p>
      <Link className="shift-ops-cta" href="/operasyon/yonetim-merkezi">
        Operasyon Yönetim Merkezi
      </Link>
    </section>
  );
}
