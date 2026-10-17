"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

import { Button } from "@/components/ui/button";
import type { QualityNcrDetail } from "../domain/quality-control";
import type {
  closeQualityNcrAction,
  createQualityCorrectiveActionAction,
  decideQualityDispositionAction,
  placeQualityHoldAction,
  releaseQualityHoldAction,
  resolveQualityNcrAction,
} from "../server/quality-control-writer";
import { getQualityStatusLabel } from "../domain/quality-control";
import { customerMessage } from "@/lib/presentation/customer-message";

export type QualityNcrActionsProps = Readonly<{
  ncr: QualityNcrDetail;
  onPlaceHold: typeof placeQualityHoldAction;
  onReleaseHold: typeof releaseQualityHoldAction;
  onDecideDisposition: typeof decideQualityDispositionAction;
  onCreateCorrectiveAction: typeof createQualityCorrectiveActionAction;
  onResolve: typeof resolveQualityNcrAction;
  onClose: typeof closeQualityNcrAction;
}>;

export function QualityNcrActions({
  ncr,
  onPlaceHold,
  onReleaseHold,
  onDecideDisposition,
  onCreateCorrectiveAction,
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
  const closingHint = canResolve
    ? "Uygunsuzluk kaydını çözülmüş olarak işaretler; kapatma yalnızca çözümden sonra yapılabilir."
    : canClose
      ? "Kayıt çözüldü. Kapatma işlemi kaydı sonlandırır."
      : `Kayıt ${getQualityStatusLabel(ncr.status).toLocaleLowerCase("tr-TR")} durumunda; çözme ve kapatma kullanılamıyor.`;

  return (
    <section className="quality-section quality-section--wide quality-action-bar" aria-label="NCR işlemleri">
      <header className="quality-section__header">
        <h2>İşlemler</h2>
      </header>

      {message ? (
        <div role="alert" className="quality-error-panel">
          <p>{customerMessage(message)}</p>
        </div>
      ) : null}

      {/* The state-changing decision comes first and carries the page's primary emphasis. */}
      <div className="quality-action-bar__group quality-action-bar__group--closing">
        <div className="quality-action-bar__closing-copy">
          <strong>NCR durumu</strong>
          <p className="quality-action-bar__hint">{closingHint}</p>
        </div>
        <div className="quality-action-bar__buttons">
          <Button
            type="button"
            variant="primary"
            disabled={pending || !canResolve}
            onClick={() => void run(() => onResolve(ncr.id, version, "NCR çözüldü"))}
          >
            NCR&apos;yi Çöz
          </Button>
          <Button
            type="button"
            variant="danger"
            disabled={pending || !canClose}
            onClick={() => void run(() => onClose(ncr.id, version, "NCR kapatıldı"))}
          >
            NCR&apos;yi Kapat
          </Button>
        </div>
      </div>

      {!holdActive && ncr.status !== "closed" && ncr.status !== "cancelled" ? (
        <details className="quality-action-bar__group quality-action-disclosure">
          <summary>Blokeyi uygula</summary>
        <form
          className="quality-action-bar__form"
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
          <div className="quality-action-bar__row">
            <div className="quality-form-field">
              <label htmlFor="hold-scope">Kapsam</label>
              <select id="hold-scope" name="scope" className="quality-form-select" defaultValue="production-order">
                <option value="production-order">Üretim emri</option>
                <option value="inspection">Kontrol</option>
              </select>
            </div>
            <div className="quality-form-field">
              <label htmlFor="hold-reason">Neden <span className="zx-required" aria-hidden="true">*</span></label>
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
              Blokeyi uygula
            </Button>
          </div>
        </form>
        </details>
      ) : null}

      {holdActive ? (
        <details className="quality-action-bar__group quality-action-disclosure" open>
          <summary>Blokeyi kaldır</summary>
        <form
          className="quality-action-bar__form"
          onSubmit={(event) => {
            event.preventDefault();
            const formData = new FormData(event.currentTarget);
            void run(() =>
              onReleaseHold(ncr.id, version, String(formData.get("releaseReason") ?? "")),
            );
          }}
        >
          <div className="quality-action-bar__row">
            <div className="quality-form-field">
              <label htmlFor="release-reason">Serbest bırakma nedeni <span className="zx-required" aria-hidden="true">*</span></label>
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
              Blokeyi kaldır
            </Button>
          </div>
        </form>
        </details>
      ) : null}

      {ncr.dispositionStatus !== "decided" && ncr.status !== "closed" ? (
        <details className="quality-action-bar__group quality-action-disclosure">
          <summary>Uygunsuzluk kararı belirle</summary>
        <form
          className="quality-action-bar__form"
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
              <input id="disposition-note" name="note" type="text" className="quality-form-input" placeholder="Uygunsuzluk kararı notu" />
            </div>
            <Button type="submit" variant="secondary" disabled={pending}>
              Uygunsuzluk kararı belirle
            </Button>
          </div>
        </form>
        </details>
      ) : null}

      {ncr.status !== "closed" && ncr.status !== "cancelled" ? (
        <details className="quality-action-bar__group quality-action-disclosure">
          <summary>Düzeltici aksiyon ekle</summary>
        <form
          className="quality-action-bar__form"
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
          <div className="quality-form-grid">
            <div className="quality-form-field">
              <label htmlFor="ca-title">Başlık <span className="zx-required" aria-hidden="true">*</span></label>
              <input id="ca-title" name="title" type="text" className="quality-form-input" placeholder="Başlık" required />
            </div>
            <div className="quality-form-field">
              <label htmlFor="ca-description">Açıklama <span className="zx-required" aria-hidden="true">*</span></label>
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
              Düzeltici aksiyon ekle
            </Button>
          </div>
        </form>
        </details>
      ) : null}

    </section>
  );
}
