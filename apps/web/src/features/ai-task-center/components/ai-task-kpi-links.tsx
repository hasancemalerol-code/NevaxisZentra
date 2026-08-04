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
  createAiTaskKpiHref,
  isAiTaskKpiFilterActive,
  type AiTaskKpiFilterId,
} from "../presentation/ai-task-filter-link";
import { parseAiTaskFilterSearchParams } from "../presentation/ai-task-filter-url";

type AiTaskKpiLinksProps = Readonly<{
  kpis: AiTaskKpiSummary;
}>;

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
}>;

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

export function AiTaskKpiLinks({
  kpis,
}: AiTaskKpiLinksProps) {
  const searchParams = useSearchParams();

  const filterValue =
    parseAiTaskFilterSearchParams(
      new URLSearchParams(
        searchParams.toString(),
      ),
    );

  const items: readonly AiTaskKpiItem[] = [
    {
      id: "open",
      ariaLabel: "Açık görevleri görüntüle",
      label: "Açık görev",
      detail: `${kpis.total} görev içinden`,
      value: String(kpis.open),
      tone: "information",
      icon: <ListChecks />,
    },
    {
      id: "awaiting-approval",
      ariaLabel:
        "Onay bekleyen görevleri görüntüle",
      label: "Onay bekleyen",
      detail: "İnsan kararı gerekiyor",
      value: String(kpis.awaitingApproval),
      tone: "warning",
      icon: <Clock3 />,
    },
    {
      id: "critical",
      ariaLabel: "Kritik görevleri görüntüle",
      label: "Kritik risk",
      detail: "Öncelikli müdahale",
      value: String(kpis.criticalRisk),
      tone: "neutral",
      icon: <ShieldAlert />,
    },
    {
      id: "overdue",
      ariaLabel: "Geciken görevleri görüntüle",
      label: "Geciken görev",
      detail: `${kpis.failed} başarısız görev`,
      value: String(kpis.overdue),
      tone: "warning",
      icon: <TriangleAlert />,
    },
  ];

  return (
    <div
      aria-label="AI görev merkezi özet göstergeleri"
      className="kpi-grid ai-task-kpi-links"
    >
      {items.map((item) => (
        <AiTaskKpiLink
          active={isAiTaskKpiFilterActive(
            filterValue,
            item.id,
          )}
          ariaLabel={item.ariaLabel}
          href={createAiTaskKpiHref(item.id)}
          key={item.id}
        >
          <KpiCard
            detail={item.detail}
            icon={item.icon}
            label={item.label}
            preview
            tone={item.tone}
            value={item.value}
          />
        </AiTaskKpiLink>
      ))}
    </div>
  );
}