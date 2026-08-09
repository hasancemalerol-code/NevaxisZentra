"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

import { Button } from "@/components/ui/button";
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
    <section className="quality-section quality-section--wide quality-action-bar" aria-label="NCR işlemleri">
      <header className="quality-section__header">
        <div>
          <h2>İşlemler</h2>
          <p>Sürüm: {version}</p>
        </div>
      </header>

      {message ? (
        <div role="alert" className="quality-error-panel">
          <p>{message}</p>
        </div>
      ) : null}

      {!holdActive && ncr.status !== "closed" && ncr.status !== "cancelled" ? (
        <form
          className="quality-action-bar__group"
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
          <strong>Kalite Blokesi Koy</strong>
          <div className="quality-action-bar__row">
            <div className="quality-form-field">
              <label htmlFor="hold-scope">Kapsam</label>
              <select id="hold-scope" name="scope" className="quality-form-select" defaultValue="production-order">
                <option value="production-order">Üretim emri</option>
                <option value="inspection">Kontrol</option>
              </select>
            </div>
            <div className="quality-form-field">
              <label htmlFor="hold-reason">Neden</label>
              <input
                id="hold-reason"
                name="reason"
                type="text"
                className="quality-form-input"
                placeholder="Blokaj nedeni"
                required
              />
            </div>
            <Button type="submit" variant="secondary" disabled={pending}>
              Kalite Blokesi Koy
            </Button>
          </div>
        </form>
      ) : null}

      {holdActive ? (
        <form
          className="quality-action-bar__group"
          onSubmit={(event) => {
            event.preventDefault();
            const formData = new FormData(event.currentTarget);
            void run(() =>
              onReleaseHold(ncr.id, version, String(formData.get("releaseReason") ?? "")),
            );
          }}
        >
          <strong>Blokeyi Kaldır</strong>
          <div className="quality-action-bar__row">
            <div className="quality-form-field">
              <label htmlFor="release-reason">Serbest bırakma nedeni</label>
              <input
                id="release-reason"
                name="releaseReason"
                type="text"
                className="quality-form-input"
                placeholder="Serbest bırakma nedeni"
                required
              />
            </div>
            <Button type="submit" variant="secondary" disabled={pending}>
              Blokeyi Kaldır
            </Button>
          </div>
        </form>
      ) : null}

      {ncr.dispositionStatus !== "decided" && ncr.status !== "closed" ? (
        <form
          className="quality-action-bar__group"
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
          <div className="quality-action-bar__row">
            <div className="quality-form-field">
              <label htmlFor="disposition-type">Karar</label>
              <select id="disposition-type" name="dispositionType" className="quality-form-select" defaultValue="rework">
                <option value="use-as-is">Olduğu Gibi Kullan</option>
                <option value="rework">Yeniden İşle</option>
                <option value="scrap">Hurda</option>
                <option value="return-to-supplier">Tedarikçiye İade</option>
                <option value="sort-or-screen">Ayır / Kontrol Et</option>
              </select>
            </div>
            <div className="quality-form-field">
              <label htmlFor="disposition-note">Not</label>
              <input id="disposition-note" name="note" type="text" className="quality-form-input" placeholder="Disposition notu" />
            </div>
            <Button type="submit" variant="secondary" disabled={pending}>
              Disposition Belirle
            </Button>
          </div>
        </form>
      ) : null}

      {ncr.status !== "closed" && ncr.status !== "cancelled" ? (
        <form
          className="quality-action-bar__group"
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
          <div className="quality-form-grid">
            <div className="quality-form-field">
              <label htmlFor="ca-title">Başlık</label>
              <input id="ca-title" name="title" type="text" className="quality-form-input" placeholder="Başlık" required />
            </div>
            <div className="quality-form-field">
              <label htmlFor="ca-description">Açıklama</label>
              <input id="ca-description" name="description" type="text" className="quality-form-input" placeholder="Açıklama" required />
            </div>
            <div className="quality-form-field">
              <label htmlFor="ca-owner">Sorumlu</label>
              <input id="ca-owner" name="ownerSubjectId" type="text" className="quality-form-input" placeholder="Opsiyonel" />
            </div>
            <div className="quality-form-field">
              <label htmlFor="ca-due">Termin</label>
              <input id="ca-due" name="dueAtUtc" type="datetime-local" className="quality-form-input" />
            </div>
          </div>
          <div className="quality-form-actions">
            <Button type="submit" variant="secondary" disabled={pending}>
              Düzeltici Aksiyon Ekle
            </Button>
          </div>
        </form>
      ) : null}

      {ncr.correctiveActions.map((action) => (
        <div key={action.id} className="quality-action-bar__group">
          <div className="quality-ca-item__body">
            <strong>{action.title}</strong>
            <span>{action.description}</span>
          </div>
          <div className="quality-action-bar__buttons">
            {action.status === "open" ? (
              <Button
                type="button"
                variant="secondary"
                size="small"
                disabled={pending}
                onClick={() => void run(() => onStartCorrectiveAction(action.id, action.version))}
              >
                Başlat
              </Button>
            ) : null}
            {action.status === "in-progress" ? (
              <Button
                type="button"
                variant="primary"
                size="small"
                disabled={pending}
                onClick={() =>
                  void run(() =>
                    onCompleteCorrectiveAction(action.id, action.version, "Tamamlandı"),
                  )
                }
              >
                Tamamla
              </Button>
            ) : null}
          </div>
        </div>
      ))}

      <div className="quality-action-bar__buttons">
        {canResolve ? (
          <Button
            type="button"
            variant="primary"
            disabled={pending}
            onClick={() => void run(() => onResolve(ncr.id, version, "NCR çözüldü"))}
          >
            Çöz
          </Button>
        ) : null}
        {canClose ? (
          <Button
            type="button"
            variant="danger"
            disabled={pending}
            onClick={() => void run(() => onClose(ncr.id, version, "NCR kapatıldı"))}
          >
            Kapat
          </Button>
        ) : null}
      </div>
    </section>
  );
}
