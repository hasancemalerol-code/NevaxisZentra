"use client";

import Link from "next/link";
import { ArrowRight, ClipboardList, Factory } from "lucide-react";

import { OperationalBadge, mapOperationalStatus } from "@/components/workspace/operational-status";
import { PageHeader } from "@/components/ui/page";
import { VisuallyHidden } from "@/components/ui/visually-hidden";
import { lineStateLabel } from "@/lib/presentation/throughput-labels";

import type { MixedProductionLineSummaryApi } from "../server/mixed-production-api-client";

import "./mixed-production.css";

type MixedProductionLinesViewProps = Readonly<{
  lines: readonly MixedProductionLineSummaryApi[];
}>;

export function MixedProductionLinesView({ lines }: MixedProductionLinesViewProps) {
  return (
    <div className="fact-workspace mlp-detail" data-testid="mixed-production-lines-view">
      <PageHeader
        eyebrow="ÜRETİM"
        title="Üretim Hatları"
        description="Otomatik, yarı otomatik ve operatörlü istasyonlar tek akışta"
        actions={
          <Link href="/uretim" className="button button-secondary button-medium"><ClipboardList aria-hidden="true" size={16} />
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
            Üretim hatları fabrika verisi veya entegrasyon bağlantısı kurulduğunda burada listelenir.
          </p>
          <Link href="/ayarlar/entegrasyonlar" className="button button-secondary button-medium">
            Veri ve entegrasyon ayarları
          </Link>
        </div>
      ) : (
        <section className="fact-production-board mlp-line-board" aria-label="Üretim hatları">
          {lines.map((line) => (
            <Link
              key={line.lineId}
              href={`/uretim/hatlar/${line.lineId}`}
              className="mlp-line-row"
              data-testid={`mixed-line-card-${line.code}`}
            >
              <div className="mlp-line-row__identity">
                <span className="mlp-line-row__code">{line.code}</span>
                <strong className="mlp-line-row__name">{line.name}</strong>
              </div>
              <dl className="mlp-line-row__facts">
                <div>
                  <dt>Ürün</dt>
                  <dd className={line.currentProductCode ? undefined : "mlp-line-row__placeholder"}>{line.currentProductCode ?? <><span aria-hidden="true">—</span><VisuallyHidden>Atanmadı</VisuallyHidden></>}</dd>
                </div>
                <div>
                  <dt>Emir</dt>
                  <dd className={line.currentOrderReference ? undefined : "mlp-line-row__placeholder"}>{line.currentOrderReference ?? <><span aria-hidden="true">—</span><VisuallyHidden>Atanmadı</VisuallyHidden></>}</dd>
                </div>
                <div>
                  <dt>İyi</dt>
                  <dd>{line.goodOutputToday}</dd>
                </div>
                <div>
                  <dt>Hatalı</dt>
                  <dd>{line.rejectOutputToday}</dd>
                </div>
              </dl>
              <div className="mlp-line-row__end">
                <OperationalBadge status={mapOperationalStatus(line.lineState)} label={lineStateLabel(line.lineState)} />
                <ArrowRight aria-hidden="true" size={16} />
              </div>
            </Link>
          ))}
        </section>
      )}
    </div>
  );
}
