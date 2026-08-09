export type MaintenanceWorkspaceSummary = Readonly<{
  openRequestCount: number;
  openWorkOrderCount: number;
  criticalWorkOrderCount: number;
  inProgressCount: number;
  waitingCount: number;
  completedTodayCount: number;
  overduePreventiveCount: number;
}>;

export type MaintenanceRequestSummary = Readonly<{
  id: string;
  requestNumber: string;
  assetType: string;
  assetId?: string;
  assetReference: string;
  assetName?: string;
  title: string;
  type: string;
  priority: string;
  status: string;
  reportedBySubjectId: string;
  reportedAtUtc: string;
  version: number;
  updatedAtUtc: string;
}>;

export type MaintenanceActivity = Readonly<{
  action: string;
  eventType: string;
  occurredAtUtc: string;
  actorSubjectId?: string;
  summary: string;
}>;

export type MaintenanceRequestDetail = Readonly<{
  id: string;
  requestNumber: string;
  assetType: string;
  assetId?: string;
  assetReference: string;
  assetName?: string;
  productionOrderId?: string;
  productionOrderReference?: string;
  productionOrderStatus?: string;
  type: string;
  priority: string;
  status: string;
  title: string;
  description: string;
  source: string;
  reportedBySubjectId: string;
  reportedAtUtc: string;
  version: number;
  createdAtUtc: string;
  updatedAtUtc: string;
  activity: readonly MaintenanceActivity[];
}>;

export type MaintenanceWorkOrderSummary = Readonly<{
  id: string;
  workOrderNumber: string;
  maintenanceRequestId?: string;
  maintenanceRequestNumber?: string;
  assetType: string;
  assetId?: string;
  assetReference: string;
  assetName?: string;
  type: string;
  priority: string;
  status: string;
  assignedTechnicianSubjectId?: string;
  plannedStartUtc?: string;
  plannedEndUtc?: string;
  startedAtUtc?: string;
  version: number;
  updatedAtUtc: string;
}>;

export type MaintenanceWorkOrderDetail = Readonly<{
  id: string;
  workOrderNumber: string;
  maintenanceRequestId?: string;
  maintenanceRequestNumber?: string;
  assetType: string;
  assetId?: string;
  assetReference: string;
  assetName?: string;
  productionOrderId?: string;
  productionOrderReference?: string;
  productionOrderStatus?: string;
  preventivePlanId?: string;
  preventivePlanCode?: string;
  preventiveDueId?: string;
  externalServiceCost?: number;
  otherCost?: number;
  type: string;
  priority: string;
  status: string;
  title: string;
  description: string;
  assignedTechnicianSubjectId?: string;
  plannedStartUtc?: string;
  plannedEndUtc?: string;
  startedAtUtc?: string;
  completedAtUtc?: string;
  completionNote?: string;
  resolutionCode?: string;
  createdBySubjectId: string;
  version: number;
  createdAtUtc: string;
  updatedAtUtc: string;
  activity: readonly MaintenanceActivity[];
}>;

export type MaintenanceWorkNote = Readonly<{
  id: string;
  workOrderId: string;
  noteType: string;
  text: string;
  isImportant: boolean;
  createdAtUtc: string;
  createdBySubjectId: string;
}>;

export type MaintenanceTimelineEntry = Readonly<{
  entryType: string;
  title: string;
  detail?: string;
  occurredAtUtc: string;
  actorSubjectId?: string;
}>;

export type MaintenancePartUsage = Readonly<{
  id: string;
  workOrderId: string;
  partReference: string;
  partName?: string;
  quantity: number;
  unit: string;
  sourceSystem?: string;
  sourceReference?: string;
  estimatedUnitCost?: number;
  actualUnitCost?: number;
  estimatedLineCost?: number;
  actualLineCost?: number;
  recordedAtUtc: string;
  recordedBySubjectId: string;
}>;

export type MaintenanceCostSummary = Readonly<{
  workOrderId: string;
  estimatedPartsCost?: number;
  actualPartsCost?: number;
  externalServiceCost?: number;
  otherCost?: number;
  totalMaintenanceCost?: number;
}>;

export type PreventiveMaintenanceDueSummary = Readonly<{
  id: string;
  preventivePlanId: string;
  preventivePlanCode?: string;
  preventivePlanName?: string;
  assetType: string;
  assetReference: string;
  dueAtUtc: string;
  status: string;
  generatedAtUtc: string;
  workOrderId?: string;
  workOrderNumber?: string;
  completedAtUtc?: string;
  version: number;
}>;

export type PreventiveMaintenanceDueDetail = Readonly<{
  id: string;
  preventivePlanId: string;
  preventivePlanCode: string;
  preventivePlanName: string;
  assetType: string;
  assetReference: string;
  dueAtUtc: string;
  status: string;
  generatedAtUtc: string;
  workOrderId?: string;
  workOrderNumber?: string;
  completedAtUtc?: string;
  version: number;
}>;

export type MaintenanceAssetHistory = Readonly<{
  assetReference: string;
  requests: readonly MaintenanceRequestSummary[];
  workOrders: readonly MaintenanceWorkOrderSummary[];
  preventivePlans: readonly PreventiveMaintenancePlanSummary[];
  preventiveDues: readonly PreventiveMaintenanceDueSummary[];
  workNotes: readonly MaintenanceWorkNote[];
  partUsages: readonly MaintenancePartUsage[];
  costSummaries: readonly MaintenanceCostSummary[];
}>;

export type PreventiveMaintenancePlanSummary = Readonly<{
  id: string;
  code: string;
  name: string;
  assetType: string;
  assetId?: string;
  assetReference: string;
  intervalType: string;
  intervalValue: number;
  nextDueAtUtc?: string;
  status: string;
  version: number;
  updatedAtUtc: string;
}>;
