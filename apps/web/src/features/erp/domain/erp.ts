export type ErpConnectionStatus =
  | "draft"
  | "configured"
  | "healthy"
  | "degraded"
  | "unavailable"
  | "disabled";

export type ErpSyncRunStatus =
  | "running"
  | "completed"
  | "completed-with-errors"
  | "failed";

export type ErpConnection = Readonly<{
  id: string;
  facilityId: string;
  provider: string;
  name: string;
  externalCompanyId: string | null;
  externalCompanyCode: string | null;
  connectionMode: string;
  status: ErpConnectionStatus;
  lastSuccessfulConnectionAtUtc: string | null;
  lastFailedConnectionAtUtc: string | null;
  lastHealthCheckAtUtc: string | null;
  lastHealthLatencyMs: number | null;
  lastErrorCode: string | null;
  lastErrorMessage: string | null;
  lastSuccessfulSyncAtUtc: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  version: number;
  hasCredentialReference: boolean;
  providerDisplayName: string;
}>;

export type ErpSyncRun = Readonly<{
  id: string;
  connectionProfileId: string;
  entityType: string;
  startedAtUtc: string;
  completedAtUtc: string | null;
  status: ErpSyncRunStatus;
  recordsRead: number;
  recordsCreated: number;
  recordsUpdated: number;
  recordsSkipped: number;
  recordsFailed: number;
  errorCode: string | null;
  errorMessage: string | null;
  triggeredBySubjectId: string;
  errors: readonly ErpIntegrationError[];
}>;

export type ErpIntegrationError = Readonly<{
  code: string;
  entityType: string;
  externalId: string | null;
  message: string;
  occurredAtUtc: string;
}>;

export type CanonicalErpCustomer = Readonly<{
  id: string;
  connectionProfileId: string;
  provider: string;
  externalId: string;
  externalCode: string | null;
  name: string;
  taxNumber: string | null;
  taxOffice: string | null;
  email: string | null;
  phone: string | null;
  countryCode: string | null;
  isActive: boolean;
  sourceVersion: string | null;
  sourceUpdatedAtUtc: string | null;
  importedAtUtc: string;
  updatedAtUtc: string;
}>;

export type CanonicalErpProduct = Readonly<{
  id: string;
  connectionProfileId: string;
  provider: string;
  externalId: string;
  externalCode: string | null;
  name: string;
  unit: string | null;
  barcode: string | null;
  isActive: boolean;
  sourceVersion: string | null;
  sourceUpdatedAtUtc: string | null;
  importedAtUtc: string;
  updatedAtUtc: string;
}>;

export type ErpWorkspaceSummary = Readonly<{
  connectionCount: number;
  healthyCount: number;
  problematicCount: number;
  lastSyncAtUtc: string | null;
  customerCount: number;
  productCount: number;
}>;
