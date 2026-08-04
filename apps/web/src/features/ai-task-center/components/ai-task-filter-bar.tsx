"use client";

import {
  RotateCcw,
  Search,
  SlidersHorizontal,
} from "lucide-react";

import { Button } from "@/components/ui/button";

import type { AiTaskFilterValue } from "../application/ai-task-filter";
import type {
  AiTaskCategory,
  AiTaskPriority,
  AiTaskStatus,
} from "../domain/ai-task";
import {
  AI_TASK_CATEGORIES,
  AI_TASK_PRIORITIES,
  AI_TASK_STATUSES,
} from "../domain/ai-task";
import {
  getAiTaskCategoryLabel,
  getAiTaskPriorityLabel,
  getAiTaskStatusLabel,
} from "../presentation/ai-task-labels";

type AiTaskFilterBarProps = Readonly<{
  value: AiTaskFilterValue;
  resultCount: number;
  onChange: (value: AiTaskFilterValue) => void;
  onReset: () => void;
}>;

function updateFilterValue<
  K extends keyof AiTaskFilterValue,
>(
  value: AiTaskFilterValue,
  key: K,
  nextValue: AiTaskFilterValue[K],
): AiTaskFilterValue {
  return {
    ...value,
    [key]: nextValue,
  };
}

export function AiTaskFilterBar({
  value,
  resultCount,
  onChange,
  onReset,
}: AiTaskFilterBarProps) {
  const hasActiveFilters =
    value.search.trim().length > 0 ||
    value.category !== "all" ||
    value.priority !== "all" ||
    value.status !== "all" ||
    value.approvalPendingOnly ||
    value.overdueOnly;

  return (
    <section
      aria-label="AI görev filtreleri"
      className="ai-task-filter-bar"
    >
      <div className="ai-task-filter-bar__heading">
        <div>
          <span>
            <SlidersHorizontal aria-hidden="true" />
            Görev filtreleri
          </span>

          <strong>
            {resultCount} görev gösteriliyor
          </strong>
        </div>

        <Button
          disabled={!hasActiveFilters}
          leadingIcon={<RotateCcw />}
          onClick={onReset}
          size="small"
          type="button"
          variant="secondary"
        >
          Filtreleri temizle
        </Button>
      </div>

      <div className="ai-task-filter-bar__controls">
        <label className="ai-task-filter-bar__search">
          <span>
            Görev ara
          </span>

          <div>
            <Search aria-hidden="true" />

            <input
              onChange={(event) =>
                onChange(
                  updateFilterValue(
                    value,
                    "search",
                    event.target.value,
                  ),
                )
              }
              placeholder="Görev, makine, kalıp veya sipariş ara"
              type="search"
              value={value.search}
            />
          </div>
        </label>

        <label>
          <span>
            Kategori
          </span>

          <select
            onChange={(event) =>
              onChange(
                updateFilterValue(
                  value,
                  "category",
                  event.target.value as
                    | AiTaskCategory
                    | "all",
                ),
              )
            }
            value={value.category}
          >
            <option value="all">
              Tüm kategoriler
            </option>

            {AI_TASK_CATEGORIES.map((category) => (
              <option
                key={category}
                value={category}
              >
                {getAiTaskCategoryLabel(category)}
              </option>
            ))}
          </select>
        </label>

        <label>
          <span>
            Öncelik
          </span>

          <select
            onChange={(event) =>
              onChange(
                updateFilterValue(
                  value,
                  "priority",
                  event.target.value as
                    | AiTaskPriority
                    | "all",
                ),
              )
            }
            value={value.priority}
          >
            <option value="all">
              Tüm öncelikler
            </option>

            {AI_TASK_PRIORITIES.map((priority) => (
              <option
                key={priority}
                value={priority}
              >
                {getAiTaskPriorityLabel(priority)}
              </option>
            ))}
          </select>
        </label>

        <label>
          <span>
            Durum
          </span>

          <select
            onChange={(event) =>
              onChange(
                updateFilterValue(
                  value,
                  "status",
                  event.target.value as
                    | AiTaskStatus
                    | "all",
                ),
              )
            }
            value={value.status}
          >
            <option value="all">
              Tüm durumlar
            </option>

            {AI_TASK_STATUSES.map((status) => (
              <option
                key={status}
                value={status}
              >
                {getAiTaskStatusLabel(status)}
              </option>
            ))}
          </select>
        </label>
      </div>

      <div className="ai-task-filter-bar__toggles">
        <label>
          <input
            checked={value.approvalPendingOnly}
            onChange={(event) =>
              onChange(
                updateFilterValue(
                  value,
                  "approvalPendingOnly",
                  event.target.checked,
                ),
              )
            }
            type="checkbox"
          />

          <span>
            Yalnızca onay bekleyenler
          </span>
        </label>

        <label>
          <input
            checked={value.overdueOnly}
            onChange={(event) =>
              onChange(
                updateFilterValue(
                  value,
                  "overdueOnly",
                  event.target.checked,
                ),
              )
            }
            type="checkbox"
          />

          <span>
            Yalnızca gecikenler
          </span>
        </label>
      </div>
    </section>
  );
}