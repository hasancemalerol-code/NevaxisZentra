export type SaasAdminWorkspaceSummary = Readonly<{
  activeMembers: number;
  pendingInvitations: number;
  suspendedMembers: number;
  activeSessions: number;
  tenantDisplayName: string;
  tenantKey: string;
}>;

export type SaasAdminMember = Readonly<{
  memberReference: string;
  displayName: string;
  emailMasked: string;
  membershipStatus: string;
  roleReferences: readonly string[];
  facilityCodes: readonly string[];
  mfaEnrolled: boolean;
  mfaRequired: boolean;
  lastSessionActivityUtc: string | null;
  authorizationVersion: string;
}>;

export type SaasAdminInvitation = Readonly<{
  invitationReference: string;
  emailMasked: string;
  deliveryStatus: string;
  expiresAtUtc: string;
  createdAtUtc: string;
  intendedRoleReference: string | null;
}>;

export type SaasAdminRole = Readonly<{
  roleReference: string;
  displayName: string;
  description: string | null;
  systemTemplateOrigin: string | null;
  isProtected: boolean;
  status: string;
  version: number;
  permissionKeys: readonly string[];
}>;

export type SaasAdminPermission = Readonly<{
  permissionKey: string;
  turkishLabel: string;
  description: string;
  moduleResourceCode: string;
  actionCode: string;
  status: string;
}>;

export type SaasAdminFacility = Readonly<{
  facilityCode: string;
  displayName: string;
  status: string;
}>;

export type SaasAdminSession = Readonly<{
  memberReference: string;
  accountDisplayName: string;
  emailMasked: string;
  createdAtUtc: string;
  lastSeenAtUtc: string;
  idleExpiresAtUtc: string;
  absoluteExpiresAtUtc: string;
  mfaSatisfied: boolean;
  isRevoked: boolean;
  deviceLabel: string | null;
  revokeToken: string;
}>;

export type SaasAdminAuditEvent = Readonly<{
  eventType: string;
  entityType: string;
  entityReference: string;
  actorReference: string;
  createdAtUtc: string;
  before: unknown;
  after: unknown;
}>;

export type SaasAdminEffectiveAccess = Readonly<{
  memberReference: string;
  authorizationVersion: string;
  roleReferences: readonly string[];
  facilityCodes: readonly string[];
  grantedPermissionKeys: readonly string[];
  deniedPermissionKeys: readonly string[];
}>;

export type SaasAdminCapabilities = Readonly<{
  canRead: boolean;
  canManage: boolean;
}>;

export type SaasAdminCommercialPlan = Readonly<{
  planCode: string;
  displayNameTr: string;
  descriptionTr: string | null;
  lifecycleState: string;
  availabilityState: string;
  versions: readonly Readonly<{
    versionNumber: number;
    billingCadence: string;
    publicationStatus: string;
    sellable: boolean;
  }>[];
}>;

export type SaasAdminCommercialCatalog = Readonly<{
  plans: readonly SaasAdminCommercialPlan[];
}>;
