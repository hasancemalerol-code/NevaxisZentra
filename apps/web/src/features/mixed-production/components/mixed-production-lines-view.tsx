"use client";

import Link from "next/link";
import { ArrowRight, Factory } from "lucide-react";

import type { MixedProductionLineSummaryApi } from "../server/mixed-production-api-client";

type MixedProductionLinesViewProps = Readonly<{
  lines: readonly MixedProductionLineSummaryApi[];
}>;

export function MixedProductionLinesView({ lines }: MixedProductionLinesViewProps) {
  return (
    <main className="production-execution-page" data-testid="mixed-production-lines-view">
      <header className="production-execution-header">
        <p>ÜRETİM · KARMA HATLAR</p>
        <h1>Üretim Hatları</h1>
        <span>
          Otomatik, yarı otomatik ve insan operasyonlu istasyonları tek kanonik üretim akışında izleyin.
        </span>
        <Link href="/uretim" className="production-execution-header__link">
          Üretim Emirleri
        </Link>
      </header>

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
        <section className="production-execution-workspace__list" aria-label="Üretim hatları">
          {lines.map((line) => (
            <Link
              key={line.lineId}
              href={`/uretim/hatlar/${line.lineId}`}
              className="production-order-card"
              data-testid={`mixed-line-card-${line.code}`}
            >
              <div className="production-order-card__header">
                <div>
                  <p>{line.code}</p>
                  <h2>{line.name}</h2>
                </div>
                <span className="production-order-card__status">{line.lineState}</span>
              </div>
              <div className="production-order-card__meta">
                <span>Ürün: {line.currentProductCode ?? "—"}</span>
                <span>Emir: {line.currentOrderReference ?? "—"}</span>
                <span>GOOD: {line.goodOutputToday}</span>
                <span>REJECT: {line.rejectOutputToday}</span>
              </div>
              <div className="production-order-card__footer">
                Hat detayı
                <ArrowRight aria-hidden="true" size={16} />
              </div>
            </Link>
          ))}
        </section>
      )}
    </main>
  );
}
