"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

import type { QualityNcrDetail } from "../domain/quality-control";
import type {
  closeQualityNcrAction,
  completeQualityCorrectiveActionAction,
  createQualityCorrectiveActionAction,
  decideQualityDispositionAction,
  placeQualityHoldAction,
  releaseQualityHoldAction,
  resolveQualityNcrAction,
  startQualityCorrectiveActionAction,
} from "../server/quality-control-writer";

export type QualityNcrActionsProps = Readonly<{
  ncr: QualityNcrDetail;
  onPlaceHold: typeof placeQualityHoldAction;
  onReleaseHold: typeof releaseQualityHoldAction;
  onDecideDisposition: typeof decideQualityDispositionAction;
  onCreateCorrectiveAction: typeof createQualityCorrectiveActionAction;
  onStartCorrectiveAction: typeof startQualityCorrectiveActionAction;
  onCompleteCorrectiveAction: typeof completeQualityCorrectiveActionAction;
  onResolve: typeof resolveQualityNcrAction;
  onClose: typeof closeQualityNcrAction;
}>;

export function QualityNcrActions({
  ncr,
  onPlaceHold,
  onReleaseHold,
  onDecideDisposition,
  onCreateCorrectiveAction,
  onStartCorrectiveAction,
  onCompleteCorrectiveAction,
  onResolve,
  onClose,
}: QualityNcrActionsProps) {
  const router = useRouter();
  const [version, setVersion] = useState(ncr.version);
  const [message, setMessage] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  async function run(action: () => Promise<{ status: string; message?: string; version?: number }>) {
    setPending(true);
    setMessage(null);
    const result = await action();
    setPending(false);
    if (result.status === "failure") {
      setMessage(result.message ?? "İşlem başarısız.");
      return;
    }
    if (result.version) {
      setVersion(result.version);
    }
    router.refresh();
  }

  const holdActive = ncr.holdStatus === "active" || ncr.activeHold?.status === "active";
  const canResolve = ncr.status !== "resolved" && ncr.status !== "closed" && ncr.status !== "cancelled";
  const canClose = ncr.status === "resolved";

  return (
    <section className="production-execution-panel" aria-label="NCR işlemleri">
      <header>
        <h2>İşlemler</h2>
        <p>Sürüm: {version}</p>
      </header>

      {message ? (
        <div role="alert" className="state-panel error-state">
          <p>{message}</p>
        </div>
      ) : null}

      {!holdActive && ncr.status !== "closed" && ncr.status !== "cancelled" ? (
        <form
          className="production-execution-form-row"
          onSubmit={(event) => {
            event.preventDefault();
            const formData = new FormData(event.currentTarget);
            void run(() =>
              onPlaceHold(
                ncr.id,
                version,
                String(formData.get("scope") ?? "production-order"),
                String(formData.get("reason") ?? ""),
              ),
            );
          }}
        >
          <strong>Hold Koy</strong>
          <select name="scope" defaultValue="production-order">
            <option value="production-order">Üretim emri</option>
            <option value="inspection">Kontrol</option>
          </select>
          <input name="reason" type="text" placeholder="Blokaj nedeni" required />
          <button type="submit" className="production-execution-action-button" disabled={pending}>
            Hold Koy
          </button>
        </form>
      ) : null}

      {holdActive ? (
        <form
          className="production-execution-form-row"
          onSubmit={(event) => {
            event.preventDefault();
            const formData = new FormData(event.currentTarget);
            void run(() =>
              onReleaseHold(ncr.id, version, String(formData.get("releaseReason") ?? "")),
            );
          }}
        >
          <strong>Hold Kaldır</strong>
          <input name="releaseReason" type="text" placeholder="Serbest bırakma nedeni" required />
          <button type="submit" className="production-execution-action-button" disabled={pending}>
            Hold Kaldır
          </button>
        </form>
      ) : null}

      {ncr.dispositionStatus !== "decided" && ncr.status !== "closed" ? (
        <form
          className="production-execution-form-row"
          onSubmit={(event) => {
            event.preventDefault();
            const formData = new FormData(event.currentTarget);
            void run(() =>
              onDecideDisposition(
                ncr.id,
                version,
                String(formData.get("dispositionType") ?? "pending"),
                String(formData.get("note") ?? "") || undefined,
              ),
            );
          }}
        >
          <strong>Disposition Belirle</strong>
          <select name="dispositionType" defaultValue="rework">
            <option value="use-as-is">Olduğu gibi kullan</option>
            <option value="rework">Yeniden işle</option>
            <option value="scrap">Hurda</option>
            <option value="return-to-supplier">Tedarikçiye iade</option>
            <option value="sort-or-screen">Ayıkla / ele</option>
          </select>
          <input name="note" type="text" placeholder="Disposition notu" />
          <button type="submit" className="production-execution-action-button" disabled={pending}>
            Disposition Belirle
          </button>
        </form>
      ) : null}

      {ncr.status !== "closed" && ncr.status !== "cancelled" ? (
        <form
          className="production-execution-form-row"
          onSubmit={(event) => {
            event.preventDefault();
            const formData = new FormData(event.currentTarget);
            void run(() =>
              onCreateCorrectiveAction(
                ncr.id,
                String(formData.get("title") ?? ""),
                String(formData.get("description") ?? ""),
                String(formData.get("ownerSubjectId") ?? "") || undefined,
                String(formData.get("dueAtUtc") ?? "") || undefined,
              ),
            );
          }}
        >
          <strong>Düzeltici Aksiyon Ekle</strong>
          <input name="title" type="text" placeholder="Başlık" required />
          <input name="description" type="text" placeholder="Açıklama" required />
          <input name="ownerSubjectId" type="text" placeholder="Sorumlu (opsiyonel)" />
          <input name="dueAtUtc" type="datetime-local" />
          <button type="submit" className="production-execution-action-button" disabled={pending}>
            Düzeltici Aksiyon Ekle
          </button>
        </form>
      ) : null}

      {ncr.correctiveActions.map((action) => (
        <div key={action.id} className="production-execution-form-row">
          <div>
            <strong>{action.title}</strong>
            <p>{action.description}</p>
            <p>Durum: {action.status}</p>
          </div>
          {action.status === "open" ? (
            <button
              type="button"
              className="production-execution-action-button"
              disabled={pending}
              onClick={() => void run(() => onStartCorrectiveAction(action.id, action.version))}
            >
              Başlat
            </button>
          ) : null}
          {action.status === "in-progress" ? (
            <button
              type="button"
              className="production-execution-action-button"
              disabled={pending}
              onClick={() =>
                void run(() =>
                  onCompleteCorrectiveAction(action.id, action.version, "Tamamlandı"),
                )
              }
            >
              Tamamla
            </button>
          ) : null}
        </div>
      ))}

      {canResolve ? (
        <button
          type="button"
          className="production-execution-action-button"
          disabled={pending}
          onClick={() => void run(() => onResolve(ncr.id, version, "NCR çözüldü"))}
        >
          Çöz
        </button>
      ) : null}

      {canClose ? (
        <button
          type="button"
          className="production-execution-action-button"
          disabled={pending}
          onClick={() => void run(() => onClose(ncr.id, version, "NCR kapatıldı"))}
        >
          Kapat
        </button>
      ) : null}
    </section>
  );
}
