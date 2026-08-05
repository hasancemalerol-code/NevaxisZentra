import Link from "next/link";
import {
  CircleAlert,
  DatabaseZap,
  FileCheck2,
  Layers3,
  ShieldCheck,
  type LucideIcon,
} from "lucide-react";

import type { OrderDraftSummary } from "../application/order-draft-query";
import { createOrderDraftFilterUrl } from "../application/order-draft-filter-url";

type OrderDraftKpiLinksProps = Readonly<{
  summary: OrderDraftSummary;
}>;

type OrderDraftKpiItem = Readonly<{
  id: string;
  label: string;
  value: number;
  description: string;
  href: string;
  icon: LucideIcon;
  tone:
    | "neutral"
    | "information"
    | "success"
    | "danger";
}>;

export function OrderDraftKpiLinks({
  summary,
}: OrderDraftKpiLinksProps) {
  const items: readonly OrderDraftKpiItem[] = [
    {
      id: "total",
      label: "Toplam taslak",
      value: summary.totalCount,
      description:
        "Tüm kanallardan oluşturulan sipariş taslakları",
      href: "/siparisler",
      icon: Layers3,
      tone: "neutral",
    },
    {
      id: "needs-review",
      label: "İnceleme gerekiyor",
      value: summary.needsReviewCount,
      description:
        "Kullanıcı kontrolü veya veri doğrulaması bekleyenler",
      href: createOrderDraftFilterUrl({
        status: "needs-review",
      }),
      icon: FileCheck2,
      tone: "information",
    },
    {
      id: "awaiting-approval",
      label: "Onay bekliyor",
      value: summary.awaitingApprovalCount,
      description:
        "Yetkili kullanıcı kararına sunulan taslaklar",
      href: createOrderDraftFilterUrl({
        awaitingApproval: true,
      }),
      icon: ShieldCheck,
      tone: "success",
    },
    {
      id: "blocking-issues",
      label: "Engelleyici sorun",
      value: summary.blockingIssueCount,
      description:
        "Siparişin kesinleşmesini engelleyen açık sorunlar",
      href: createOrderDraftFilterUrl({
        hasBlockingIssues: true,
      }),
      icon: CircleAlert,
      tone: "danger",
    },
    {
      id: "transfer-failed",
      label: "ERP aktarım hatası",
      value: summary.transferFailedCount,
      description:
        "Bağlı ERP sistemine aktarılamayan siparişler",
      href: createOrderDraftFilterUrl({
        transferStatus: "failed",
      }),
      icon: DatabaseZap,
      tone: "danger",
    },
  ];

  return (
    <nav
      aria-label="Sipariş taslağı özet filtreleri"
      className="order-draft-kpi-links"
    >
      {items.map((item) => {
        const Icon = item.icon;

        return (
          <Link
            className="order-draft-kpi-link"
            data-tone={item.tone}
            href={item.href}
            key={item.id}
          >
            <span className="order-draft-kpi-link__icon">
              <Icon aria-hidden="true" />
            </span>

            <span className="order-draft-kpi-link__content">
              <span>{item.label}</span>
              <strong>{item.value}</strong>
              <small>{item.description}</small>
            </span>
          </Link>
        );
      })}
    </nav>
  );
}