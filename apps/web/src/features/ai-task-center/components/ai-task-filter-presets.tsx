"use client";

import {
  CircleX,
  ClockAlert,
  ListChecks,
  ShieldAlert,
  UserCheck,
  type LucideIcon,
} from "lucide-react";

import {
  AI_TASK_FILTER_PRESET_VALUES,
  type AiTaskFilterPresetId,
  type AiTaskFilterValue,
} from "../application/ai-task-filter";

type AiTaskFilterPreset = Readonly<{
  id: AiTaskFilterPresetId;
  label: string;
  description: string;
  icon: LucideIcon;
}>;

type AiTaskFilterPresetsProps = Readonly<{
  activePresetId?: AiTaskFilterPresetId;
  onSelect: (
    presetId: AiTaskFilterPresetId,
    value: AiTaskFilterValue,
  ) => void;
}>;

export const AI_TASK_FILTER_PRESETS:
  readonly AiTaskFilterPreset[] = [
    {
      id: "all",
      label: "Tüm görevler",
      description: "Aktif görev filtrelerini temizle",
      icon: ListChecks,
    },
    {
      id: "critical",
      label: "Kritik görevler",
      description: "Kritik öncelikli görevleri göster",
      icon: ShieldAlert,
    },
    {
      id: "awaiting-approval",
      label: "Onay bekleyenler",
      description: "İnsan kararı bekleyen görevleri göster",
      icon: UserCheck,
    },
    {
      id: "overdue",
      label: "Gecikenler",
      description: "Son tarihi geçen açık görevleri göster",
      icon: ClockAlert,
    },
    {
      id: "failed",
      label: "Başarısız işlemler",
      description: "Uygulama sırasında hata alan görevleri göster",
      icon: CircleX,
    },
  ];

export function AiTaskFilterPresets({
  activePresetId,
  onSelect,
}: AiTaskFilterPresetsProps) {
  return (
    <nav
      aria-label="Hazır AI görev filtreleri"
      className="ai-task-filter-presets"
    >
      {AI_TASK_FILTER_PRESETS.map((preset) => {
        const Icon = preset.icon;
        const active = activePresetId === preset.id;

        return (
          <button
            aria-pressed={active}
            className="ai-task-filter-preset"
            data-active={active}
            key={preset.id}
            onClick={() =>
              onSelect(
                preset.id,
                {
                  ...AI_TASK_FILTER_PRESET_VALUES[
                    preset.id
                  ],
                },
              )
            }
            title={preset.description}
            type="button"
          >
            <Icon aria-hidden="true" />

            <span>
              {preset.label}
            </span>
          </button>
        );
      })}
    </nav>
  );
}