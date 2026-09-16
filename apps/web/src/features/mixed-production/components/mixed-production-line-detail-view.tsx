"use client";

import Link from "next/link";
import { ArrowLeft, PlayCircle } from "lucide-react";

import type {
  MixedProductionLineDetailApi,
  MixedProductionPlaybackApi,
} from "../server/mixed-production-api-client";

type MixedProductionLineDetailViewProps = Readonly<{
  detail: MixedProductionLineDetailApi;
  playback: MixedProductionPlaybackApi | null;
}>;

function stateTone(state: string): string {
  if (state === "RUNNING") return "information";
  if (state === "FAULTED" || state === "BLOCKED") return "danger";
  if (state === "STARVED") return "warning";
  return "neutral";
}

export function MixedProductionLineDetailView({ detail, playback }: MixedProductionLineDetailViewProps) {
  const { line, stations, timeline } = detail;
  const latestPlaybackFrame = playback?.frames[playback.frames.length - 1] ?? null;

  return (
    <main className="production-execution-page" data-testid="mixed-production-line-detail-view">
      <header className="production-execution-header">
        <Link href="/uretim/hatlar" className="production-execution-header__link">
          <ArrowLeft aria-hidden="true" size={16} />
          Hatlar
        </Link>
        <p>ÜRETİM · HAT OPERASYONU</p>
        <h1>{line.name}</h1>
        <span>
          {line.code} · {line.currentProductCode ?? "—"} · {line.currentOrderReference ?? "—"} · Vardiya {line.currentShiftCode ?? "—"}
        </span>
      </header>

      <section className="production-execution-kpis" aria-label="Hat özet">
        <article className="production-execution-kpis__item--total">
          <div>
            <span>Hat Durumu</span>
            <strong>{line.lineState}</strong>
          </div>
        </article>
        <article className="production-execution-kpis__item--completed">
          <div>
            <span>GOOD (bugün)</span>
            <strong>{line.goodOutputToday}</strong>
          </div>
        </article>
        <article className="production-execution-kpis__item--paused">
          <div>
            <span>REJECT (bugün)</span>
            <strong>{line.rejectOutputToday}</strong>
          </div>
        </article>
      </section>

      <section className="production-execution-workspace" aria-label="Hat akış görünümü">
        <div className="production-execution-workspace__heading">
          <div>
            <p>AKIŞ GÖRÜNÜMÜ</p>
            <h2>İstasyon durumu</h2>
          </div>
        </div>

        <div
          style={{ display: "grid", gap: "0.75rem", gridTemplateColumns: "repeat(auto-fit, minmax(180px, 1fr))" }}
          data-testid="mixed-line-flow-view"
        >
          {stations.map((station) => (
            <article key={station.stationId} className="production-order-card">
              <div className="production-order-card__header">
                <div>
                  <p>{station.code}</p>
                  <h2>{station.name}</h2>
                </div>
                <span className={`production-order-card__status production-order-card__status--${stateTone(station.state)}`}>
                  {station.state}
                </span>
              </div>
              <div className="production-order-card__meta">
                <span>Kaynak: {station.resourceCode ?? "—"} ({station.resourceType ?? "—"})</span>
                <span>Operasyon: {station.operationCode ?? "—"}</span>
                {station.operatorCode ? <span>Operatör: {station.operatorCode}</span> : null}
                {station.bufferQuantity != null ? (
                  <span>WIP: {station.bufferQuantity}/{station.bufferCapacity ?? "—"}</span>
                ) : null}
                <span>GOOD: {station.goodCount} · REJECT: {station.rejectCount}</span>
              </div>
            </article>
          ))}
        </div>
      </section>

      <section className="production-execution-workspace" style={{ marginTop: "1.5rem" }} aria-label="Üretim zaman çizelgesi">
        <div className="production-execution-workspace__heading">
          <div>
            <p>ÜRETİM ZAMAN ÇİZELGESİ</p>
            <h2>Korelasyonlu olaylar</h2>
            <span>Makine, operatör, buffer, kalite ve üretim olayları tek zaman çizelgesinde.</span>
          </div>
        </div>

        {timeline.length === 0 ? (
          <p className="production-execution-empty__desc">Henüz olay kaydı yok.</p>
        ) : (
          <div style={{ display: "grid", gap: "0.5rem" }} data-testid="mixed-line-timeline">
            {timeline.map((entry) => (
              <article key={entry.eventId} className="production-order-card">
                <div className="production-order-card__meta">
                  <span>{new Date(entry.occurredAtUtc).toLocaleString("tr-TR")}</span>
                  <span>{entry.sourceCategory}</span>
                  <span>{entry.eventType}</span>
                  <span>{entry.summary}</span>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>

      <section className="production-execution-workspace" style={{ marginTop: "1.5rem" }} aria-label="Hat playback">
        <div className="production-execution-workspace__heading">
          <div>
            <p>PLAYBACK</p>
            <h2>Geçmiş pencere yeniden oluşturma</h2>
            <span>{detail.playbackWindowLabel}</span>
          </div>
          <PlayCircle aria-hidden="true" />
        </div>

        {!latestPlaybackFrame ? (
          <p className="production-execution-empty__desc">Playback için yeterli olay yok.</p>
        ) : (
          <div data-testid="mixed-line-playback">
            <p>
              Son kare: {new Date(latestPlaybackFrame.timestampUtc).toLocaleString("tr-TR")} · {latestPlaybackFrame.lineState}
            </p>
            <div style={{ display: "grid", gap: "0.5rem", marginTop: "0.75rem" }}>
              {latestPlaybackFrame.stations.map((station) => (
                <div key={station.stationCode} className="production-order-card__meta">
                  <span>{station.stationCode}</span>
                  <span>{station.state}</span>
                  {station.bufferQuantity != null ? <span>WIP: {station.bufferQuantity}</span> : null}
                  {station.operatorCode ? <span>Operatör: {station.operatorCode}</span> : null}
                </div>
              ))}
            </div>
          </div>
        )}
      </section>
    </main>
  );
}
