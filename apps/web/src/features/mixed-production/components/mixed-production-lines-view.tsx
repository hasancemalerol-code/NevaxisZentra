"use client";

import Link from "next/link";
import { ArrowRight, Factory } from "lucide-react";

import { OperationalBadge, mapOperationalStatus } from "@/components/workspace/operational-status";
import { WorkspaceHeader } from "@/components/workspace/workspace-header";

import type { MixedProductionLineSummaryApi } from "../server/mixed-production-api-client";

type MixedProductionLinesViewProps = Readonly<{
  lines: readonly MixedProductionLineSummaryApi[];
}>;

export function MixedProductionLinesView({ lines }: MixedProductionLinesViewProps) {
  return (
    <main className="fact-workspace" data-testid="mixed-production-lines-view">
      <WorkspaceHeader
        context="ÜRETİM · KARMA HATLAR"
        title="Üretim Hatları"
        meta="Otomatik, yarı otomatik ve insan operasyonlu istasyonlar — tek kanonik akış"
        actions={
          <Link href="/uretim" className="button button-secondary button-medium">
            Üretim Emirleri
          </Link>
        }
      />

      {lines.length === 0 ? (
        <div className="production-execution-empty" role="status">
          <div className="production-execution-empty__icon">
            <Factory aria-hidden="true" />
          </div>
          <div>
            <p>HAT YOK</p>
            <h2>Henüz tanımlı üretim hattı bulunmuyor</h2>
          </div>
          <p className="production-execution-empty__desc">
            Geliştirme ortamında MIX-LINE-01 fixture seed endpoint ile deterministik hat oluşturulabilir.
          </p>
        </div>
      ) : (
        <section className="fact-production-board" aria-label="Üretim hatları">
          {lines.map((line) => (
            <Link
              key={line.lineId}
              href={`/uretim/hatlar/${line.lineId}`}
              className="fact-production-board__row"
              data-testid={`mixed-line-card-${line.code}`}
            >
              <div>
                <p className="fact-workspace__meta">{line.code}</p>
                <strong className="fact-workspace__title" style={{ fontSize: "var(--fact-text-object)" }}>{line.name}</strong>
              </div>
              <div className="fact-workspace__meta">
                <span>Ürün: {line.currentProductCode ?? "—"}</span>
                <span> · Emir: {line.currentOrderReference ?? "—"}</span>
                <span> · GOOD: {line.goodOutputToday}</span>
                <span> · REJECT: {line.rejectOutputToday}</span>
              </div>
              <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
                <OperationalBadge status={mapOperationalStatus(line.lineState)} label={line.lineState} />
                <ArrowRight aria-hidden="true" size={16} />
              </div>
            </Link>
          ))}
        </section>
      )}
    </main>
  );
}
