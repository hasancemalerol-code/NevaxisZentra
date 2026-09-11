export type ShiftInstanceStatus =
  | "planned"
  | "ready"
  | "active"
  | "closing"
  | "handover-pending"
  | "handed-over"
  | "closed"
  | "exception";

export type ShiftOpenItemCategory =
  | "production"
  | "machine"
  | "maintenance"
  | "quality"
  | "material"
  | "supplier"
  | "delivery"
  | "mold"
  | "approval"
  | "digital-workforce"
  | "task"
  | "other";
