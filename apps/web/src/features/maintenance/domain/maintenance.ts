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
