"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { Info } from "lucide-react";
import { useState } from "react";

import { Button } from "@/components/ui/button";
import type {
  MaintenanceCostSummary,
  MaintenancePartUsage,
  MaintenanceTimelineEntry,
  MaintenanceWorkNote,
  MaintenanceWorkOrderDetail,
} from "../domain/maintenance";
import {
  formatMaintenanceCurrency,
  formatMaintenanceDateTime,
  getMaintenanceWorkNoteTypeLabel,
  localizeMaintenanceEventText,
} from "../presentation/maintenance-labels";
import type {
  createMaintenancePartUsageAction,
  createMaintenanceWorkNoteAction,
} from "../server/maintenance-writer";
import { customerMessage } from "@/lib/presentation/customer-message";
import { displayActor } from "@/lib/presentation/display-label";
import { enumLabel } from "@/lib/presentation/enum-labels";

const NOTE_TYPE_OPTIONS = [
  { value: "general", label: "Genel" },
  { value: "diagnosis", label: "Teşhis" },
  { value: "action", label: "İşlem" },
  { value: "observation", label: "Gözlem" },
  { value: "handover", label: "Devir" },
];

type MaintenanceWorkOrderNotesPanelProps = Readonly<{
  workOrderId: string;
  notes: readonly MaintenanceWorkNote[];
  onCreateNote: typeof createMaintenanceWorkNoteAction;
}>;

export function MaintenanceWorkOrderNotesPanel({
  workOrderId,
  notes,
  onCreateNote,
}: MaintenanceWorkOrderNotesPanelProps) {
  const router = useRouter();
  const [message, setMessage] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formData = new FormData(event.currentTarget);
    const noteType = String(formData.get("noteType") ?? "general");
    const text = String(formData.get("text") ?? "").trim();
    const isImportant = formData.get("isImportant") === "on";

    if (!text) {
      setMessage("Not metni zorunludur.");
      return;
    }

    setPending(true);
    setMessage(null);
    const result = await onCreateNote({ workOrderId, noteType, text, isImportant });
    setPending(false);

    if (result.status === "failure") {
      setMessage(result.message ?? "Not kaydedilemedi.");
      return;
    }

    event.currentTarget.reset();
    router.refresh();
  }

  return (
    <section className="maintenance-section maintenance-section--wide" aria-label="Bakım Notları">
      <header className="maintenance-section__header">
        <h2>Bakım Notları</h2>
      </header>
      <div className="maintenance-section__body">
        {notes.length === 0 ? (
          <p className="maintenance-empty-state">Henüz bakım notu eklenmemiş.</p>
        ) : (
          <ul className="maintenance-ca-list">
            {notes.map((note) => (
              <li key={note.id} className="maintenance-ca-item">
                <div className="maintenance-ca-item__body">
                  <strong>
                    {getMaintenanceWorkNoteTypeLabel(note.noteType)}
                    {note.isImportant ? " · Önemli" : ""}
                  </strong>
                  <p>{note.text}</p>
                  <div className="maintenance-ca-item__meta">
                    <span>{formatMaintenanceDateTime(note.createdAtUtc)}</span>
                    <span>{displayActor(note.createdBySubjectId)}</span>
                  </div>
                </div>
              </li>
            ))}
          </ul>
        )}

        {message ? (
          <div role="alert" className="maintenance-error-panel">
            <p>{customerMessage(message)}</p>
          </div>
        ) : null}

        <details className="maintenance-form-disclosure">
          <summary>Not ekle</summary>
          <form className="maintenance-form-stack" onSubmit={(event) => void handleSubmit(event)}>
            <div className="maintenance-form-grid">
              <div className="maintenance-form-field">
                <label htmlFor="noteType">Not Türü</label>
                <select
                  id="noteType"
                  name="noteType"
                  className="maintenance-form-select"
                  defaultValue="general"
                  disabled={pending}
                >
                  {NOTE_TYPE_OPTIONS.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </div>
              <div className="maintenance-form-field maintenance-form-field--check">
                <label htmlFor="isImportant" className="maintenance-form-check">
                  <input id="isImportant" name="isImportant" type="checkbox" disabled={pending} />
                  Önemli not olarak işaretle
                </label>
              </div>
            </div>
            <div className="maintenance-form-field">
              <label htmlFor="text">Not <span className="zx-required" aria-hidden="true">*</span></label>
              <textarea
                id="text"
                name="text"
                className="maintenance-form-textarea"
                placeholder="Bakım notu girin"
                disabled={pending}
                required
              />
            </div>
            <div className="maintenance-form-actions">
              <Button type="submit" variant="primary" disabled={pending}>
                Not Ekle
              </Button>
            </div>
          </form>
        </details>
      </div>
    </section>
  );
}

type MaintenanceWorkOrderPartsPanelProps = Readonly<{
  workOrderId: string;
  parts: readonly MaintenancePartUsage[];
  onCreatePart: typeof createMaintenancePartUsageAction;
}>;

export function MaintenanceWorkOrderPartsPanel({
  workOrderId,
  parts,
  onCreatePart,
}: MaintenanceWorkOrderPartsPanelProps) {
  const router = useRouter();
  const [message, setMessage] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formData = new FormData(event.currentTarget);
    const partReference = String(formData.get("partReference") ?? "").trim();
    const partNameRaw = String(formData.get("partName") ?? "").trim();
    const quantityRaw = String(formData.get("quantity") ?? "").trim();
    const unit = String(formData.get("unit") ?? "").trim();
    const actualUnitCostRaw = String(formData.get("actualUnitCost") ?? "").trim();

    if (!partReference || !unit || !quantityRaw) {
      setMessage("Parça referansı, miktar ve birim zorunludur.");
      return;
    }

    const quantity = Number(quantityRaw);
    if (Number.isNaN(quantity) || quantity <= 0) {
      setMessage("Geçerli bir miktar girin.");
      return;
    }

    const actualUnitCost =
      actualUnitCostRaw && !Number.isNaN(Number(actualUnitCostRaw))
        ? Number(actualUnitCostRaw)
        : undefined;

    setPending(true);
    setMessage(null);
    const result = await onCreatePart({
      workOrderId,
      partReference,
      partName: partNameRaw || undefined,
      quantity,
      unit,
      actualUnitCost,
    });
    setPending(false);

    if (result.status === "failure") {
      setMessage(result.message ?? "Parça kaydı oluşturulamadı.");
      return;
    }

    event.currentTarget.reset();
    router.refresh();
  }

  return (
    <section className="maintenance-section maintenance-section--wide" aria-label="Kullanılan Parçalar">
      <header className="maintenance-section__header">
        <div>
          <h2>Kullanılan Parçalar</h2>
          <p>Bu kayıt bakım kullanım bilgisidir; stok hareketi oluşturmaz.</p>
        </div>
      </header>
      <div className="maintenance-section__body">
        {parts.length === 0 ? (
          <p className="maintenance-empty-state">Henüz parça kullanım kaydı yok.</p>
        ) : (
          <div className="maintenance-table-wrap" tabIndex={0} role="region" aria-label="Kullanılan Parçalar (kaydırılabilir)">
            <table aria-label="Kullanılan Parçalar" className="maintenance-table maintenance-table--compact">
              <thead>
                <tr>
                  <th>Parça</th>
                  <th>Miktar</th>
                  <th>Birim Maliyet</th>
                  <th>Satır Maliyet</th>
                  <th>Kayıt</th>
                </tr>
              </thead>
              <tbody>
                {parts.map((part) => (
                  <tr key={part.id}>
                    <td>
                      <strong>{part.partReference}</strong>
                      {part.partName ? <small>{part.partName}</small> : null}
                    </td>
                    <td>
                      {part.quantity} {part.unit}
                    </td>
                    <td>{formatMaintenanceCurrency(part.actualUnitCost ?? part.estimatedUnitCost)}</td>
                    <td>{formatMaintenanceCurrency(part.actualLineCost ?? part.estimatedLineCost)}</td>
                    <td>
                      {formatMaintenanceDateTime(part.recordedAtUtc)}
                      <small>{displayActor(part.recordedBySubjectId)}</small>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {message ? (
          <div role="alert" className="maintenance-error-panel">
            <p>{customerMessage(message)}</p>
          </div>
        ) : null}

        <details className="maintenance-form-disclosure">
          <summary>Parça kullanımı ekle</summary>
          <form className="maintenance-form-stack" onSubmit={(event) => void handleSubmit(event)}>
            <div className="maintenance-form-grid">
              <div className="maintenance-form-field">
                <label htmlFor="partReference">Parça Referansı <span className="zx-required" aria-hidden="true">*</span></label>
                <input
                  id="partReference"
                  name="partReference"
                  className="maintenance-form-input"
                  disabled={pending}
                  required
                />
              </div>
              <div className="maintenance-form-field">
                <label htmlFor="partName">Parça Adı</label>
                <input
                  id="partName"
                  name="partName"
                  className="maintenance-form-input"
                  disabled={pending}
                />
              </div>
              <div className="maintenance-form-field">
                <label htmlFor="quantity">Miktar <span className="zx-required" aria-hidden="true">*</span></label>
                <input
                  id="quantity"
                  name="quantity"
                  type="number"
                  min="0"
                  step="any"
                  className="maintenance-form-input"
                  disabled={pending}
                  required
                />
              </div>
              <div className="maintenance-form-field">
                <label htmlFor="unit">Birim <span className="zx-required" aria-hidden="true">*</span></label>
                <input
                  id="unit"
                  name="unit"
                  className="maintenance-form-input"
                  defaultValue="adet"
                  disabled={pending}
                  required
                />
              </div>
              <div className="maintenance-form-field">
                <label htmlFor="actualUnitCost">Birim Maliyet</label>
                <input
                  id="actualUnitCost"
                  name="actualUnitCost"
                  type="number"
                  min="0"
                  step="any"
                  className="maintenance-form-input"
                  disabled={pending}
                />
              </div>
            </div>
            <div className="maintenance-form-actions">
              <Button type="submit" variant="primary" disabled={pending}>
                Parça Kullanımı Ekle
              </Button>
            </div>
          </form>
        </details>
      </div>
    </section>
  );
}

type MaintenanceWorkOrderCostSummaryPanelProps = Readonly<{
  costSummary: MaintenanceCostSummary | null;
  workOrder: MaintenanceWorkOrderDetail;
}>;

export function MaintenanceWorkOrderCostSummaryPanel({
  costSummary,
  workOrder,
}: MaintenanceWorkOrderCostSummaryPanelProps) {
  const externalServiceCost = costSummary?.externalServiceCost ?? workOrder.externalServiceCost;
  const otherCost = costSummary?.otherCost ?? workOrder.otherCost;
  const hasAnyCost = [
    costSummary?.estimatedPartsCost,
    costSummary?.actualPartsCost,
    externalServiceCost,
    otherCost,
    costSummary?.totalMaintenanceCost,
  ].some((value) => value !== undefined && value !== null);

  return (
    <section className="maintenance-section maintenance-section--wide" aria-label="Maliyet Özeti">
      <header className="maintenance-section__header">
        <h2>Maliyet Özeti</h2>
      </header>
      {hasAnyCost ? (
        <dl className="maintenance-detail-dl">
          <div>
            <dt>Tahmini Parça</dt>
            <dd>{formatMaintenanceCurrency(costSummary?.estimatedPartsCost)}</dd>
          </div>
          <div>
            <dt>Gerçekleşen Parça</dt>
            <dd>{formatMaintenanceCurrency(costSummary?.actualPartsCost)}</dd>
          </div>
          <div>
            <dt>Dış Hizmet</dt>
            <dd>{formatMaintenanceCurrency(externalServiceCost)}</dd>
          </div>
          <div>
            <dt>Diğer</dt>
            <dd>{formatMaintenanceCurrency(otherCost)}</dd>
          </div>
          <div>
            <dt>Toplam Maliyet</dt>
            <dd>{formatMaintenanceCurrency(costSummary?.totalMaintenanceCost)}</dd>
          </div>
        </dl>
      ) : (
        <p className="maintenance-empty-state">Henüz maliyet girilmedi.</p>
      )}
    </section>
  );
}

type MaintenanceWorkOrderTimelinePanelProps = Readonly<{
  timeline: readonly MaintenanceTimelineEntry[];
}>;

export function MaintenanceWorkOrderTimelinePanel({
  timeline,
}: MaintenanceWorkOrderTimelinePanelProps) {
  const rows = timeline.map((entry) => {
    const event = localizeMaintenanceEventText(entry.entryType) ?? enumLabel(entry.entryType);
    const title = localizeMaintenanceEventText(entry.title) ?? null;
    const detail = localizeMaintenanceEventText(entry.detail) ?? null;
    return { entry, event, title: title && title !== event ? title : null, detail };
  });
  const showTitle = rows.some((row) => row.title);
  const showDetail = rows.some((row) => row.detail);

  return (
    <section className="maintenance-section maintenance-section--wide" aria-label="Zaman Çizelgesi">
      <header className="maintenance-section__header">
        <h2>Zaman Çizelgesi</h2>
      </header>
      {timeline.length === 0 ? (
        <p className="maintenance-empty-state">Zaman çizelgesi kaydı bulunmuyor.</p>
      ) : (
        <div className="maintenance-table-wrap" tabIndex={0} role="region" aria-label="Zaman Çizelgesi (kaydırılabilir)">
          <table aria-label="Zaman Çizelgesi" className="maintenance-table maintenance-table--compact maintenance-table--timeline">
            <thead>
              <tr>
                <th>Zaman</th>
                <th>Olay</th>
                {showTitle ? <th>Başlık</th> : null}
                {showDetail ? <th>Detay</th> : null}
                <th>Aktör</th>
              </tr>
            </thead>
            <tbody>
              {rows.map(({ entry, event, title, detail }, index) => (
                <tr key={`${entry.occurredAtUtc}-${entry.entryType}-${index}`}>
                  <td data-label="Zaman">{formatMaintenanceDateTime(entry.occurredAtUtc)}</td>
                  <td data-label="Olay">{event}</td>
                  {showTitle ? <td data-label="Başlık" className="maintenance-table__cell--wrap">{title ?? "—"}</td> : null}
                  {showDetail ? <td data-label="Detay" className="maintenance-table__cell--wrap">{detail ?? "—"}</td> : null}
                  <td data-label="Aktör">{entry.actorSubjectId ? displayActor(entry.actorSubjectId) : "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}

type MaintenanceWorkOrderPreventiveSourcePanelProps = Readonly<{
  workOrder: MaintenanceWorkOrderDetail;
}>;

export function MaintenanceWorkOrderPreventiveSourcePanel({
  workOrder,
}: MaintenanceWorkOrderPreventiveSourcePanelProps) {
  const hasSource =
    workOrder.preventivePlanId ||
    workOrder.preventivePlanCode ||
    workOrder.preventiveDueId ||
    workOrder.type === "preventive";

  if (!hasSource) {
    return null;
  }

  return (
    <section className="maintenance-section maintenance-section--wide" aria-label="Periyodik Bakım Kaynağı">
      <header className="maintenance-section__header">
        <h2>Periyodik Bakım Kaynağı</h2>
      </header>
      <dl className="maintenance-detail-dl">
        <div>
          <dt>Plan Kodu</dt>
          <dd>{workOrder.preventivePlanCode ?? "Plan kodu girilmedi"}</dd>
        </div>
      </dl>
      <div className="maintenance-footer-note">
        <Info aria-hidden="true" />
        <span>
          Bu bakım emri periyodik bakım planından oluşturulmuş olabilir.{" "}
          <Link href="/bakim/planlama">Bakım planlamasını aç</Link>
        </span>
      </div>
    </section>
  );
}
