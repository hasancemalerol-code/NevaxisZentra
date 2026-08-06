"use client";

import {
  RotateCcw,
  Search,
  SlidersHorizontal,
} from "lucide-react";

import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";

import type {
  ProductionPlanConfidence,
  ProductionPlanOrigin,
  ProductionPlanPriority,
  ProductionPlanStatus,
  ProductionPlanTargetSystem,
} from "../domain/production-plan";
import type { ProductionPlanFilter } from "../application/production-plan-query";
import {
  getProductionPlanConfidenceLabel,
  getProductionPlanOriginLabel,
  getProductionPlanPriorityLabel,
  getProductionPlanStatusLabel,
  getProductionPlanTargetSystemLabel,
} from "../presentation/production-plan-labels";
import { hasActiveProductionPlanFilter } from "../presentation/production-plan-filter-url";

type ProductionPlanFilterBarProps =
  Readonly<{
    filter: ProductionPlanFilter;
    onChange(
      updates: Partial<ProductionPlanFilter>,
    ): void;
    onClear(): void;
  }>;

const STATUS_OPTIONS:
  readonly ProductionPlanStatus[] = [
    "draft",
    "needs-review",
    "ready-for-approval",
    "approved",
    "scheduled",
    "in-progress",
    "completed",
    "rejected",
    "cancelled",
  ];

const ORIGIN_OPTIONS:
  readonly ProductionPlanOrigin[] = [
    "approved-order",
    "erp-production-request",
    "manual",
    "ai-recommendation",
  ];

const PRIORITY_OPTIONS:
  readonly ProductionPlanPriority[] = [
    "low",
    "normal",
    "high",
    "urgent",
  ];

const CONFIDENCE_OPTIONS:
  readonly ProductionPlanConfidence[] = [
    "low",
    "medium",
    "high",
  ];

const TARGET_SYSTEM_OPTIONS:
  readonly ProductionPlanTargetSystem[] = [
    "erp",
    "mes",
    "crevafactos",
  ];

export function ProductionPlanFilterBar({
  filter,
  onChange,
  onClear,
}: ProductionPlanFilterBarProps) {
  const hasActiveFilter =
    hasActiveProductionPlanFilter(filter);

  return (
    <Card className="production-plan-filter-bar">
      <div className="production-plan-filter-bar__heading">
        <div>
          <SlidersHorizontal aria-hidden="true" />

          <div>
            <p>Plan filtreleri</p>
            <h2>Üretim planlarını daralt</h2>
          </div>
        </div>

        <Button
          disabled={!hasActiveFilter}
          leadingIcon={<RotateCcw />}
          onClick={onClear}
          variant="secondary"
        >
          Filtreleri temizle
        </Button>
      </div>

      <div className="production-plan-filter-bar__grid">
        <label className="production-plan-filter-bar__search">
          <span>Arama</span>

          <div>
            <Search aria-hidden="true" />

            <input
              onChange={(event) => {
                onChange({
                  search:
                    event.target.value ||
                    undefined,
                });
              }}
              placeholder="Plan, sipariş, müşteri, ürün, makine veya kalıp ara"
              type="search"
              value={filter.search ?? ""}
            />
          </div>
        </label>

        <label>
          <span>Durum</span>

          <select
            onChange={(event) => {
              onChange({
                status:
                  (event.target.value ||
                    undefined) as
                    | ProductionPlanStatus
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
                {getProductionPlanStatusLabel(
                  status,
                )}
              </option>
            ))}
          </select>
        </label>

        <label>
          <span>Kaynak</span>

          <select
            onChange={(event) => {
              onChange({
                origin:
                  (event.target.value ||
                    undefined) as
                    | ProductionPlanOrigin
                    | undefined,
              });
            }}
            value={filter.origin ?? ""}
          >
            <option value="">
              Tüm kaynaklar
            </option>

            {ORIGIN_OPTIONS.map((origin) => (
              <option
                key={origin}
                value={origin}
              >
                {getProductionPlanOriginLabel(
                  origin,
                )}
              </option>
            ))}
          </select>
        </label>

        <label>
          <span>Öncelik</span>

          <select
            onChange={(event) => {
              onChange({
                priority:
                  (event.target.value ||
                    undefined) as
                    | ProductionPlanPriority
                    | undefined,
              });
            }}
            value={filter.priority ?? ""}
          >
            <option value="">
              Tüm öncelikler
            </option>

            {PRIORITY_OPTIONS.map(
              (priority) => (
                <option
                  key={priority}
                  value={priority}
                >
                  {getProductionPlanPriorityLabel(
                    priority,
                  )}
                </option>
              ),
            )}
          </select>
        </label>

        <label>
          <span>AI güveni</span>

          <select
            onChange={(event) => {
              onChange({
                confidence:
                  (event.target.value ||
                    undefined) as
                    | ProductionPlanConfidence
                    | undefined,
              });
            }}
            value={filter.confidence ?? ""}
          >
            <option value="">
              Tüm güven seviyeleri
            </option>

            {CONFIDENCE_OPTIONS.map(
              (confidence) => (
                <option
                  key={confidence}
                  value={confidence}
                >
                  {getProductionPlanConfidenceLabel(
                    confidence,
                  )}
                </option>
              ),
            )}
          </select>
        </label>

        <label>
          <span>Hedef sistem</span>

          <select
            onChange={(event) => {
              onChange({
                targetSystem:
                  (event.target.value ||
                    undefined) as
                    | ProductionPlanTargetSystem
                    | undefined,
              });
            }}
            value={
              filter.targetSystem ?? ""
            }
          >
            <option value="">
              Tüm hedef sistemler
            </option>

            {TARGET_SYSTEM_OPTIONS.map(
              (targetSystem) => (
                <option
                  key={targetSystem}
                  value={targetSystem}
                >
                  {getProductionPlanTargetSystemLabel(
                    targetSystem,
                  )}
                </option>
              ),
            )}
          </select>
        </label>
      </div>

      <div className="production-plan-filter-bar__toggles">
        <label>
          <input
            checked={
              filter.blockingOnly ??
              false
            }
            onChange={(event) => {
              onChange({
                blockingOnly:
                  event.target.checked ||
                  undefined,
              });
            }}
            type="checkbox"
          />

          <span>
            Yalnızca engelleyici riskler
          </span>
        </label>

        <label>
          <input
            checked={
              filter.approvalPendingOnly ??
              false
            }
            onChange={(event) => {
              onChange({
                approvalPendingOnly:
                  event.target.checked ||
                  undefined,
              });
            }}
            type="checkbox"
          />

          <span>Yalnızca onay bekleyenler</span>
        </label>

        <label>
          <input
            checked={
              filter.transferableOnly ??
              false
            }
            onChange={(event) => {
              onChange({
                transferableOnly:
                  event.target.checked ||
                  undefined,
              });
            }}
            type="checkbox"
          />

          <span>Yalnızca aktarıma hazırlar</span>
        </label>
      </div>
    </Card>
  );
}