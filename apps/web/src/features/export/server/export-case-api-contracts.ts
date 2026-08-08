import "./export-case-server-boundary";

import type {
  ExportActivity,
  ExportActivitySourceType,
  ExportCaseDetail,
  ExportCaseStatus,
  ExportCaseSummary,
  ExportCaseWriteSuccess,
  ExportDocument,
  ExportDocumentStatus,
  ExportReadiness,
  ExportReadinessStatus,
  ExportRequirement,
} from "../domain/export-case";

const EXPORT_CASE_STATUSES = new Set<ExportCaseStatus>([
  "open",
  "preparing",
  "blocked",
  "ready",
  "closed",
]);

const EXPORT_READINESS_STATUSES = new Set<ExportReadinessStatus>([
  "not-ready",
  "attention",
  "ready",
]);

const EXPORT_DOCUMENT_STATUSES = new Set<ExportDocumentStatus>([
  "missing",
  "draft",
  "received",
  "verified",
  "rejected",
]);

const EXPORT_ACTIVITY_SOURCE_TYPES = new Set<ExportActivitySourceType>([
  "system",
  "user",
  "integration",
  "ai",
]);

export type ParseResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | Readonly<{ status: "failure"; message: string }>;

function fail<T>(message: string): ParseResult<T> {
  return { status: "failure", message };
}

function ok<T>(value: T): ParseResult<T> {
  return { status: "success", value };
}

function isString(v: unknown): v is string {
  return typeof v === "string";
}

function isOptionalString(v: unknown): v is string | undefined {
  return v === undefined || v === null || typeof v === "string";
}

function isNumber(v: unknown): v is number {
  return typeof v === "number" && Number.isFinite(v);
}

function isBoolean(v: unknown): v is boolean {
  return typeof v === "boolean";
}

function isValidCaseStatus(value: unknown): value is ExportCaseStatus {
  return typeof value === "string" && EXPORT_CASE_STATUSES.has(value as ExportCaseStatus);
}

function isValidReadinessStatus(value: unknown): value is ExportReadinessStatus {
  return (
    typeof value === "string" &&
    EXPORT_READINESS_STATUSES.has(value as ExportReadinessStatus)
  );
}

function isValidDocumentStatus(value: unknown): value is ExportDocumentStatus {
  return (
    typeof value === "string" &&
    EXPORT_DOCUMENT_STATUSES.has(value as ExportDocumentStatus)
  );
}

function isValidActivitySourceType(
  value: unknown,
): value is ExportActivitySourceType {
  return (
    typeof value === "string" &&
    EXPORT_ACTIVITY_SOURCE_TYPES.has(value as ExportActivitySourceType)
  );
}

function optionalString(value: unknown): string | undefined {
  if (value === undefined || value === null) {
    return undefined;
  }

  return typeof value === "string" ? value : undefined;
}

export function parseExportCaseSummaryApiResponse(
  raw: unknown,
): ParseResult<ExportCaseSummary> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.id)) return fail("id is required.");
  if (!isString(o.facilityId)) return fail("facilityId is required.");
  if (!isString(o.sourceOrderId)) return fail("sourceOrderId is required.");
  if (!isString(o.caseNumber)) return fail("caseNumber is required.");
  if (!isString(o.sourceOrderReference)) {
    return fail("sourceOrderReference is required.");
  }
  if (!isString(o.customerName)) return fail("customerName is required.");
  if (!isString(o.destinationCountryCode)) {
    return fail("destinationCountryCode is required.");
  }
  if (!isValidCaseStatus(o.status)) {
    return fail(`status has an invalid value: '${String(o.status)}'.`);
  }
  if (!isValidReadinessStatus(o.readinessStatus)) {
    return fail(
      `readinessStatus has an invalid value: '${String(o.readinessStatus)}'.`,
    );
  }
  if (!isNumber(o.readinessPercent)) return fail("readinessPercent is required.");
  if (!isNumber(o.blockingIssueCount)) {
    return fail("blockingIssueCount is required.");
  }
  if (!isNumber(o.missingRequirementCount)) {
    return fail("missingRequirementCount is required.");
  }
  if (!isNumber(o.version)) return fail("version is required.");
  if (!isString(o.updatedAtUtc)) return fail("updatedAtUtc is required.");

  return ok<ExportCaseSummary>({
    id: o.id,
    facilityId: o.facilityId,
    sourceOrderId: o.sourceOrderId,
    caseNumber: o.caseNumber,
    sourceOrderReference: o.sourceOrderReference,
    customerName: o.customerName,
    destinationCountryCode: o.destinationCountryCode,
    originCountryCode: optionalString(o.originCountryCode),
    incotermCode: optionalString(o.incotermCode),
    status: o.status,
    readinessStatus: o.readinessStatus,
    readinessPercent: o.readinessPercent,
    blockingIssueCount: o.blockingIssueCount,
    missingRequirementCount: o.missingRequirementCount,
    version: o.version,
    updatedAtUtc: o.updatedAtUtc,
  });
}

export function parseExportCaseListApiResponse(
  raw: unknown,
): ParseResult<ExportCaseSummary[]> {
  if (!Array.isArray(raw)) {
    return fail("Expected an array.");
  }

  const items: ExportCaseSummary[] = [];

  for (let i = 0; i < raw.length; i++) {
    const parsed = parseExportCaseSummaryApiResponse(raw[i]);

    if (parsed.status === "failure") {
      return fail(`Item ${i}: ${parsed.message}`);
    }

    items.push(parsed.value);
  }

  return ok(items);
}

export function parseExportCaseDetailApiResponse(
  raw: unknown,
): ParseResult<ExportCaseDetail> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.id)) return fail("id is required.");
  if (!isString(o.facilityId)) return fail("facilityId is required.");
  if (!isString(o.sourceOrderId)) return fail("sourceOrderId is required.");
  if (!isString(o.caseNumber)) return fail("caseNumber is required.");
  if (!isString(o.sourceOrderReference)) {
    return fail("sourceOrderReference is required.");
  }
  if (!isString(o.customerName)) return fail("customerName is required.");
  if (!isString(o.destinationCountryCode)) {
    return fail("destinationCountryCode is required.");
  }
  if (!isValidCaseStatus(o.status)) {
    return fail(`status has an invalid value: '${String(o.status)}'.`);
  }
  if (!isValidReadinessStatus(o.readinessStatus)) {
    return fail(
      `readinessStatus has an invalid value: '${String(o.readinessStatus)}'.`,
    );
  }
  if (!isNumber(o.readinessPercent)) return fail("readinessPercent is required.");
  if (!isNumber(o.blockingIssueCount)) {
    return fail("blockingIssueCount is required.");
  }
  if (!isNumber(o.missingRequirementCount)) {
    return fail("missingRequirementCount is required.");
  }
  if (!isString(o.createdBySubjectId)) {
    return fail("createdBySubjectId is required.");
  }
  if (!isString(o.lastModifiedBySubjectId)) {
    return fail("lastModifiedBySubjectId is required.");
  }
  if (!isNumber(o.version)) return fail("version is required.");
  if (!isString(o.createdAtUtc)) return fail("createdAtUtc is required.");
  if (!isString(o.updatedAtUtc)) return fail("updatedAtUtc is required.");

  return ok<ExportCaseDetail>({
    id: o.id,
    facilityId: o.facilityId,
    sourceOrderId: o.sourceOrderId,
    caseNumber: o.caseNumber,
    sourceOrderReference: o.sourceOrderReference,
    customerName: o.customerName,
    destinationCountryCode: o.destinationCountryCode,
    originCountryCode: optionalString(o.originCountryCode),
    incotermCode: optionalString(o.incotermCode),
    status: o.status,
    readinessStatus: o.readinessStatus,
    readinessPercent: o.readinessPercent,
    blockingIssueCount: o.blockingIssueCount,
    missingRequirementCount: o.missingRequirementCount,
    createdBySubjectId: o.createdBySubjectId,
    lastModifiedBySubjectId: o.lastModifiedBySubjectId,
    version: o.version,
    createdAtUtc: o.createdAtUtc,
    updatedAtUtc: o.updatedAtUtc,
  });
}

function parseExportRequirement(
  raw: unknown,
): ParseResult<ExportRequirement> {
  if (typeof raw !== "object" || raw === null) {
    return fail("requirement must be an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.key)) return fail("requirement.key is required.");
  if (!isString(o.label)) return fail("requirement.label is required.");
  if (!isBoolean(o.isBlocking)) return fail("requirement.isBlocking is required.");
  if (!isBoolean(o.isSatisfied)) {
    return fail("requirement.isSatisfied is required.");
  }
  if (!isString(o.status)) return fail("requirement.status is required.");

  return ok<ExportRequirement>({
    key: o.key,
    label: o.label,
    isBlocking: o.isBlocking,
    isSatisfied: o.isSatisfied,
    status: o.status,
  });
}

export function parseExportReadinessApiResponse(
  raw: unknown,
): ParseResult<ExportReadiness> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.exportCaseId)) return fail("exportCaseId is required.");
  if (!isValidReadinessStatus(o.readinessStatus)) {
    return fail(
      `readinessStatus has an invalid value: '${String(o.readinessStatus)}'.`,
    );
  }
  if (!isNumber(o.readinessPercent)) return fail("readinessPercent is required.");
  if (!isNumber(o.blockingIssueCount)) {
    return fail("blockingIssueCount is required.");
  }
  if (!isNumber(o.missingRequirementCount)) {
    return fail("missingRequirementCount is required.");
  }
  if (!isValidCaseStatus(o.suggestedCaseStatus)) {
    return fail(
      `suggestedCaseStatus has an invalid value: '${String(o.suggestedCaseStatus)}'.`,
    );
  }
  if (!Array.isArray(o.requirements)) {
    return fail("requirements must be an array.");
  }

  const requirements: ExportRequirement[] = [];

  for (let i = 0; i < o.requirements.length; i++) {
    const parsed = parseExportRequirement(o.requirements[i]);

    if (parsed.status === "failure") {
      return fail(`requirements[${i}]: ${parsed.message}`);
    }

    requirements.push(parsed.value);
  }

  return ok<ExportReadiness>({
    exportCaseId: o.exportCaseId,
    readinessStatus: o.readinessStatus,
    readinessPercent: o.readinessPercent,
    blockingIssueCount: o.blockingIssueCount,
    missingRequirementCount: o.missingRequirementCount,
    suggestedCaseStatus: o.suggestedCaseStatus,
    requirements,
  });
}

export function parseExportDocumentApiResponse(
  raw: unknown,
): ParseResult<ExportDocument> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.id)) return fail("id is required.");
  if (!isString(o.exportCaseId)) return fail("exportCaseId is required.");
  if (!isString(o.documentType)) return fail("documentType is required.");
  if (!isValidDocumentStatus(o.documentStatus)) {
    return fail(
      `documentStatus has an invalid value: '${String(o.documentStatus)}'.`,
    );
  }
  if (!isString(o.displayName)) return fail("displayName is required.");
  if (!isString(o.source)) return fail("source is required.");
  if (!isNumber(o.version)) return fail("version is required.");
  if (!isString(o.createdAtUtc)) return fail("createdAtUtc is required.");
  if (!isString(o.updatedAtUtc)) return fail("updatedAtUtc is required.");

  if (!isOptionalString(o.fileReference)) {
    return fail("fileReference must be a string.");
  }
  if (!isOptionalString(o.documentNumber)) {
    return fail("documentNumber must be a string.");
  }
  if (!isOptionalString(o.issuedAtUtc)) {
    return fail("issuedAtUtc must be a string.");
  }
  if (!isOptionalString(o.expiresAtUtc)) {
    return fail("expiresAtUtc must be a string.");
  }
  if (!isOptionalString(o.rejectionReason)) {
    return fail("rejectionReason must be a string.");
  }
  if (!isOptionalString(o.verifiedAtUtc)) {
    return fail("verifiedAtUtc must be a string.");
  }
  if (!isOptionalString(o.verifiedBySubjectId)) {
    return fail("verifiedBySubjectId must be a string.");
  }

  return ok<ExportDocument>({
    id: o.id,
    exportCaseId: o.exportCaseId,
    documentType: o.documentType,
    documentStatus: o.documentStatus,
    displayName: o.displayName,
    fileReference: optionalString(o.fileReference),
    documentNumber: optionalString(o.documentNumber),
    issuedAtUtc: optionalString(o.issuedAtUtc),
    expiresAtUtc: optionalString(o.expiresAtUtc),
    source: o.source,
    rejectionReason: optionalString(o.rejectionReason),
    verifiedAtUtc: optionalString(o.verifiedAtUtc),
    verifiedBySubjectId: optionalString(o.verifiedBySubjectId),
    version: o.version,
    createdAtUtc: o.createdAtUtc,
    updatedAtUtc: o.updatedAtUtc,
  });
}

export function parseExportDocumentListApiResponse(
  raw: unknown,
): ParseResult<ExportDocument[]> {
  if (!Array.isArray(raw)) {
    return fail("Expected an array.");
  }

  const items: ExportDocument[] = [];

  for (let i = 0; i < raw.length; i++) {
    const parsed = parseExportDocumentApiResponse(raw[i]);

    if (parsed.status === "failure") {
      return fail(`Item ${i}: ${parsed.message}`);
    }

    items.push(parsed.value);
  }

  return ok(items);
}

export function parseExportActivityApiResponse(
  raw: unknown,
): ParseResult<ExportActivity> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.id)) return fail("id is required.");
  if (!isString(o.exportCaseId)) return fail("exportCaseId is required.");
  if (!isString(o.eventType)) return fail("eventType is required.");
  if (!isString(o.title)) return fail("title is required.");
  if (!isString(o.occurredAtUtc)) return fail("occurredAtUtc is required.");
  if (!isValidActivitySourceType(o.sourceType)) {
    return fail(`sourceType has an invalid value: '${String(o.sourceType)}'.`);
  }
  if (!isString(o.correlationId)) return fail("correlationId is required.");

  return ok<ExportActivity>({
    id: o.id,
    exportCaseId: o.exportCaseId,
    eventType: o.eventType,
    title: o.title,
    summary: optionalString(o.summary),
    occurredAtUtc: o.occurredAtUtc,
    sourceType: o.sourceType,
    actorSubjectId: optionalString(o.actorSubjectId),
    relatedEntityType: optionalString(o.relatedEntityType),
    relatedEntityId: optionalString(o.relatedEntityId),
    correlationId: o.correlationId,
  });
}

export function parseExportActivityListApiResponse(
  raw: unknown,
): ParseResult<ExportActivity[]> {
  if (!Array.isArray(raw)) {
    return fail("Expected an array.");
  }

  const items: ExportActivity[] = [];

  for (let i = 0; i < raw.length; i++) {
    const parsed = parseExportActivityApiResponse(raw[i]);

    if (parsed.status === "failure") {
      return fail(`Item ${i}: ${parsed.message}`);
    }

    items.push(parsed.value);
  }

  return ok(items);
}

export function parseExportCaseWriteSuccessApiResponse(
  raw: unknown,
): ParseResult<ExportCaseWriteSuccess> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.exportCaseId)) return fail("exportCaseId is required.");
  if (!isString(o.caseNumber)) return fail("caseNumber is required.");
  if (!isValidCaseStatus(o.status)) {
    return fail(`status has an invalid value: '${String(o.status)}'.`);
  }
  if (!isValidReadinessStatus(o.readinessStatus)) {
    return fail(
      `readinessStatus has an invalid value: '${String(o.readinessStatus)}'.`,
    );
  }
  if (!isNumber(o.readinessPercent)) return fail("readinessPercent is required.");
  if (!isNumber(o.version)) return fail("version is required.");
  if (!isString(o.createdAtUtc)) return fail("createdAtUtc is required.");
  if (!isString(o.updatedAtUtc)) return fail("updatedAtUtc is required.");
  if (!isBoolean(o.alreadyExisted)) return fail("alreadyExisted is required.");

  return ok<ExportCaseWriteSuccess>({
    exportCaseId: o.exportCaseId,
    caseNumber: o.caseNumber,
    status: o.status,
    readinessStatus: o.readinessStatus,
    readinessPercent: o.readinessPercent,
    version: o.version,
    createdAtUtc: o.createdAtUtc,
    updatedAtUtc: o.updatedAtUtc,
    alreadyExisted: o.alreadyExisted,
  });
}

export type ExportDocumentWriteSuccess = Readonly<{
  documentId: string;
  documentVersion: number;
  caseVersion: number;
}>;

export function parseExportDocumentWriteSuccessApiResponse(
  raw: unknown,
): ParseResult<ExportDocumentWriteSuccess> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.documentId)) return fail("documentId is required.");
  if (!isNumber(o.documentVersion)) return fail("documentVersion is required.");
  if (!isNumber(o.caseVersion)) return fail("caseVersion is required.");

  return ok<ExportDocumentWriteSuccess>({
    documentId: o.documentId,
    documentVersion: o.documentVersion,
    caseVersion: o.caseVersion,
  });
}
