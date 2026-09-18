"use client";

import { useEffect, useState } from "react";

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
    <main className="min-h-screen bg-[#0b1220] text-white">
      <div className="mx-auto flex min-h-screen max-w-lg items-center px-4 py-10">
        <div className="w-full rounded-2xl border border-white/10 bg-white/5 p-6 shadow-2xl backdrop-blur">
          <div className="mb-6 text-center">
            <p className="text-xs uppercase tracking-[0.2em] text-white/60">Nevaxis Zentra</p>
            <h1 className="mt-2 text-2xl font-semibold">Güvenli Görev Paylaşımı</h1>
          </div>

          {state === "loading" ? (
            <p className="text-center text-white/70">Yükleniyor…</p>
          ) : null}
          {state === "valid" && projection ? (
            <div className="space-y-4">
              <div>
                <p className="text-sm text-white/60">Referans</p>
                <p className="font-medium">{projection.publicReference}</p>
              </div>
              <div>
                <h2 className="text-xl font-semibold">{projection.taskTitle}</h2>
                <p className="mt-2 text-sm text-white/80">{projection.safeSummary}</p>
              </div>
              <div className="grid grid-cols-2 gap-3 text-sm">
                <div>
                  <p className="text-white/60">Öncelik</p>
                  <p>{projection.priority}</p>
                </div>
                <div>
                  <p className="text-white/60">Durum</p>
                  <p>{projection.statusLabel}</p>
                </div>
              </div>
              {projection.machineDisplayName ? (
                <p className="text-sm text-white/70">{projection.machineDisplayName}</p>
              ) : null}
              <p className="rounded-lg border border-amber-400/30 bg-amber-400/10 p-3 text-sm text-amber-100">
                Bu sayfa yalnızca görüntüleme amaçlıdır. Görev durumu değiştirilemez.
              </p>
              <p className="text-xs text-white/50">
                Bağlantı süresi: {new Date(projection.expiresAt).toLocaleString("tr-TR")}
              </p>
            </div>
          ) : null}
          {state === "expired" ? (
            <p className="text-center text-white/80">Bağlantının süresi doldu.</p>
          ) : null}
          {state === "revoked" ? (
            <p className="text-center text-white/80">Bağlantı iptal edildi.</p>
          ) : null}
          {state === "invalid" ? (
            <p className="text-center text-white/80">Bağlantı geçersiz.</p>
          ) : null}
          {state === "unavailable" ? (
            <p className="text-center text-white/80">Geçici olarak kullanılamıyor.</p>
          ) : null}
        </div>
      </div>
    </main>
  );
}
