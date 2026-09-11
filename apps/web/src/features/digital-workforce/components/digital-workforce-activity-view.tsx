"use client";

import Link from "next/link";

import { useDigitalWorkforceRuntime } from "../application/digital-workforce-runtime";
import { resolveWorkerUiId } from "../domain/digital-workforce-api-ids";
import { DemoBadge } from "./digital-workforce-primitives";

export function DigitalWorkforceActivityView() {
  const runtime = useDigitalWorkforceRuntime();
  const activity = runtime.activity;

  return (
    <>
      <header className="dwf-hero">
        <div>
          <p className="dwf-hero__eyebrow">DİJİTAL İŞ GÜCÜ</p>
          <h1>Aktivite</h1>
          <p>Ne yaptı? Kronolojik iş, aksiyon, neden, sonuç ve kanıt.</p>
        </div>
      </header>

      <section className="dwf-panel">
        <h2>Ne Yaptı?</h2>
        {activity.length === 0 ? (
          <p className="dwf-note">Henüz kayıtlı aktivite yok.</p>
        ) : (
          <ol className="dwf-timeline" style={{ marginTop: "0.7rem" }}>
            {activity.map((item) => {
              const workerUiId = resolveWorkerUiId(item.workerId);
              return (
                <li key={item.id}>
                  <strong>{new Date(item.createdAtUtc).toLocaleString("tr-TR")}</strong>
                  <div>
                    <p><strong>{item.actionType}</strong></p>
                    <p>{item.description}</p>
                    <p className="dwf-meta">
                      {item.workerCode} · Sonuç: {item.result}
                      {item.sourceClassification === "demo" ? " · Demo" : ""}
                    </p>
                    {workerUiId ? (
                      <Link className="dwf-btn dwf-btn--ghost" href={`/dijital-is-gucu/${workerUiId}`} style={{ marginTop: "0.4rem" }}>
                        Çalışanı aç
                      </Link>
                    ) : null}
                  </div>
                </li>
              );
            })}
          </ol>
        )}
      </section>

      {activity.some((item) => item.sourceClassification === "demo") ? (
        <p className="dwf-note" style={{ marginTop: "0.5rem" }}>
          <DemoBadge label="Demo kayıtlar dahil" />
        </p>
      ) : null}
    </>
  );
}
