import Link from "next/link";
import { notFound } from "next/navigation";

import { Badge } from "@/components/ui/badge";
import {
  PageContent,
  PageHeader,
  Section,
} from "@/components/ui/page";

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { TeamsHandoverActions } from "@/features/teams/components/teams-handover-actions";
import { TeamsHandoverForm } from "@/features/teams/components/teams-handover-form";
import {
  TEAMS_HANDOVER_AUDIT_EVENTS,
  TEAMS_HANDOVER_DISCLAIMER,
  TEAMS_HANDOVER_ITEM_KINDS,
  TEAMS_HANDOVER_SHIFT_TYPES,
  TEAMS_HANDOVER_STATUSES,
} from "@/features/teams/domain/teams-constants";
import { readTeamsHandover } from "@/features/teams/server/teams-queries";
import "@/features/teams/components/teams-workspace.css";
import { FailureStatePage } from "@/components/ui/failure-state";

export const dynamic = "force-dynamic";

type PageParams = Readonly<{ handoverId: string }>;

function statusLabel(status: string): string {
  return TEAMS_HANDOVER_STATUSES[status as keyof typeof TEAMS_HANDOVER_STATUSES] ?? status;
}

function shiftLabel(shiftType: string): string {
  return TEAMS_HANDOVER_SHIFT_TYPES[shiftType as keyof typeof TEAMS_HANDOVER_SHIFT_TYPES] ?? shiftType;
}

function itemKindLabel(itemKind: string): string {
  return TEAMS_HANDOVER_ITEM_KINDS[itemKind as keyof typeof TEAMS_HANDOVER_ITEM_KINDS] ?? itemKind;
}

function auditLabel(eventType: string): string {
  return TEAMS_HANDOVER_AUDIT_EVENTS[eventType as keyof typeof TEAMS_HANDOVER_AUDIT_EVENTS] ?? eventType;
}

export default async function TeamsHandoverDetailPage({
  params,
}: Readonly<{ params: Promise<PageParams> }>) {
  await forbidUnlessGranted("teams.handover.read");
  const { handoverId } = await params;
  const result = await readTeamsHandover(handoverId);

  if (result.status === "failure") {
    if (result.code === "not-found") {
      notFound();
    }

    return (
      <PageContent>
        <PageHeader
          description="Vardiya devri detayı."
          eyebrow="Operasyon iletişimi"
          title="Vardiya devri"
        />
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Vardiya devri yüklenemedi" inline />
      </PageContent>
    );
  }

  const detail = result.value;

  return (
    <PageContent data-testid="teams-handover-detail-page">
      <PageHeader
        description={`${detail.channelName} · ${detail.shiftDate} · ${shiftLabel(detail.shiftType)}`}
        eyebrow="Vardiya devri"
        title={`${detail.shiftDate} vardiya devri`}
        actions={<Badge tone="neutral">{statusLabel(detail.status)}</Badge>}
      />

      <Section title="Özet">
        <p style={{ whiteSpace: "pre-wrap" }} data-testid="teams-handover-summary">
          {detail.summary}
        </p>
        {detail.safetyNote ? (
          <p style={{ whiteSpace: "pre-wrap" }} data-testid="teams-handover-safety-note">
            Güvenlik notu: {detail.safetyNote}
          </p>
        ) : null}
        <p>Yazar: {detail.authorLabel}</p>
      </Section>

      <Section title="Maddeler">
        <ul className="teams-approval-list" data-testid="teams-handover-items">
          {detail.items.map((item) => (
            <li key={item.id} data-testid={`teams-handover-item-view-${item.id}`}>
              <strong>{itemKindLabel(item.itemKind)} · {item.title}</strong>
              <p style={{ whiteSpace: "pre-wrap", margin: "0.35rem 0 0" }}>{item.body}</p>
              {item.sourceKind ? (
                item.canOpen && item.deepLinkPath ? (
                  <Link href={item.deepLinkPath} data-testid={`teams-handover-item-source-${item.id}`}>
                    {item.sourceLabel ?? "Kaynağı aç"}
                  </Link>
                ) : (
                  <p data-testid={`teams-handover-item-source-unavailable-${item.id}`}>
                    {item.unavailableReason ?? "Kaynak bağlamına erişiminiz yok."}
                  </p>
                )
              ) : null}
            </li>
          ))}
        </ul>
      </Section>

      {detail.status === "draft" && detail.canEdit ? (
        <Section title="Taslağı düzenle">
          <TeamsHandoverForm
            channels={[{ id: detail.channelId, name: detail.channelName }]}
            handoverId={detail.id}
            initialItems={detail.items.map((item) => ({
              itemKind: item.itemKind,
              title: item.title,
              body: item.body,
              linkKind: item.messageId ? "message" as const : item.aiTaskId ? "task" as const : "none" as const,
              messageId: item.messageId ?? "",
              aiTaskId: item.aiTaskId ?? "",
            }))}
            initialSafetyNote={detail.safetyNote}
            initialShiftDate={detail.shiftDate}
            initialShiftType={detail.shiftType}
            initialSummary={detail.summary}
            lockedChannelId={detail.channelId}
            mode="edit"
          />
        </Section>
      ) : null}

      <Section title="Teslim alanlar">
        {detail.acknowledgements.length === 0 ? (
          <p data-testid="teams-handover-acks-empty">Henüz teslim alan yok.</p>
        ) : (
          <ul data-testid="teams-handover-acknowledgements">
            {detail.acknowledgements.map((ack) => (
              <li key={ack.tenantMembershipId}>
                {ack.displayLabel}
              </li>
            ))}
          </ul>
        )}
      </Section>

      <Section title="İşlemler">
        <TeamsHandoverActions
          canAcknowledge={detail.canAcknowledge}
          canPublish={detail.canPublish}
          handoverId={detail.id}
        />
      </Section>

      <Section title="Denetim geçmişi">
        <ul data-testid="teams-handover-audit">
          {detail.auditEvents.map((event) => (
            <li key={event.id}>
              {event.actorLabel}: {auditLabel(event.eventType)}
            </li>
          ))}
        </ul>
        <p className="teams-disclaimer">{TEAMS_HANDOVER_DISCLAIMER}</p>
      </Section>
    </PageContent>
  );
}
