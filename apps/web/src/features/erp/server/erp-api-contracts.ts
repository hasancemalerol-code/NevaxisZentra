import type {
  CanonicalErpCustomer,
  CanonicalErpProduct,
  ErpConnection,
  ErpSyncRun,
  ErpWorkspaceSummary,
} from "../domain/erp";

function asRecord(value: unknown): Record<string, unknown> | null {
  return value !== null && typeof value === "object" ? (value as Record<string, unknown>) : null;
}

function asString(value: unknown): string | null {
  return typeof value === "string" ? value : null;
}

function asNumber(value: unknown): number | null {
  return typeof value === "number" && Number.isFinite(value) ? value : null;
}

function asBoolean(value: unknown): boolean | null {
  return typeof value === "boolean" ? value : null;
}

export function parseErpConnectionListApiResponse(payload: unknown): readonly ErpConnection[] {
  if (!Array.isArray(payload)) {
    throw new Error("Invalid ERP connection list response.");
  }

  return payload.map(parseErpConnection);
}

export function parseErpConnection(payload: unknown): ErpConnection {
  const row = asRecord(payload);
  if (!row) {
    throw new Error("Invalid ERP connection response.");
  }

  const id = asString(row.id);
  const facilityId = asString(row.facilityId);
  const provider = asString(row.provider);
  const name = asString(row.name);
  const connectionMode = asString(row.connectionMode);
  const status = asString(row.status);
  const createdAtUtc = asString(row.createdAtUtc);
  const updatedAtUtc = asString(row.updatedAtUtc);
  const version = asNumber(row.version);
  const providerDisplayName = asString(row.providerDisplayName) ?? provider ?? "";

  if (!id || !facilityId || !provider || !name || !connectionMode || !status || !createdAtUtc || !updatedAtUtc || version === null) {
    throw new Error("ERP connection response is missing required fields.");
  }

  return {
    id,
    facilityId,
    provider,
    name,
    externalCompanyId: asString(row.externalCompanyId),
    externalCompanyCode: asString(row.externalCompanyCode),
    connectionMode,
    status: status as ErpConnection["status"],
    lastSuccessfulConnectionAtUtc: asString(row.lastSuccessfulConnectionAtUtc),
    lastFailedConnectionAtUtc: asString(row.lastFailedConnectionAtUtc),
    lastHealthCheckAtUtc: asString(row.lastHealthCheckAtUtc),
    lastHealthLatencyMs: asNumber(row.lastHealthLatencyMs),
    lastErrorCode: asString(row.lastErrorCode),
    lastErrorMessage: asString(row.lastErrorMessage),
    lastSuccessfulSyncAtUtc: asString(row.lastSuccessfulSyncAtUtc),
    createdAtUtc,
    updatedAtUtc,
    version,
    hasCredentialReference: asBoolean(row.hasCredentialReference) ?? false,
    providerDisplayName,
  };
}

export function parseErpSyncRunListApiResponse(payload: unknown): readonly ErpSyncRun[] {
  if (!Array.isArray(payload)) {
    throw new Error("Invalid ERP sync run list response.");
  }

  return payload.map(parseErpSyncRun);
}

export function parseErpSyncRun(payload: unknown): ErpSyncRun {
  const row = asRecord(payload);
  if (!row) {
    throw new Error("Invalid ERP sync run response.");
  }

  const id = asString(row.id);
  const connectionProfileId = asString(row.connectionProfileId);
  const entityType = asString(row.entityType);
  const startedAtUtc = asString(row.startedAtUtc);
  const status = asString(row.status);
  const triggeredBySubjectId = asString(row.triggeredBySubjectId);

  if (!id || !connectionProfileId || !entityType || !startedAtUtc || !status || !triggeredBySubjectId) {
    throw new Error("ERP sync run response is missing required fields.");
  }

  return {
    id,
    connectionProfileId,
    entityType,
    startedAtUtc,
    completedAtUtc: asString(row.completedAtUtc),
    status: status as ErpSyncRun["status"],
    recordsRead: asNumber(row.recordsRead) ?? 0,
    recordsCreated: asNumber(row.recordsCreated) ?? 0,
    recordsUpdated: asNumber(row.recordsUpdated) ?? 0,
    recordsSkipped: asNumber(row.recordsSkipped) ?? 0,
    recordsFailed: asNumber(row.recordsFailed) ?? 0,
    errorCode: asString(row.errorCode),
    errorMessage: asString(row.errorMessage),
    triggeredBySubjectId,
    errors: Array.isArray(row.errors)
      ? row.errors.map((item) => {
          const error = asRecord(item);
          return {
            code: asString(error?.code) ?? "unknown",
            entityType: asString(error?.entityType) ?? entityType,
            externalId: asString(error?.externalId),
            message: asString(error?.message) ?? "",
            occurredAtUtc: asString(error?.occurredAtUtc) ?? startedAtUtc,
          };
        })
      : [],
  };
}

export function parseCanonicalErpCustomerListApiResponse(
  payload: unknown,
): readonly CanonicalErpCustomer[] {
  if (!Array.isArray(payload)) {
    throw new Error("Invalid ERP customer list response.");
  }

  return payload.map((item) => {
    const row = asRecord(item);
    if (!row) {
      throw new Error("Invalid ERP customer row.");
    }

    const id = asString(row.id);
    const connectionProfileId = asString(row.connectionProfileId);
    const provider = asString(row.provider);
    const externalId = asString(row.externalId);
    const name = asString(row.name);
    const importedAtUtc = asString(row.importedAtUtc);
    const updatedAtUtc = asString(row.updatedAtUtc);

    if (!id || !connectionProfileId || !provider || !externalId || !name || !importedAtUtc || !updatedAtUtc) {
      throw new Error("ERP customer response is missing required fields.");
    }

    return {
      id,
      connectionProfileId,
      provider,
      externalId,
      externalCode: asString(row.externalCode),
      name,
      taxNumber: asString(row.taxNumber),
      taxOffice: asString(row.taxOffice),
      email: asString(row.email),
      phone: asString(row.phone),
      countryCode: asString(row.countryCode),
      isActive: asBoolean(row.isActive) ?? true,
      sourceVersion: asString(row.sourceVersion),
      sourceUpdatedAtUtc: asString(row.sourceUpdatedAtUtc),
      importedAtUtc,
      updatedAtUtc,
    };
  });
}

export function parseCanonicalErpProductListApiResponse(
  payload: unknown,
): readonly CanonicalErpProduct[] {
  if (!Array.isArray(payload)) {
    throw new Error("Invalid ERP product list response.");
  }

  return payload.map((item) => {
    const row = asRecord(item);
    if (!row) {
      throw new Error("Invalid ERP product row.");
    }

    const id = asString(row.id);
    const connectionProfileId = asString(row.connectionProfileId);
    const provider = asString(row.provider);
    const externalId = asString(row.externalId);
    const name = asString(row.name);
    const importedAtUtc = asString(row.importedAtUtc);
    const updatedAtUtc = asString(row.updatedAtUtc);

    if (!id || !connectionProfileId || !provider || !externalId || !name || !importedAtUtc || !updatedAtUtc) {
      throw new Error("ERP product response is missing required fields.");
    }

    return {
      id,
      connectionProfileId,
      provider,
      externalId,
      externalCode: asString(row.externalCode),
      name,
      unit: asString(row.unit),
      barcode: asString(row.barcode),
      isActive: asBoolean(row.isActive) ?? true,
      sourceVersion: asString(row.sourceVersion),
      sourceUpdatedAtUtc: asString(row.sourceUpdatedAtUtc),
      importedAtUtc,
      updatedAtUtc,
    };
  });
}

export function parseErpWorkspaceSummaryApiResponse(payload: unknown): ErpWorkspaceSummary {
  const row = asRecord(payload);
  if (!row) {
    throw new Error("Invalid ERP workspace summary response.");
  }

  return {
    connectionCount: asNumber(row.connectionCount) ?? 0,
    healthyCount: asNumber(row.healthyCount) ?? 0,
    problematicCount: asNumber(row.problematicCount) ?? 0,
    lastSyncAtUtc: asString(row.lastSyncAtUtc),
    customerCount: asNumber(row.customerCount) ?? 0,
    productCount: asNumber(row.productCount) ?? 0,
  };
}
