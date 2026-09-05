import Link from "next/link";

import { Badge } from "@/components/ui/badge";
import { EmptyState } from "@/components/ui/states";
import { PageContent, PageHeader, Section } from "@/components/ui/page";
import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import {
  SHIFT_HANDOVER_SHIFT_PERIODS,
  SHIFT_HANDOVER_STATUSES,
} from "@/features/shift-handover/domain/shift-handover-constants";
import { ShiftHandoverCreateForm } from "@/features/shift-handover/components/shift-handover-create-form";
import { readShiftHandoverList } from "@/features/shift-handover/server/shift-handover-queries";
import "@/features/shift-handover/components/shift-handover.css";

export const dynamic = "force-dynamic";

function label<T extends Record<string, string>>(map: T, key: string): string {
  return map[key as keyof T] ?? key;
}

export default async function ShiftHandoverListPage() {
  await forbidUnlessGranted("shift-handover.read");
  const result = await readShiftHandoverList();

  if (result.status === "failure") {
    return (
      <PageContent>
        <EmptyState description={result.message} title="Vardiya devir listesi yüklenemedi" />
      </PageContent>
    );
  }

  const list = result.value;

  return (
    <PageContent className="shift-handover-shell" data-testid="shift-handover-list-page">
      <PageHeader
        description="Fabrika vardiya devir teslim kayıtları."
        eyebrow="Operasyon"
        title="Vardiya Devir Teslimi"
      />

      {list.capabilities.canManage ? (
        <Section title="Yeni Devir Oluştur">
          <ShiftHandoverCreateForm />
        </Section>
      ) : null}

      <Section title="Açık ve Güncel Devirler">
        {list.items.length === 0 ? (
          <EmptyState
            description="Henüz vardiya devri kaydı yok. Yeni devir oluşturarak başlayın."
            title="Kayıt bulunamadı"
          />
        ) : (
          <div className="shift-handover-items">
            {list.items.map((item) => (
              <Link className="shift-handover-item" href={`/vardiya-devir/${item.id}`} key={item.id}>
                <div className="shift-handover-item-header">
                  <div>
                    <strong>{item.handoverReference}</strong>
                    <div className="shift-handover-meta">
                      <span>{item.shiftDate} · {label(SHIFT_HANDOVER_SHIFT_PERIODS, item.shiftPeriod)}</span>
                      <span>{item.department}</span>
                      <span>{item.outgoingLabel} → {item.incomingLabel ?? "—"}</span>
                    </div>
                  </div>
                  <Badge tone="neutral">{label(SHIFT_HANDOVER_STATUSES, item.status)}</Badge>
                </div>
                <div className="shift-handover-meta" style={{ marginTop: "0.5rem" }}>
                  <span>Açık: {item.unresolvedItemCount}</span>
                  <span>Takip: {item.followUpItemCount}</span>
                </div>
              </Link>
            ))}
          </div>
        )}
      </Section>

      <p style={{ color: "var(--muted-foreground)", fontSize: "0.875rem" }}>
        {list.automationDisclaimer}
      </p>
    </PageContent>
  );
}
