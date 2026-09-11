"use client";

import Link from "next/link";

import { useDigitalWorkforceRuntime } from "../application/digital-workforce-runtime";
import { resolveWorkerUiId } from "../domain/digital-workforce-api-ids";

const DECISION_LABELS = {
  approved: "Onaylandı",
  rejected: "Reddedildi",
  modified: "Değiştirildi — tekrar taslak",
} as const;

export function DigitalWorkforceApprovalsView() {
  const runtime = useDigitalWorkforceRuntime();
  const pending = runtime.approvals.filter((item) => item.status === "pending");
  const decided = runtime.approvals.filter((item) => item.status !== "pending");

  return (
    <>
      <header className="dwf-hero">
        <div>
          <p className="dwf-hero__eyebrow">DİJİTAL İŞ GÜCÜ</p>
          <h1>Bekleyen Onaylar</h1>
          <p>Dijital çalışanların uygulama, taslak ve eşik üstü işlemleri burada görünür. Gizli onay durumu yoktur.</p>
        </div>
        <div className="dwf-hero__count">
          <strong>{pending.length}</strong>
          <span>Bekleyen onay</span>
        </div>
      </header>

      {pending.length === 0 ? (
        <section className="dwf-panel">
          <h2>Bekleyen onay yok</h2>
          <p>Tüm görünür kararlar alındı.</p>
        </section>
      ) : null}

      {pending.map((item) => {
        const workerUiId = resolveWorkerUiId(item.workerId);
        return (
          <article className="dwf-panel dwf-approval" key={item.id}>
            <h2>{item.action}</h2>
            <p><strong>Dijital Çalışan:</strong> {item.workerCode}</p>
            <p><strong>İstenen aksiyon:</strong> {item.action}</p>
            <p><strong>Neden:</strong> {item.businessReason}</p>
            <p><strong>Etki:</strong> {item.impact}</p>
            <p><strong>Risk:</strong> {item.riskLevel}</p>
            {item.amount !== null ? <p><strong>Tutar / limit:</strong> ₺{item.amount.toFixed(2)}</p> : null}
            <p className="dwf-meta">
              Oluşturulma: {new Date(item.createdAtUtc).toLocaleString("tr-TR")}
              {item.expiresAtUtc ? ` · Son karar: ${new Date(item.expiresAtUtc).toLocaleString("tr-TR")}` : ""}
            </p>
            <div className="dwf-actions" style={{ marginTop: "0.75rem" }}>
              <button
                className="dwf-btn"
                onClick={() => runtime.decideApproval(item.id, "approved", item.concurrencyToken)}
                type="button"
              >
                Onayla
              </button>
              <button
                className="dwf-btn dwf-btn--ghost"
                onClick={() => runtime.decideApproval(item.id, "rejected", item.concurrencyToken)}
                type="button"
              >
                Reddet
              </button>
              <button
                className="dwf-btn dwf-btn--ghost"
                onClick={() => runtime.decideApproval(item.id, "modified", item.concurrencyToken)}
                type="button"
              >
                Değiştir
              </button>
              {workerUiId ? (
                <Link className="dwf-btn dwf-btn--ghost" href={`/dijital-is-gucu/${workerUiId}?case=${item.caseId}`}>
                  Çalışanı aç
                </Link>
              ) : null}
            </div>
          </article>
        );
      })}

      {decided.length > 0 ? (
        <section className="dwf-panel">
          <h2>Karar verilenler</h2>
          {decided.map((item) => (
            <article key={item.id} style={{ marginTop: "0.7rem" }}>
              <p><strong>{item.workerCode}</strong> · {item.action}</p>
              <p className="dwf-meta">
                {DECISION_LABELS[item.status as keyof typeof DECISION_LABELS] ?? item.status}
                {" · "}
                {new Date(item.createdAtUtc).toLocaleString("tr-TR")}
              </p>
            </article>
          ))}
        </section>
      ) : null}
    </>
  );
}
