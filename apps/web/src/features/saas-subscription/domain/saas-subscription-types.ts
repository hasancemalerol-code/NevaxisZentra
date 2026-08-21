export type SaasSubscriptionPlanVersionReference = Readonly<{
  commercialPlanVersionId: string;
  planCode: string | null;
  versionNumber: number | null;
}>;

export type SaasSubscriptionGraceState = Readonly<{
  inGracePeriod: boolean;
  graceUntilUtc: string | null;
}>;

export type SaasSubscriptionLimitEvidence = Readonly<{
  entitlementCode: string;
  semantics: string;
  limitValue: number | null;
  usageValue: number | null;
  limitConfigured: boolean;
}>;

export type TenantEntitlementEvaluation = Readonly<{
  allowed: boolean;
  reasonCode: string;
  explanationKey: string;
  subscriptionStatus: string | null;
  planVersion: SaasSubscriptionPlanVersionReference | null;
  entitlementCode: string | null;
  limit: SaasSubscriptionLimitEvidence | null;
  graceState: SaasSubscriptionGraceState | null;
  evaluatedAtUtc: string;
}>;

export type SaasSubscriptionSummary = Readonly<{
  subscriptionId: string;
  subscriptionStatus: string;
  billingCadence: string;
  commercialPlanVersionId: string;
  planCode: string | null;
  planVersionNumber: number | null;
  startsAtUtc: string;
  currentPeriodStartUtc: string;
  currentPeriodEndUtc: string;
  trialEndsAtUtc: string | null;
  graceUntilUtc: string | null;
  suspendedAtUtc: string | null;
  cancelledAtUtc: string | null;
  cancelAtPeriodEnd: boolean;
  sourceProvenance: string;
  isCurrent: boolean;
  version: number;
}>;

export type SaasCommercialEntitlement = Readonly<{
  code: string;
  displayNameTr: string;
  descriptionTr: string | null;
  valueKind: string;
  semantics: string;
  booleanValue: boolean | null;
  integerLimitValue: number | null;
}>;

export type TenantSubscriptionCurrent = Readonly<{
  subscription: SaasSubscriptionSummary;
  paidApplicationAccess: TenantEntitlementEvaluation;
  entitlements: readonly SaasCommercialEntitlement[];
  limits: Readonly<{
    users: TenantEntitlementEvaluation;
    facilities: TenantEntitlementEvaluation;
    connectedMachines: TenantEntitlementEvaluation;
  }>;
  moduleAccess: Readonly<{
    shiftWorkforce: TenantEntitlementEvaluation;
  }>;
  evaluatedAtUtc: string;
}>;

export type SubscriptionAccessDenialReason =
  | "paid-application"
  | "module"
  | "service-unavailable"
  | null;

export type SubscriptionAccessFailureKind = "session-expired" | "forbidden" | null;

export type SubscriptionAccessEvaluation = Readonly<{
  allowed: boolean;
  graceWarning: boolean;
  denialReason: SubscriptionAccessDenialReason;
  subscription: TenantSubscriptionCurrent | null;
  failureKind: SubscriptionAccessFailureKind;
}>;
