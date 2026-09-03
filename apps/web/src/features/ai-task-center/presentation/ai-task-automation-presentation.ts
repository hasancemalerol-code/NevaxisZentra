import type { AiTaskAutomationAction } from "../domain/ai-task-automation";
import {
  AI_TASK_POLICY_LIBRARY_CLOSURE_POLICIES,
  AI_TASK_POLICY_LIBRARY_SLA_POLICIES,
} from "../domain/ai-task-policy-library";
import {
  formatAutomationAssignmentValue,
  resolveAutomationTeamChannelLabel,
} from "./ai-task-automation-entity-lookup";
import { getAiTaskAutomationActionTypeLabel } from "./ai-task-automation-labels";
import { getAiTaskPriorityLabel } from "./ai-task-labels";

export type AiTaskAutomationActionChip = Readonly<{
  label: string;
  tone: "neutral" | "information" | "success" | "warning" | "ai";
}>;

function formatTeamAssignmentChipLabel(teamChannelId: string | null | undefined): string {
  const label = resolveAutomationTeamChannelLabel(teamChannelId);
  if (label.endsWith(" Ekibi")) {
    return `${label.slice(0, -" Ekibi".length)} Ekibine Ata`;
  }
  return `${label}ne Ata`;
}

function formatTeamQueueChipLabel(teamChannelId: string | null | undefined): string {
  const label = resolveAutomationTeamChannelLabel(teamChannelId);
  if (label.endsWith(" Ekibi")) {
    return `${label.slice(0, -" Ekibi".length)} Kuyruğuna Yönlendir`;
  }
  return `${label} Kuyruğuna Yönlendir`;
}

const SLA_POLICY_DISPLAY_NAMES: Readonly<Record<string, string>> = {
  "critical-default": "Kritik SLA",
  "high-default": "Yüksek SLA",
  "medium-default": "Orta SLA",
  "low-default": "Düşük SLA",
};

const CLOSURE_POLICY_DISPLAY_NAMES: Readonly<Record<string, string>> = {
  "critical-maintenance": "Kritik bakım kapanış politikası",
  "quality-ncr": "Kalite NCR kapanış politikası",
  "high-priority-default": "Yüksek öncelik kapanış politikası",
};

const PROPOSED_FIELD_LABELS: Readonly<Record<string, string>> = {
  assignment: "Atama",
  priority: "Öncelik",
  slaPolicy: "SLA politikası",
  evidenceRequired: "Kanıt zorunluluğu",
  closureApprovalRequired: "Kapanış onayı",
  fourEyesRequired: "Çift göz onayı",
  closurePolicy: "Kapanış politikası",
  teamsNotification: "Teams bildirimi",
  tag: "Etiket",
};

export function getSlaPolicyDisplayName(policyKey: string): string {
  return SLA_POLICY_DISPLAY_NAMES[policyKey]
    ?? AI_TASK_POLICY_LIBRARY_SLA_POLICIES.find((p) => p.policyKey === policyKey)?.name
    ?? policyKey;
}

export function getClosurePolicyDisplayName(policyKey: string): string {
  return CLOSURE_POLICY_DISPLAY_NAMES[policyKey]
    ?? AI_TASK_POLICY_LIBRARY_CLOSURE_POLICIES.find((p) => p.policyKey === policyKey)?.name
    ?? policyKey;
}

export function formatAutomationDurationMinutes(minutes: number): string {
  if (minutes < 60) return `${minutes} dk`;
  if (minutes < 1440) {
    const hours = Math.round((minutes / 60) * 10) / 10;
    return Number.isInteger(hours) ? `${hours} sa` : `${hours} sa`;
  }
  const days = Math.round((minutes / 1440) * 10) / 10;
  return Number.isInteger(days) ? `${days} gün` : `${days} gün`;
}

export function formatAutomationScopeLabel(facilityScoped: boolean): string {
  return facilityScoped ? "Tesis kapsamı" : "Genel kapsam";
}

export function formatAutomationProposedFieldLabel(field: string): string {
  return PROPOSED_FIELD_LABELS[field] ?? field;
}

export function formatAutomationProposedValue(field: string, value: string | null): string {
  if (!value) return "—";
  if (field === "assignment") return formatAutomationAssignmentValue(value);
  if (field === "priority") {
    return getAiTaskPriorityLabel(value as "low" | "medium" | "high" | "critical");
  }
  if (field === "slaPolicy" || field === "closurePolicy") {
    return field === "slaPolicy"
      ? getSlaPolicyDisplayName(value)
      : getClosurePolicyDisplayName(value);
  }
  if (field === "evidenceRequired") return `${value} adet`;
  if (field === "closureApprovalRequired" || field === "fourEyesRequired") {
    return value === "true" ? "Evet" : "Hayır";
  }
  if (field === "teamsNotification") {
    return resolveAutomationTeamChannelLabel(value);
  }
  return value;
}

export function formatAutomationActionChip(action: AiTaskAutomationAction): AiTaskAutomationActionChip {
  switch (action.type) {
    case "assignToTeam":
      return {
        label: formatTeamAssignmentChipLabel(action.guidValue),
        tone: "information",
      };
    case "routeToQueue":
      return {
        label: formatTeamQueueChipLabel(action.guidValue),
        tone: "information",
      };
    case "assignToMembership":
      return { label: "Üyeye Ata", tone: "information" };
    case "applySlaPolicy":
      return { label: getSlaPolicyDisplayName(action.value ?? ""), tone: "ai" };
    case "applyClosurePolicy":
      return { label: getClosurePolicyDisplayName(action.value ?? ""), tone: "ai" };
    case "requireEvidence":
      return { label: "Kanıt Zorunlu", tone: "warning" };
    case "requireClosureApproval":
      return { label: "Kapanış Onayı Zorunlu", tone: "warning" };
    case "enableFourEyes":
      return { label: "Çift Göz Onayı", tone: "warning" };
    case "setPriority":
      return {
        label: `Öncelik: ${getAiTaskPriorityLabel((action.value ?? "medium") as "low" | "medium" | "high" | "critical")}`,
        tone: "neutral",
      };
    case "notifyTeamsChannel":
      return {
        label: `${resolveAutomationTeamChannelLabel(action.guidValue)} Bildirimi`,
        tone: "information",
      };
    case "addTaskTag":
      return { label: `Etiket: ${action.value ?? ""}`, tone: "neutral" };
    default:
      return { label: getAiTaskAutomationActionTypeLabel(action.type), tone: "neutral" };
  }
}

export function formatAutomationActionSummaryChip(summary: string): AiTaskAutomationActionChip {
  const teamMatch = summary.match(/^Assign team ([0-9a-f-]{36})$/i);
  if (teamMatch) {
    return {
      label: formatTeamAssignmentChipLabel(teamMatch[1]),
      tone: "information",
    };
  }

  const slaMatch = summary.match(/^SLA (.+)$/i);
  if (slaMatch) {
    return { label: getSlaPolicyDisplayName(slaMatch[1]), tone: "ai" };
  }

  const priorityMatch = summary.match(/^Priority (.+)$/i);
  if (priorityMatch) {
    return {
      label: `Öncelik: ${getAiTaskPriorityLabel(priorityMatch[1] as "low" | "medium" | "high" | "critical")}`,
      tone: "neutral",
    };
  }

  if (/^Require evidence$/i.test(summary)) {
    return { label: "Kanıt Zorunlu", tone: "warning" };
  }

  if (/^Require closure approval$/i.test(summary)) {
    return { label: "Kapanış Onayı Zorunlu", tone: "warning" };
  }

  if (/^Assign team/i.test(summary)) {
    return { label: "Takıma Ata", tone: "information" };
  }

  return { label: summary, tone: "neutral" };
}

export function formatAutomationExecutionActionChip(actionText: string): AiTaskAutomationActionChip {
  if (actionText.includes("team:") || actionText.includes("membership:")) {
    return {
      label: formatAutomationAssignmentValue(actionText.match(/team:[0-9a-f-]+/i)?.[0] ?? actionText),
      tone: "information",
    };
  }

  return formatAutomationActionSummaryChip(actionText);
}

export function formatAutomationOutcomeSummary(summary: string): string {
  if (summary === "No automation rules matched.") {
    return "Eşleşen otomasyon kuralı yok.";
  }

  if (summary === "Automation already applied (idempotent replay).") {
    return "Otomasyon zaten uygulandı (idempotent tekrar).";
  }

  const simulationMatch = summary.match(
    /^Simulation: (\d+) rule\(s\) matched, (\d+) change\(s\) proposed\.$/,
  );
  if (simulationMatch) {
    return `Simülasyon: ${simulationMatch[1]} kural eşleşti, ${simulationMatch[2]} değişiklik önerildi.`;
  }

  const appliedMatch = summary.match(/^Applied automation from (\d+) rule\(s\)\.$/);
  if (appliedMatch) {
    return `${appliedMatch[1]} kuraldan otomasyon uygulandı.`;
  }

  return summary;
}

export function formatAutomationConflictResolution(resolution: string): string {
  const winsMatch = resolution.match(
    /^Higher-priority rule '(.+)' wins over '(.+)'\.$/,
  );
  if (winsMatch) {
    return `Yüksek öncelikli kural «${winsMatch[1]}», «${winsMatch[2]}» kuralına göre öncelikli.`;
  }

  const retainedMatch = resolution.match(/^Higher-priority rule '(.+)' retained\.$/);
  if (retainedMatch) {
    return `Yüksek öncelikli kural «${retainedMatch[1]}» korundu.`;
  }

  return resolution;
}
