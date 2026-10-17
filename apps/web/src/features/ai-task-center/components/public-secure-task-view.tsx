"use client";

import { useEffect, useState } from "react";
import { enumLabel } from "@/lib/presentation/enum-labels";

type SecureProjection = Readonly<{
  productName: string;
  taskTitle: string;
  safeSummary: string;
  priority: string;
  statusLabel: string;
  dueAt: string | null;
  facilityDisplayName: string | null;
  machineDisplayName: string | null;
  assignedDepartment: string | null;
  publicReference: string;
  issuedAt: string;
  expiresAt: string;
}>;

type ViewState = "loading" | "valid" | "expired" | "revoked" | "invalid" | "unavailable";

export function PublicSecureTaskView() {
  const [state, setState] = useState<ViewState>("loading");
  const [projection, setProjection] = useState<SecureProjection | null>(null);

  useEffect(() => {
    const resolveFromHash = () => {
      const hash = window.location.hash.startsWith("#access=")
        ? window.location.hash.slice("#access=".length)
        : "";
      const token = decodeURIComponent(hash);

      if (!token) {
        setState("invalid");
        return;
      }

      setState("loading");
      void (async () => {
        try {
          const response = await fetch("/api/public/ai-task-access/resolve", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ accessToken: token }),
            cache: "no-store",
          });

          if (!response.ok) {
            const payload = (await response.json().catch(() => null)) as
              | { code?: string }
              | null;
            if (payload?.code?.includes("expired")) {
              setState("expired");
            } else if (payload?.code?.includes("revoked")) {
              setState("revoked");
            } else {
              setState("invalid");
            }
            return;
          }

          const data = (await response.json()) as SecureProjection;
          setProjection(data);
          setState("valid");
        } catch {
          setState("unavailable");
        }
      })();
    };

    resolveFromHash();
    window.addEventListener("hashchange", resolveFromHash);
    return () => window.removeEventListener("hashchange", resolveFromHash);
  }, []);

  return (
    <main className="zx-public-page" id="main-content">
      <div className="zx-public-card">
        <header className="zx-public-header">
          <p className="zx-public-brand">Nevaxis Zentra</p>
          <h1>Güvenli Görev Paylaşımı</h1>
        </header>

        {state === "loading" ? (
          <div className="zx-public-state" role="status" aria-live="polite">
            <span className="skeleton zx-skeleton-line" aria-hidden="true" />
            <p>Bağlantı doğrulanıyor…</p>
          </div>
        ) : null}
        {state === "valid" && projection ? (
          <div className="zx-public-body">
            <dl className="zx-detail-list">
              <div>
                <dt>Referans</dt>
                <dd>{projection.publicReference}</dd>
              </div>
              <div>
                <dt>Öncelik</dt>
                <dd>{enumLabel(projection.priority)}</dd>
              </div>
              <div>
                <dt>Durum</dt>
                <dd>{projection.statusLabel}</dd>
              </div>
            </dl>
            <section aria-labelledby="public-task-title" className="zx-block">
              <h2 id="public-task-title" className="zx-panel__title">{projection.taskTitle}</h2>
              <p>{projection.safeSummary}</p>
              {projection.machineDisplayName ? <p className="zx-hint">{projection.machineDisplayName}</p> : null}
            </section>
            <div className="zx-notice zx-notice--warning">
              <div className="zx-notice__body">
                <p>Bu sayfa yalnızca görüntüleme amaçlıdır. Görev durumu değiştirilemez.</p>
              </div>
            </div>
            <p className="zx-hint">Bağlantı süresi: {new Date(projection.expiresAt).toLocaleString("tr-TR")}</p>
          </div>
        ) : null}
        {state === "expired" ? (
          <p className="zx-public-state" role="status">Bağlantının süresi doldu. Yeni bir bağlantı için görevi paylaşan kişiyle iletişime geçin.</p>
        ) : null}
        {state === "revoked" ? (
          <p className="zx-public-state" role="status">Bağlantı iptal edildi.</p>
        ) : null}
        {state === "invalid" ? (
          <p className="zx-public-state" role="status">Bağlantı geçersiz.</p>
        ) : null}
        {state === "unavailable" ? (
          <p className="zx-public-state" role="status">Geçici olarak kullanılamıyor. Lütfen biraz sonra yeniden deneyin.</p>
        ) : null}
      </div>
    </main>
  );
}
