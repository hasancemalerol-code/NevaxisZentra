import Link from "next/link";
import { notFound } from "next/navigation";

import { Badge } from "@/components/ui/badge";
import {
  PageContent,
  PageHeader,
  Section,
} from "@/components/ui/page";
import { EmptyState } from "@/components/ui/states";
import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { TeamsApprovalDecisionForm } from "@/features/teams/components/teams-approval-decision-form";
import { TEAMS_APPROVAL_DISCLAIMER, TEAMS_APPROVAL_STATUSES } from "@/features/teams/domain/teams-constants";
import { readTeamsApproval, readTeamsWorkspace } from "@/features/teams/server/teams-queries";
import "@/features/teams/components/teams-workspace.css";

export const dynamic = "force-dynamic";

type PageParams = Readonly<{ requestId: string }>;

function statusLabel(status: string): string {
  return TEAMS_APPROVAL_STATUSES[status as keyof typeof TEAMS_APPROVAL_STATUSES] ?? status;
}

export default async function TeamsApprovalDetailPage({
  params,
}: Readonly<{ params: Promise<PageParams> }>) {
  await forbidUnlessGranted("teams.read");
  const { requestId } = await params;
  const result = await readTeamsApproval(requestId);

  if (result.status === "failure") {
    if (result.code === "not-found") {
      notFound();
    }

    return (
      <PageContent>
        <PageHeader
          description="Onay isteği detayı."
          eyebrow="Teams"
          title="Onay isteği"
        />
        <EmptyState description={result.message} title="Onay isteği yüklenemedi" />
      </PageContent>
    );
  }

  const detail = result.value;
  const workspace = await readTeamsWorkspace(detail.channelId);
  const assigned = new Set(detail.reviewers.map((reviewer) => reviewer.tenantMembershipId));
  const reviewerCandidates = workspace.status === "success"
    ? (workspace.selectedChannel?.members ?? [])
        .filter((member) => member.tenantMembershipId !== detail.requestedByTenantMembershipId)
        .filter((member) => !assigned.has(member.tenantMembershipId))
        .map((member) => ({
          tenantMembershipId: member.tenantMembershipId,
          displayLabel: member.displayLabel,
        }))
    : [];

  return (
    <PageContent data-testid="teams-approval-detail-page">
      <PageHeader
        description={detail.channelName}
        eyebrow="Teams onay isteği"
        title={detail.title}
        actions={<Badge tone="neutral">{statusLabel(detail.status)}</Badge>}
      />

      <Section title="Kaynak bağlam">
        <p data-testid="teams-approval-source-kind">
          {detail.source.sourceKind === "ai-task" ? "AI görevi" : "Teams mesajı"}
        </p>
        {detail.source.canOpen && detail.source.deepLinkPath ? (
          <Link href={detail.source.deepLinkPath} data-testid="teams-approval-source-link">
            {detail.source.label ?? "Kaynağı aç"}
          </Link>
        ) : (
          <p data-testid="teams-approval-source-unavailable">
            {detail.source.unavailableReason ?? "Kaynak bağlamına erişiminiz yok."}
          </p>
        )}
      </Section>

      <Section title="Gerekçe">
        <p style={{ whiteSpace: "pre-wrap" }}>{detail.rationale}</p>
        <p>İsteyen: {detail.requestedByLabel}</p>
      </Section>

      <Section title="İnceleyenler">
        <ul>
          {detail.reviewers.map((reviewer) => (
            <li key={reviewer.tenantMembershipId}>
              {reviewer.displayLabel} — {reviewer.role === "final-reviewer" ? "Nihai inceleyen" : "İnceleyen"}
            </li>
          ))}
        </ul>
      </Section>

      {detail.decision ? (
        <Section title="Karar">
          <p data-testid="teams-approval-recorded-decision">
            {statusLabel(detail.decision.decision)} — {detail.decision.reviewerLabel}
          </p>
          {detail.decision.note ? <p>{detail.decision.note}</p> : null}
        </Section>
      ) : null}

      <Section title="İşlemler">
        <TeamsApprovalDecisionForm
          canCancel={detail.canCancel}
          canDecide={detail.canDecide}
          canManageReviewers={detail.canManageReviewers}
          requestId={detail.id}
          reviewerCandidates={reviewerCandidates}
        />
      </Section>

      <Section title="Denetim geçmişi">
        <ul data-testid="teams-approval-audit">
          {detail.auditEvents.map((event) => (
            <li key={event.id}>
              {event.actorLabel}: {event.eventType}
            </li>
          ))}
        </ul>
        <p className="teams-disclaimer">{TEAMS_APPROVAL_DISCLAIMER}</p>
      </Section>
    </PageContent>
  );
}
