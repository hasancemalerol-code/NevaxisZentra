// ── 5L: Shift Assignment ────────────────────────────────────────────────────────

export type ShiftAssignmentSummary = Readonly<{
  id: string;
  productionShiftId: string;
  shiftCode: string;
  shiftDate: string; // yyyy-MM-dd
  shiftStartAtUtc: string;
  shiftEndAtUtc: string;
  assignedAtUtc: string;
  assignedBySubjectId: string;
  releasedAtUtc?: string;
  releasedBySubjectId?: string;
  releaseReason?: string;
  isActive: boolean;
}>;

// ── 5L: Operator Assignment ─────────────────────────────────────────────────────

export type OperatorAssignmentRole = "primary-operator" | "operator" | "supervisor";

export const OPERATOR_ASSIGNMENT_ROLES: OperatorAssignmentRole[] = [
  "primary-operator",
  "operator",
  "supervisor",
];

export function getOperatorRoleLabel(role: OperatorAssignmentRole): string {
  switch (role) {
    case "primary-operator":
      return "Ana Operatör";
    case "operator":
      return "Operatör";
    case "supervisor":
      return "Sorumlu";
  }
}

export type OperatorAssignmentSummary = Readonly<{
  id: string;
  subjectId: string;
  assignmentRole: OperatorAssignmentRole;
  assignedAtUtc: string;
  assignedBySubjectId: string;
  releasedAtUtc?: string;
  releasedBySubjectId?: string;
  releaseReason?: string;
  isActive: boolean;
}>;

// ── 5L: Shift Handover ──────────────────────────────────────────────────────────

export type ShiftHandoverSummary = Readonly<{
  id: string;
  fromShiftAssignmentId: string;
  toShiftAssignmentId?: string;
  note: string;
  openIssuesSummary?: string;
  createdAtUtc: string;
  createdBySubjectId: string;
}>;

// ── 5L: Exception Ownership ─────────────────────────────────────────────────────

export type ExceptionOwnershipLabel =
  | "Sahipsiz"
  | "Bana Atandı"
  | "Başka Operatöre Atandı"
  | "Çözüldü";

export function getExceptionOwnershipLabel(
  status: string,
  assignedToSubjectId: string | undefined,
  currentSubjectId: string | undefined,
): ExceptionOwnershipLabel {
  if (status === "resolved") return "Çözüldü";
  if (!assignedToSubjectId) return "Sahipsiz";
  if (currentSubjectId && assignedToSubjectId === currentSubjectId) return "Bana Atandı";
  return "Başka Operatöre Atandı";
}

// ── 5L: Shift Execution Summary ─────────────────────────────────────────────────

export type ShiftExecutionSummary = Readonly<{
  shiftId: string;
  shiftCode: string;
  shiftStartAtUtc: string;
  shiftEndAtUtc: string;
  activeOrderCount: number;
  completedOrderCount: number;
  goodQuantity: number;
  scrapQuantity: number;
  downtimeSeconds: number;
  setupSeconds: number;
  manualCycleCount: number;
  openExceptionCount: number;
  criticalExceptionCount: number;
}>;

// ── 5L: Operator Execution Summary ─────────────────────────────────────────────

export type OperatorExecutionSummary = Readonly<{
  subjectId: string;
  activeOrderCount: number;
  completedOrderCount: number;
  goodQuantity: number;
  scrapQuantity: number;
  assignedExceptionCount: number;
  lastActivityAtUtc?: string;
}>;
