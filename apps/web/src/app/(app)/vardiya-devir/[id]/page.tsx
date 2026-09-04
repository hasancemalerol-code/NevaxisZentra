import Link from "next/link";
import { notFound } from "next/navigation";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/states";
import { PageContent, PageHeader, Section } from "@/components/ui/page";
import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import {
  SHIFT_HANDOVER_AUDIT_EVENTS,
  SHIFT_HANDOVER_ITEM_CATEGORIES,
  SHIFT_HANDOVER_ITEM_SEVERITIES,
  SHIFT_HANDOVER_SHIFT_PERIODS,
  SHIFT_HANDOVER_STATUSES,
} from "@/features/shift-handover/domain/shift-handover-constants";
import {
  ShiftHandoverFollowUpTasks,
  ShiftHandoverLifecycleActions,
} from "@/features/shift-handover/components/shift-handover-actions";
import { addShiftHandoverItemAction } from "@/features/shift-handover/server/shift-handover-actions";
import { readShiftHandoverDetail } from "@/features/shift-handover/server/shift-handover-queries";
import "@/features/shift-handover/components/shift-handover.css";

export const dynamic = "force-dynamic";

type PageParams = Readonly<{ id: string }>;

function label<T extends Record<string, string>>(map: T, key: string): string {
  return map[key as keyof T] ?? key;
}

export default async function ShiftHandoverDetailPage({
  params,
}: Readonly<{ params: Promise<PageParams> }>) {
  await forbidUnlessGranted("shift-handover.read");
  const { id } = await params;
  const result = await readShiftHandoverDetail(id);

  if (result.status === "failure") {
    if (result.code === "not-found") notFound();
    return (
      <PageContent>
        <EmptyState description={result.message} title="Vardiya devri yüklenemedi" />
      </PageContent>
    );
  }

  const detail = result.value;

  return (
    <PageContent className="shift-handover-shell" data-testid="shift-handover-detail-page">
      <PageHeader
        description={`${detail.facilityName} · ${detail.department}`}
        eyebrow="Vardiya Devir Teslimi"
        title={`${detail.handoverReference} · ${detail.shiftDate}`}
        actions={<Badge tone="neutral">{label(SHIFT_HANDOVER_STATUSES, detail.status)}</Badge>}
      />

      <Section title="Özet Bilgiler">
        <div className="shift-handover-meta">
          <span>Vardiya: {label(SHIFT_HANDOVER_SHIFT_PERIODS, detail.shiftPeriod)}</span>
          <span>Devreden: {detail.outgoingLabel}</span>
          <span>Devralan: {detail.incomingLabel ?? "Belirtilmedi"}</span>
          <span>Açık madde: {detail.items.filter((i) => i.status === "open").length}</span>
        </div>
        <p style={{ whiteSpace: "pre-wrap", marginTop: "0.75rem" }}>{detail.summary || "Özet girilmedi."}</p>
        <ShiftHandoverLifecycleActions detail={detail} />
      </Section>

      <Section title="Operasyonel Maddeler">
        <div className="shift-handover-items">
          {detail.items.length === 0 ? (
            <EmptyState description="Henüz madde eklenmedi." title="Madde yok" />
          ) : (
            detail.items.map((item) => (
              <div className="shift-handover-item" data-testid="shift-handover-item" key={item.id}>
                <div className="shift-handover-item-header">
                  <div>
                    <strong>{item.title}</strong>
                    <div className="shift-handover-meta">
                      <span>{label(SHIFT_HANDOVER_ITEM_CATEGORIES, item.category)}</span>
                      <span>{label(SHIFT_HANDOVER_ITEM_SEVERITIES, item.severity)}</span>
                      <span>{item.status}</span>
                    </div>
                  </div>
                </div>
                <p style={{ marginTop: "0.5rem", whiteSpace: "pre-wrap" }}>{item.description}</p>
              </div>
            ))
          )}
        </div>

        {detail.capabilities.canManage && (detail.status === "draft" || detail.status === "ready-for-handover") ? (
          <form
            action={async (formData) => {
              await addShiftHandoverItemAction(detail.id, formData);
            }}
            className="shift-handover-form-grid"
            style={{ marginTop: "1rem" }}
          >
            <label>
              Kategori
              <select defaultValue="general" name="category">
                {Object.entries(SHIFT_HANDOVER_ITEM_CATEGORIES).map(([value, text]) => (
                  <option key={value} value={value}>{text}</option>
                ))}
              </select>
            </label>
            <label>
              Başlık
              <input name="title" required />
            </label>
            <label>
              Açıklama
              <textarea name="description" rows={3} />
            </label>
            <label>
              Öncelik
              <select defaultValue="medium" name="severity">
                {Object.entries(SHIFT_HANDOVER_ITEM_SEVERITIES).map(([value, text]) => (
                  <option key={value} value={value}>{text}</option>
                ))}
              </select>
            </label>
            <label>
              <input name="requiresFollowUp" type="checkbox" /> Takip gerektirir
            </label>
            <Button type="submit">Madde Ekle</Button>
          </form>
        ) : null}
      </Section>

      <Section title="Takip Görevleri">
        <ShiftHandoverFollowUpTasks detail={detail} />
      </Section>

      <Section title="Zaman Çizelgesi">
        <div className="shift-handover-timeline">
          {detail.events.map((event) => (
            <div key={event.id}>
              <strong>{label(SHIFT_HANDOVER_AUDIT_EVENTS, event.eventType)}</strong>
              {" · "}
              {event.actorLabel}
              {" · "}
              {new Date(event.occurredAt).toLocaleString("tr-TR")}
            </div>
          ))}
        </div>
      </Section>

      <p style={{ color: "var(--muted-foreground)", fontSize: "0.875rem" }}>
        {detail.automationDisclaimer}
      </p>
      <Link href="/vardiya-devir">← Listeye dön</Link>
    </PageContent>
  );
}
