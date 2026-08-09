import Link from "next/link";
import {
  AlertTriangle,
  CheckCircle2,
  ClipboardCheck,
  ClipboardList,
  Plus,
  ShieldCheck,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import { getQualityStatusLabel } from "@/features/quality-control/domain/quality-control";
import { readQualityWorkspace } from "@/features/quality-control/server/quality-control-reader";

export default async function QualityControlPage() {
  const result = await readQualityWorkspace();

  if (result.status === "failure") {
    return (
      <main className="production-execution-page">
        <header className="production-execution-header">
          <p>KALİTE KONTROL</p>
          <h1>Kalite Kontrol</h1>
        </header>
        <div role="alert" className="state-panel error-state">
          <ShieldCheck aria-hidden="true" />
          <div>
            <h3>Kalite verisi yüklenemedi</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  const { summary, plans, inspections } = result;
  const activeInspections = inspections.filter(
    (item) => item.status === "draft" || item.status === "in-progress",
  );
  const recentInspections = [...inspections]
    .sort((a, b) => b.updatedAtUtc.localeCompare(a.updatedAtUtc))
    .slice(0, 8);

  return (
    <main className="production-execution-page">
      <header className="production-execution-header">
        <p>KALİTE KONTROL</p>
        <h1>Kalite Kontrol</h1>
        <span>
          Kontrol planları, aktif incelemeler ve kalite serbest bırakma durumunu tek
          operasyonel görünümde yönetin.
        </span>
        <div className="production-execution-header__actions">
          <Link href="/kalite/planlar/yeni" className="production-execution-header__link">
            <Plus aria-hidden="true" />
            Yeni Kontrol Planı
          </Link>
          <Link href="/kalite/kontroller/yeni" className="production-execution-header__link">
            <ClipboardCheck aria-hidden="true" />
            Yeni Kalite Kontrolü
          </Link>
        </div>
      </header>

      <section className="production-execution-kpis" aria-label="Kalite özeti">
        <Card className="production-execution-kpis__item--in-progress">
          <ClipboardList aria-hidden="true" />
          <div>
            <span>Açık Kontroller</span>
            <strong>{summary.openInspectionCount}</strong>
          </div>
        </Card>
        <Card className="production-execution-kpis__item--ready">
          <AlertTriangle aria-hidden="true" />
          <div>
            <span>İnceleme Bekleyenler</span>
            <strong>{summary.pendingReviewCount}</strong>
          </div>
        </Card>
        <Card className="production-execution-kpis__item--paused">
          <ShieldCheck aria-hidden="true" />
          <div>
            <span>Başarısız Kontroller</span>
            <strong>{summary.failedInspectionCount}</strong>
          </div>
        </Card>
        <Card className="production-execution-kpis__item--completed">
          <CheckCircle2 aria-hidden="true" />
          <div>
            <span>Serbest Bırakılanlar</span>
            <strong>{summary.releasedInspectionCount}</strong>
          </div>
        </Card>
      </section>

      <section className="production-execution-grid">
        <Card className="production-execution-panel">
          <header>
            <h2>Kontrol Planları</h2>
          </header>
          <ul className="production-execution-list">
            {plans.map((plan) => (
              <li key={plan.id}>
                <div>
                  <strong>{plan.code}</strong>
                  <span>{plan.name}</span>
                </div>
                <Badge tone={plan.status === "active" ? "success" : "neutral"}>
                  {getQualityStatusLabel(plan.status)}
                </Badge>
              </li>
            ))}
          </ul>
        </Card>

        <Card className="production-execution-panel">
          <header>
            <h2>Aktif Kontroller</h2>
          </header>
          <ul className="production-execution-list">
            {activeInspections.length === 0 ? (
              <li>Aktif kontrol yok.</li>
            ) : (
              activeInspections.map((inspection) => (
                <li key={inspection.id}>
                  <Link href={`/kalite/kontroller/${inspection.id}`}>
                    <strong>{inspection.inspectionNumber}</strong>
                    <span>{inspection.productionOrderReference ?? inspection.productionOrderId}</span>
                  </Link>
                  <Badge tone="information">{getQualityStatusLabel(inspection.status)}</Badge>
                </li>
              ))
            )}
          </ul>
        </Card>

        <Card className="production-execution-panel production-execution-panel--wide">
          <header>
            <h2>Son Kontroller</h2>
          </header>
          <ul className="production-execution-list">
            {recentInspections.map((inspection) => (
              <li key={inspection.id}>
                <Link href={`/kalite/kontroller/${inspection.id}`}>
                  <strong>{inspection.inspectionNumber}</strong>
                  <span>
                    {inspection.planCode} · {inspection.productReference ?? "—"}
                  </span>
                </Link>
                <Badge
                  tone={
                    inspection.overallResult === "fail"
                      ? "danger"
                      : inspection.releaseStatus === "released"
                        ? "success"
                        : "neutral"
                  }
                >
                  {getQualityStatusLabel(inspection.overallResult)}
                </Badge>
              </li>
            ))}
          </ul>
        </Card>
      </section>
    </main>
  );
}
