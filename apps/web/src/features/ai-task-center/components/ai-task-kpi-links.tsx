"use client";

import type { ReactNode } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import {
  Clock3,
  ListChecks,
  CalendarClock,
  ShieldAlert,
  ShieldCheck,
  TriangleAlert,
} from "lucide-react";

import { KpiCard } from "@/components/ui/card";

import type { AiTaskKpiSummary } from "../application/ai-task-query";
import {
  parseAiTaskCenterView,
  type AiTaskCenterView,
} from "../domain/ai-task-center-item";

/** A summary metric that a host screen adds to the task KPI row (shown once, not as a second row). */
export type AiTaskExtraKpi = Readonly<{
  label: string;
  value: number;
  detail: string;
  href?: string;
}>;

type AiTaskKpiLinksProps = Readonly<{
  kpis: AiTaskKpiSummary;
  listPath?: string;
  extraKpis?: readonly AiTaskExtraKpi[];
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
  extraKpis = [],
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
      ariaLabel: "Onay ve teslimleri görüntüle",
      label: "Onay ve teslim",
      detail: "İnsan kararı gereken kayıt",
      value: String(kpis.awaitingApproval),
      tone: "warning",
      icon: <Clock3 />,
      view: KPI_VIEW_MAP["awaiting-approval"],
    },
    {
      id: "critical",
      ariaLabel: "Hizmet seviyesi riski taşıyan görevleri görüntüle",
      label: "Süre riski",
      detail: "Aşılan veya yaklaşan hedef",
      value: String(kpis.criticalRisk),
      tone: "neutral",
      icon: <ShieldAlert />,
      view: KPI_VIEW_MAP.critical,
    },
    {
      id: "overdue",
      ariaLabel: "Geciken görevleri görüntüle",
      label: "Geciken görev",
      detail: "Çözüm tarihi geçen görev",
      value: String(kpis.overdue),
      tone: "warning",
      icon: <TriangleAlert />,
      view: KPI_VIEW_MAP.overdue,
    },
  ];

  return (
    <div
      aria-label="Yapay zekâ görev merkezi özet göstergeleri"
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
      {extraKpis.map((extra) => {
        const card = (
          <KpiCard detail={extra.detail} icon={extra.label === "Doğrulama" ? <ShieldCheck /> : extra.label === "Vardiya konusu" ? <CalendarClock /> : <ListChecks />} label={extra.label} tone="neutral" value={String(extra.value)} />
        );
        return extra.href ? (
          <Link aria-label={`${extra.label} görüntüle`} className="ai-task-kpi-link" href={extra.href} key={extra.label}>
            {card}
          </Link>
        ) : (
          <div className="ai-task-kpi-link" key={extra.label}>{card}</div>
        );
      })}
    </div>
  );
}
