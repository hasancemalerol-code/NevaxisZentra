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
    orderCount: asNumber(row.orderCount) ?? 0,
  };
}

export function parseCanonicalErpOrderListApiResponse(
  payload: unknown,
): readonly import("../domain/erp").CanonicalErpOrderSummary[] {
  if (!Array.isArray(payload)) {
    throw new Error("Invalid ERP order list response.");
  }

  return payload.map((item) => {
    const row = asRecord(item);
    if (!row) {
      throw new Error("Invalid ERP order row.");
    }

    const id = asString(row.id);
    const connectionProfileId = asString(row.connectionProfileId);
    const provider = asString(row.provider);
    const externalId = asString(row.externalId);
    const orderNumber = asString(row.orderNumber);
    const externalCustomerId = asString(row.externalCustomerId);
    const orderDateUtc = asString(row.orderDateUtc);
    const status = asString(row.status);
    const importedAtUtc = asString(row.importedAtUtc);
    const updatedAtUtc = asString(row.updatedAtUtc);

    if (
      !id ||
      !connectionProfileId ||
      !provider ||
      !externalId ||
      !orderNumber ||
      !externalCustomerId ||
      !orderDateUtc ||
      !status ||
      !importedAtUtc ||
      !updatedAtUtc
    ) {
      throw new Error("ERP order response is missing required fields.");
    }

    return {
      id,
      connectionProfileId,
      provider,
      externalId,
      externalCode: asString(row.externalCode),
      orderNumber,
      externalCustomerId,
      externalCustomerCode: asString(row.externalCustomerCode),
      orderDateUtc,
      requestedDeliveryDateUtc: asString(row.requestedDeliveryDateUtc),
      currencyCode: asString(row.currencyCode),
      status,
      reference: asString(row.reference),
      sourceVersion: asString(row.sourceVersion),
      sourceUpdatedAtUtc: asString(row.sourceUpdatedAtUtc),
      importedAtUtc,
      updatedAtUtc,
      lineCount: asNumber(row.lineCount) ?? 0,
      customerMappingStatus: asString(row.customerMappingStatus) ?? "unmapped",
      readyForOperationalUse: asBoolean(row.readyForOperationalUse) ?? false,
      sourceFreshness: asString(row.sourceFreshness) ?? "unknown",
    };
  });
}

export function parseCanonicalErpOrderDetailApiResponse(
  payload: unknown,
): import("../domain/erp").CanonicalErpOrderDetail {
  const row = asRecord(payload);
  if (!row) {
    throw new Error("Invalid ERP order detail response.");
  }

  const summary = parseCanonicalErpOrderListApiResponse([payload])[0];
  const readinessRow = asRecord(row.readiness);
  const lines = Array.isArray(row.lines) ? row.lines : [];

  return {
    ...summary,
    notes: asString(row.notes),
    lines: lines.map((lineItem) => {
      const line = asRecord(lineItem);
      const lineId = asString(line?.id);
      const externalLineId = asString(line?.externalLineId);
      const externalProductId = asString(line?.externalProductId);
      const unit = asString(line?.unit);

      if (!lineId || !externalLineId || !externalProductId || !unit) {
        throw new Error("ERP order line response is missing required fields.");
      }

      return {
        id: lineId,
        externalLineId,
        lineNumber: asNumber(line?.lineNumber) ?? 0,
        externalProductId,
        externalProductCode: asString(line?.externalProductCode),
        description: asString(line?.description),
        quantity: asNumber(line?.quantity) ?? 0,
        unit,
        requestedDeliveryDateUtc: asString(line?.requestedDeliveryDateUtc),
        sourceVersion: asString(line?.sourceVersion),
        productMappingStatus: asString(line?.productMappingStatus) ?? "unmapped",
        mappedInternalProductId: asString(line?.mappedInternalProductId),
      };
    }),
    readiness: {
      orderId: asString(readinessRow?.orderId) ?? summary.id,
      customerMapped: asBoolean(readinessRow?.customerMapped) ?? false,
      allProductsMapped: asBoolean(readinessRow?.allProductsMapped) ?? false,
      missingProductMappings: asNumber(readinessRow?.missingProductMappings) ?? 0,
      sourceFreshness: asString(readinessRow?.sourceFreshness) ?? "unknown",
      readyForOperationalUse: asBoolean(readinessRow?.readyForOperationalUse) ?? false,
      blockingReasons: Array.isArray(readinessRow?.blockingReasons)
        ? readinessRow.blockingReasons.map((reason) => {
            const r = asRecord(reason);
            return {
              code: asString(r?.code) ?? "unknown",
              message: asString(r?.message) ?? "",
            };
          })
        : [],
    },
  };
}

function parseReconciliationBase(row: Record<string, unknown>) {
  const connectionProfileId = asString(row.connectionProfileId);
  const provider = asString(row.provider);
  const externalId = asString(row.externalId);
  const name = asString(row.name);
  const updatedAtUtc = asString(row.updatedAtUtc);
  const mappingStatus = asString(row.mappingStatus);

  if (!connectionProfileId || !provider || !externalId || !name || !updatedAtUtc || !mappingStatus) {
    throw new Error("ERP reconciliation response is missing required fields.");
  }

  return {
    connectionProfileId,
    provider,
    externalId,
    externalCode: asString(row.externalCode),
    name,
    mappingStatus,
    mappingId: asString(row.mappingId),
    mappedInternalEntityId: asString(row.mappedInternalEntityId),
    suggestedInternalEntityId: asString(row.suggestedInternalEntityId),
    suggestedReason: asString(row.suggestedReason),
    sourceVersion: asString(row.sourceVersion),
    sourceUpdatedAtUtc: asString(row.sourceUpdatedAtUtc),
    updatedAtUtc,
    mappingVersion: asNumber(row.mappingVersion),
  };
}

export function parseErpCustomerReconciliationListApiResponse(
  payload: unknown,
): readonly import("../domain/erp").ErpCustomerReconciliation[] {
  if (!Array.isArray(payload)) {
    throw new Error("Invalid ERP customer reconciliation list response.");
  }

  return payload.map((item) => {
    const row = asRecord(item);
    if (!row) {
      throw new Error("Invalid ERP customer reconciliation row.");
    }

    const canonicalCustomerId = asString(row.canonicalCustomerId);
    const taxNumber = asString(row.taxNumber);
    const base = parseReconciliationBase({ ...row, name: asString(row.name) ?? "" });

    if (!canonicalCustomerId) {
      throw new Error("ERP customer reconciliation response is missing canonicalCustomerId.");
    }

    return {
      canonicalCustomerId,
      taxNumber,
      ...base,
    };
  });
}

export function parseErpProductReconciliationListApiResponse(
  payload: unknown,
): readonly import("../domain/erp").ErpProductReconciliation[] {
  if (!Array.isArray(payload)) {
    throw new Error("Invalid ERP product reconciliation list response.");
  }

  return payload.map((item) => {
    const row = asRecord(item);
    if (!row) {
      throw new Error("Invalid ERP product reconciliation row.");
    }

    const canonicalProductId = asString(row.canonicalProductId);
    const base = parseReconciliationBase({ ...row, name: asString(row.name) ?? "" });

    if (!canonicalProductId) {
      throw new Error("ERP product reconciliation response is missing canonicalProductId.");
    }

    return {
      canonicalProductId,
      unit: asString(row.unit),
      barcode: asString(row.barcode),
      ...base,
    };
  });
}

function parseWarehouseBreakdown(payload: unknown): import("../domain/erp").ErpInventoryWarehouseBreakdown {
  const row = asRecord(payload);
  const externalWarehouseId = asString(row?.externalWarehouseId);

  if (!externalWarehouseId) {
    throw new Error("ERP inventory warehouse breakdown is missing externalWarehouseId.");
  }

  return {
    externalWarehouseId,
    externalWarehouseCode: asString(row?.externalWarehouseCode),
    warehouseName: asString(row?.warehouseName),
    onHand: asNumber(row?.onHand) ?? 0,
    available: asNumber(row?.available),
    reserved: asNumber(row?.reserved),
    onOrder: asNumber(row?.onOrder),
  };
}

export function parseCanonicalErpWarehouseListApiResponse(
  payload: unknown,
): readonly import("../domain/erp").CanonicalErpWarehouse[] {
  if (!Array.isArray(payload)) {
    throw new Error("Invalid ERP warehouse list response.");
  }

  return payload.map((item) => {
    const row = asRecord(item);
    if (!row) {
      throw new Error("Invalid ERP warehouse row.");
    }

    const id = asString(row.id);
    const connectionProfileId = asString(row.connectionProfileId);
    const provider = asString(row.provider);
    const externalId = asString(row.externalId);
    const name = asString(row.name);
    const importedAtUtc = asString(row.importedAtUtc);
    const updatedAtUtc = asString(row.updatedAtUtc);

    if (!id || !connectionProfileId || !provider || !externalId || !name || !importedAtUtc || !updatedAtUtc) {
      throw new Error("ERP warehouse response is missing required fields.");
    }

    return {
      id,
      connectionProfileId,
      provider,
      externalId,
      externalCode: asString(row.externalCode),
      name,
      isActive: asBoolean(row.isActive) ?? true,
      sourceVersion: asString(row.sourceVersion),
      sourceUpdatedAtUtc: asString(row.sourceUpdatedAtUtc),
      importedAtUtc,
      updatedAtUtc,
      freshness: asString(row.freshness) ?? "unknown",
    };
  });
}

export function parseCanonicalErpInventoryBalanceListApiResponse(
  payload: unknown,
): readonly import("../domain/erp").CanonicalErpInventoryBalance[] {
  if (!Array.isArray(payload)) {
    throw new Error("Invalid ERP inventory balance list response.");
  }

  return payload.map((item) => {
    const row = asRecord(item);
    if (!row) {
      throw new Error("Invalid ERP inventory balance row.");
    }

    const id = asString(row.id);
    const connectionProfileId = asString(row.connectionProfileId);
    const provider = asString(row.provider);
    const externalProductId = asString(row.externalProductId);
    const externalWarehouseId = asString(row.externalWarehouseId);
    const unit = asString(row.unit);
    const importedAtUtc = asString(row.importedAtUtc);
    const updatedAtUtc = asString(row.updatedAtUtc);

    if (
      !id ||
      !connectionProfileId ||
      !provider ||
      !externalProductId ||
      !externalWarehouseId ||
      !unit ||
      !importedAtUtc ||
      !updatedAtUtc
    ) {
      throw new Error("ERP inventory balance response is missing required fields.");
    }

    return {
      id,
      connectionProfileId,
      provider,
      externalProductId,
      externalProductCode: asString(row.externalProductCode),
      productName: asString(row.productName),
      externalWarehouseId,
      externalWarehouseCode: asString(row.externalWarehouseCode),
      warehouseName: asString(row.warehouseName),
      quantityOnHand: asNumber(row.quantityOnHand) ?? 0,
      quantityAvailable: asNumber(row.quantityAvailable),
      quantityReserved: asNumber(row.quantityReserved),
      quantityOnOrder: asNumber(row.quantityOnOrder),
      unit,
      sourceVersion: asString(row.sourceVersion),
      sourceUpdatedAtUtc: asString(row.sourceUpdatedAtUtc),
      importedAtUtc,
      updatedAtUtc,
      freshness: asString(row.freshness) ?? "unknown",
    };
  });
}

export function parseErpInventoryAvailabilityListApiResponse(
  payload: unknown,
): readonly import("../domain/erp").ErpInventoryAvailability[] {
  if (!Array.isArray(payload)) {
    throw new Error("Invalid ERP inventory availability list response.");
  }

  return payload.map((item) => {
    const row = asRecord(item);
    const externalProductId = asString(row?.externalProductId);

    if (!externalProductId) {
      throw new Error("ERP inventory availability response is missing externalProductId.");
    }

    return {
      externalProductId,
      externalProductCode: asString(row?.externalProductCode),
      productName: asString(row?.productName),
      totalOnHand: asNumber(row?.totalOnHand) ?? 0,
      totalAvailable: asNumber(row?.totalAvailable),
      warehouseBreakdown: Array.isArray(row?.warehouseBreakdown)
        ? row.warehouseBreakdown.map(parseWarehouseBreakdown)
        : [],
      freshness: asString(row?.freshness) ?? "unknown",
      lastSuccessfulSyncAtUtc: asString(row?.lastSuccessfulSyncAtUtc),
    };
  });
}

function parsePurchaseOrderSummaryFromRow(row: Record<string, unknown>) {
  const id = asString(row.id);
  const connectionProfileId = asString(row.connectionProfileId);
  const provider = asString(row.provider);
  const externalId = asString(row.externalId);
  const purchaseOrderNumber = asString(row.purchaseOrderNumber);
  const orderDateUtc = asString(row.orderDateUtc);
  const status = asString(row.status);
  const importedAtUtc = asString(row.importedAtUtc);
  const updatedAtUtc = asString(row.updatedAtUtc);

  if (
    !id ||
    !connectionProfileId ||
    !provider ||
    !externalId ||
    !purchaseOrderNumber ||
    !orderDateUtc ||
    !status ||
    !importedAtUtc ||
    !updatedAtUtc
  ) {
    throw new Error("ERP purchase order response is missing required fields.");
  }

  return {
    id,
    connectionProfileId,
    provider,
    externalId,
    externalCode: asString(row.externalCode),
    purchaseOrderNumber,
    externalSupplierId: asString(row.externalSupplierId),
    externalSupplierCode: asString(row.externalSupplierCode),
    supplierName: asString(row.supplierName),
    orderDateUtc,
    expectedDeliveryDateUtc: asString(row.expectedDeliveryDateUtc),
    status,
    currencyCode: asString(row.currencyCode),
    sourceVersion: asString(row.sourceVersion),
    sourceUpdatedAtUtc: asString(row.sourceUpdatedAtUtc),
    importedAtUtc,
    updatedAtUtc,
    lineCount: asNumber(row.lineCount) ?? 0,
    openLineCount: asNumber(row.openLineCount) ?? 0,
    freshness: asString(row.freshness) ?? "unknown",
  };
}

export function parseCanonicalErpPurchaseOrderListApiResponse(
  payload: unknown,
): readonly import("../domain/erp").CanonicalErpPurchaseOrderSummary[] {
  if (!Array.isArray(payload)) {
    throw new Error("Invalid ERP purchase order list response.");
  }

  return payload.map((item) => {
    const row = asRecord(item);
    if (!row) {
      throw new Error("Invalid ERP purchase order row.");
    }

    return parsePurchaseOrderSummaryFromRow(row);
  });
}

export function parseCanonicalErpPurchaseOrderDetailApiResponse(
  payload: unknown,
): import("../domain/erp").CanonicalErpPurchaseOrderDetail {
  const row = asRecord(payload);
  if (!row) {
    throw new Error("Invalid ERP purchase order detail response.");
  }

  const summary = parsePurchaseOrderSummaryFromRow(row);
  const lines = Array.isArray(row.lines) ? row.lines : [];

  return {
    ...summary,
    lines: lines.map((lineItem) => {
      const line = asRecord(lineItem);
      const lineId = asString(line?.id);
      const externalLineId = asString(line?.externalLineId);
      const externalProductId = asString(line?.externalProductId);
      const unit = asString(line?.unit);

      if (!lineId || !externalLineId || !externalProductId || !unit) {
        throw new Error("ERP purchase order line response is missing required fields.");
      }

      return {
        id: lineId,
        externalLineId,
        lineNumber: asNumber(line?.lineNumber) ?? 0,
        externalProductId,
        externalProductCode: asString(line?.externalProductCode),
        description: asString(line?.description),
        orderedQuantity: asNumber(line?.orderedQuantity) ?? 0,
        receivedQuantity: asNumber(line?.receivedQuantity),
        openQuantity: asNumber(line?.openQuantity),
        unit,
        expectedDeliveryDateUtc: asString(line?.expectedDeliveryDateUtc),
        sourceVersion: asString(line?.sourceVersion),
      };
    }),
  };
}

export function parseErpSyncReliabilityListApiResponse(
  payload: unknown,
): readonly import("../domain/erp").ErpSyncReliability[] {
  if (!Array.isArray(payload)) {
    throw new Error("Invalid ERP sync reliability list response.");
  }

  return payload.map((item) => {
    const row = asRecord(item);
    const entityType = asString(row?.entityType);

    if (!entityType) {
      throw new Error("ERP sync reliability response is missing entityType.");
    }

    return {
      entityType,
      lastSuccessfulSyncAtUtc: asString(row?.lastSuccessfulSyncAtUtc),
      lastFailedSyncAtUtc: asString(row?.lastFailedSyncAtUtc),
      lastRunStatus: asString(row?.lastRunStatus),
      lastRecordsRead: asNumber(row?.lastRecordsRead) ?? 0,
      lastRecordsFailed: asNumber(row?.lastRecordsFailed) ?? 0,
      consecutiveFailures: asNumber(row?.consecutiveFailures) ?? 0,
      freshnessStatus: asString(row?.freshnessStatus) ?? "unknown",
      currentErrorCode: asString(row?.currentErrorCode),
      currentErrorMessage: asString(row?.currentErrorMessage),
    };
  });
}

function parsePurchaseCoverage(payload: unknown): import("../domain/erp").ErpPurchaseCoverage | null {
  const row = asRecord(payload);
  if (!row) {
    return null;
  }

  return {
    openPurchaseQuantity: asNumber(row.openPurchaseQuantity) ?? 0,
    nearestExpectedDeliveryDateUtc: asString(row.nearestExpectedDeliveryDate),
    coverageStatus: asString(row.coverageStatus) ?? "unknown",
  };
}

export function parseErpOrderMaterialAvailabilityApiResponse(
  payload: unknown,
): import("../domain/erp").ErpOrderMaterialAvailability {
  const row = asRecord(payload);
  if (!row) {
    throw new Error("Invalid ERP order material availability response.");
  }

  const orderId = asString(row.orderId);
  const orderNumber = asString(row.orderNumber);
  const freshnessNote = asString(row.freshnessNote);
  const lines = Array.isArray(row.lines) ? row.lines : [];

  if (!orderId || !orderNumber || !freshnessNote) {
    throw new Error("ERP order material availability response is missing required fields.");
  }

  return {
    orderId,
    orderNumber,
    freshnessNote,
    lines: lines.map((lineItem) => {
      const line = asRecord(lineItem);
      const externalLineId = asString(line?.externalLineId);
      const externalProductId = asString(line?.externalProductId);

      if (!externalLineId || !externalProductId) {
        throw new Error("ERP material availability line response is missing required fields.");
      }

      return {
        externalLineId,
        lineNumber: asNumber(line?.lineNumber) ?? 0,
        externalProductId,
        externalProductCode: asString(line?.externalProductCode),
        description: asString(line?.description),
        requiredQuantity: asNumber(line?.requiredQuantity) ?? 0,
        availableQuantity: asNumber(line?.availableQuantity),
        shortageQuantity: asNumber(line?.shortageQuantity),
        status: asString(line?.status) ?? "unknown",
        purchaseCoverage: parsePurchaseCoverage(line?.purchaseCoverage),
      };
    }),
  };
}
