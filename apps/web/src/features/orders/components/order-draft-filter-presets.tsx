"use client";

import {
  CircleAlert,
  DatabaseZap,
  Inbox,
  MessageCircle,
  RotateCcw,
  ShieldCheck,
} from "lucide-react";

import type { OrderDraftFilter } from "../application/order-draft-query";

type OrderDraftFilterPresetsProps = Readonly<{
  filter: OrderDraftFilter;
  onFilterChange: (
    filter: OrderDraftFilter,
  ) => void;
}>;

type OrderDraftFilterPreset = Readonly<{
  id: string;
  label: string;
  icon: typeof Inbox;
  filter: OrderDraftFilter;
}>;

const FILTER_PRESETS: readonly OrderDraftFilterPreset[] = [
  {
    id: "all",
    label: "Tüm siparişler",
    icon: Inbox,
    filter: {},
  },
  {
    id: "whatsapp",
    label: "WhatsApp siparişleri",
    icon: MessageCircle,
    filter: {
      source: "whatsapp",
    },
  },
  {
    id: "needs-review",
    label: "İnceleme gerekenler",
    icon: CircleAlert,
    filter: {
      status: "needs-review",
    },
  },
  {
    id: "awaiting-approval",
    label: "Onay bekleyenler",
    icon: ShieldCheck,
    filter: {
      awaitingApproval: true,
    },
  },
  {
    id: "blocking-issues",
    label: "Engelleyici sorunlar",
    icon: CircleAlert,
    filter: {
      hasBlockingIssues: true,
    },
  },
  {
    id: "transfer-failed",
    label: "ERP aktarım hataları",
    icon: DatabaseZap,
    filter: {
      transferStatus: "failed",
    },
  },
];

function filtersAreEqual(
  left: OrderDraftFilter,
  right: OrderDraftFilter,
): boolean {
  return (
    left.search === right.search &&
    left.source === right.source &&
    left.status === right.status &&
    left.confidenceLevel ===
      right.confidenceLevel &&
    left.transferStatus ===
      right.transferStatus &&
    left.hasBlockingIssues ===
      right.hasBlockingIssues &&
    left.awaitingApproval ===
      right.awaitingApproval
  );
}

export function OrderDraftFilterPresets({
  filter,
  onFilterChange,
}: OrderDraftFilterPresetsProps) {
  return (
    <div
      aria-label="Hızlı sipariş filtreleri"
      className="order-draft-filter-presets"
    >
      {FILTER_PRESETS.map((preset) => {
        const Icon = preset.icon;
        const isActive = filtersAreEqual(
          filter,
          preset.filter,
        );

        return (
          <button
            aria-pressed={isActive}
            className="order-draft-filter-presets__button"
            key={preset.id}
            onClick={() => {
              onFilterChange(preset.filter);
            }}
            type="button"
          >
            <Icon aria-hidden="true" />
            {preset.label}
          </button>
        );
      })}

      {filtersAreEqual(filter, {}) ? null : (
        <button
          className="order-draft-filter-presets__reset"
          onClick={() => {
            onFilterChange({});
          }}
          type="button"
        >
          <RotateCcw aria-hidden="true" />
          Sıfırla
        </button>
      )}
    </div>
  );
}