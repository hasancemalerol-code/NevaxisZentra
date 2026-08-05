import Link from "next/link";
import { AiTaskCenterSummary } from "@/components/dashboard/ai-task-center-summary";
import { CriticalRisksPanel } from "@/components/dashboard/critical-risks-panel";
import { DailyOperationsSummary } from "@/components/dashboard/daily-operations-summary";
import { DashboardMetrics } from "@/components/dashboard/dashboard-metrics";
import { LiveAlarmsPanel } from "@/components/dashboard/live-alarms-panel";
import { MachineStatusPanel } from "@/components/dashboard/machine-status-panel";
import { MaintenanceStatusPanel } from "@/components/dashboard/maintenance-status-panel";
import { MaterialRisksPanel } from "@/components/dashboard/material-risks-panel";
import { MoldStatusPanel } from "@/components/dashboard/mold-status-panel";
import { PlanConfidencePanel } from "@/components/dashboard/plan-confidence-panel";
import { ProductionOrdersTable } from "@/components/dashboard/production-orders-table";
import { QuickActionsPanel } from "@/components/dashboard/quick-actions-panel";
import { SystemHealthPanel } from "@/components/dashboard/system-health-panel";
import { dashboardPreviewData } from "@/lib/mock-data/dashboard";

export default function DashboardPage() {
  const {
    aiTasks,
    criticalRisks,
    dailyOperations,
    machines,
    materialRisks,
    metrics,
    molds,
    planConfidence,
    productionOrders,
  } = dashboardPreviewData;

  return (
    <main className="command-center">
      <header className="command-center__titlebar">
        <div className="command-center__titlebar-copy">
          <p className="command-center__eyebrow">
            CrevaFactOS Operasyon Merkezi
          </p>

          <h1>AI Görev Merkezi</h1>

          <p className="command-center__description">
            Üretim, planlama, risk ve sistem durumlarını tek
            merkezden yönetin.
          </p>
        </div>

        <div className="command-center__titlebar-actions">
          <Link
            className="command-center__ai-task-button"
            href="/ai-gorevleri/yeni"
          >
            <span
              aria-hidden="true"
              className="command-center__ai-task-button-icon"
            >
              <svg
                fill="none"
                height="20"
                viewBox="0 0 24 24"
                width="20"
              >
                <path
                  d="M12 5v14M5 12h14"
                  stroke="currentColor"
                  strokeLinecap="round"
                  strokeWidth="2"
                />
              </svg>
            </span>

            <span className="command-center__ai-task-button-copy">
              <strong>AI&apos;ya Görev Ver</strong>
              <span>Yeni analiz veya işlem başlat</span>
            </span>
          </Link>
          <button
            aria-label="CrevaFact Teams mesajlarını görüntüle"
            className="command-center__teams-button"
            type="button"
          >
            <span
              aria-hidden="true"
              className="command-center__teams-button-icon"
            >
              <svg
                fill="none"
                height="20"
                viewBox="0 0 24 24"
                width="20"
              >
                <path
                  d="M7.5 18.5 4 20l1.1-3.4A7.5 7.5 0 1 1 19.5 13"
                  stroke="currentColor"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth="1.8"
                />
                <path
                  d="M8 10h8M8 13.5h5"
                  stroke="currentColor"
                  strokeLinecap="round"
                  strokeWidth="1.8"
                />
              </svg>
            </span>

            <span className="command-center__teams-button-copy">
              <strong>CrevaFact Teams</strong>
              <span>Yeni ekip mesajları</span>
            </span>

            <span
              aria-label="3 okunmamış mesaj"
              className="command-center__teams-button-badge"
            >
              3
            </span>
          </button>

          <div className="command-center__titlebar-status">
            <span
              aria-hidden="true"
              className="command-center__live-dot"
            />

            <div>
              <strong>Canlı operasyon görünümü</strong>
              <span>Son güncelleme: şimdi</span>
            </div>
          </div>
        </div>
      </header>

      <section
        aria-label="Temel performans göstergeleri"
        className="command-center__metrics"
      >
        <DashboardMetrics metrics={metrics} />
      </section>

      <section
        aria-label="Hızlı işlemler"
        className="command-center__quick-actions-strip"
      >
        <QuickActionsPanel />
      </section>

      <section
        aria-label="Karar ve risk merkezi"
        className="command-center__decision-grid"
      >
        <AiTaskCenterSummary tasks={aiTasks} />

        <DailyOperationsSummary
          metrics={dailyOperations}
          shifts={[]}
        />

        <CriticalRisksPanel risks={criticalRisks} />

      </section>

      <section
        aria-label="Canlı üretim operasyonları"
        className="command-center__operations-grid"
      >
        <MachineStatusPanel machines={machines} />

        <ProductionOrdersTable orders={productionOrders} />

        <LiveAlarmsPanel />
      </section>

      <section
        aria-label="Destek operasyonları"
        className="command-center__support-grid"
      >
        <MaterialRisksPanel risks={materialRisks} />

        <MoldStatusPanel molds={molds} />

        <MaintenanceStatusPanel items={[]} />

        <PlanConfidencePanel data={planConfidence} />
      </section>

      <section
        aria-label="Sistem sağlığı"
        className="command-center__health-strip"
      >
        <SystemHealthPanel />
      </section>

      <footer className="command-center__footer">
        <span>
          CrevaFactOS · Human-in-the-Loop Factory Operating
          System
        </span>

        <span>
          Önizleme verileri · Gerçek entegrasyonlar sonraki
          paketlerde bağlanacaktır
        </span>
      </footer>
    </main>
  );
}