import type {
  ShiftClosingPreviewApi,
  ShiftInstanceApiSummary,
  ShiftOpenItemApiSummary,
} from "../server/shift-operations-api-client";
import {
  HANDOVER_PRIORITY_LABELS,
  OPEN_ITEM_CATEGORY_LABELS,
  SHIFT_STATUS_LABELS,
} from "../domain/shift-operations-labels";

const HANDOVER_CATEGORIES = [
  "production", "machine", "maintenance", "quality", "material", "mold",
  "delivery", "approval", "digital-workforce", "task", "other",
] as const;

export function ShiftOperationsHandoverView({
  shift,
  openItems,
  preview,
}: Readonly<{
  shift: ShiftInstanceApiSummary | null;
  openItems: readonly ShiftOpenItemApiSummary[];
  preview: ShiftClosingPreviewApi | null;
}>) {
  const items = preview?.candidates.length ? preview.candidates : openItems;
  const grouped = HANDOVER_CATEGORIES.map((category) => ({
    category,
    items: items.filter((item) => item.category === category),
  })).filter((group) => group.items.length > 0);

  return (
    <>
      <header className="shift-ops-hero">
        <p className="shift-ops-hero__eyebrow">VARDİYA TESLİMİ</p>
        <h1>Vardiya Teslim Paketi</h1>
        <p>
          {shift
            ? `${shift.name} · ${SHIFT_STATUS_LABELS[shift.status as keyof typeof SHIFT_STATUS_LABELS] ?? shift.status}`
            : "Aktif vardiya seçilmedi"}
        </p>
      </header>

      <div className="shift-ops-handover-layout">
        <aside className="shift-ops-card">
          <h3>Kategoriler</h3>
          <ul className="shift-ops-item-list">
            {grouped.map((group) => (
              <li key={group.category}>
                {OPEN_ITEM_CATEGORY_LABELS[group.category] ?? group.category} ({group.items.length})
              </li>
            ))}
          </ul>
        </aside>

        <section className="shift-ops-card">
          <h3>Seçili Kalemler</h3>
          <ul className="shift-ops-item-list">
            {items.map((item) => (
              <li className="shift-ops-item" data-priority={item.handoverPriority} key={item.id}>
                <strong>{item.title}</strong>
                <p>{item.description}</p>
                <p>
                  {OPEN_ITEM_CATEGORY_LABELS[item.category] ?? item.category}
                  {" · "}
                  {HANDOVER_PRIORITY_LABELS[item.handoverPriority] ?? item.handoverPriority}
                </p>
              </li>
            ))}
          </ul>
          {preview?.conflicts.length ? (
            <div role="alert">
              <h4>Çakışma uyarıları</h4>
              <ul>
                {preview.conflicts.map((conflict) => (
                  <li key={conflict.code}>{conflict.message}</li>
                ))}
              </ul>
            </div>
          ) : null}
        </section>

        <aside className="shift-ops-card">
          <h3>Teslim Hazırlığı</h3>
          <p className="shift-ops-readiness">{preview?.readinessScore ?? shift?.handoverReadinessScore ?? 0}%</p>
          <h4>Sonraki Vardiyanın Bilmesi Gerekenler</h4>
          <ol className="shift-ops-must-know">
            {(preview?.nextShiftMustKnow ?? []).map((line) => (
              <li key={line}>{line}</li>
            ))}
          </ol>
          <p><em>Önizleme modu — iş kuralı değişikliği yapmaz.</em></p>
        </aside>
      </div>
    </>
  );
}
