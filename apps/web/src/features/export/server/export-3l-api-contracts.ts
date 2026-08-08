import "./export-case-server-boundary";

import type {
  ComplianceEvaluationResult,
  ComplianceRequirementStatus,
  ComplianceSeverity,
  CountryComplianceRuleSummary,
  CountryKnowledgeEntry,
  CountryResearchCaseSummary,
  CountryResearchDetail,
  CountryResearchFinding,
  CountryRulePackSummary,
  ExportComplianceRequirement,
  ExportComplianceSummary,
  ExportTraceability,
  ExportTraceabilityBrokerRef,
  ExportTraceabilityComplianceRef,
  ExportTraceabilityDocumentRef,
  ExportTraceabilityPackingRef,
  ExportTraceabilityResearchRef,
  ExportTraceabilityShipmentRef,
  KnowledgeAuthority,
  ResearchCaseStatus,
  ResearchFindingCategory,
  RulePackStatus,
  SourceVerificationStatus,
} from "../domain/export-3l";

const COMPLIANCE_RESULTS = new Set<ComplianceEvaluationResult>([
  "ready",
  "attention",
  "blocked",
  "unable-to-evaluate",
]);

const REQUIREMENT_STATUSES = new Set<ComplianceRequirementStatus>([
  "missing",
  "pending",
  "satisfied",
  "not-applicable",
  "unable-to-evaluate",
]);

const SEVERITIES = new Set<ComplianceSeverity>([
  "info",
  "warning",
  "blocking",
]);

const RULE_PACK_STATUSES = new Set<RulePackStatus>([
  "draft",
  "under-review",
  "verified",
  "superseded",
  "archived",
]);

const RESEARCH_STATUSES = new Set<ResearchCaseStatus>([
  "draft",
  "researching",
  "review-required",
  "verified",
  "completed",
  "archived",
]);

const FINDING_CATEGORIES = new Set<ResearchFindingCategory>([
  "customs",
  "tariff",
  "documentation",
  "origin",
  "technical",
  "certification",
  "labeling",
  "packaging",
  "restricted-goods",
  "trade-agreement",
  "logistics",
  "broker-question",
  "other",
]);

const VERIFICATION_STATUSES = new Set<SourceVerificationStatus>([
  "unverified",
  "under-review",
  "verified",
  "superseded",
  "revalidation-required",
]);

const KNOWLEDGE_AUTHORITIES = new Set<KnowledgeAuthority>([
  "verified-rule",
  "historical-observation",
  "customer-requirement",
  "broker-input",
  "manual-entry",
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

function isNumber(v: unknown): v is number {
  return typeof v === "number" && Number.isFinite(v);
}

function isBoolean(v: unknown): v is boolean {
  return typeof v === "boolean";
}

function optionalString(value: unknown): string | undefined {
  if (value === undefined || value === null) return undefined;
  return typeof value === "string" ? value : undefined;
}

function parseRulePack(raw: unknown): ParseResult<CountryRulePackSummary> {
  if (typeof raw !== "object" || raw === null) {
    return fail("rule pack must be an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.id)) return fail("rulePack.id is required.");
  if (!isString(o.countryCode)) return fail("rulePack.countryCode is required.");
  if (!isString(o.name)) return fail("rulePack.name is required.");
  if (
    !isString(o.status) ||
    !RULE_PACK_STATUSES.has(o.status as RulePackStatus)
  ) {
    return fail(`rulePack.status has an invalid value: '${String(o.status)}'.`);
  }
  if (!isNumber(o.versionNumber)) return fail("rulePack.versionNumber is required.");
  if (!isString(o.effectiveFrom)) return fail("rulePack.effectiveFrom is required.");
  if (!isString(o.sourceTitle)) return fail("rulePack.sourceTitle is required.");
  if (!isString(o.sourcePublisher)) {
    return fail("rulePack.sourcePublisher is required.");
  }
  if (!isNumber(o.version)) return fail("rulePack.version is required.");
  if (!isString(o.updatedAtUtc)) return fail("rulePack.updatedAtUtc is required.");

  return ok({
    id: o.id,
    facilityId: optionalString(o.facilityId),
    countryCode: o.countryCode,
    name: o.name,
    status: o.status as RulePackStatus,
    versionNumber: o.versionNumber,
    effectiveFrom: o.effectiveFrom,
    effectiveTo: optionalString(o.effectiveTo),
    sourceTitle: o.sourceTitle,
    sourceUrl: optionalString(o.sourceUrl),
    sourcePublisher: o.sourcePublisher,
    verifiedAtUtc: optionalString(o.verifiedAtUtc),
    verifiedBySubjectId: optionalString(o.verifiedBySubjectId),
    version: o.version,
    updatedAtUtc: o.updatedAtUtc,
  });
}

function parseComplianceRule(
  raw: unknown,
): ParseResult<CountryComplianceRuleSummary> {
  if (typeof raw !== "object" || raw === null) {
    return fail("rule must be an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.id)) return fail("rule.id is required.");
  if (!isString(o.rulePackId)) return fail("rule.rulePackId is required.");
  if (!isString(o.ruleCode)) return fail("rule.ruleCode is required.");
  if (!isString(o.title)) return fail("rule.title is required.");
  if (!isString(o.description)) return fail("rule.description is required.");
  if (!isString(o.ruleType)) return fail("rule.ruleType is required.");
  if (
    !isString(o.severity) ||
    !SEVERITIES.has(o.severity as ComplianceSeverity)
  ) {
    return fail(`rule.severity has an invalid value: '${String(o.severity)}'.`);
  }
  if (!isString(o.requirementMode)) {
    return fail("rule.requirementMode is required.");
  }
  if (!isBoolean(o.blocking)) return fail("rule.blocking is required.");
  if (!isString(o.sourceReference)) {
    return fail("rule.sourceReference is required.");
  }
  if (!isNumber(o.version)) return fail("rule.version is required.");

  return ok({
    id: o.id,
    rulePackId: o.rulePackId,
    ruleCode: o.ruleCode,
    title: o.title,
    description: o.description,
    ruleType: o.ruleType,
    severity: o.severity as ComplianceSeverity,
    requirementMode: o.requirementMode,
    blocking: o.blocking,
    requiredDocumentType: optionalString(o.requiredDocumentType),
    productScope: optionalString(o.productScope),
    hsCodePattern: optionalString(o.hsCodePattern),
    sourceReference: o.sourceReference,
    version: o.version,
  });
}

function parseRequirement(raw: unknown): ParseResult<ExportComplianceRequirement> {
  if (typeof raw !== "object" || raw === null) {
    return fail("requirement must be an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.id)) return fail("requirement.id is required.");
  if (!isString(o.exportCaseId)) {
    return fail("requirement.exportCaseId is required.");
  }
  if (!isString(o.evaluationId)) {
    return fail("requirement.evaluationId is required.");
  }
  if (!isString(o.requirementCode)) {
    return fail("requirement.requirementCode is required.");
  }
  if (!isString(o.title)) return fail("requirement.title is required.");
  if (!isString(o.description)) {
    return fail("requirement.description is required.");
  }
  if (
    !isString(o.status) ||
    !REQUIREMENT_STATUSES.has(o.status as ComplianceRequirementStatus)
  ) {
    return fail(
      `requirement.status has an invalid value: '${String(o.status)}'.`,
    );
  }
  if (
    !isString(o.severity) ||
    !SEVERITIES.has(o.severity as ComplianceSeverity)
  ) {
    return fail(
      `requirement.severity has an invalid value: '${String(o.severity)}'.`,
    );
  }
  if (!isBoolean(o.blocking)) return fail("requirement.blocking is required.");
  if (!isString(o.sourceReference)) {
    return fail("requirement.sourceReference is required.");
  }
  if (!isNumber(o.version)) return fail("requirement.version is required.");

  return ok({
    id: o.id,
    exportCaseId: o.exportCaseId,
    evaluationId: o.evaluationId,
    requirementCode: o.requirementCode,
    title: o.title,
    description: o.description,
    status: o.status as ComplianceRequirementStatus,
    severity: o.severity as ComplianceSeverity,
    blocking: o.blocking,
    requiredDocumentType: optionalString(o.requiredDocumentType),
    relatedDocumentId: optionalString(o.relatedDocumentId),
    sourceReference: o.sourceReference,
    version: o.version,
  });
}

export function parseExportComplianceSummaryApiResponse(
  raw: unknown,
): ParseResult<ExportComplianceSummary> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.exportCaseId)) return fail("exportCaseId is required.");
  if (
    !isString(o.complianceStatus) ||
    !COMPLIANCE_RESULTS.has(o.complianceStatus as ComplianceEvaluationResult)
  ) {
    return fail(
      `complianceStatus has an invalid value: '${String(o.complianceStatus)}'.`,
    );
  }
  if (!isNumber(o.complianceReadinessPercent)) {
    return fail("complianceReadinessPercent is required.");
  }
  if (!isBoolean(o.revalidationRequired)) {
    return fail("revalidationRequired is required.");
  }
  if (!isNumber(o.blockingCount)) return fail("blockingCount is required.");
  if (!isNumber(o.missingCount)) return fail("missingCount is required.");
  if (!Array.isArray(o.requirements)) {
    return fail("requirements must be an array.");
  }

  const requirements: ExportComplianceRequirement[] = [];
  for (let i = 0; i < o.requirements.length; i++) {
    const parsed = parseRequirement(o.requirements[i]);
    if (parsed.status === "failure") {
      return fail(`requirements[${i}]: ${parsed.message}`);
    }
    requirements.push(parsed.value);
  }

  let rulePack: CountryRulePackSummary | undefined;
  if (o.rulePack !== undefined && o.rulePack !== null) {
    const parsedRulePack = parseRulePack(o.rulePack);
    if (parsedRulePack.status === "failure") {
      return fail(`rulePack: ${parsedRulePack.message}`);
    }
    rulePack = parsedRulePack.value;
  }

  return ok({
    exportCaseId: o.exportCaseId,
    complianceStatus: o.complianceStatus as ComplianceEvaluationResult,
    complianceReadinessPercent: o.complianceReadinessPercent,
    revalidationRequired: o.revalidationRequired,
    currentEvaluationId: optionalString(o.currentEvaluationId),
    blockingCount: o.blockingCount,
    missingCount: o.missingCount,
    requirements,
    rulePack,
  });
}

export function parseExportComplianceRequirementListApiResponse(
  raw: unknown,
): ParseResult<ExportComplianceRequirement[]> {
  if (!Array.isArray(raw)) return fail("Expected an array.");

  const items: ExportComplianceRequirement[] = [];
  for (let i = 0; i < raw.length; i++) {
    const parsed = parseRequirement(raw[i]);
    if (parsed.status === "failure") {
      return fail(`Item ${i}: ${parsed.message}`);
    }
    items.push(parsed.value);
  }

  return ok(items);
}

function parseResearchCase(raw: unknown): ParseResult<CountryResearchCaseSummary> {
  if (typeof raw !== "object" || raw === null) {
    return fail("research case must be an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.id)) return fail("researchCase.id is required.");
  if (!isString(o.facilityId)) return fail("researchCase.facilityId is required.");
  if (!isString(o.researchNumber)) {
    return fail("researchCase.researchNumber is required.");
  }
  if (!isString(o.countryCode)) return fail("researchCase.countryCode is required.");
  if (
    !isString(o.status) ||
    !RESEARCH_STATUSES.has(o.status as ResearchCaseStatus)
  ) {
    return fail(
      `researchCase.status has an invalid value: '${String(o.status)}'.`,
    );
  }
  if (!isString(o.createdAtUtc)) {
    return fail("researchCase.createdAtUtc is required.");
  }
  if (!isNumber(o.version)) return fail("researchCase.version is required.");
  if (!isString(o.updatedAtUtc)) {
    return fail("researchCase.updatedAtUtc is required.");
  }

  return ok({
    id: o.id,
    facilityId: o.facilityId,
    researchNumber: o.researchNumber,
    countryCode: o.countryCode,
    productReference: optionalString(o.productReference),
    productDescription: optionalString(o.productDescription),
    productGroup: optionalString(o.productGroup),
    hsCode: optionalString(o.hsCode),
    originCountryCode: optionalString(o.originCountryCode),
    status: o.status as ResearchCaseStatus,
    purposeNotes: optionalString(o.purposeNotes),
    linkedExportCaseId: optionalString(o.linkedExportCaseId),
    createdAtUtc: o.createdAtUtc,
    reviewedAtUtc: optionalString(o.reviewedAtUtc),
    completedAtUtc: optionalString(o.completedAtUtc),
    version: o.version,
    updatedAtUtc: o.updatedAtUtc,
  });
}

function parseFinding(raw: unknown): ParseResult<CountryResearchFinding> {
  if (typeof raw !== "object" || raw === null) {
    return fail("finding must be an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.id)) return fail("finding.id is required.");
  if (!isString(o.countryResearchCaseId)) {
    return fail("finding.countryResearchCaseId is required.");
  }
  if (
    !isString(o.category) ||
    !FINDING_CATEGORIES.has(o.category as ResearchFindingCategory)
  ) {
    return fail(
      `finding.category has an invalid value: '${String(o.category)}'.`,
    );
  }
  if (!isString(o.title)) return fail("finding.title is required.");
  if (!isString(o.summary)) return fail("finding.summary is required.");
  if (!isString(o.sourcePublisher)) {
    return fail("finding.sourcePublisher is required.");
  }
  if (!isString(o.sourceTitle)) return fail("finding.sourceTitle is required.");
  if (!isNumber(o.confidence)) return fail("finding.confidence is required.");
  if (
    !isString(o.verificationStatus) ||
    !VERIFICATION_STATUSES.has(o.verificationStatus as SourceVerificationStatus)
  ) {
    return fail(
      `finding.verificationStatus has an invalid value: '${String(o.verificationStatus)}'.`,
    );
  }
  if (!isNumber(o.version)) return fail("finding.version is required.");
  if (!isString(o.createdAtUtc)) return fail("finding.createdAtUtc is required.");

  return ok({
    id: o.id,
    countryResearchCaseId: o.countryResearchCaseId,
    category: o.category as ResearchFindingCategory,
    title: o.title,
    summary: o.summary,
    sourcePublisher: o.sourcePublisher,
    sourceTitle: o.sourceTitle,
    sourceUrl: optionalString(o.sourceUrl),
    confidence: o.confidence,
    verificationStatus: o.verificationStatus as SourceVerificationStatus,
    version: o.version,
    createdAtUtc: o.createdAtUtc,
  });
}

export function parseCountryResearchListApiResponse(
  raw: unknown,
): ParseResult<CountryResearchCaseSummary[]> {
  if (!Array.isArray(raw)) return fail("Expected an array.");

  const items: CountryResearchCaseSummary[] = [];
  for (let i = 0; i < raw.length; i++) {
    const parsed = parseResearchCase(raw[i]);
    if (parsed.status === "failure") {
      return fail(`Item ${i}: ${parsed.message}`);
    }
    items.push(parsed.value);
  }

  return ok(items);
}

export function parseCountryResearchDetailApiResponse(
  raw: unknown,
): ParseResult<CountryResearchDetail> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  const researchCaseResult = parseResearchCase(o.researchCase);
  if (researchCaseResult.status === "failure") {
    return fail(`researchCase: ${researchCaseResult.message}`);
  }

  if (!Array.isArray(o.findings)) return fail("findings must be an array.");

  const findings: CountryResearchFinding[] = [];
  for (let i = 0; i < o.findings.length; i++) {
    const parsed = parseFinding(o.findings[i]);
    if (parsed.status === "failure") {
      return fail(`findings[${i}]: ${parsed.message}`);
    }
    findings.push(parsed.value);
  }

  return ok({
    researchCase: researchCaseResult.value,
    findings,
  });
}

function parseKnowledgeEntry(raw: unknown): ParseResult<CountryKnowledgeEntry> {
  if (typeof raw !== "object" || raw === null) {
    return fail("knowledge entry must be an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.id)) return fail("entry.id is required.");
  if (!isString(o.countryCode)) return fail("entry.countryCode is required.");
  if (
    !isString(o.category) ||
    !FINDING_CATEGORIES.has(o.category as ResearchFindingCategory)
  ) {
    return fail(`entry.category has an invalid value: '${String(o.category)}'.`);
  }
  if (!isString(o.title)) return fail("entry.title is required.");
  if (!isString(o.summary)) return fail("entry.summary is required.");
  if (!isString(o.sourcePublisher)) {
    return fail("entry.sourcePublisher is required.");
  }
  if (!isString(o.sourceTitle)) return fail("entry.sourceTitle is required.");
  if (
    !isString(o.authority) ||
    !KNOWLEDGE_AUTHORITIES.has(o.authority as KnowledgeAuthority)
  ) {
    return fail(`entry.authority has an invalid value: '${String(o.authority)}'.`);
  }
  if (
    !isString(o.verificationStatus) ||
    !VERIFICATION_STATUSES.has(o.verificationStatus as SourceVerificationStatus)
  ) {
    return fail(
      `entry.verificationStatus has an invalid value: '${String(o.verificationStatus)}'.`,
    );
  }
  if (!isBoolean(o.revalidationRequired)) {
    return fail("entry.revalidationRequired is required.");
  }
  if (!isNumber(o.version)) return fail("entry.version is required.");
  if (!isString(o.updatedAtUtc)) return fail("entry.updatedAtUtc is required.");

  return ok({
    id: o.id,
    countryCode: o.countryCode,
    category: o.category as ResearchFindingCategory,
    title: o.title,
    summary: o.summary,
    sourcePublisher: o.sourcePublisher,
    sourceTitle: o.sourceTitle,
    sourceUrl: optionalString(o.sourceUrl),
    authority: o.authority as KnowledgeAuthority,
    verificationStatus: o.verificationStatus as SourceVerificationStatus,
    verifiedAtUtc: optionalString(o.verifiedAtUtc),
    revalidationRequired: o.revalidationRequired,
    version: o.version,
    updatedAtUtc: o.updatedAtUtc,
  });
}

export function parseCountryKnowledgeListApiResponse(
  raw: unknown,
): ParseResult<CountryKnowledgeEntry[]> {
  if (!Array.isArray(raw)) return fail("Expected an array.");

  const items: CountryKnowledgeEntry[] = [];
  for (let i = 0; i < raw.length; i++) {
    const parsed = parseKnowledgeEntry(raw[i]);
    if (parsed.status === "failure") {
      return fail(`Item ${i}: ${parsed.message}`);
    }
    items.push(parsed.value);
  }

  return ok(items);
}

export function parseExportTraceabilityApiResponse(
  raw: unknown,
): ParseResult<ExportTraceability> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.exportCaseId)) return fail("exportCaseId is required.");
  if (!isString(o.caseNumber)) return fail("caseNumber is required.");
  if (!isString(o.sourceOrderId)) return fail("sourceOrderId is required.");
  if (!isString(o.sourceOrderReference)) {
    return fail("sourceOrderReference is required.");
  }
  if (!isString(o.customerName)) return fail("customerName is required.");
  if (!isString(o.destinationCountryCode)) {
    return fail("destinationCountryCode is required.");
  }
  if (!Array.isArray(o.documents)) return fail("documents must be an array.");
  if (!Array.isArray(o.packingUnits)) {
    return fail("packingUnits must be an array.");
  }
  if (!Array.isArray(o.shipments)) return fail("shipments must be an array.");
  if (!isNumber(o.activityCount)) return fail("activityCount is required.");

  const documents: ExportTraceabilityDocumentRef[] = o.documents.map((item) => {
    const d = item as Record<string, unknown>;
    return {
      id: String(d.id),
      documentType: String(d.documentType),
      status: String(d.status),
      version: Number(d.version),
    };
  });

  const packingUnits: ExportTraceabilityPackingRef[] = o.packingUnits.map(
    (item) => {
      const p = item as Record<string, unknown>;
      return {
        id: String(p.id),
        unitCode: String(p.unitCode),
        status: String(p.status),
      };
    },
  );

  const shipments: ExportTraceabilityShipmentRef[] = o.shipments.map((item) => {
    const s = item as Record<string, unknown>;
    return {
      id: String(s.id),
      shipmentReference: String(s.shipmentReference),
      status: String(s.status),
      version: Number(s.version),
    };
  });

  let broker: ExportTraceabilityBrokerRef | undefined;
  if (o.broker !== undefined && o.broker !== null) {
    const b = o.broker as Record<string, unknown>;
    broker = {
      workspaceId: String(b.workspaceId),
      packageVersionNumber:
        typeof b.packageVersionNumber === "number"
          ? b.packageVersionNumber
          : undefined,
      status: String(b.status),
    };
  }

  let compliance: ExportTraceabilityComplianceRef | undefined;
  if (o.compliance !== undefined && o.compliance !== null) {
    const c = o.compliance as Record<string, unknown>;
    compliance = {
      evaluationId: optionalString(c.evaluationId),
      evaluationNumber:
        typeof c.evaluationNumber === "number" ? c.evaluationNumber : undefined,
      result: String(c.result),
      rulePackVersion:
        typeof c.rulePackVersion === "number" ? c.rulePackVersion : undefined,
    };
  }

  let research: ExportTraceabilityResearchRef | undefined;
  if (o.research !== undefined && o.research !== null) {
    const r = o.research as Record<string, unknown>;
    research = {
      researchCaseId: optionalString(r.researchCaseId),
      researchNumber: optionalString(r.researchNumber),
      status: String(r.status),
    };
  }

  return ok({
    exportCaseId: o.exportCaseId,
    caseNumber: o.caseNumber,
    sourceOrderId: o.sourceOrderId,
    sourceOrderReference: o.sourceOrderReference,
    customerName: o.customerName,
    destinationCountryCode: o.destinationCountryCode,
    documents,
    packingUnits,
    shipments,
    broker,
    compliance,
    research,
    activityCount: o.activityCount,
  });
}

export type ComplianceWriteSuccess = Readonly<{
  evaluationId: string;
  caseVersion: number;
  result: string;
}>;

export type ComplianceRequirementWriteSuccess = Readonly<{
  requirementVersion: number;
  caseVersion: number;
}>;

export type ResearchWriteSuccess = Readonly<{
  researchCaseId: string;
  version: number;
}>;

export type ResearchFindingWriteSuccess = Readonly<{
  findingId: string;
  researchVersion: number;
}>;

export type KnowledgeWriteSuccess = Readonly<{
  knowledgeEntryId: string;
  version: number;
}>;

export function parseComplianceWriteSuccessApiResponse(
  raw: unknown,
): ParseResult<ComplianceWriteSuccess> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.evaluationId)) return fail("evaluationId is required.");
  if (!isNumber(o.caseVersion)) return fail("caseVersion is required.");
  if (!isString(o.result)) return fail("result is required.");

  return ok({
    evaluationId: o.evaluationId,
    caseVersion: o.caseVersion,
    result: o.result,
  });
}

export function parseComplianceRequirementWriteSuccessApiResponse(
  raw: unknown,
): ParseResult<ComplianceRequirementWriteSuccess> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isNumber(o.requirementVersion)) {
    return fail("requirementVersion is required.");
  }
  if (!isNumber(o.caseVersion)) return fail("caseVersion is required.");

  return ok({
    requirementVersion: o.requirementVersion,
    caseVersion: o.caseVersion,
  });
}

export function parseResearchWriteSuccessApiResponse(
  raw: unknown,
): ParseResult<ResearchWriteSuccess> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.researchCaseId)) return fail("researchCaseId is required.");
  if (!isNumber(o.version)) return fail("version is required.");

  return ok({
    researchCaseId: o.researchCaseId,
    version: o.version,
  });
}

export function parseResearchFindingWriteSuccessApiResponse(
  raw: unknown,
): ParseResult<ResearchFindingWriteSuccess> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.findingId)) return fail("findingId is required.");
  if (!isNumber(o.researchVersion)) {
    return fail("researchVersion is required.");
  }

  return ok({
    findingId: o.findingId,
    researchVersion: o.researchVersion,
  });
}

export function parseKnowledgeWriteSuccessApiResponse(
  raw: unknown,
): ParseResult<KnowledgeWriteSuccess> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.knowledgeEntryId)) {
    return fail("knowledgeEntryId is required.");
  }
  if (!isNumber(o.version)) return fail("version is required.");

  return ok({
    knowledgeEntryId: o.knowledgeEntryId,
    version: o.version,
  });
}
