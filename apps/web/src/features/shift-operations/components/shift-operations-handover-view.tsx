
import { ClipboardCheck, ClipboardList, Layers } from "lucide-react";

import { EmptyPanel } from "@/components/ui/empty-panel";
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
import { formatPercent } from "@/lib/presentation/format";
import { enumLabel } from "@/lib/presentation/enum-labels";
import { deferredSurfacesEnabled } from "@/lib/runtime/deferred-surfaces";

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
  const showWorkforce = deferredSurfacesEnabled();
  const allItems = preview?.candidates.length ? preview.candidates : openItems;
  const items = showWorkforce ? allItems : allItems.filter((item) => item.category !== "digital-workforce");
  const grouped = HANDOVER_CATEGORIES.map((category) => ({
    category,
    items: items.filter((item) => item.category === category),
  })).filter((group) => group.items.length > 0);
  const categoryLabel = (category: string) =>
    (OPEN_ITEM_CATEGORY_LABELS as Record<string, string | undefined>)[category] ?? enumLabel(category);

  return (
    <>
      {shift ? (
        <p className="shift-ops-meta">
          <strong>{shift.name}</strong>
          <span className="shift-ops-status" data-tone={shift.status}>
            {SHIFT_STATUS_LABELS[shift.status as keyof typeof SHIFT_STATUS_LABELS] ?? shift.status}
          </span>
        </p>
      ) : null}

      <div className="shift-ops-handover-layout">
        <aside className="shift-ops-card" aria-label="Teslim kategorileri">
          <h2>Kategoriler</h2>
          {grouped.length === 0 ? (
            <EmptyPanel compact icon={<Layers />} title="Henüz kategori yok" description="Devredilecek kalem eklendiğinde kategoriler burada listelenir." />
          ) : (
            <ul className="shift-ops-item-list">
              {grouped.map((group) => (
                <li key={group.category}>
                  {categoryLabel(group.category)} ({group.items.length})
                </li>
              ))}
            </ul>
          )}
        </aside>

        <section className="shift-ops-card">
          <h2>Seçili Kalemler</h2>
          {items.length === 0 ? (
            <EmptyPanel
              compact
              icon={<ClipboardList />}
              title="Seçili kalem yok"
              description="Vardiya kapanırken devredilecek açık konular burada seçilir."
            />
          ) : null}
          <ul className="shift-ops-item-list">
            {items.map((item) => (
              <li className="shift-ops-item" data-priority={item.handoverPriority} key={item.id}>
                <strong>{item.title}</strong>
                <p>{item.description}</p>
                <p>
                  {categoryLabel(item.category)}
                  {" · "}
                  {(HANDOVER_PRIORITY_LABELS as Record<string, string | undefined>)[item.handoverPriority] ?? enumLabel(item.handoverPriority)}
                </p>
              </li>
            ))}
          </ul>
          {preview?.conflicts.length ? (
            <div role="alert">
              <h3>Çakışma uyarıları</h3>
              <ul>
                {preview.conflicts.map((conflict) => (
                  <li key={conflict.code}>{conflict.message}</li>
                ))}
              </ul>
            </div>
          ) : null}
        </section>

        <aside className="shift-ops-card" aria-label="Teslim hazırlığı">
          <h2>Teslim Hazırlığı</h2>
          {shift ? null : <p className="shift-ops-muted">Aktif vardiya seçilmedi.</p>}
          <div className="shift-ops-kpi">
            <p className="shift-ops-readiness">{shift || preview ? formatPercent(preview?.readinessScore ?? shift?.handoverReadinessScore ?? 0, 0) : "—"}</p>
            <p className="shift-ops-kpi__caption">Hazırlık puanı</p>
          </div>
          <h3 className="shift-ops-subhead">Sonraki vardiyanın bilmesi gerekenler</h3>
          {(preview?.nextShiftMustKnow ?? []).length === 0 ? (
            <EmptyPanel compact icon={<ClipboardCheck />} title="Not yok" description="Sonraki vardiya için işaretlenmiş bir not yok." />
          ) : (
            <ol className="shift-ops-must-know">
              {(preview?.nextShiftMustKnow ?? []).map((line) => (
                <li key={line}>{line}</li>
              ))}
            </ol>
          )}
        </aside>
      </div>
    </>
  );
}
