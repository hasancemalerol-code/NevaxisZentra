"use client";

import { useCallback, useEffect, useId, useMemo, useRef, useState, useTransition, type KeyboardEvent as ReactKeyboardEvent, type MutableRefObject } from "react";
import { LoaderCircle, MoreHorizontal, Search, ShieldAlert, X } from "lucide-react";

import {
  formatActionLabel,
  formatActorReference,
  formatAuditEntityReference,
  formatAuditEventType,
  formatAuditOutcome,
  formatAuditSummary,
  formatCount,
  formatDateTime,
  formatDeliveryStatus,
  formatFacilityLabel,
  formatMemberDisplayName,
  formatMemberIdentityHint,
  formatMembershipStatus,
  formatMfaBadgeClass,
  formatMfaStatus,
  formatModuleLabel,
  formatOptionalCount,
  formatRoleLabel,
  formatSessionBadgeClass,
  formatSessionStatus,
  groupPermissionsByModule,
} from "../domain/saas-admin-labels";
import type {
  SaasAdminAuditEvent,
  SaasAdminCapabilities,
  SaasAdminCommercialCatalog,
  SaasAdminFacility,
  SaasAdminInvitation,
  SaasAdminMember,
  SaasAdminPermission,
  SaasAdminRole,
  SaasAdminSession,
  SaasAdminWorkspaceSummary,
} from "../domain/saas-admin-types";
import {
  formatBillingCadence,
  formatDateTime as formatSubscriptionDateTime,
  formatGraceWarning,
  formatInsufficientData,
  formatOverLimitWarning,
  formatPlanLabel,
  formatSourceProvenance,
  formatSubscriptionStatus,
  listModuleEntitlementSummary,
} from "@/features/saas-subscription/domain/saas-subscription-labels";
import type { TenantSubscriptionCurrent } from "@/features/saas-subscription/domain/saas-subscription-types";
import type {
  BillingProviderCapability,
  BillingWorkspaceSummary,
} from "@/features/saas-billing/domain/saas-billing-types";
import {
  BILLING_DEVELOPMENT_NOTICE,
  CARD_PROVIDER_NOT_CONFIGURED_MESSAGE,
  CARD_PROVIDER_SANDBOX_MESSAGE,
  formatBillingDateTime,
  formatInvoiceStatus,
  formatMoney,
  formatPaymentMethod,
  formatPaymentStatus,
  invoiceStatusBadgeClass,
  paymentStatusBadgeClass,
} from "@/features/saas-billing/domain/saas-billing-labels";
import { loadBillingWorkspaceAction, initializeCardCheckoutAction, loadBillingInvoiceDetailAction } from "@/features/saas-billing/server/saas-billing-actions";
import {
  inviteSaasAdminUserAction,
  loadSaasAdminEffectiveAccessAction,
  reactivateSaasAdminMemberAction,
  revokeAllSaasAdminMemberSessionsAction,
  revokeSaasAdminSessionAction,
  suspendSaasAdminMemberAction,
  updateSaasAdminMemberFacilitiesAction,
  updateSaasAdminMemberRolesAction,
  updateSaasAdminRolePermissionsAction,
} from "../server/saas-admin-actions";
import "./saas-admin.css";

const TABS = [
  { id: "users", label: "Kullanıcılar" },
  { id: "roles", label: "Roller ve Yetkiler" },
  { id: "facilities", label: "Tesis Erişimi" },
  { id: "sessions", label: "Oturumlar ve Güvenlik" },
  { id: "audit", label: "Denetim Kayıtları" },
  { id: "billing", label: "Üyelik ve Faturalandırma" },
] as const;

type TabId = (typeof TABS)[number]["id"];

type SaasAdminViewProps = Readonly<{
  summary: SaasAdminWorkspaceSummary;
  members: readonly SaasAdminMember[];
  invitations: readonly SaasAdminInvitation[];
  roles: readonly SaasAdminRole[];
  permissions: readonly SaasAdminPermission[];
  facilities: readonly SaasAdminFacility[];
  sessions: readonly SaasAdminSession[];
  auditEvents: readonly SaasAdminAuditEvent[];
  commercialCatalog: SaasAdminCommercialCatalog | null;
  subscription: TenantSubscriptionCurrent | null;
  billingWorkspace: BillingWorkspaceSummary | null;
  billingProvider: BillingProviderCapability | null;
  billingLoadError: string | null;
  capabilities: SaasAdminCapabilities;
  partialErrors: readonly string[];
}>;

function createIdempotencyKey(prefix: string): string {
  return `${prefix}-${crypto.randomUUID()}`;
}

export function SaasAdminView({
  summary,
  members,
  invitations,
  roles,
  permissions,
  facilities,
  sessions,
  auditEvents,
  commercialCatalog,
  subscription,
  billingWorkspace,
  billingProvider,
  billingLoadError,
  capabilities,
  partialErrors,
}: SaasAdminViewProps) {
  const [activeTab, setActiveTab] = useState<TabId>("users");
  const [feedback, setFeedback] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isPending, startTransition] = useTransition();
  const [inviteOpen, setInviteOpen] = useState(false);
  const [selectedMemberRef, setSelectedMemberRef] = useState<string | null>(null);
  const [selectedRoleRef, setSelectedRoleRef] = useState<string | null>(null);
  const [selectedFacilityMemberRef, setSelectedFacilityMemberRef] = useState<string | null>(null);
  const [roleDialogOpen, setRoleDialogOpen] = useState(false);
  const [effectivePreview, setEffectivePreview] = useState<string | null>(null);
  const [userSearch, setUserSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState("");
  const [roleFilter, setRoleFilter] = useState("");
  const [facilityFilter, setFacilityFilter] = useState("");
  const [mfaFilter, setMfaFilter] = useState("");
  const [auditSearch, setAuditSearch] = useState("");
  const [auditOutcomeFilter, setAuditOutcomeFilter] = useState("");
  const [expandedAuditKey, setExpandedAuditKey] = useState<string | null>(null);
  const rowRefs = useRef(new Map<string, HTMLButtonElement>());
  const tabListRef = useRef<HTMLDivElement>(null);

  const activePermissions = useMemo(
    () => permissions.filter((permission) => permission.status === "Active"),
    [permissions],
  );

  const mfaEnabledCount = useMemo(
    () => members.filter((member) => member.mfaEnrolled).length,
    [members],
  );

  const customRoleCount = useMemo(
    () => roles.filter((role) => !role.systemTemplateOrigin && role.status === "Active").length,
    [roles],
  );

  const activeSessionsDisplay = useMemo(
    () => sessions.filter((session) => !session.isRevoked),
    [sessions],
  );

  const selectedMember = useMemo(
    () => members.find((member) => member.memberReference === selectedMemberRef) ?? null,
    [members, selectedMemberRef],
  );

  const selectedRole = useMemo(
    () => roles.find((role) => role.roleReference === selectedRoleRef) ?? null,
    [roles, selectedRoleRef],
  );

  const filteredMembers = useMemo(() => {
    const query = userSearch.trim().toLowerCase();
    return members.filter((member) => {
      const displayName = formatMemberDisplayName(member.displayName).toLowerCase();
      if (query && !displayName.includes(query) && !member.emailMasked.toLowerCase().includes(query)) {
        return false;
      }
      if (statusFilter && member.membershipStatus !== statusFilter) return false;
      if (roleFilter && !member.roleReferences.includes(roleFilter)) return false;
      if (facilityFilter && !member.facilityCodes.includes(facilityFilter)) return false;
      if (mfaFilter === "enrolled" && !member.mfaEnrolled) return false;
      if (mfaFilter === "missing" && member.mfaEnrolled) return false;
      if (mfaFilter === "required" && !member.mfaRequired) return false;
      return true;
    });
  }, [members, userSearch, statusFilter, roleFilter, facilityFilter, mfaFilter]);

  const filteredAuditEvents = useMemo(() => {
    const query = auditSearch.trim().toLowerCase();
    return auditEvents.filter((event) => {
      const outcome = formatAuditOutcome(event.eventType);
      if (auditOutcomeFilter && outcome !== auditOutcomeFilter) return false;
      if (!query) return true;
      const haystack = [
        formatAuditEventType(event.eventType),
        formatActorReference(event.actorReference),
        formatAuditEntityReference(event.entityReference, event.eventType),
        formatAuditSummary(event.after),
      ].join(" ").toLowerCase();
      return haystack.includes(query);
    });
  }, [auditEvents, auditSearch, auditOutcomeFilter]);

  const permissionGroups = useMemo(
    () => groupPermissionsByModule(activePermissions),
    [activePermissions],
  );

  const runAction = useCallback((action: () => Promise<{ status: string; message?: string }>) => {
    setFeedback(null);
    setError(null);
    startTransition(async () => {
      const result = await action();
      if (result.status === "failure") {
        setError(result.message ?? "İşlem başarısız oldu.");
        return;
      }
      setFeedback("İşlem tamamlandı.");
    });
  }, []);

  const clearUserFilters = useCallback(() => {
    setUserSearch("");
    setStatusFilter("");
    setRoleFilter("");
    setFacilityFilter("");
    setMfaFilter("");
  }, []);

  const closeMemberDetail = useCallback(() => {
    const previousRef = selectedMemberRef;
    setSelectedMemberRef(null);
    setEffectivePreview(null);
    if (previousRef) {
      requestAnimationFrame(() => rowRefs.current.get(previousRef)?.focus());
    }
  }, [selectedMemberRef]);

  const handleTabKeyDown = useCallback((event: ReactKeyboardEvent<HTMLButtonElement>, index: number) => {
    if (event.key !== "ArrowRight" && event.key !== "ArrowLeft") return;
    event.preventDefault();
    const delta = event.key === "ArrowRight" ? 1 : -1;
    const next = (index + delta + TABS.length) % TABS.length;
    const buttons = tabListRef.current?.querySelectorAll<HTMLButtonElement>('[role="tab"]');
    buttons?.[next]?.focus();
    setActiveTab(TABS[next].id);
  }, []);

  useEffect(() => {
    const onKeyDown = (event: globalThis.KeyboardEvent) => {
      if (event.key !== "Escape") return;
      if (inviteOpen) setInviteOpen(false);
      if (roleDialogOpen) setRoleDialogOpen(false);
      if (selectedMemberRef) closeMemberDetail();
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [closeMemberDetail, inviteOpen, roleDialogOpen, selectedMemberRef]);

  return (
    <main className="saas-admin-page">
      <header className="saas-admin-executive">
        <div className="saas-admin-executive__top">
          <div className="saas-admin-executive__intro">
            <p className="saas-admin-executive__eyebrow">YÖNETİM VE GÜVENLİK</p>
            <h1>Ayarlar</h1>
            <p className="saas-admin-executive__subtitle">
              Kiracı kullanıcıları, rolleri, tesis erişimini, oturum güvenliğini ve denetim kayıtlarını yönetin.
            </p>
            <p className="saas-admin-tenant-context">
              Organizasyon: {summary.tenantDisplayName} · {summary.tenantKey}
            </p>
          </div>
          <div className="saas-admin-executive__actions">
            {capabilities.canManage ? (
              <button
                type="button"
                className="saas-admin-button saas-admin-button--primary"
                onClick={() => setInviteOpen(true)}
              >
                Kullanıcı davet et
              </button>
            ) : (
              <span className="saas-admin-readonly" role="status">
                Davet oluşturma yetkiniz yok.
              </span>
            )}
          </div>
        </div>
        <div className="saas-admin-executive__kpis" aria-label="Özet göstergeler">
          <KpiCard label="Aktif kullanıcı" value={formatCount(summary.activeMembers)} tone="neutral" />
          <KpiCard label="Bekleyen davet" value={formatCount(summary.pendingInvitations)} tone="warning" />
          <KpiCard label="Pasif üyelik" value={formatCount(summary.suspendedMembers)} tone="warning" />
          <KpiCard label="Aktif oturum" value={formatCount(summary.activeSessions)} tone="info" />
          <KpiCard label="MFA kayıtlı" value={formatCount(mfaEnabledCount)} tone="success" />
          <KpiCard label="Özel rol" value={formatCount(customRoleCount)} tone="neutral" />
        </div>
      </header>

      {partialErrors.length > 0 ? (
        <div className="saas-admin-alert" role="status">
          <ShieldAlert aria-hidden="true" size={16} />
          Bazı bölümler kısmi olarak yüklenemedi: {partialErrors.join(" · ")}
        </div>
      ) : null}

      {error ? (
        <div className="saas-admin-alert" role="alert">
          <ShieldAlert aria-hidden="true" size={16} /> {error}
        </div>
      ) : null}

      {feedback ? (
        <div className="saas-admin-status" role="status">
          {feedback}
        </div>
      ) : null}

      <div className="saas-admin-tab-rail-wrap">
        <div className="saas-admin-tab-rail" ref={tabListRef} role="tablist" aria-label="Ayarlar sekmeleri">
          {TABS.map((tab, index) => (
            <button
              key={tab.id}
              type="button"
              className="saas-admin-tab-rail__tab"
              role="tab"
              id={`saas-admin-tab-${tab.id}`}
              aria-selected={activeTab === tab.id}
              aria-controls={`saas-admin-panel-${tab.id}`}
              onClick={() => setActiveTab(tab.id)}
              onKeyDown={(event) => handleTabKeyDown(event, index)}
            >
              {tab.label}
            </button>
          ))}
        </div>
      </div>

      {activeTab === "users" ? (
        <UsersWorkspace
          members={filteredMembers}
          allMembersCount={members.length}
          invitations={invitations}
          facilities={facilities}
          roles={roles}
          sessions={sessions}
          capabilities={capabilities}
          isPending={isPending}
          selectedMember={selectedMember}
          selectedMemberRef={selectedMemberRef}
          userSearch={userSearch}
          statusFilter={statusFilter}
          roleFilter={roleFilter}
          facilityFilter={facilityFilter}
          mfaFilter={mfaFilter}
          effectivePreview={effectivePreview}
          rowRefs={rowRefs}
          onSearchChange={setUserSearch}
          onStatusFilterChange={setStatusFilter}
          onRoleFilterChange={setRoleFilter}
          onFacilityFilterChange={setFacilityFilter}
          onMfaFilterChange={setMfaFilter}
          onClearFilters={clearUserFilters}
          onSelectMember={setSelectedMemberRef}
          onCloseDetail={closeMemberDetail}
          onPreviewEffective={async (memberReference) => {
            const result = await loadSaasAdminEffectiveAccessAction(memberReference);
            if (result.status === "success") {
              setEffectivePreview(
                `İzinli: ${result.value.grantedPermissionKeys.length} · Reddedilen: ${result.value.deniedPermissionKeys.length}`,
              );
            }
          }}
          onSuspend={(member) => {
            if (!window.confirm("Bu üyeliği pasifleştirmek istediğinize emin misiniz?")) return;
            runAction(() =>
              suspendSaasAdminMemberAction({
                memberReference: member.memberReference,
                expectedAuthorizationVersion: Number(member.authorizationVersion),
                idempotencyKey: createIdempotencyKey("suspend"),
              }),
            );
          }}
          onReactivate={(member) =>
            runAction(() =>
              reactivateSaasAdminMemberAction({
                memberReference: member.memberReference,
                expectedAuthorizationVersion: Number(member.authorizationVersion),
                idempotencyKey: createIdempotencyKey("reactivate"),
              }),
            )
          }
          onSaveRoles={(member, roleReferences) =>
            runAction(() =>
              updateSaasAdminMemberRolesAction({
                memberReference: member.memberReference,
                roleReferences,
                expectedAuthorizationVersion: Number(member.authorizationVersion),
                idempotencyKey: createIdempotencyKey("roles"),
              }),
            )
          }
          onSaveFacilities={(member, facilityCodes) =>
            runAction(() =>
              updateSaasAdminMemberFacilitiesAction({
                memberReference: member.memberReference,
                facilityCodes,
                expectedAuthorizationVersion: Number(member.authorizationVersion),
                idempotencyKey: createIdempotencyKey("facilities"),
              }),
            )
          }
          onRevokeSessions={(member) => {
            if (!window.confirm("Bu kullanıcının tüm oturumlarını sonlandırmak istiyor musunuz?")) return;
            runAction(() =>
              revokeAllSaasAdminMemberSessionsAction({
                memberReference: member.memberReference,
                idempotencyKey: createIdempotencyKey("revoke-all"),
              }),
            );
          }}
          onOpenSessionsTab={() => setActiveTab("sessions")}
        />
      ) : null}

      {activeTab === "roles" ? (
        <section className="saas-admin-panel" role="tabpanel" id="saas-admin-panel-roles" aria-labelledby="saas-admin-tab-roles">
          <div className={`saas-admin-registry-layout${selectedRole ? " saas-admin-registry-layout--detail" : ""}`}>
            <div className="saas-admin-registry-surface">
              <div className="saas-admin-registry-surface__header">
                <h2 className="saas-admin-registry-surface__title">Rol kaydı</h2>
                <span className="saas-admin-registry-surface__count">{formatCount(roles.length)} rol</span>
              </div>
              {roles.length === 0 ? (
                <div className="saas-admin-empty">Rol tanımı bulunamadı.</div>
              ) : (
                <div className="saas-admin-registry-list">
                  {roles.map((role) => (
                    <button
                      key={role.roleReference}
                      type="button"
                      className="saas-admin-registry-row"
                      aria-current={selectedRoleRef === role.roleReference}
                      onClick={() => setSelectedRoleRef(role.roleReference)}
                    >
                      <div className="saas-admin-registry-row__top">
                        <span className="saas-admin-registry-row__name">{role.displayName}</span>
                        <span className={`saas-admin-badge ${role.systemTemplateOrigin ? "saas-admin-badge--info" : "saas-admin-badge--neutral"}`}>
                          {role.systemTemplateOrigin ? "Yerleşik" : "Özel"}
                        </span>
                      </div>
                      <p className="saas-admin-registry-row__email">{role.description ?? "Açıklama yok"}</p>
                      <div className="saas-admin-registry-row__meta">
                        <span>{formatCount(members.filter((m) => m.roleReferences.includes(role.roleReference)).length)} üye</span>
                        <span>{formatCount(role.permissionKeys.length)} yetki</span>
                      </div>
                    </button>
                  ))}
                </div>
              )}
            </div>
            {selectedRole ? (
              <div className="saas-admin-detail-panel">
                <div className="saas-admin-detail-header">
                  <div className="saas-admin-detail-header__identity">
                    <h2>{selectedRole.displayName}</h2>
                    <p>{selectedRole.description ?? "—"}</p>
                  </div>
                </div>
                <div className="saas-admin-readonly">
                  Rol ayrıntıları ve yetki matrisi için &quot;Yetki matrisi&quot; düğmesini kullanın.
                </div>
                <div className="saas-admin-actions">
                  <button
                    type="button"
                    className="saas-admin-button saas-admin-button--primary"
                    disabled={!capabilities.canManage}
                    onClick={() => setRoleDialogOpen(true)}
                  >
                    Yetki matrisi
                  </button>
                </div>
              </div>
            ) : null}
          </div>
        </section>
      ) : null}

      {activeTab === "facilities" ? (
        <section className="saas-admin-panel" role="tabpanel" id="saas-admin-panel-facilities" aria-labelledby="saas-admin-tab-facilities">
          <div className="saas-admin-registry-toolbar">
            <label className="saas-admin-field">
              Kullanıcı seçin
              <select
                className="saas-admin-select"
                value={selectedFacilityMemberRef ?? ""}
                onChange={(event) => setSelectedFacilityMemberRef(event.target.value || null)}
              >
                <option value="">Tüm kullanıcılar</option>
                {members.map((member) => (
                  <option key={member.memberReference} value={member.memberReference}>
                    {formatMemberDisplayName(member.displayName)} · {member.emailMasked}
                  </option>
                ))}
              </select>
            </label>
          </div>
          {facilities.length === 0 ? (
            <div className="saas-admin-empty">Tesis kaydı bulunamadı.</div>
          ) : (
            <div className="saas-admin-table-wrap">
              <table className="saas-admin-table">
                <thead>
                  <tr>
                    <th>Tesis</th>
                    <th>Durum</th>
                    <th>Erişimli üye</th>
                    <th>Seçili kullanıcı erişimi</th>
                  </tr>
                </thead>
                <tbody>
                  {facilities.map((facility) => {
                    const member = members.find((item) => item.memberReference === selectedFacilityMemberRef);
                    const granted = member?.facilityCodes.includes(facility.facilityCode) ?? false;
                    return (
                      <tr key={facility.facilityCode}>
                        <td>
                          <strong>{facility.displayName}</strong>
                          <div>{facility.facilityCode}</div>
                        </td>
                        <td>
                          <span className={`saas-admin-badge ${facility.status === "Active" ? "saas-admin-badge--success" : "saas-admin-badge--neutral"}`}>
                            {facility.status === "Active" ? "Aktif" : "Pasif"}
                          </span>
                        </td>
                        <td>
                          {formatCount(members.filter((item) => item.facilityCodes.includes(facility.facilityCode)).length)}
                        </td>
                        <td>
                          {member
                            ? (
                                <span className={`saas-admin-badge ${granted ? "saas-admin-badge--success" : "saas-admin-badge--critical"}`}>
                                  {granted ? "Erişim var" : "Erişim yok"}
                                </span>
                              )
                            : "—"}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
          {!capabilities.canManage ? (
            <div className="saas-admin-readonly" role="status">Tesis erişimi salt okunur modda görüntüleniyor.</div>
          ) : null}
        </section>
      ) : null}

      {activeTab === "sessions" ? (
        <section className="saas-admin-panel" role="tabpanel" id="saas-admin-panel-sessions" aria-labelledby="saas-admin-tab-sessions">
          <div className="saas-admin-readonly" role="note">
            Oturum listesi yalnızca aktif ve sonlandırılmış oturum durumlarını gösterir. Mevcut oturum işareti API yanıtında ayrı bir alan olarak sağlanmadığından bu görünümde gösterilemiyor.
          </div>
          {sessions.length === 0 ? (
            <div className="saas-admin-empty">Oturum kaydı bulunamadı.</div>
          ) : (
            <div className="saas-admin-table-wrap">
              <table className="saas-admin-table">
                <thead>
                  <tr>
                    <th>Kullanıcı</th>
                    <th>Cihaz</th>
                    <th>Oluşturulma</th>
                    <th>Son görülme</th>
                    <th>Bitiş</th>
                    <th>MFA</th>
                    <th>Durum</th>
                    <th>İşlem</th>
                  </tr>
                </thead>
                <tbody>
                  {sessions.map((session) => (
                    <tr key={`${session.memberReference}-${session.createdAtUtc}-${session.lastSeenAtUtc}`}>
                      <td>
                        {formatMemberDisplayName(session.accountDisplayName)}
                        <div>{session.emailMasked}</div>
                      </td>
                      <td>{session.deviceLabel ?? "Bilinmeyen cihaz"}</td>
                      <td>{formatDateTime(session.createdAtUtc)}</td>
                      <td>{formatDateTime(session.lastSeenAtUtc)}</td>
                      <td>{formatDateTime(session.absoluteExpiresAtUtc)}</td>
                      <td>
                        <span className={`saas-admin-badge ${session.mfaSatisfied ? "saas-admin-badge--success" : "saas-admin-badge--warning"}`}>
                          {session.mfaSatisfied ? "Tamamlandı" : "Bekliyor"}
                        </span>
                      </td>
                      <td>
                        <span className={`saas-admin-badge ${formatSessionBadgeClass(session.isRevoked)}`}>
                          {formatSessionStatus(session.isRevoked)}
                        </span>
                      </td>
                      <td>
                        {capabilities.canManage && !session.isRevoked ? (
                          <button
                            type="button"
                            className="saas-admin-button saas-admin-button--danger saas-admin-button--compact"
                            disabled={isPending}
                            onClick={() => {
                              if (!window.confirm("Bu oturumu sonlandırmak istediğinize emin misiniz?")) return;
                              runAction(() =>
                                revokeSaasAdminSessionAction({
                                  sessionReference: session.revokeToken,
                                  idempotencyKey: createIdempotencyKey("revoke-session"),
                                }),
                              );
                            }}
                          >
                            Oturumu sonlandır
                          </button>
                        ) : "—"}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          <p className="saas-admin-registry-surface__count">
            Aktif oturum sayısı (özet): {formatCount(activeSessionsDisplay.length)} · API özeti: {formatCount(summary.activeSessions)}
          </p>
        </section>
      ) : null}

      {activeTab === "audit" ? (
        <section className="saas-admin-panel" role="tabpanel" id="saas-admin-panel-audit" aria-labelledby="saas-admin-tab-audit">
          <div className="saas-admin-registry-toolbar">
            <div className="saas-admin-registry-toolbar__search">
              <Search className="saas-admin-registry-toolbar__search-icon" aria-hidden="true" />
              <input
                type="search"
                placeholder="İşlem, aktör veya hedef ara"
                value={auditSearch}
                onChange={(event) => setAuditSearch(event.target.value)}
                aria-label="Denetim kaydı ara"
              />
            </div>
            <div className="saas-admin-registry-toolbar__filters">
              <label className="saas-admin-registry-toolbar__filter">
                Sonuç
                <select value={auditOutcomeFilter} onChange={(event) => setAuditOutcomeFilter(event.target.value)}>
                  <option value="">Tümü</option>
                  <option value="Başarılı">Başarılı</option>
                  <option value="Reddedildi">Reddedildi</option>
                  <option value="Kısıtlandı">Kısıtlandı</option>
                </select>
              </label>
            </div>
          </div>
          {filteredAuditEvents.length === 0 ? (
            <div className="saas-admin-empty">Denetim kaydı bulunamadı.</div>
          ) : (
            <div className="saas-admin-table-wrap">
              <table className="saas-admin-table">
                <thead>
                  <tr>
                    <th>Zaman</th>
                    <th>İşlem</th>
                    <th>Aktör</th>
                    <th>Hedef</th>
                    <th>Sonuç</th>
                    <th>Özet</th>
                    <th />
                  </tr>
                </thead>
                <tbody>
                  {filteredAuditEvents.map((event) => {
                    const key = `${event.eventType}-${event.createdAtUtc}-${event.entityReference}`;
                    return (
                      <tr key={key}>
                        <td>{formatDateTime(event.createdAtUtc)}</td>
                        <td>{formatAuditEventType(event.eventType)}</td>
                        <td>{formatActorReference(event.actorReference)}</td>
                        <td>{formatAuditEntityReference(event.entityReference, event.eventType)}</td>
                        <td>
                          <span className="saas-admin-badge saas-admin-badge--neutral">
                            {formatAuditOutcome(event.eventType)}
                          </span>
                        </td>
                        <td>{event.after ? formatAuditSummary(event.after) : "—"}</td>
                        <td>
                          <button
                            type="button"
                            className="saas-admin-button saas-admin-button--compact"
                            onClick={() => setExpandedAuditKey(expandedAuditKey === key ? null : key)}
                          >
                            {expandedAuditKey === key ? "Gizle" : "Ayrıntı"}
                          </button>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
          {expandedAuditKey ? (
            <div className="saas-admin-audit-detail" role="region" aria-label="Denetim ayrıntısı">
              {JSON.stringify(
                sanitizeAuditDetail(
                  filteredAuditEvents.find(
                    (event) => `${event.eventType}-${event.createdAtUtc}-${event.entityReference}` === expandedAuditKey,
                  ),
                ),
                null,
                2,
              )}
            </div>
          ) : null}
        </section>
      ) : null}

      {activeTab === "billing" ? (
        <BillingWorkspace
          commercialCatalog={commercialCatalog}
          subscription={subscription}
          billingWorkspace={billingWorkspace}
          billingProvider={billingProvider}
          billingLoadError={billingLoadError}
          canManage={capabilities.canManage}
        />
      ) : null}

      {isPending ? (
        <div className="saas-admin-empty" role="status">
          <LoaderCircle aria-hidden="true" className="animate-spin" /> İşlem sürüyor...
        </div>
      ) : null}

      {inviteOpen ? (
        <InviteDialog
          roles={roles}
          facilities={facilities}
          permissions={activePermissions}
          isPending={isPending}
          onClose={() => setInviteOpen(false)}
          onSubmit={(payload) =>
            runAction(async () => {
              const result = await inviteSaasAdminUserAction(payload);
              if (result.status === "success") setInviteOpen(false);
              return result;
            })
          }
        />
      ) : null}

      {selectedRole && roleDialogOpen ? (
        <RolePermissionsDialog
          role={selectedRole}
          permissionGroups={permissionGroups}
          capabilities={capabilities}
          isPending={isPending}
          onClose={() => setRoleDialogOpen(false)}
          onSave={(grants) =>
            runAction(() =>
              updateSaasAdminRolePermissionsAction({
                roleReference: selectedRole.roleReference,
                grants,
                expectedVersion: selectedRole.version,
                idempotencyKey: createIdempotencyKey("role-permissions"),
              }),
            )
          }
        />
      ) : null}
    </main>
  );
}

function KpiCard({ label, value, tone }: Readonly<{ label: string; value: string; tone: string }>) {
  return (
    <article className={`saas-admin-executive__kpi saas-admin-executive__kpi--${tone}`}>
      <span className="saas-admin-executive__kpi-label">{label}</span>
      <strong className="saas-admin-executive__kpi-value">{value}</strong>
    </article>
  );
}

function UsersWorkspace({
  members,
  allMembersCount,
  invitations,
  facilities,
  roles,
  sessions,
  capabilities,
  isPending,
  selectedMember,
  selectedMemberRef,
  userSearch,
  statusFilter,
  roleFilter,
  facilityFilter,
  mfaFilter,
  effectivePreview,
  rowRefs,
  onSearchChange,
  onStatusFilterChange,
  onRoleFilterChange,
  onFacilityFilterChange,
  onMfaFilterChange,
  onClearFilters,
  onSelectMember,
  onCloseDetail,
  onPreviewEffective,
  onSuspend,
  onReactivate,
  onSaveRoles,
  onSaveFacilities,
  onRevokeSessions,
  onOpenSessionsTab,
}: Readonly<{
  members: readonly SaasAdminMember[];
  allMembersCount: number;
  invitations: readonly SaasAdminInvitation[];
  facilities: readonly SaasAdminFacility[];
  roles: readonly SaasAdminRole[];
  sessions: readonly SaasAdminSession[];
  capabilities: SaasAdminCapabilities;
  isPending: boolean;
  selectedMember: SaasAdminMember | null;
  selectedMemberRef: string | null;
  userSearch: string;
  statusFilter: string;
  roleFilter: string;
  facilityFilter: string;
  mfaFilter: string;
  effectivePreview: string | null;
  rowRefs: MutableRefObject<Map<string, HTMLButtonElement>>;
  onSearchChange: (value: string) => void;
  onStatusFilterChange: (value: string) => void;
  onRoleFilterChange: (value: string) => void;
  onFacilityFilterChange: (value: string) => void;
  onMfaFilterChange: (value: string) => void;
  onClearFilters: () => void;
  onSelectMember: (ref: string) => void;
  onCloseDetail: () => void;
  onPreviewEffective: (memberReference: string) => Promise<void>;
  onSuspend: (member: SaasAdminMember) => void;
  onReactivate: (member: SaasAdminMember) => void;
  onSaveRoles: (member: SaasAdminMember, roleReferences: readonly string[]) => void;
  onSaveFacilities: (member: SaasAdminMember, facilityCodes: readonly string[]) => void;
  onRevokeSessions: (member: SaasAdminMember) => void;
  onOpenSessionsTab: () => void;
}>) {
  const [menuOpen, setMenuOpen] = useState(false);

  const memberSessions = useMemo(
    () => (selectedMember
      ? sessions.filter((session) => session.memberReference === selectedMember.memberReference)
      : []),
    [selectedMember, sessions],
  );

  return (
    <section
      className="saas-admin-panel"
      role="tabpanel"
      id="saas-admin-panel-users"
      aria-labelledby="saas-admin-tab-users"
    >
      <div className={`saas-admin-registry-layout${selectedMember ? " saas-admin-registry-layout--detail" : ""}`}>
        <div className="saas-admin-registry-surface">
          <div className="saas-admin-registry-surface__header">
            <h2 className="saas-admin-registry-surface__title">Kullanıcı kaydı</h2>
            <span className="saas-admin-registry-surface__count">
              {formatCount(members.length)} / {formatCount(allMembersCount)} sonuç
            </span>
          </div>
          <div className="saas-admin-registry-toolbar">
            <div className="saas-admin-registry-toolbar__search">
              <Search className="saas-admin-registry-toolbar__search-icon" aria-hidden="true" />
              <input
                type="search"
                placeholder="Ad veya e-posta ara"
                value={userSearch}
                onChange={(event) => onSearchChange(event.target.value)}
                aria-label="Kullanıcı ara"
              />
            </div>
            <div className="saas-admin-registry-toolbar__filters">
              <label className="saas-admin-registry-toolbar__filter">
                Durum
                <select value={statusFilter} onChange={(event) => onStatusFilterChange(event.target.value)}>
                  <option value="">Tümü</option>
                  <option value="active">Aktif</option>
                  <option value="suspended">Pasif</option>
                </select>
              </label>
              <label className="saas-admin-registry-toolbar__filter">
                Rol
                <select value={roleFilter} onChange={(event) => onRoleFilterChange(event.target.value)}>
                  <option value="">Tümü</option>
                  {roles.map((role) => (
                    <option key={role.roleReference} value={role.roleReference}>{role.displayName}</option>
                  ))}
                </select>
              </label>
              <label className="saas-admin-registry-toolbar__filter">
                Tesis
                <select value={facilityFilter} onChange={(event) => onFacilityFilterChange(event.target.value)}>
                  <option value="">Tümü</option>
                  {facilities.map((facility) => (
                    <option key={facility.facilityCode} value={facility.facilityCode}>{facility.displayName}</option>
                  ))}
                </select>
              </label>
              <label className="saas-admin-registry-toolbar__filter">
                MFA
                <select value={mfaFilter} onChange={(event) => onMfaFilterChange(event.target.value)}>
                  <option value="">Tümü</option>
                  <option value="enrolled">Kayıtlı</option>
                  <option value="missing">Kayıtlı değil</option>
                  <option value="required">Zorunlu</option>
                </select>
              </label>
              <button type="button" className="saas-admin-button saas-admin-button--ghost saas-admin-button--compact" onClick={onClearFilters}>
                Filtreleri temizle
              </button>
            </div>
          </div>
          {members.length === 0 ? (
            <div className="saas-admin-empty">Eşleşen kullanıcı bulunamadı.</div>
          ) : (
            <div className="saas-admin-registry-list">
              {members.map((member) => (
                <button
                  key={member.memberReference}
                  type="button"
                  className="saas-admin-registry-row"
                  aria-current={selectedMemberRef === member.memberReference}
                  ref={(node) => {
                    if (node) rowRefs.current.set(member.memberReference, node);
                  }}
                  onClick={() => onSelectMember(member.memberReference)}
                >
                  <div className="saas-admin-registry-row__top">
                    <span className="saas-admin-registry-row__name">{formatMemberDisplayName(member.displayName)}</span>
                    <span className="saas-admin-badge saas-admin-badge--neutral">{formatMembershipStatus(member.membershipStatus)}</span>
                  </div>
                  <span className="saas-admin-registry-row__email">{member.emailMasked}</span>
                  <div className="saas-admin-registry-row__meta">
                    <span>{member.roleReferences.map((role) => formatRoleLabel(role)).join(", ") || "—"}</span>
                    <span className={`saas-admin-badge ${formatMfaBadgeClass(member)}`}>{formatMfaStatus(member)}</span>
                    <span>{formatDateTime(member.lastSessionActivityUtc)}</span>
                  </div>
                </button>
              ))}
            </div>
          )}
        </div>

        {selectedMember ? (
          <MemberDetailPanel
            key={selectedMember.memberReference}
            member={selectedMember}
            facilities={facilities}
            roles={roles}
            memberSessions={memberSessions}
            capabilities={capabilities}
            isPending={isPending}
            menuOpen={menuOpen}
            effectivePreview={effectivePreview}
            onToggleMenu={() => setMenuOpen((open) => !open)}
            onClose={onCloseDetail}
            onPreview={() => onPreviewEffective(selectedMember.memberReference)}
            onSuspend={() => onSuspend(selectedMember)}
            onReactivate={() => onReactivate(selectedMember)}
            onRevokeSessions={() => onRevokeSessions(selectedMember)}
            onOpenSessions={onOpenSessionsTab}
            onSaveRoles={(roleReferences) => onSaveRoles(selectedMember, roleReferences)}
            onSaveFacilities={(facilityCodes) => onSaveFacilities(selectedMember, facilityCodes)}
          />
        ) : null}
      </div>

      {invitations.length > 0 ? (
        <div className="saas-admin-invitations-block" aria-label="Bekleyen davetler">
          <h3>Bekleyen davetler ({formatCount(invitations.length)})</h3>
          <div className="saas-admin-table-wrap">
            <table className="saas-admin-table">
              <thead>
                <tr>
                  <th>E-posta</th>
                  <th>Teslimat</th>
                  <th>Rol</th>
                  <th>Bitiş</th>
                </tr>
              </thead>
              <tbody>
                {invitations.map((invitation) => (
                  <tr key={invitation.invitationReference}>
                    <td>{invitation.emailMasked}</td>
                    <td>{formatDeliveryStatus(invitation.deliveryStatus)}</td>
                    <td>{invitation.intendedRoleReference ? formatRoleLabel(invitation.intendedRoleReference) : "—"}</td>
                    <td>{formatDateTime(invitation.expiresAtUtc)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      ) : (
        <div className="saas-admin-empty">Bekleyen davet yok.</div>
      )}
    </section>
  );
}

function MemberDetailPanel({
  member,
  facilities,
  roles,
  memberSessions,
  capabilities,
  isPending,
  menuOpen,
  effectivePreview,
  onToggleMenu,
  onClose,
  onPreview,
  onSuspend,
  onReactivate,
  onRevokeSessions,
  onOpenSessions,
  onSaveRoles,
  onSaveFacilities,
}: Readonly<{
  member: SaasAdminMember;
  facilities: readonly SaasAdminFacility[];
  roles: readonly SaasAdminRole[];
  memberSessions: readonly SaasAdminSession[];
  capabilities: SaasAdminCapabilities;
  isPending: boolean;
  menuOpen: boolean;
  effectivePreview: string | null;
  onToggleMenu: () => void;
  onClose: () => void;
  onPreview: () => void;
  onSuspend: () => void;
  onReactivate: () => void;
  onRevokeSessions: () => void;
  onOpenSessions: () => void;
  onSaveRoles: (roleReferences: readonly string[]) => void;
  onSaveFacilities: (facilityCodes: readonly string[]) => void;
}>) {
  const [editRoles, setEditRoles] = useState(() => new Set(member.roleReferences));
  const [editFacilities, setEditFacilities] = useState(() => new Set(member.facilityCodes));
  const identityHint = formatMemberIdentityHint(member.displayName);
  return (
    <aside className="saas-admin-detail-panel" aria-label="Kullanıcı detayı">
      <div className="saas-admin-detail-header">
        <div className="saas-admin-detail-header__identity">
          <h2>{formatMemberDisplayName(member.displayName)}</h2>
          <p>{member.emailMasked}</p>
          {identityHint ? <p>{identityHint}</p> : null}
        </div>
        <div className="saas-admin-actions">
          {capabilities.canManage ? (
            <div className="saas-admin-menu">
              <button type="button" className="saas-admin-detail-close" aria-label="İşlemler menüsü" onClick={onToggleMenu}>
                <MoreHorizontal size={16} aria-hidden="true" />
              </button>
              {menuOpen ? (
                <div className="saas-admin-menu__panel" role="menu">
                  {member.membershipStatus === "active" ? (
                    <button type="button" className="saas-admin-menu__item saas-admin-menu__item--danger" role="menuitem" onClick={onSuspend}>
                      Üyeliği pasifleştir
                    </button>
                  ) : null}
                  {member.membershipStatus === "suspended" ? (
                    <button type="button" className="saas-admin-menu__item" role="menuitem" onClick={onReactivate}>
                      Yeniden etkinleştir
                    </button>
                  ) : null}
                  <button type="button" className="saas-admin-menu__item saas-admin-menu__item--danger" role="menuitem" onClick={onRevokeSessions}>
                    Tüm oturumları sonlandır
                  </button>
                  <button type="button" className="saas-admin-menu__item" role="menuitem" onClick={onOpenSessions}>
                    Oturumları görüntüle
                  </button>
                </div>
              ) : null}
            </div>
          ) : null}
          <button type="button" className="saas-admin-detail-close" aria-label="Kullanıcı detayını kapat" onClick={onClose}>
            <X size={16} aria-hidden="true" />
          </button>
        </div>
      </div>

      <div className="saas-admin-detail-section">
        <h3>Üyelik özeti</h3>
        <p>Durum: {formatMembershipStatus(member.membershipStatus)}</p>
        <p>Son etkinlik: {formatDateTime(member.lastSessionActivityUtc)}</p>
      </div>

      <div className="saas-admin-detail-section">
        <h3>Atanan roller</h3>
        <div className="saas-admin-chip-list">
          {roles.map((role) => (
            <label key={role.roleReference} className="saas-admin-chip">
              <input
                type="checkbox"
                checked={editRoles.has(role.roleReference)}
                disabled={!capabilities.canManage}
                onChange={(event) => {
                  const next = new Set(editRoles);
                  if (event.target.checked) next.add(role.roleReference);
                  else next.delete(role.roleReference);
                  setEditRoles(next);
                }}
              />
              {role.displayName}
            </label>
          ))}
        </div>
        {capabilities.canManage ? (
          <button type="button" className="saas-admin-button saas-admin-button--primary saas-admin-button--compact" disabled={isPending} onClick={() => onSaveRoles([...editRoles])}>
            Rolleri kaydet
          </button>
        ) : (
          <div className="saas-admin-readonly">Rol düzenleme yetkiniz yok.</div>
        )}
      </div>

      <div className="saas-admin-detail-section">
        <h3>Tesis erişimi</h3>
        <div className="saas-admin-chip-list">
          {facilities.map((facility) => (
            <label key={facility.facilityCode} className="saas-admin-chip">
              <input
                type="checkbox"
                checked={editFacilities.has(facility.facilityCode)}
                disabled={!capabilities.canManage}
                onChange={(event) => {
                  const next = new Set(editFacilities);
                  if (event.target.checked) next.add(facility.facilityCode);
                  else next.delete(facility.facilityCode);
                  setEditFacilities(next);
                }}
              />
              {formatFacilityLabel(facility.facilityCode, facilities)}
            </label>
          ))}
        </div>
        {capabilities.canManage ? (
          <button type="button" className="saas-admin-button saas-admin-button--primary saas-admin-button--compact" disabled={isPending} onClick={() => onSaveFacilities([...editFacilities])}>
            Tesis erişimini kaydet
          </button>
        ) : null}
      </div>

      <div className="saas-admin-detail-section">
        <h3>Güvenlik</h3>
        <p>MFA: <span className={`saas-admin-badge ${formatMfaBadgeClass(member)}`}>{formatMfaStatus(member)}</span></p>
        <button type="button" className="saas-admin-button saas-admin-button--compact" onClick={onPreview}>Etkin erişimi önizle</button>
        {effectivePreview ? <p>{effectivePreview}</p> : null}
      </div>

      <div className="saas-admin-detail-section">
        <h3>Oturumlar</h3>
        {memberSessions.length === 0 ? (
          <p>Aktif oturum kaydı yok.</p>
        ) : (
          <ul>
            {memberSessions.map((session) => (
              <li key={`${session.createdAtUtc}-${session.lastSeenAtUtc}`}>
                {session.deviceLabel ?? "Bilinmeyen cihaz"} · {formatSessionStatus(session.isRevoked)} · {formatDateTime(session.lastSeenAtUtc)}
              </li>
            ))}
          </ul>
        )}
      </div>
    </aside>
  );
}

function InviteDialog({
  roles,
  facilities,
  permissions,
  isPending,
  onClose,
  onSubmit,
}: Readonly<{
  roles: readonly SaasAdminRole[];
  facilities: readonly SaasAdminFacility[];
  permissions: readonly SaasAdminPermission[];
  isPending: boolean;
  onClose: () => void;
  onSubmit: (payload: {
    email: string;
    intendedRoleReference: string | null;
    idempotencyKey: string;
  }) => void;
}>) {
  const titleId = useId();
  const [email, setEmail] = useState("");
  const [role, setRole] = useState("");
  const [facilityCodes, setFacilityCodes] = useState<string[]>([]);

  const impactCount = role
    ? permissions.filter((permission) => permission.status === "Active").length
    : 0;

  return (
    <div className="saas-admin-dialog-backdrop" role="presentation" onClick={onClose}>
      <div
        className="saas-admin-dialog saas-admin-dialog--wide"
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        onClick={(event) => event.stopPropagation()}
      >
        <h2 id={titleId}>Kullanıcı davet et</h2>
        <label className="saas-admin-field">
          E-posta
          <input className="saas-admin-input" type="email" value={email} onChange={(event) => setEmail(event.target.value)} />
        </label>
        <label className="saas-admin-field">
          Başlangıç rolü
          <select className="saas-admin-select" value={role} onChange={(event) => setRole(event.target.value)}>
            <option value="">Seçilmedi</option>
            {roles.filter((item) => item.status === "Active").map((item) => (
              <option key={item.roleReference} value={item.roleReference}>{item.displayName}</option>
            ))}
          </select>
        </label>
        <fieldset className="saas-admin-field">
          <legend>Tesis kapsamı (bilgilendirme)</legend>
          <div className="saas-admin-chip-list">
            {facilities.map((facility) => (
              <label key={facility.facilityCode} className="saas-admin-chip">
                <input
                  type="checkbox"
                  checked={facilityCodes.includes(facility.facilityCode)}
                  onChange={(event) => {
                    setFacilityCodes((current) => event.target.checked
                      ? [...current, facility.facilityCode]
                      : current.filter((code) => code !== facility.facilityCode));
                  }}
                />
                {formatFacilityLabel(facility.facilityCode, facilities)}
              </label>
            ))}
          </div>
        </fieldset>
        <div className="saas-admin-readonly">
          Davet kapsamı: {role ? formatRoleLabel(role) : "Rol seçilmedi"} · Tesis: {facilityCodes.length > 0 ? facilityCodes.map((code) => formatFacilityLabel(code, facilities)).join(", ") : "Belirtilmedi"}
          <br />
          Yetki etkisi özeti: {role ? `${impactCount} tanımlı yetki modülü değerlendirilir` : "Rol seçilince gösterilir"}
          <br />
          E-posta teslimatı geliştirme ortamında gerçek posta gönderimi yapmaz; davet kaydı oluşturulur.
        </div>
        <div className="saas-admin-actions">
          <button type="button" className="saas-admin-button" onClick={onClose}>Vazgeç</button>
          <button
            type="button"
            className="saas-admin-button saas-admin-button--primary"
            disabled={isPending || email.trim().length === 0}
            onClick={() => onSubmit({
              email,
              intendedRoleReference: role || null,
              idempotencyKey: createIdempotencyKey("invite"),
            })}
          >
            Davet oluştur
          </button>
        </div>
      </div>
    </div>
  );
}

function RolePermissionsDialog({
  role,
  permissionGroups,
  capabilities,
  isPending,
  onClose,
  onSave,
}: Readonly<{
  role: SaasAdminRole;
  permissionGroups: ReadonlyMap<string, readonly SaasAdminPermission[]>;
  capabilities: SaasAdminCapabilities;
  isPending: boolean;
  onClose: () => void;
  onSave: (grants: readonly Readonly<{ permissionKey: string; effect: string; recordScope?: string | null }>[]) => void;
}>) {
  const [selected, setSelected] = useState(new Set(role.permissionKeys));
  const [showTechnical, setShowTechnical] = useState(false);

  return (
    <div className="saas-admin-dialog-backdrop" role="presentation" onClick={onClose}>
      <div className="saas-admin-dialog saas-admin-dialog--wide" role="dialog" aria-modal="true" onClick={(event) => event.stopPropagation()}>
        <h2>{role.displayName} · Yetki matrisi</h2>
        <p>{role.isProtected ? "Korumalı rol: son sahip koruması ve platform yönetici reddi sunucu tarafında uygulanır." : "Değişiklikler kaydedilmeden önce etkin erişim etkisini gözden geçirin."}</p>
        <button type="button" className="saas-admin-button saas-admin-button--compact" onClick={() => setShowTechnical((value) => !value)}>
          {showTechnical ? "Teknik ayrıntıları gizle" : "Teknik ayrıntıları göster"}
        </button>
        <div className="saas-admin-matrix">
          {[...permissionGroups.entries()].map(([moduleCode, modulePermissions]) => (
            <div key={moduleCode} className="saas-admin-matrix-module">
              <div className="saas-admin-matrix-module__header">{formatModuleLabel(moduleCode)}</div>
              {modulePermissions.map((permission) => (
                <div key={permission.permissionKey} className="saas-admin-matrix-row">
                  <div>
                    <div className="saas-admin-matrix-row__label">{permission.turkishLabel || formatActionLabel(permission.actionCode)}</div>
                    {showTechnical ? (
                      <div className="saas-admin-matrix-row__technical">{permission.permissionKey}</div>
                    ) : null}
                  </div>
                  <label>
                    <input
                      type="checkbox"
                      checked={selected.has(permission.permissionKey)}
                      disabled={!capabilities.canManage}
                      onChange={(event) => {
                        const next = new Set(selected);
                        if (event.target.checked) next.add(permission.permissionKey);
                        else next.delete(permission.permissionKey);
                        setSelected(next);
                      }}
                    />{" "}
                    {formatActionLabel(permission.actionCode)}
                  </label>
                </div>
              ))}
            </div>
          ))}
        </div>
        <div className="saas-admin-actions">
          <button type="button" className="saas-admin-button" onClick={onClose}>Kapat</button>
          {capabilities.canManage ? (
            <button
              type="button"
              className="saas-admin-button saas-admin-button--primary"
              disabled={isPending}
              onClick={() => {
                if (!window.confirm("Rol yetkilerini güncellemek istediğinize emin misiniz?")) return;
                const grants = [...permissionGroups.values()].flat().flatMap((permission) => {
                  if (selected.has(permission.permissionKey)) {
                    return [{ permissionKey: permission.permissionKey, effect: "allow", recordScope: null }];
                  }
                  if (role.permissionKeys.includes(permission.permissionKey)) {
                    return [{ permissionKey: permission.permissionKey, effect: "deny", recordScope: null }];
                  }
                  return [];
                });
                onSave(grants);
              }}
            >
              Yetkileri kaydet
            </button>
          ) : (
            <div className="saas-admin-readonly">Yetki düzenleme yetkiniz yok.</div>
          )}
        </div>
      </div>
    </div>
  );
}

function sanitizeAuditDetail(event: SaasAdminAuditEvent | undefined) {
  if (!event) return null;
  return {
    eventType: formatAuditEventType(event.eventType),
    actorReference: formatActorReference(event.actorReference),
    entityReference: formatAuditEntityReference(event.entityReference, event.eventType),
    createdAtUtc: event.createdAtUtc,
    outcome: formatAuditOutcome(event.eventType),
    summary: event.after ? formatAuditSummary(event.after) : null,
  };
}

function BillingWorkspace({
  commercialCatalog,
  subscription,
  billingWorkspace,
  billingProvider,
  billingLoadError,
  canManage,
}: Readonly<{
  commercialCatalog: SaasAdminCommercialCatalog | null;
  subscription: TenantSubscriptionCurrent | null;
  billingWorkspace: BillingWorkspaceSummary | null;
  billingProvider: BillingProviderCapability | null;
  billingLoadError: string | null;
  canManage: boolean;
}>) {
  const [invoiceStatusFilter, setInvoiceStatusFilter] = useState("");
  const [invoiceSearch, setInvoiceSearch] = useState("");
  const [selectedInvoiceId, setSelectedInvoiceId] = useState<string | null>(null);
  const [loadedWorkspace, setLoadedWorkspace] = useState(billingWorkspace);
  const [loadedProvider, setLoadedProvider] = useState(billingProvider);
  const [billingLoading, setBillingLoading] = useState(!billingWorkspace);
  const [checkoutBusy, setCheckoutBusy] = useState(false);
  const [checkoutMessage, setCheckoutMessage] = useState<string | null>(null);

  useEffect(() => {
    if (billingWorkspace) {
      setLoadedWorkspace(billingWorkspace);
      setLoadedProvider(billingProvider);
      setBillingLoading(false);
      return;
    }

    let cancelled = false;
    setBillingLoading(true);
    void loadBillingWorkspaceAction().then((result) => {
      if (cancelled) return;
      if (result.status === "success") {
        setLoadedWorkspace(result.workspace);
        setLoadedProvider(result.provider);
      }
      setBillingLoading(false);
    });

    return () => {
      cancelled = true;
    };
  }, [billingProvider, billingWorkspace]);

  const activeWorkspace = loadedWorkspace;
  const activeProvider = loadedProvider;

  const plans = commercialCatalog?.plans ?? [];
  const moduleSummary = subscription ? listModuleEntitlementSummary(subscription.entitlements) : [];
  const overLimitWarnings = subscription
    ? [
        formatOverLimitWarning(subscription.limits.users),
        formatOverLimitWarning(subscription.limits.facilities),
        formatOverLimitWarning(subscription.limits.connectedMachines),
      ].filter((warning): warning is string => Boolean(warning))
    : [];
  const graceWarning = formatGraceWarning(subscription);

  const invoices = activeWorkspace?.invoices ?? [];
  const filteredInvoices = useMemo(() => {
    return invoices.filter((invoice) => {
      if (invoiceStatusFilter && invoice.invoiceStatus !== invoiceStatusFilter) return false;
      if (invoiceSearch.trim()) {
        const term = invoiceSearch.trim().toLowerCase();
        if (!invoice.invoiceNumber.toLowerCase().includes(term)) return false;
      }
      return true;
    });
  }, [invoices, invoiceSearch, invoiceStatusFilter]);

  const selectedInvoice = useMemo(
    () => filteredInvoices.find((item) => item.invoiceId === selectedInvoiceId) ?? filteredInvoices[0] ?? null,
    [filteredInvoices, selectedInvoiceId],
  );

  const paymentAttempts = activeWorkspace?.recentPaymentAttempts ?? [];
  const invoicePayments = selectedInvoice
    ? paymentAttempts.filter((item) => item.invoiceId === selectedInvoice.invoiceId)
    : paymentAttempts;

  const account = activeWorkspace?.billingAccount;
  const providerStatus = activeProvider?.provider ?? activeWorkspace?.providerStatus;
  const providerMode = activeProvider?.providerMode ?? providerStatus?.providerMode ?? "not-configured";
  const cardCheckoutAvailable = providerStatus?.cardCheckoutAvailable === true;
  const selectedInvoicePayable = selectedInvoice
    ? selectedInvoice.invoiceStatus === "open" || selectedInvoice.invoiceStatus === "overdue"
    : false;

  async function handleCardCheckout() {
    if (!selectedInvoice || checkoutBusy || !canManage || !cardCheckoutAvailable) {
      return;
    }

    setCheckoutBusy(true);
    setCheckoutMessage(null);
    try {
      const detail = await loadBillingInvoiceDetailAction(selectedInvoice.invoiceId);
      if (detail.status === "failure") {
        setCheckoutMessage(detail.message);
        return;
      }

      const idempotencyKey = createIdempotencyKey("SAAS-PAYMENTS-1-CLOSURE-CHECKOUT");
      const init = await initializeCardCheckoutAction(
        selectedInvoice.invoiceId,
        detail.value.version,
        idempotencyKey,
      );
      if (init.status === "failure") {
        setCheckoutMessage(init.message);
        return;
      }

      window.location.assign(init.value.hostedCheckoutUrl);
    } finally {
      setCheckoutBusy(false);
    }
  }

  return (
    <section className="saas-admin-panel" role="tabpanel" id="saas-admin-panel-billing" aria-labelledby="saas-admin-tab-billing">
      <div className="saas-admin-readonly" role="note">
        {BILLING_DEVELOPMENT_NOTICE}
      </div>

      {billingLoadError ? (
        <div className="saas-admin-alert" role="alert">
          <ShieldAlert aria-hidden="true" /> {billingLoadError}
        </div>
      ) : null}

      {billingLoading ? (
        <div className="saas-admin-empty" role="status">
          <LoaderCircle aria-hidden="true" className="animate-spin" /> Faturalandırma verileri yükleniyor...
        </div>
      ) : null}

      <div className="saas-admin-billing-kpi-strip" role="group" aria-label="Fatura özeti">
        <article className="saas-admin-billing-kpi">
          <p>Açık faturalar</p>
          <strong>{formatCount(activeWorkspace?.openInvoiceCount ?? 0)}</strong>
        </article>
        <article className="saas-admin-billing-kpi">
          <p>Gecikmiş faturalar</p>
          <strong>{formatCount(activeWorkspace?.overdueInvoiceCount ?? 0)}</strong>
        </article>
        <article className="saas-admin-billing-kpi">
          <p>Doğrulama bekleyen</p>
          <strong>{formatCount(activeWorkspace?.awaitingVerificationCount ?? 0)}</strong>
        </article>
      </div>

      {subscription ? (
        <article className="saas-admin-billing-current">
          <header className="saas-admin-billing-current__header">
            <div>
              <p className="saas-admin-billing-current__eyebrow">MEVCUT ABONELİK</p>
              <h3>{formatPlanLabel(subscription.subscription)}</h3>
            </div>
            <span className="saas-admin-badge saas-admin-badge--info">
              {formatSubscriptionStatus(subscription.subscription.subscriptionStatus)}
            </span>
          </header>

          <dl className="saas-admin-billing-grid">
            <div>
              <dt>Ortam</dt>
              <dd>{formatSourceProvenance(subscription.subscription.sourceProvenance)}</dd>
            </div>
            <div>
              <dt>Faturalama döngüsü</dt>
              <dd>{formatBillingCadence(subscription.subscription.billingCadence)}</dd>
            </div>
            <div>
              <dt>Dönem başlangıcı</dt>
              <dd>{formatSubscriptionDateTime(subscription.subscription.currentPeriodStartUtc)}</dd>
            </div>
            <div>
              <dt>Dönem bitişi</dt>
              <dd>{formatSubscriptionDateTime(subscription.subscription.currentPeriodEndUtc)}</dd>
            </div>
          </dl>

          {account ? (
            <section className="saas-admin-billing-section">
              <h4>Faturalama hesabı</h4>
              <dl className="saas-admin-billing-grid">
                <div>
                  <dt>Görünen ad</dt>
                  <dd>{account.displayName}</dd>
                </div>
                <div>
                  <dt>İletişim</dt>
                  <dd>{account.billingContactMasked}</dd>
                </div>
                <div>
                  <dt>Tercih edilen ödeme</dt>
                  <dd>{account.preferredPaymentMethod ? formatPaymentMethod(account.preferredPaymentMethod) : "—"}</dd>
                </div>
                <div>
                  <dt>Vergi ülkesi</dt>
                  <dd>{account.taxCountryCode ?? "—"}</dd>
                </div>
              </dl>
            </section>
          ) : (
            <p className="saas-admin-billing-muted">Faturalama hesabı henüz oluşturulmadı.</p>
          )}

          {graceWarning ? (
            <div className="saas-admin-billing-warning" role="status">{graceWarning}</div>
          ) : null}

          <section className="saas-admin-billing-section">
            <h4>Modül erişimi</h4>
            {moduleSummary.length > 0 ? (
              <ul className="saas-admin-billing-module-list">
                {moduleSummary.map((label) => (
                  <li key={label}>{label}</li>
                ))}
              </ul>
            ) : (
              <p className="saas-admin-billing-muted">Modül erişim özeti yüklenemedi.</p>
            )}
          </section>

          {overLimitWarnings.length > 0 ? (
            <div className="saas-admin-billing-warning" role="alert">
              {overLimitWarnings.map((warning) => (
                <p key={warning}>{warning}</p>
              ))}
            </div>
          ) : null}

          <button type="button" className="saas-admin-button" disabled>
            Plan değiştirme akışı henüz kullanıma hazır değil.
          </button>
        </article>
      ) : (
        <div className="saas-admin-empty">{formatInsufficientData()}</div>
      )}

      <section className="saas-admin-billing-section" aria-label="Fatura kayıtları">
        <header className="saas-admin-billing-registry__header">
          <h4>Fatura kayıtları</h4>
          <div className="saas-admin-billing-registry__filters">
            <label className="saas-admin-field">
              <span className="saas-admin-field__label">Durum</span>
              <select
                value={invoiceStatusFilter}
                onChange={(event) => setInvoiceStatusFilter(event.target.value)}
                className="saas-admin-input"
              >
                <option value="">Tümü</option>
                <option value="draft">Taslak</option>
                <option value="open">Açık</option>
                <option value="paid">Ödendi</option>
                <option value="overdue">Gecikmiş</option>
                <option value="void">İptal</option>
              </select>
            </label>
            <label className="saas-admin-field saas-admin-field--search">
              <span className="saas-admin-field__label">Ara</span>
              <input
                type="search"
                value={invoiceSearch}
                onChange={(event) => setInvoiceSearch(event.target.value)}
                placeholder="Fatura numarası"
                className="saas-admin-input"
              />
            </label>
          </div>
        </header>

        {!activeWorkspace ? (
          <div className="saas-admin-empty">Fatura verileri yüklenemedi.</div>
        ) : filteredInvoices.length === 0 ? (
          <div className="saas-admin-empty">Eşleşen fatura bulunamadı.</div>
        ) : (
          <div className="saas-admin-billing-registry">
            <div className="saas-admin-billing-registry__list" role="list">
              {filteredInvoices.map((invoice) => (
                <button
                  key={invoice.invoiceId}
                  type="button"
                  role="listitem"
                  className={`saas-admin-billing-invoice-row${selectedInvoice?.invoiceId === invoice.invoiceId ? " is-selected" : ""}`}
                  onClick={() => setSelectedInvoiceId(invoice.invoiceId)}
                >
                  <span className="saas-admin-billing-invoice-row__number">{invoice.invoiceNumber}</span>
                  <span className={invoiceStatusBadgeClass(invoice.invoiceStatus)}>
                    {formatInvoiceStatus(invoice.invoiceStatus)}
                  </span>
                  <span>{formatMoney(invoice.totalAmount, invoice.currencyCode)}</span>
                  <span className="saas-admin-billing-muted">{formatBillingDateTime(invoice.dueAtUtc)}</span>
                </button>
              ))}
            </div>

            {selectedInvoice ? (
              <article className="saas-admin-billing-invoice-detail">
                <header>
                  <h5>{selectedInvoice.invoiceNumber}</h5>
                  <span className={invoiceStatusBadgeClass(selectedInvoice.invoiceStatus)}>
                    {formatInvoiceStatus(selectedInvoice.invoiceStatus)}
                  </span>
                </header>
                <dl className="saas-admin-billing-grid">
                  <div>
                    <dt>Toplam</dt>
                    <dd>{formatMoney(selectedInvoice.totalAmount, selectedInvoice.currencyCode)}</dd>
                  </div>
                  <div>
                    <dt>Dönem</dt>
                    <dd>
                      {formatBillingDateTime(selectedInvoice.periodStartUtc)}
                      {" — "}
                      {formatBillingDateTime(selectedInvoice.periodEndUtc)}
                    </dd>
                  </div>
                  <div>
                    <dt>Vade</dt>
                    <dd>{formatBillingDateTime(selectedInvoice.dueAtUtc)}</dd>
                  </div>
                  <div>
                    <dt>Ödeme zamanı</dt>
                    <dd>{formatBillingDateTime(selectedInvoice.paidAtUtc)}</dd>
                  </div>
                </dl>
                <p className="saas-admin-billing-muted">
                  Fatura kalemleri fatura açıldıktan sonra değiştirilemez.
                </p>
              </article>
            ) : null}
          </div>
        )}
      </section>

      <section className="saas-admin-billing-section" aria-label="Ödeme geçmişi">
        <h4>Ödeme denemeleri</h4>
        {invoicePayments.length === 0 ? (
          <p className="saas-admin-billing-muted">Ödeme denemesi kaydı yok.</p>
        ) : (
          <ul className="saas-admin-billing-payment-list">
            {invoicePayments.map((attempt) => (
              <li key={attempt.paymentAttemptId} className="saas-admin-billing-payment-item">
                <span>{formatPaymentMethod(attempt.paymentMethod)}</span>
                <span className={paymentStatusBadgeClass(attempt.paymentStatus)}>
                  {formatPaymentStatus(attempt.paymentStatus)}
                </span>
                <span>{formatMoney(attempt.amount, attempt.currencyCode)}</span>
                {attempt.paymentStatus === "awaiting-manual-verification" ? (
                  <span className="saas-admin-billing-warning" role="status">
                    Banka havalesi manuel doğrulama bekliyor.
                  </span>
                ) : null}
                {attempt.failureCode === "saas.billing.provider-not-configured" ? (
                  <span className="saas-admin-billing-muted">{CARD_PROVIDER_NOT_CONFIGURED_MESSAGE}</span>
                ) : null}
              </li>
            ))}
          </ul>
        )}
      </section>

      <section className="saas-admin-billing-section" aria-label="Ödeme yöntemleri">
        <h4>Ödeme yöntemleri</h4>
        {providerStatus ? (
          <dl className="saas-admin-billing-grid">
            <div>
              <dt>Sağlayıcı</dt>
              <dd>{providerStatus.isProduction ? "Üretim" : providerMode === "sandbox" ? "Test" : "Yapılandırılmadı"}</dd>
            </div>
            <div>
              <dt>Kart ödemesi</dt>
              <dd>{cardCheckoutAvailable ? "Kullanılabilir" : CARD_PROVIDER_NOT_CONFIGURED_MESSAGE}</dd>
            </div>
            <div>
              <dt>Desteklenen yöntemler</dt>
              <dd>
                {providerStatus.supportedPaymentMethods.length > 0
                  ? providerStatus.supportedPaymentMethods.map(formatPaymentMethod).join(", ")
                  : "—"}
              </dd>
            </div>
          </dl>
        ) : (
          <p className="saas-admin-billing-muted">Ödeme sağlayıcı durumu yüklenemedi.</p>
        )}
        {providerMode === "sandbox" ? (
          <p className="saas-admin-billing-muted" role="status">{CARD_PROVIDER_SANDBOX_MESSAGE}</p>
        ) : null}
        {!cardCheckoutAvailable ? (
          <button type="button" className="saas-admin-button" disabled>
            {CARD_PROVIDER_NOT_CONFIGURED_MESSAGE}
          </button>
        ) : (
          <button
            type="button"
            className="saas-admin-button"
            disabled={!canManage || !selectedInvoicePayable || checkoutBusy}
            onClick={() => void handleCardCheckout()}
          >
            {checkoutBusy ? "Yönlendiriliyor..." : "Kartla öde"}
          </button>
        )}
        {checkoutMessage ? (
          <p className="saas-admin-billing-warning" role="alert">{checkoutMessage}</p>
        ) : null}
        {!canManage ? (
          <p className="saas-admin-billing-muted" role="status">Salt okunur görünüm — faturalandırma yönetim yetkisi yok.</p>
        ) : null}
      </section>

      {plans.length > 0 ? (
        <div className="saas-admin-matrix">
          {plans.map((plan) => {
            const latestVersion = plan.versions?.[0];
            return (
              <article key={plan.planCode} className="saas-admin-billing-card">
                <h3>{plan.displayNameTr}</h3>
                <p>{plan.descriptionTr ?? "—"}</p>
                <p>Sürüm: {formatOptionalCount(latestVersion?.versionNumber)}</p>
                <p>Faturalama döngüsü: {latestVersion?.billingCadence ?? "—"}</p>
              </article>
            );
          })}
        </div>
      ) : null}
    </section>
  );
}
