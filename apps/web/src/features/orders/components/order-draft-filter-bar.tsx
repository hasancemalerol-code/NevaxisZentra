"use client";

import {
  CircleAlert,
  RotateCcw,
  Search,
  ShieldCheck,
} from "lucide-react";

import { Button } from "@/components/ui/button";

import type { OrderDraftFilter } from "../application/order-draft-query";
import type {
  OrderDraftStatus,
  OrderIntakeSource,
  OrderMatchConfidence,
} from "../domain/order-draft";

type OrderDraftFilterBarProps = Readonly<{
  filter: OrderDraftFilter;
  onFilterChange: (
    filter: OrderDraftFilter,
  ) => void;
}>;

const SOURCE_OPTIONS: readonly Readonly<{
  value: OrderIntakeSource;
  label: string;
}>[] = [
  {
    value: "email",
    label: "E-posta",
  },
  {
    value: "whatsapp",
    label: "WhatsApp",
  },
  {
    value: "erp",
    label: "ERP",
  },
  {
    value: "customer-portal",
    label: "Müşteri Portalı",
  },
  {
    value: "sales-representative",
    label: "Satış Temsilcisi",
  },
  {
    value: "web-form",
    label: "Web Formu",
  },
  {
    value: "file-upload",
    label: "Dosya Yükleme",
  },
];

const STATUS_OPTIONS: readonly Readonly<{
  value: OrderDraftStatus;
  label: string;
}>[] = [
  {
    value: "collecting",
    label: "Veri toplanıyor",
  },
  {
    value: "needs-review",
    label: "İnceleme gerekiyor",
  },
  {
    value: "ready-for-approval",
    label: "Onaya hazır",
  },
  {
    value: "approved",
    label: "Onaylandı",
  },
  {
    value: "rejected",
    label: "Reddedildi",
  },
  {
    value: "transferring",
    label: "ERP'ye aktarılıyor",
  },
  {
    value: "transferred",
    label: "ERP'ye aktarıldı",
  },
  {
    value: "transfer-failed",
    label: "Aktarım başarısız",
  },
];

const CONFIDENCE_OPTIONS: readonly Readonly<{
  value: OrderMatchConfidence;
  label: string;
}>[] = [
  {
    value: "high",
    label: "Yüksek güven",
  },
  {
    value: "medium",
    label: "Orta güven",
  },
  {
    value: "low",
    label: "Düşük güven",
  },
];

function hasActiveFilters(
  filter: OrderDraftFilter,
): boolean {
  return Boolean(
    filter.search ||
      filter.source ||
      filter.status ||
      filter.confidenceLevel ||
      filter.transferStatus ||
      filter.hasBlockingIssues !== undefined ||
      filter.awaitingApproval !== undefined,
  );
}

export function OrderDraftFilterBar({
  filter,
  onFilterChange,
}: OrderDraftFilterBarProps) {
  const updateFilter = (
    patch: Partial<OrderDraftFilter>,
  ) => {
    onFilterChange({
      ...filter,
      ...patch,
    });
  };

  return (
    <div className="order-draft-filter-bar">
      <label className="order-draft-filter-bar__search">
        <Search aria-hidden="true" />

        <span className="sr-only">
          Sipariş taslaklarında ara
        </span>

        <input
          onChange={(event) => {
            updateFilter({
              search:
                event.target.value || undefined,
            });
          }}
          placeholder="Müşteri, ürün, mesaj veya dosya ara"
          type="search"
          value={filter.search ?? ""}
        />
      </label>

      <label className="order-draft-filter-bar__field">
        <span>Kaynak</span>

        <select
          onChange={(event) => {
            updateFilter({
              source:
                (event.target.value ||
                  undefined) as
                  | OrderIntakeSource
                  | undefined,
            });
          }}
          value={filter.source ?? ""}
        >
          <option value="">
            Tüm kaynaklar
          </option>

          {SOURCE_OPTIONS.map((option) => (
            <option
              key={option.value}
              value={option.value}
            >
              {option.label}
            </option>
          ))}
        </select>
      </label>

      <label className="order-draft-filter-bar__field">
        <span>Durum</span>

        <select
          onChange={(event) => {
            updateFilter({
              status:
                (event.target.value ||
                  undefined) as
                  | OrderDraftStatus
                  | undefined,
            });
          }}
          value={filter.status ?? ""}
        >
          <option value="">
            Tüm durumlar
          </option>

          {STATUS_OPTIONS.map((option) => (
            <option
              key={option.value}
              value={option.value}
            >
              {option.label}
            </option>
          ))}
        </select>
      </label>

      <label className="order-draft-filter-bar__field">
        <span>AI güveni</span>

        <select
          onChange={(event) => {
            updateFilter({
              confidenceLevel:
                (event.target.value ||
                  undefined) as
                  | OrderMatchConfidence
                  | undefined,
            });
          }}
          value={filter.confidenceLevel ?? ""}
        >
          <option value="">
            Tüm güven seviyeleri
          </option>

          {CONFIDENCE_OPTIONS.map((option) => (
            <option
              key={option.value}
              value={option.value}
            >
              {option.label}
            </option>
          ))}
        </select>
      </label>

      <div className="order-draft-filter-bar__toggles">
        <button
          aria-pressed={
            filter.hasBlockingIssues === true
          }
          className="order-draft-filter-bar__toggle"
          onClick={() => {
            updateFilter({
              hasBlockingIssues:
                filter.hasBlockingIssues === true
                  ? undefined
                  : true,
            });
          }}
          type="button"
        >
          <CircleAlert aria-hidden="true" />
          Engelleyici sorunlar
        </button>

        <button
          aria-pressed={
            filter.awaitingApproval === true
          }
          className="order-draft-filter-bar__toggle"
          onClick={() => {
            updateFilter({
              awaitingApproval:
                filter.awaitingApproval === true
                  ? undefined
                  : true,
            });
          }}
          type="button"
        >
          <ShieldCheck aria-hidden="true" />
          Onay bekleyenler
        </button>
      </div>

      <Button
        disabled={!hasActiveFilters(filter)}
        leadingIcon={<RotateCcw />}
        onClick={() => {
          onFilterChange({});
        }}
        variant="secondary"
      >
        Temizle
      </Button>
    </div>
  );
}