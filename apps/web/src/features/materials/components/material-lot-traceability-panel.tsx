"use client";

import type { MaterialLotTraceability } from "../domain/materials";
import {
  formatMaterialDateOnly,
  formatMaterialDateTime,
  formatMaterialLotLocation,
  getMaterialLotCertificateTypeLabel,
  getMaterialLotEventTypeLabel,
  getMaterialLotQualityStatusLabel,
  getMaterialLotStatusLabel,
  MATERIAL_LOT_QUANTITY_DISCLAIMER,
} from "../presentation/material-labels";

type MaterialLotTraceabilityPanelProps = Readonly<{
  traceability: MaterialLotTraceability | null;
  lotNumber?: string;
}>;

export function MaterialLotTraceabilityPanel({
  traceability,
  lotNumber,
}: MaterialLotTraceabilityPanelProps) {
  if (!traceability || (!traceability.backward && !traceability.forward)) {
    return (
      <section className="material-lot-traceability" aria-label="Lot izlenebilirliği">
        <header className="material-lot-traceability__header">
          <p>İZLENEBİLİRLİK</p>
          <h3>Lot izlenebilirliği</h3>
          <span>{lotNumber ? `${lotNumber} için veri seçin veya yükleyin.` : "Lot seçiniz."}</span>
        </header>
      </section>
    );
  }

  const backward = traceability.backward;
  const forward = traceability.forward;

  return (
    <section className="material-lot-traceability" aria-label="Lot izlenebilirliği">
      <header className="material-lot-traceability__header">
        <div>
          <p>İZLENEBİLİRLİK</p>
          <h3>Lot izlenebilirlik zinciri</h3>
          <span>{lotNumber ?? forward?.lotNumber ?? backward?.lotNumber}</span>
        </div>
      </header>

      <div className="material-lot-traceability__grid">
        {backward ? (
          <article className="material-overview-card">
            <h4 className="material-overview-card__title">Geriye İzlenebilirlik</h4>
            <dl className="material-registry-meta-grid">
              <div>
                <dt>Malzeme</dt>
                <dd>
                  {backward.materialCode} · {backward.materialName}
                </dd>
              </div>
              <div>
                <dt>Tedarikçi/Üretici Lotu</dt>
                <dd>
                  {[backward.supplierLotNumber, backward.manufacturerLotNumber].filter(Boolean).join(" · ") ||
                    "—"}
                </dd>
              </div>
              <div>
                <dt>Mal Kabul</dt>
                <dd>{backward.goodsReceiptReference ?? "—"}</dd>
              </div>
              <div>
                <dt>İlk Konum</dt>
                <dd>
                  {formatMaterialLotLocation(
                    backward.initialWarehouseReference,
                    backward.initialLocationReference,
                  )}
                </dd>
              </div>
            </dl>
            {backward.certificates.length > 0 ? (
              <ul className="material-lot-document-list">
                {backward.certificates.map((certificate) => (
                  <li key={certificate.id}>
                    <strong>{getMaterialLotCertificateTypeLabel(certificate.certificateType)}</strong>
                    <span>{certificate.documentReference}</span>
                  </li>
                ))}
              </ul>
            ) : null}
            {backward.sourceEvents.length > 0 ? (
              <div className="material-registry-history">
                {backward.sourceEvents.map((entry) => (
                  <article key={entry.id} className="material-registry-history-item">
                    <strong>{getMaterialLotEventTypeLabel(entry.eventType)}</strong>
                    <small>{formatMaterialDateTime(entry.eventAtUtc)}</small>
                  </article>
                ))}
              </div>
            ) : null}
          </article>
        ) : null}

        {forward ? (
          <article className="material-overview-card">
            <h4 className="material-overview-card__title">İleriye İzlenebilirlik</h4>
            <dl className="material-registry-meta-grid">
              <div>
                <dt>Güncel Durum</dt>
                <dd>{getMaterialLotStatusLabel(forward.status)}</dd>
              </div>
              <div>
                <dt>Kalite</dt>
                <dd>{getMaterialLotQualityStatusLabel(forward.qualityStatus)}</dd>
              </div>
              <div>
                <dt>Güncel Konum</dt>
                <dd>
                  {formatMaterialLotLocation(
                    forward.currentWarehouseReference,
                    forward.currentLocationReference,
                  )}
                </dd>
              </div>
            </dl>
            {forward.locationTransfers.length > 0 ? (
              <div className="material-registry-history">
                <p className="material-form-section__label">Konum Transferleri</p>
                {forward.locationTransfers.map((entry) => (
                  <article key={entry.id} className="material-registry-history-item">
                    <strong>
                      {formatMaterialLotLocation(entry.fromWarehouseReference, entry.fromLocationReference)} →{" "}
                      {formatMaterialLotLocation(entry.toWarehouseReference, entry.toLocationReference)}
                    </strong>
                    <small>{formatMaterialDateTime(entry.eventAtUtc)}</small>
                  </article>
                ))}
              </div>
            ) : null}
            {forward.statusChanges.length > 0 ? (
              <div className="material-registry-history">
                <p className="material-form-section__label">Durum Değişiklikleri</p>
                {forward.statusChanges.map((entry) => (
                  <article key={entry.id} className="material-registry-history-item">
                    <strong>
                      {entry.statusBefore ? getMaterialLotStatusLabel(entry.statusBefore) : "—"} →{" "}
                      {entry.statusAfter ? getMaterialLotStatusLabel(entry.statusAfter) : "—"}
                    </strong>
                    {entry.reason ? <p>{entry.reason}</p> : null}
                  </article>
                ))}
              </div>
            ) : null}
            {forward.qualityChanges.length > 0 ? (
              <div className="material-registry-history">
                <p className="material-form-section__label">Kalite Değişiklikleri</p>
                {forward.qualityChanges.map((entry) => (
                  <article key={entry.id} className="material-registry-history-item">
                    <strong>
                      {entry.qualityStatusBefore
                        ? getMaterialLotQualityStatusLabel(entry.qualityStatusBefore)
                        : "—"}{" "}
                      →{" "}
                      {entry.qualityStatusAfter
                        ? getMaterialLotQualityStatusLabel(entry.qualityStatusAfter)
                        : "—"}
                    </strong>
                  </article>
                ))}
              </div>
            ) : null}
            {forward.certificates.length > 0 ? (
              <ul className="material-lot-document-list">
                {forward.certificates.map((certificate) => (
                  <li key={certificate.id}>
                    <strong>{getMaterialLotCertificateTypeLabel(certificate.certificateType)}</strong>
                    <span>{certificate.documentReference}</span>
                    {certificate.issuedDate ? <em>{formatMaterialDateOnly(certificate.issuedDate)}</em> : null}
                  </li>
                ))}
              </ul>
            ) : null}
          </article>
        ) : null}
      </div>

      <p className="material-lot-disclaimer">{MATERIAL_LOT_QUANTITY_DISCLAIMER}</p>
    </section>
  );
}
