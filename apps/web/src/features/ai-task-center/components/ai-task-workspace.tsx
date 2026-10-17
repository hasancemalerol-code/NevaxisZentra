"use client";

import { useMemo } from "react";
import { ListFilter } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { EmptyState } from "@/components/ui/states";

import { toAiTaskQueryFilter } from "../application/ai-task-filter-query";
import {
  filterAiTasks,
  sortAiTasksByUrgency,
} from "../application/ai-task-query";
import { useAiTaskFilterState } from "../application/use-ai-task-filter-state";
import type { AiTask } from "../domain/ai-task";
import { AiTaskCard } from "./ai-task-card";
import { AiTaskFilterBar } from "./ai-task-filter-bar";
import { AiTaskFilterPresets } from "./ai-task-filter-presets";

type AiTaskWorkspaceProps = Readonly<{
  tasks: readonly AiTask[];
  nowIso?: string;
}>;

export function AiTaskWorkspace({
  tasks,
  nowIso,
}: AiTaskWorkspaceProps) {
  const {
    value: filterValue,
    activePresetId,
    hasActiveFilters,
    setValue: setFilterValue,
    reset: resetFilters,
    selectPreset,
  } = useAiTaskFilterState();

  const now = useMemo(() => {
    if (!nowIso) {
      return new Date();
    }

    const parsed = new Date(nowIso);

    return Number.isNaN(parsed.getTime())
      ? new Date()
      : parsed;
  }, [nowIso]);

  const visibleTasks = useMemo(() => {
    const filteredTasks = filterAiTasks(
      tasks,
      toAiTaskQueryFilter(filterValue),
      now,
    );

    return sortAiTasksByUrgency(
      filteredTasks,
      now,
    );
  }, [filterValue, now, tasks]);

  return (
    <div className="ai-task-workspace">
      <AiTaskFilterPresets
        activePresetId={activePresetId}
        onSelect={(presetId) => {
          selectPreset(presetId);
        }}
      />

      <AiTaskFilterBar
        onChange={setFilterValue}
        onReset={resetFilters}
        resultCount={visibleTasks.length}
        value={filterValue}
      />

      <div className="ai-task-workspace__summary">
        <div>
          <ListFilter aria-hidden="true" />

          <span>
            {visibleTasks.length} / {tasks.length} görev
          </span>
        </div>

        {hasActiveFilters ? (
          <Badge tone="information">
            Filtre aktif
          </Badge>
        ) : (
          <Badge tone="neutral">
            Tüm görevler
          </Badge>
        )}
      </div>

      {visibleTasks.length > 0 ? (
        <div
          aria-label="Filtrelenmiş yapay zekâ görevleri"
          className="ai-task-list"
        >
          {visibleTasks.map((task) => (
            <AiTaskCard
              href={`/ai-gorevleri/${task.id}`}
              key={task.id}
              now={now}
              task={task}
            />
          ))}
        </div>
      ) : (
        <EmptyState
          description={
            hasActiveFilters
              ? "Seçili filtrelerle eşleşen görev bulunamadı. Filtreleri temizleyerek tüm görevleri yeniden görüntüleyebilirsiniz."
              : "Gösterilecek yapay zekâ görevi bulunmuyor."
          }
          title="Görev bulunamadı"
        />
      )}
    </div>
  );
}