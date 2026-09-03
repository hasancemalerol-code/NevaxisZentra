/**
 * Referans politika kütüphanesi — seed verisi ve backend sabitleriyle uyumlu.
 * Otomasyon kurallarında applySlaPolicy / applyClosurePolicy aksiyonları bu anahtarları kullanır.
 */

export type AiTaskPolicyLibrarySlaEntry = Readonly<{
  policyKey: string;
  name: string;
  priority: string;
  acknowledgmentTargetMinutes: number;
  responseTargetMinutes: number;
  resolutionTargetMinutes: number;
}>;

export type AiTaskPolicyLibraryClosureEntry = Readonly<{
  policyKey: string;
  name: string;
  category: string | null;
  priority: string | null;
  minimumEvidenceCount: number;
  requiredEvidenceTypes: readonly string[];
  approvalRequired: boolean;
  fourEyesRequired: boolean;
  resolutionSummaryRequired: boolean;
}>;

export const AI_TASK_POLICY_LIBRARY_SLA_POLICIES: readonly AiTaskPolicyLibrarySlaEntry[] = [
  {
    policyKey: "critical-default",
    name: "Kritik SLA",
    priority: "critical",
    acknowledgmentTargetMinutes: 15,
    responseTargetMinutes: 30,
    resolutionTargetMinutes: 120,
  },
  {
    policyKey: "high-default",
    name: "Yüksek SLA",
    priority: "high",
    acknowledgmentTargetMinutes: 30,
    responseTargetMinutes: 60,
    resolutionTargetMinutes: 240,
  },
  {
    policyKey: "medium-default",
    name: "Orta SLA",
    priority: "medium",
    acknowledgmentTargetMinutes: 60,
    responseTargetMinutes: 240,
    resolutionTargetMinutes: 1440,
  },
  {
    policyKey: "low-default",
    name: "Düşük SLA",
    priority: "low",
    acknowledgmentTargetMinutes: 240,
    responseTargetMinutes: 480,
    resolutionTargetMinutes: 2880,
  },
];

export const AI_TASK_POLICY_LIBRARY_CLOSURE_POLICIES: readonly AiTaskPolicyLibraryClosureEntry[] = [
  {
    policyKey: "critical-maintenance",
    name: "Kritik bakım kapanış politikası",
    category: "maintenance",
    priority: "critical",
    minimumEvidenceCount: 1,
    requiredEvidenceTypes: [],
    approvalRequired: true,
    fourEyesRequired: true,
    resolutionSummaryRequired: true,
  },
  {
    policyKey: "quality-ncr",
    name: "Kalite NCR kapanış politikası",
    category: "quality",
    priority: null,
    minimumEvidenceCount: 1,
    requiredEvidenceTypes: ["quality-record", "note"],
    approvalRequired: true,
    fourEyesRequired: true,
    resolutionSummaryRequired: true,
  },
  {
    policyKey: "high-priority-default",
    name: "Yüksek öncelik kapanış politikası",
    category: null,
    priority: "high",
    minimumEvidenceCount: 0,
    requiredEvidenceTypes: [],
    approvalRequired: true,
    fourEyesRequired: true,
    resolutionSummaryRequired: true,
  },
];
