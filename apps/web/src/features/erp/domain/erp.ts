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
  orderCount: number;
}>;

export type ErpOrderStatus =
  | "open"
  | "confirmed"
  | "partially-fulfilled"
  | "fulfilled"
  | "cancelled"
  | "unknown";

export type ErpMappingStatus =
  | "unmapped"
  | "suggested"
  | "mapped"
  | "ignored"
  | "conflict";

export type ErpSourceFreshness = "fresh" | "stale" | "unknown";

export type CanonicalErpOrderSummary = Readonly<{
  id: string;
  connectionProfileId: string;
  provider: string;
  externalId: string;
  externalCode: string | null;
  orderNumber: string;
  externalCustomerId: string;
  externalCustomerCode: string | null;
  orderDateUtc: string;
  requestedDeliveryDateUtc: string | null;
  currencyCode: string | null;
  status: ErpOrderStatus | string;
  reference: string | null;
  sourceVersion: string | null;
  sourceUpdatedAtUtc: string | null;
  importedAtUtc: string;
  updatedAtUtc: string;
  lineCount: number;
  customerMappingStatus: ErpMappingStatus | string;
  readyForOperationalUse: boolean;
  sourceFreshness: ErpSourceFreshness | string;
}>;

export type CanonicalErpOrderLine = Readonly<{
  id: string;
  externalLineId: string;
  lineNumber: number;
  externalProductId: string;
  externalProductCode: string | null;
  description: string | null;
  quantity: number;
  unit: string;
  requestedDeliveryDateUtc: string | null;
  sourceVersion: string | null;
  productMappingStatus: ErpMappingStatus | string;
  mappedInternalProductId: string | null;
}>;

export type ErpReadinessBlockingReason = Readonly<{
  code: string;
  message: string;
}>;

export type ErpOrderReadiness = Readonly<{
  orderId: string;
  customerMapped: boolean;
  allProductsMapped: boolean;
  missingProductMappings: number;
  sourceFreshness: ErpSourceFreshness | string;
  readyForOperationalUse: boolean;
  blockingReasons: readonly ErpReadinessBlockingReason[];
}>;

export type CanonicalErpOrderDetail = Readonly<{
  id: string;
  connectionProfileId: string;
  provider: string;
  externalId: string;
  externalCode: string | null;
  orderNumber: string;
  externalCustomerId: string;
  externalCustomerCode: string | null;
  orderDateUtc: string;
  requestedDeliveryDateUtc: string | null;
  currencyCode: string | null;
  status: ErpOrderStatus | string;
  reference: string | null;
  notes: string | null;
  sourceVersion: string | null;
  sourceUpdatedAtUtc: string | null;
  importedAtUtc: string;
  updatedAtUtc: string;
  lines: readonly CanonicalErpOrderLine[];
  readiness: ErpOrderReadiness;
}>;

export type ErpMapping = Readonly<{
  id: string;
  connectionProfileId: string;
  entityType: string;
  externalId: string;
  canonicalEntityId: string | null;
  internalEntityId: string | null;
  suggestedInternalEntityId: string | null;
  suggestedReason: string | null;
  status: ErpMappingStatus | string;
  createdAtUtc: string;
  updatedAtUtc: string;
  version: number;
}>;

export type ErpCustomerReconciliation = Readonly<{
  canonicalCustomerId: string;
  connectionProfileId: string;
  provider: string;
  externalId: string;
  externalCode: string | null;
  name: string;
  taxNumber: string | null;
  mappingStatus: ErpMappingStatus | string;
  mappingId: string | null;
  mappedInternalEntityId: string | null;
  suggestedInternalEntityId: string | null;
  suggestedReason: string | null;
  sourceVersion: string | null;
  sourceUpdatedAtUtc: string | null;
  updatedAtUtc: string;
  mappingVersion: number | null;
}>;

export type ErpProductReconciliation = Readonly<{
  canonicalProductId: string;
  connectionProfileId: string;
  provider: string;
  externalId: string;
  externalCode: string | null;
  name: string;
  unit: string | null;
  barcode: string | null;
  mappingStatus: ErpMappingStatus | string;
  mappingId: string | null;
  mappedInternalEntityId: string | null;
  suggestedInternalEntityId: string | null;
  suggestedReason: string | null;
  sourceVersion: string | null;
  sourceUpdatedAtUtc: string | null;
  updatedAtUtc: string;
  mappingVersion: number | null;
}>;
