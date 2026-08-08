export type ComplianceEvaluationResult =
  | "ready"
  | "attention"
  | "blocked"
  | "unable-to-evaluate";

export type ComplianceRequirementStatus =
  | "missing"
  | "pending"
  | "satisfied"
  | "not-applicable"
  | "unable-to-evaluate";

export type ComplianceSeverity = "info" | "warning" | "blocking";

export type RulePackStatus =
  | "draft"
  | "under-review"
  | "verified"
  | "superseded"
  | "archived";

export type ResearchCaseStatus =
  | "draft"
  | "researching"
  | "review-required"
  | "verified"
  | "completed"
  | "archived";

export type ResearchFindingCategory =
  | "customs"
  | "tariff"
  | "documentation"
  | "origin"
  | "technical"
  | "certification"
  | "labeling"
  | "packaging"
  | "restricted-goods"
  | "trade-agreement"
  | "logistics"
  | "broker-question"
  | "other";

export type SourceVerificationStatus =
  | "unverified"
  | "under-review"
  | "verified"
  | "superseded"
  | "revalidation-required";

export type KnowledgeAuthority =
  | "verified-rule"
  | "historical-observation"
  | "customer-requirement"
  | "broker-input"
  | "manual-entry";

export type CountryRulePackSummary = Readonly<{
  id: string;
  facilityId?: string;
  countryCode: string;
  name: string;
  status: RulePackStatus;
  versionNumber: number;
  effectiveFrom: string;
  effectiveTo?: string;
  sourceTitle: string;
  sourceUrl?: string;
  sourcePublisher: string;
  verifiedAtUtc?: string;
  verifiedBySubjectId?: string;
  version: number;
  updatedAtUtc: string;
}>;

export type CountryComplianceRuleSummary = Readonly<{
  id: string;
  rulePackId: string;
  ruleCode: string;
  title: string;
  description: string;
  ruleType: string;
  severity: ComplianceSeverity;
  requirementMode: string;
  blocking: boolean;
  requiredDocumentType?: string;
  productScope?: string;
  hsCodePattern?: string;
  sourceReference: string;
  version: number;
}>;

export type ExportComplianceRequirement = Readonly<{
  id: string;
  exportCaseId: string;
  evaluationId: string;
  requirementCode: string;
  title: string;
  description: string;
  status: ComplianceRequirementStatus;
  severity: ComplianceSeverity;
  blocking: boolean;
  requiredDocumentType?: string;
  relatedDocumentId?: string;
  sourceReference: string;
  version: number;
}>;

export type ExportComplianceSummary = Readonly<{
  exportCaseId: string;
  complianceStatus: ComplianceEvaluationResult;
  complianceReadinessPercent: number;
  revalidationRequired: boolean;
  currentEvaluationId?: string;
  blockingCount: number;
  missingCount: number;
  requirements: readonly ExportComplianceRequirement[];
  rulePack?: CountryRulePackSummary;
}>;

export type CountryResearchCaseSummary = Readonly<{
  id: string;
  facilityId: string;
  researchNumber: string;
  countryCode: string;
  productReference?: string;
  productDescription?: string;
  productGroup?: string;
  hsCode?: string;
  originCountryCode?: string;
  status: ResearchCaseStatus;
  purposeNotes?: string;
  linkedExportCaseId?: string;
  createdAtUtc: string;
  reviewedAtUtc?: string;
  completedAtUtc?: string;
  version: number;
  updatedAtUtc: string;
}>;

export type CountryResearchFinding = Readonly<{
  id: string;
  countryResearchCaseId: string;
  category: ResearchFindingCategory;
  title: string;
  summary: string;
  sourcePublisher: string;
  sourceTitle: string;
  sourceUrl?: string;
  confidence: number;
  verificationStatus: SourceVerificationStatus;
  version: number;
  createdAtUtc: string;
}>;

export type CountryResearchDetail = Readonly<{
  researchCase: CountryResearchCaseSummary;
  findings: readonly CountryResearchFinding[];
}>;

export type CountryKnowledgeEntry = Readonly<{
  id: string;
  countryCode: string;
  category: ResearchFindingCategory;
  title: string;
  summary: string;
  sourcePublisher: string;
  sourceTitle: string;
  sourceUrl?: string;
  authority: KnowledgeAuthority;
  verificationStatus: SourceVerificationStatus;
  verifiedAtUtc?: string;
  revalidationRequired: boolean;
  version: number;
  updatedAtUtc: string;
}>;

export type ExportTraceabilityDocumentRef = Readonly<{
  id: string;
  documentType: string;
  status: string;
  version: number;
}>;

export type ExportTraceabilityPackingRef = Readonly<{
  id: string;
  unitCode: string;
  status: string;
}>;

export type ExportTraceabilityShipmentRef = Readonly<{
  id: string;
  shipmentReference: string;
  status: string;
  version: number;
}>;

export type ExportTraceabilityBrokerRef = Readonly<{
  workspaceId: string;
  packageVersionNumber?: number;
  status: string;
}>;

export type ExportTraceabilityComplianceRef = Readonly<{
  evaluationId?: string;
  evaluationNumber?: number;
  result: string;
  rulePackVersion?: number;
}>;

export type ExportTraceabilityResearchRef = Readonly<{
  researchCaseId?: string;
  researchNumber?: string;
  status: string;
}>;

export type ExportTraceability = Readonly<{
  exportCaseId: string;
  caseNumber: string;
  sourceOrderId: string;
  sourceOrderReference: string;
  customerName: string;
  destinationCountryCode: string;
  documents: readonly ExportTraceabilityDocumentRef[];
  packingUnits: readonly ExportTraceabilityPackingRef[];
  shipments: readonly ExportTraceabilityShipmentRef[];
  broker?: ExportTraceabilityBrokerRef;
  compliance?: ExportTraceabilityComplianceRef;
  research?: ExportTraceabilityResearchRef;
  activityCount: number;
}>;

export type EvaluateComplianceInput = Readonly<{
  expectedCaseVersion: number;
  shipmentId?: string;
}>;

export type SatisfyComplianceRequirementInput = Readonly<{
  expectedRequirementVersion: number;
  resolutionType: string;
  resolutionNote: string;
}>;

export type CreateResearchCaseInput = Readonly<{
  countryCode: string;
  productReference?: string;
  productDescription?: string;
  productGroup?: string;
  hsCode?: string;
  originCountryCode?: string;
  purposeNotes?: string;
}>;

export type AddResearchFindingInput = Readonly<{
  expectedResearchVersion: number;
  category: string;
  title: string;
  summary: string;
  sourcePublisher: string;
  sourceTitle: string;
  sourceUrl?: string;
  sourcePublicationDate?: string;
  confidence: number;
  blockingPotential: boolean;
}>;

export type CreateKnowledgeEntryInput = Readonly<{
  countryCode: string;
  category: string;
  title: string;
  summary: string;
  sourcePublisher: string;
  sourceTitle: string;
  sourceUrl?: string;
  authority: string;
  productScope?: string;
  productGroup?: string;
  hsCodePattern?: string;
  createdFromResearchCaseId?: string;
}>;

export function getComplianceStatusLabel(
  status: ComplianceEvaluationResult,
): string {
  switch (status) {
    case "ready":
      return "Hazır";
    case "attention":
      return "Dikkat";
    case "blocked":
      return "Blokeli";
    case "unable-to-evaluate":
      return "Değerlendirilemedi";
    default:
      return status;
  }
}

export function getComplianceRequirementStatusLabel(
  status: ComplianceRequirementStatus,
): string {
  switch (status) {
    case "missing":
      return "Eksik";
    case "pending":
      return "Beklemede";
    case "satisfied":
      return "Karşılandı";
    case "not-applicable":
      return "Uygulanmaz";
    case "unable-to-evaluate":
      return "Değerlendirilemedi";
    default:
      return status;
  }
}

export function getResearchCaseStatusLabel(status: ResearchCaseStatus): string {
  switch (status) {
    case "draft":
      return "Taslak";
    case "researching":
      return "Araştırılıyor";
    case "review-required":
      return "İnceleme gerekli";
    case "verified":
      return "Doğrulandı";
    case "completed":
      return "Tamamlandı";
    case "archived":
      return "Arşiv";
    default:
      return status;
  }
}

export function getResearchFindingCategoryLabel(category: string): string {
  switch (category) {
    case "customs":
      return "Gümrük";
    case "tariff":
      return "Tarife";
    case "documentation":
      return "Belgeleme";
    case "origin":
      return "Menşe";
    case "technical":
      return "Teknik";
    case "certification":
      return "Sertifikasyon";
    case "labeling":
      return "Etiketleme";
    case "packaging":
      return "Ambalaj";
    case "restricted-goods":
      return "Kısıtlı ürün";
    case "trade-agreement":
      return "Ticaret anlaşması";
    case "logistics":
      return "Lojistik";
    case "broker-question":
      return "Broker sorusu";
    case "other":
      return "Diğer";
    default:
      return category;
  }
}

export function getKnowledgeAuthorityLabel(authority: string): string {
  switch (authority) {
    case "verified-rule":
      return "Doğrulanmış kural";
    case "historical-observation":
      return "Geçmiş gözlem";
    case "customer-requirement":
      return "Müşteri gereksinimi";
    case "broker-input":
      return "Broker girdisi";
    case "manual-entry":
      return "Manuel kayıt";
    default:
      return authority;
  }
}

export function getSourceVerificationStatusLabel(status: string): string {
  switch (status) {
    case "unverified":
      return "Doğrulanmadı";
    case "under-review":
      return "İncelemede";
    case "verified":
      return "Doğrulandı";
    case "superseded":
      return "Yerini aldı";
    case "revalidation-required":
      return "Yeniden doğrulama gerekli";
    default:
      return status;
  }
}

export const RESEARCH_FINDING_CATEGORY_OPTIONS: readonly ResearchFindingCategory[] =
  [
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
  ] as const;

export const KNOWLEDGE_AUTHORITY_OPTIONS: readonly KnowledgeAuthority[] = [
  "verified-rule",
  "historical-observation",
  "customer-requirement",
  "broker-input",
  "manual-entry",
] as const;
