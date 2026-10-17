"use client";

import { Search } from "lucide-react";

import type { OrderDraftFilter } from "../application/order-draft-query";
import type {
  OrderDraftStatus,
  OrderIntakeSource,
  OrderMatchConfidence,
  OrderTransferStatus,
} from "../domain/order-draft";
import {
  getOrderConfidenceLabel,
  getOrderDraftStatusLabel,
  getOrderIntakeSourceLabel,
  getOrderTransferStatusLabel,
} from "../presentation/order-draft-labels";

type OrderDraftFilterBarProps = Readonly<{
  filter: OrderDraftFilter;
  onFilterChange: (
    filter: OrderDraftFilter,
  ) => void;
}>;

const SOURCE_OPTIONS: readonly OrderIntakeSource[] = [
  "email",
  "whatsapp",
  "erp",
  "customer-portal",
  "sales-representative",
  "web-form",
  "file-upload",
];

const STATUS_OPTIONS: readonly OrderDraftStatus[] = [
  "collecting",
  "needs-review",
  "ready-for-approval",
  "approved",
  "rejected",
  "transferring",
  "transferred",
  "transfer-failed",
];

const CONFIDENCE_OPTIONS:
  readonly OrderMatchConfidence[] = [
    "high",
    "medium",
    "low",
  ];

const TRANSFER_OPTIONS:
  readonly OrderTransferStatus[] = [
    "not-started",
    "pending",
    "in-progress",
    "completed",
    "failed",
  ];

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

          {SOURCE_OPTIONS.map((source) => (
            <option
              key={source}
              value={source}
            >
              {getOrderIntakeSourceLabel(source)}
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

          {STATUS_OPTIONS.map((status) => (
            <option
              key={status}
              value={status}
            >
              {getOrderDraftStatusLabel(status)}
            </option>
          ))}
        </select>
      </label>

      <label className="order-draft-filter-bar__field">
        <span>Yapay zekâ güveni</span>

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

          {CONFIDENCE_OPTIONS.map(
            (confidenceLevel) => (
              <option
                key={confidenceLevel}
                value={confidenceLevel}
              >
                {getOrderConfidenceLabel(
                  confidenceLevel,
                )}
              </option>
            ),
          )}
        </select>
      </label>

      <label className="order-draft-filter-bar__field">
        <span>ERP aktarımı</span>

        <select
          onChange={(event) => {
            updateFilter({
              transferStatus:
                (event.target.value ||
                  undefined) as
                  | OrderTransferStatus
                  | undefined,
            });
          }}
          value={filter.transferStatus ?? ""}
        >
          <option value="">
            Tüm aktarım durumları
          </option>

          {TRANSFER_OPTIONS.map(
            (transferStatus) => (
              <option
                key={transferStatus}
                value={transferStatus}
              >
                {getOrderTransferStatusLabel(
                  transferStatus,
                )}
              </option>
            ),
          )}
        </select>
      </label>
    </div>
  );
}