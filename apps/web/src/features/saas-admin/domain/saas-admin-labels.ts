import type { SaasAdminFacility, SaasAdminPermission } from "./saas-admin-types";

const ROLE_LABELS: Record<string, string> = {
  "tenant-owner": "Kiracı sahibi",
  "tenant-admin": "Kiracı yöneticisi",
  "facility-manager": "Tesis yöneticisi",
  "production-manager": "Üretim yöneticisi",
  planner: "Planlamacı",
  "shift-supervisor": "Vardiya amiri",
  "machine-responsible": "Makine sorumlusu",
  operator: "Operatör",
  "maintenance-team": "Bakım ekibi",
  "quality-team": "Kalite ekibi",
  "material-warehouse": "Malzeme depo",
  "sales-order": "Satış sipariş",
  purchasing: "Satın alma",
  "billing-manager": "Faturalama yöneticisi",
  "read-only-auditor": "Salt okunur denetçi",
};

const MEMBERSHIP_STATUS_LABELS: Record<string, string> = {
  active: "Aktif",
  suspended: "Pasif",
  invited: "Davetli",
};

const DELIVERY_STATUS_LABELS: Record<string, string> = {
  "created-delivered": "Oluşturuldu / teslim edildi",
  "created-not-delivered": "Oluşturuldu / teslim edilmedi",
};

const AUDIT_EVENT_LABELS: Record<string, string> = {
  "admin-access.role.created": "Rol oluşturuldu",
  "admin-access.role.updated": "Rol güncellendi",
  "admin-access.role.retired": "Rol emekli edildi",
  "admin-access.role.permissions-changed": "Rol yetkileri değiştirildi",
  "admin-access.member.roles-changed": "Üye rolleri değiştirildi",
  "admin-access.member.facilities-changed": "Tesis erişimi değiştirildi",
  "admin-access.member.overrides-changed": "Doğrudan yetki değişikliği",
  "admin-access.invitation.created": "Davet oluşturuldu",
  "admin-access.membership.suspended": "Üyelik pasifleştirildi",
  "admin-access.membership.reactivated": "Üyelik yeniden etkinleştirildi",
  "admin-access.session.revoked": "Oturum sonlandırıldı",
  "admin-access.operation.denied": "Hassas işlem reddedildi",
  "invitation.accepted": "Davet kabul edildi",
};

const MODULE_LABELS: Record<string, string> = {
  dashboard: "Dashboard",
  production: "Üretim",
  "production-planning": "Üretim Planlama",
  machines: "Makineler",
  molds: "Kalıplar",
  materials: "Malzemeler",
  maintenance: "Bakım",
  quality: "Kalite",
  shifts: "Vardiyalar",
  "process-oee": "Proses ve OEE",
  erp: "ERP Entegrasyonları",
  "admin-access": "Kullanıcı ve Yetki Yönetimi",
  "saas-commercial": "Üyelik ve Faturalandırma",
  "saas-identity": "Kimlik ve Oturum",
};

const ACTION_LABELS: Record<string, string> = {
  view: "Görüntüleme",
  create: "Oluşturma",
  update: "Güncelleme",
  submit: "Uygulama",
  approve: "Onaylama",
  reject: "Reddetme",
  cancel: "İptal",
  export: "Dışa aktarma",
  "external-writeback": "Dış sistem yazımı",
  "manage-settings": "Yönetme",
};

const UUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export function formatRoleLabel(roleReference: string): string {
  return ROLE_LABELS[roleReference] ?? humanizeToken(roleReference);
}

export function formatMembershipStatus(status: string): string {
  return MEMBERSHIP_STATUS_LABELS[status] ?? humanizeToken(status);
}

export function formatDeliveryStatus(status: string): string {
  return DELIVERY_STATUS_LABELS[status] ?? humanizeToken(status);
}

export function formatMemberDisplayName(displayName: string): string {
  if (displayName === "Bilinmeyen hesap") {
    return "Kimlik bilgisi kullanılamıyor";
  }
  return displayName;
}

export function formatMemberIdentityHint(displayName: string): string | null {
  if (displayName === "Bilinmeyen hesap") {
    return "Hesap kaydı bulunamadı veya kimlik eşlemesi yapılamadı.";
  }
  return null;
}

export function formatMfaStatus(member: { mfaEnrolled: boolean; mfaRequired: boolean }): string {
  if (!member.mfaEnrolled) return "Kayıtlı değil";
  return member.mfaRequired ? "Zorunlu" : "Kayıtlı";
}

export function formatMfaBadgeClass(member: { mfaEnrolled: boolean; mfaRequired: boolean }): string {
  if (!member.mfaEnrolled) return "saas-admin-badge--warning";
  return member.mfaRequired ? "saas-admin-badge--info" : "saas-admin-badge--success";
}

export function formatSessionStatus(isRevoked: boolean): string {
  return isRevoked ? "Sonlandırıldı" : "Aktif";
}

export function formatSessionBadgeClass(isRevoked: boolean): string {
  return isRevoked ? "saas-admin-badge--neutral" : "saas-admin-badge--success";
}

export function formatFacilityLabel(
  facilityCode: string,
  facilities: readonly SaasAdminFacility[],
): string {
  const facility = facilities.find((item) => item.facilityCode === facilityCode);
  if (!facility) return humanizeToken(facilityCode);
  return facility.displayName !== facility.facilityCode
    ? `${facility.displayName} (${facility.facilityCode})`
    : facility.displayName;
}

export function formatModuleLabel(moduleResourceCode: string): string {
  const normalized = moduleResourceCode.trim().toLowerCase();
  return MODULE_LABELS[normalized] ?? humanizeToken(normalized);
}

export function formatActionLabel(actionCode: string): string {
  const normalized = actionCode.trim().toLowerCase();
  return ACTION_LABELS[normalized] ?? humanizeToken(normalized);
}

export function formatActorReference(actorReference: string): string {
  if (UUID_PATTERN.test(actorReference)) return "Sistem aktörü";
  if (actorReference.includes("@")) return actorReference;
  return humanizeToken(actorReference);
}

export function formatAuditEntityReference(entityReference: string, eventType: string): string {
  if (UUID_PATTERN.test(entityReference)) {
    return eventType.includes("invitation") ? "Davet kaydı" : "Sistem kaydı";
  }
  if (entityReference.toLowerCase().includes("closure")) return "Denetim kaydı";
  return entityReference;
}

export function formatAuditSummary(after: unknown): string {
  if (!after || typeof after !== "object") return "—";

  const record = after as Record<string, unknown>;
  const parts: string[] = [];

  if (typeof record.deliveryStatus === "string") {
    parts.push(formatDeliveryStatus(record.deliveryStatus));
  }
  if (typeof record.status === "string") {
    parts.push(formatMembershipStatus(record.status));
  }
  if (Array.isArray(record.roleReferences)) {
    parts.push(
      record.roleReferences
        .filter((role): role is string => typeof role === "string")
        .map((role) => formatRoleLabel(role))
        .join(", "),
    );
  }
  if (Array.isArray(record.facilityCodes)) {
    parts.push(
      record.facilityCodes
        .filter((code): code is string => typeof code === "string")
        .map((code) => humanizeToken(code))
        .join(", "),
    );
  }

  const cleaned = parts.map((part) => part.trim()).filter(Boolean);
  return cleaned.length > 0 ? cleaned.join(" · ") : "Kayıt güncellendi";
}

export function formatAuditEventType(eventType: string): string {
  return AUDIT_EVENT_LABELS[eventType] ?? eventType.replaceAll(".", " / ");
}

export function formatAuditOutcome(eventType: string): string {
  if (eventType.includes("denied")) return "Reddedildi";
  if (eventType.includes("revoked") || eventType.includes("retired") || eventType.includes("suspended")) {
    return "Kısıtlandı";
  }
  return "Başarılı";
}

export function formatDateTime(value: string | null): string {
  if (!value) return "—";
  return new Intl.DateTimeFormat("tr-TR", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

export function formatCount(value: number | null | undefined): string {
  if (value === null || value === undefined) return "—";
  return new Intl.NumberFormat("tr-TR").format(value);
}

export function formatOptionalCount(value: number | undefined): string {
  return value === undefined ? "—" : formatCount(value);
}

export function groupPermissionsByModule(
  permissions: readonly SaasAdminPermission[],
): ReadonlyMap<string, readonly SaasAdminPermission[]> {
  const groups = new Map<string, SaasAdminPermission[]>();
  for (const permission of permissions) {
    const moduleKey = permission.moduleResourceCode || "general";
    const bucket = groups.get(moduleKey) ?? [];
    bucket.push(permission);
    groups.set(moduleKey, bucket);
  }
  return groups;
}

function humanizeToken(value: string): string {
  return value
    .replaceAll(/[._-]+/g, " ")
    .replace(/\b\w/g, (char) => char.toUpperCase());
}
