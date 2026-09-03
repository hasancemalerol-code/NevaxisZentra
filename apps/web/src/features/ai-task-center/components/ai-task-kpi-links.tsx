"use client";

import type { ReactNode } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import {
  Clock3,
  ListChecks,
  ShieldAlert,
  TriangleAlert,
} from "lucide-react";

import { KpiCard } from "@/components/ui/card";

import type { AiTaskKpiSummary } from "../application/ai-task-query";
import {
  parseAiTaskCenterView,
  type AiTaskCenterView,
} from "../domain/ai-task-center-item";

type AiTaskKpiLinksProps = Readonly<{
  kpis: AiTaskKpiSummary;
  listPath?: string;
}>;

type AiTaskKpiFilterId =
  | "open"
  | "awaiting-approval"
  | "critical"
  | "overdue";

type AiTaskKpiLinkProps = Readonly<{
  active: boolean;
  ariaLabel: string;
  href: string;
  children: ReactNode;
}>;

type AiTaskKpiItem = Readonly<{
  id: AiTaskKpiFilterId;
  ariaLabel: string;
  label: string;
  detail: string;
  value: string;
  tone: "information" | "warning" | "neutral";
  icon: ReactNode;
  view: AiTaskCenterView;
}>;

const KPI_VIEW_MAP: Readonly<Record<AiTaskKpiFilterId, AiTaskCenterView>> = {
  open: "all-tasks",
  "awaiting-approval": "approvals-handovers",
  critical: "sla-risk",
  overdue: "overdue",
};

function AiTaskKpiLink({
  active,
  ariaLabel,
  href,
  children,
}: AiTaskKpiLinkProps) {
  return (
    <Link
      aria-current={active ? "page" : undefined}
      aria-label={ariaLabel}
      className="ai-task-kpi-link"
      data-active={active}
      href={href}
    >
      {children}
    </Link>
  );
}

function createKpiHref(listPath: string, view: AiTaskCenterView): string {
  return `${listPath}?gorunum=${view}`;
}

function isKpiActive(
  currentView: AiTaskCenterView,
  filterId: AiTaskKpiFilterId,
): boolean {
  return currentView === KPI_VIEW_MAP[filterId];
}

export function AiTaskKpiLinks({
  kpis,
  listPath = "/",
}: AiTaskKpiLinksProps) {
  const searchParams = useSearchParams();
  const currentView = parseAiTaskCenterView(searchParams.get("gorunum"));

  const items: readonly AiTaskKpiItem[] = [
    {
      id: "open",
      ariaLabel: "Açık görevleri görüntüle",
      label: "Açık görev",
      detail: `${kpis.total} toplam görev`,
      value: String(kpis.open),
      tone: "information",
      icon: <ListChecks />,
      view: KPI_VIEW_MAP.open,
    },
    {
      id: "awaiting-approval",
      ariaLabel: "Onay bekleyen görevleri görüntüle",
      label: "Onay bekleyen",
      detail: "İnsan kararı gereken görev",
      value: String(kpis.awaitingApproval),
      tone: "warning",
      icon: <Clock3 />,
      view: KPI_VIEW_MAP["awaiting-approval"],
    },
    {
      id: "critical",
      ariaLabel: "Kritik risk görevlerini görüntüle",
      label: "Kritik risk",
      detail: "Öncelikli müdahale",
      value: String(kpis.criticalRisk),
      tone: "neutral",
      icon: <ShieldAlert />,
      view: KPI_VIEW_MAP.critical,
    },
    {
      id: "overdue",
      ariaLabel: "Geciken görevleri görüntüle",
      label: "Geciken görev",
      detail: "SLA / son tarih aşımı",
      value: String(kpis.overdue),
      tone: "warning",
      icon: <TriangleAlert />,
      view: KPI_VIEW_MAP.overdue,
    },
  ];

  return (
    <div
      aria-label="AI görev merkezi özet göstergeleri"
      className="kpi-grid ai-task-kpi-links"
    >
      {items.map((item) => (
        <AiTaskKpiLink
          active={isKpiActive(currentView, item.id)}
          ariaLabel={item.ariaLabel}
          href={createKpiHref(listPath, item.view)}
          key={item.id}
        >
          <KpiCard
            detail={item.detail}
            icon={item.icon}
            label={item.label}
            tone={item.tone}
            value={item.value}
          />
        </AiTaskKpiLink>
      ))}
    </div>
  );
}
