import {
  AlertTriangle,
  Bot,
  CalendarRange,
  CheckCircle2,
  Factory,
  Send,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";

import {
  calculateProductionPlanSummary,
} from "@/features/production-planning/application/production-plan-query";
import { ProductionPlanFilteredWorkspace } from "@/features/production-planning/components/production-plan-filtered-workspace";
import { readProductionPlanningWorkspace } from "@/features/production-planning/server/production-plan-reader";

export default async function ProductionPlanningPage() {
  const workspaceResult =
    await readProductionPlanningWorkspace();

  if (workspaceResult.status === "failure") {
    throw new Error(
      [
        "Production planning list could not be loaded.",
        workspaceResult.code,
        workspaceResult.message,
      ].join(" "),
    );
  }

  const productionPlans =
    workspaceResult.productionPlans;

  const summary =
    calculateProductionPlanSummary(
      productionPlans,
    );

  return (
    <main className="production-planning-page">
      <header className="production-planning-header">
        <p>Factory Operations Assistant</p>

        <h1>Üretim Planlama</h1>

        <span>
          Onaylı siparişler, ERP üretim talepleri,
          makine, kalıp, malzeme, operatör ve vardiya
          verileriyle açıklanabilir üretim planları
          oluşturun.
        </span>
      </header>

      <section className="production-planning-kpis">
        <Card>
          <Factory aria-hidden="true" />

          <div>
            <span>Toplam plan</span>
            <strong>{summary.total}</strong>
            <small>
              Aktif planlama çalışma alanı
            </small>
          </div>
        </Card>

        <Card>
          <AlertTriangle aria-hidden="true" />

          <div>
            <span>İnceleme gerekiyor</span>
            <strong>
              {summary.needsReview}
            </strong>
            <small>
              Kullanıcı müdahalesi bekliyor
            </small>
          </div>
        </Card>

        <Card>
          <CalendarRange aria-hidden="true" />

          <div>
            <span>Onay bekleyen</span>
            <strong>
              {summary.waitingForApproval}
            </strong>
            <small>
              Planlama yöneticisi kuyruğu
            </small>
          </div>
        </Card>

        <Card>
          <CheckCircle2 aria-hidden="true" />

          <div>
            <span>Onaylanan</span>
            <strong>{summary.approved}</strong>
            <small>
              Planlanabilir üretim planı
            </small>
          </div>
        </Card>

        <Card>
          <Send aria-hidden="true" />

          <div>
            <span>Aktarıma hazır</span>
            <strong>
              {summary.transferable}
            </strong>
            <small>
              MES veya ERP aktarımı bekliyor
            </small>
          </div>
        </Card>

        <Card>
          <Bot aria-hidden="true" />

          <div>
            <span>AI planı</span>
            <strong>
              {summary.aiGenerated}
            </strong>
            <small>
              AI tarafından oluşturulan taslak
            </small>
          </div>
        </Card>
      </section>

      <section className="production-planning-safety">
        <div>
          <Bot aria-hidden="true" />

          <div>
            <p>Human-in-the-Loop</p>

            <h2>
              AI üretim planını önerir, yetkili kullanıcı
              onaylar
            </h2>

            <span>
              Engelleyici kapasite, makine, kalıp,
              malzeme, operatör veya termin riski varken
              plan onaya gönderilemez. Onaylanan planlar
              yalnızca yetkili işlemle bağlı ERP veya MES
              sistemine aktarılır.
            </span>
          </div>
        </div>

        <Badge tone="ai">
          <Bot aria-hidden="true" />
          Açıklanabilir AI planlama
        </Badge>
      </section>

      <ProductionPlanFilteredWorkspace
        productionPlans={productionPlans}
      />
    </main>
  );
}