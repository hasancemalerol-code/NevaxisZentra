import Link from "next/link";
import { AiTaskCenterSummary } from "@/components/dashboard/ai-task-center-summary";
import { CriticalRisksPanel } from "@/components/dashboard/critical-risks-panel";
import { DailyOperationsSummary } from "@/components/dashboard/daily-operations-summary";
import { ActiveFactoryCasesDashboardPanel } from "@/components/dashboard/active-factory-cases-panel";
import { ActiveShiftDashboardPanel } from "@/components/dashboard/active-shift-panel";
import { DigitalWorkforceDashboardPanel } from "@/components/dashboard/digital-workforce-panel";
import { DashboardMetrics } from "@/components/dashboard/dashboard-metrics";
import { LiveAlarmsPanel } from "@/components/dashboard/live-alarms-panel";
import { MachineStatusPanel } from "@/components/dashboard/machine-status-panel";
import { MaintenanceStatusPanel } from "@/components/dashboard/maintenance-status-panel";
import { MaterialRisksPanel } from "@/components/dashboard/material-risks-panel";
import { MoldStatusPanel } from "@/components/dashboard/mold-status-panel";
import { PlanConfidencePanel } from "@/components/dashboard/plan-confidence-panel";
import { PiyasaOzetiPanel } from "@/components/dashboard/piyasa-ozeti-panel";
import { ProductionOrdersTable } from "@/components/dashboard/production-orders-table";
import { QuickActionsPanel } from "@/components/dashboard/quick-actions-panel";
import { DashboardShowcaseTvAction } from "@/components/dashboard/dashboard-showcase-tv-action";
import { SystemHealthPanel } from "@/components/dashboard/system-health-panel";
import { fetchCurrentEffectiveAccess } from "@/features/admin-access/server/admin-access-effective-access";
import { dashboardPreviewData } from "@/lib/mock-data/dashboard";

export default async function DashboardPage() {
  const access = await fetchCurrentEffectiveAccess();
  const grantedPermissionKeys = access?.grantedPermissionKeys ?? [];
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
    maintenance,
  } = dashboardPreviewData;

  return (
    <main className="command-center">
      <header className="command-center__titlebar">
        <div className="command-center__titlebar-copy">
          <p className="command-center__eyebrow">
            NEVAXIS FACTOS Operasyon Merkezi
          </p>

          <h1>AI Görev Merkezi</h1>

          <p className="command-center__description">
            Üretim, planlama, risk ve sistem durumlarını tek
            merkezden yönetin.
          </p>
        </div>

        <div className="command-center__titlebar-actions">
          <button
              aria-label="AI'ya Görev Ver. AI görev oluşturma akışı henüz kullanıma hazır değil."
              className="command-center__ai-task-button"
              disabled
              title="AI görev oluşturma akışı henüz kullanıma hazır değil."
              type="button"
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
                <span>AI görev oluşturma akışı henüz kullanıma hazır değil.</span>
              </span>
            </button>
            <DashboardShowcaseTvAction />
            <Link
              aria-label="FACTOS Teams çalışma alanını aç"
              className="command-center__teams-button"
              href="/teams"
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
                <strong>FACTOS Teams</strong>
                <span>Yeni ekip mesajları</span>
              </span>

              <span
                aria-label="3 okunmamış mesaj"
                className="command-center__teams-button-badge"
              >
                3
              </span>
            </Link>

            <div className="command-center__titlebar-context-strip">
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

              <PiyasaOzetiPanel />
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
        <QuickActionsPanel
          grantedPermissionKeys={grantedPermissionKeys}
        />
      </section>

      <section
        aria-label="Dijital İş Gücü"
        className="command-center__digital-workforce"
      >
        <ActiveShiftDashboardPanel />
        <ActiveFactoryCasesDashboardPanel />
        <DigitalWorkforceDashboardPanel />
      </section>

      <section
        aria-label="Karar ve risk merkezi"
        className="command-center__decision-grid"
      >
        <AiTaskCenterSummary
          grantedPermissionKeys={grantedPermissionKeys}
          tasks={aiTasks}
        />

        <DailyOperationsSummary
          grantedPermissionKeys={grantedPermissionKeys}
          metrics={dailyOperations}
          shifts={[]}
        />

        <CriticalRisksPanel
          grantedPermissionKeys={grantedPermissionKeys}
          risks={criticalRisks}
        />

      </section>

      <section
        aria-label="Canlı üretim operasyonları"
        className="command-center__operations-grid"
      >
        <MachineStatusPanel
          grantedPermissionKeys={grantedPermissionKeys}
          machines={machines}
        />

        <ProductionOrdersTable
          grantedPermissionKeys={grantedPermissionKeys}
          orders={productionOrders}
        />

        <LiveAlarmsPanel />
      </section>

      <section
        aria-label="Destek operasyonları"
        className="command-center__support-grid"
      >
        <MaterialRisksPanel
          grantedPermissionKeys={grantedPermissionKeys}
          risks={materialRisks}
        />

        <MoldStatusPanel
          grantedPermissionKeys={grantedPermissionKeys}
          molds={molds}
        />

        <MaintenanceStatusPanel
          grantedPermissionKeys={grantedPermissionKeys}
          items={maintenance}
        />

        <PlanConfidencePanel
          data={planConfidence}
          grantedPermissionKeys={grantedPermissionKeys}
        />
      </section>

      <section
        aria-label="Sistem sağlığı"
        className="command-center__health-strip"
      >
        <SystemHealthPanel />
      </section>

      <footer className="command-center__footer">
        <span>
          NEVAXIS FACTOS · Human-in-the-Loop Factory Operating
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