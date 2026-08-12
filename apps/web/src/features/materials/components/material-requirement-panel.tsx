"use client";

import { useEffect, useState, useTransition } from "react";
import { AlertTriangle, ClipboardList, LoaderCircle, Scale } from "lucide-react";

import type {
  MaterialConsumptionObservation,
  MaterialRequirementOverview,
  MaterialRequirementProfileDetail,
  MaterialRequirementProfileSummary,
} from "../domain/materials";
import {
  formatMaterialDateTime,
  formatMaterialGrams,
  formatMaterialPercent,
  formatMaterialQuantity,
  getMaterialAssessmentDriverStateLabel,
  getMaterialAssessmentEvidenceFieldLabel,
  getMaterialConsumptionObservationTypeLabel,
  getMaterialRequirementAssessmentStatusLabel,
  getMaterialRequirementTypeLabel,
  getMaterialSourceLabel,
  MATERIAL_CONSUMPTION_OBSERVATION_FORM_OPTIONS,
  MATERIAL_REQUIREMENT_CONSUMPTION_DISCLAIMER,
  MATERIAL_REQUIREMENT_THEORETICAL_DISCLAIMER,
  MATERIAL_REQUIREMENT_TYPE_FORM_OPTIONS,
  MATERIAL_SOURCE_FORM_OPTIONS,
} from "../presentation/material-labels";
import {
  loadMaterialRequirementOverviewAction,
  loadMaterialRequirementProfileAction,
} from "../server/material-actions";
import {
  createMaterialConsumptionObservationAction,
  createMaterialRequirementProfileAction,
  evaluateMaterialRequirementAssessmentsAction,
  updateMaterialRequirementProfileAction,
} from "../server/material-writer";

type MaterialRequirementPanelProps = Readonly<{
  materialId: string;
  materialCode: string;
  materialName: string;
  unitOfMeasure: string;
  canManage: boolean;
  section?: "consumption" | "regrind-policy";
}>;

type RequirementPanelSection = "consumption" | "regrind-policy";
type RequirementActionPanel = "create-profile" | "update-profile" | "observation" | "evaluate" | null;

const PANEL_SECTIONS: readonly [RequirementPanelSection, string][] = [
  ["consumption", "İhtiyaç ve Tüketim"],
  ["regrind-policy", "Regrind Politikası"],
];

function assessmentStatusBadgeClass(status: string): string {
  switch (status) {
    case "calculated":
    case "within-tolerance":
    case "compliant":
      return "material-registry-badge material-registry-badge--success";
    case "above-plan":
    case "below-plan":
    case "exceeds-limit":
      return "material-registry-badge material-registry-badge--warning";
    case "prohibited":
      return "material-registry-badge material-registry-badge--quarantined";
    case "insufficient-data":
      return "material-registry-badge material-registry-badge--neutral";
    default:
      return "material-registry-badge material-registry-badge--neutral";
  }
}

function driverStateBadgeClass(state: string): string {
  switch (state) {
    case "pass":
      return "material-registry-badge material-registry-badge--success";
    case "warn":
      return "material-registry-badge material-registry-badge--warning";
    case "fail":
      return "material-registry-badge material-registry-badge--quarantined";
    default:
      return "material-registry-badge material-registry-badge--neutral";
  }
}

function profileReferenceText(profile: MaterialRequirementProfileSummary): string {
  const parts = [
    profile.productionOrderReference,
    profile.productReference,
    profile.moldReference,
  ].filter(Boolean);
  return parts.length > 0 ? parts.join(" · ") : "—";
}

function formatKpiGrams(value?: number): string {
  if (value === undefined || value === null) {
    return "—";
  }
  return formatMaterialGrams(value);
}

export function MaterialRequirementPanel({
  materialId,
  materialCode,
  materialName,
  unitOfMeasure,
  canManage,
  section = "consumption",
}: MaterialRequirementPanelProps) {
  const [panelSection, setPanelSection] = useState<RequirementPanelSection>(section);
  const [overview, setOverview] = useState<MaterialRequirementOverview | undefined>();
  const [requirementCanManage, setRequirementCanManage] = useState(canManage);
  const [selectedProfileId, setSelectedProfileId] = useState("");
  const [profileDetail, setProfileDetail] = useState<MaterialRequirementProfileDetail | undefined>();
  const [actionPanel, setActionPanel] = useState<RequirementActionPanel>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loaded, setLoaded] = useState(false);
  const [isPending, startTransition] = useTransition();

  const profiles = overview?.profiles ?? [];
  const observations = overview?.recentObservations ?? [];
  const requirementAssessment = overview?.currentRequirementAssessment;
  const varianceAssessment = overview?.currentVarianceAssessment;
  const regrindAssessment = overview?.currentRegrindAssessment;

  const theoreticalDisclaimer =
    overview?.theoreticalRequirementDisclaimer ?? MATERIAL_REQUIREMENT_THEORETICAL_DISCLAIMER;
  const consumptionDisclaimer =
    overview?.consumptionObservationDisclaimer ?? MATERIAL_REQUIREMENT_CONSUMPTION_DISCLAIMER;

  const hasInsufficientRequirementData = requirementAssessment?.status === "insufficient-data";
  const hasInsufficientVarianceData = varianceAssessment?.status === "insufficient-data";
  const hasInsufficientRegrindData = regrindAssessment?.status === "insufficient-data";

  function refreshOverview(nextProfileId = selectedProfileId) {
    startTransition(async () => {
      setError(null);
      const result = await loadMaterialRequirementOverviewAction(materialId);
      if (result.status === "failure") {
        setError(result.message);
        return;
      }

      setOverview(result.overview);
      setRequirementCanManage(result.canManage);
      setLoaded(true);

      if (nextProfileId) {
        const profileResult = await loadMaterialRequirementProfileAction(nextProfileId);
        if (profileResult.status === "success") {
          setProfileDetail(profileResult.profile);
          setRequirementCanManage(profileResult.canManage);
        }
      }
    });
  }

  useEffect(() => {
    if (!loaded) {
      void refreshOverview();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- initial load only when materialId changes
  }, [loaded, materialId]);

  function selectProfile(profile: MaterialRequirementProfileSummary) {
    setSelectedProfileId(profile.id);
    setProfileDetail(undefined);
    setActionPanel(null);
    setMessage(null);
    setError(null);

    startTransition(async () => {
      const result = await loadMaterialRequirementProfileAction(profile.id);
      if (result.status === "success") {
        setProfileDetail(result.profile);
        setRequirementCanManage(result.canManage);
      } else {
        setError(result.message);
      }
    });
  }

  function runAction(action: () => Promise<{ status: string; message?: string }>) {
    startTransition(async () => {
      setMessage(null);
      setError(null);
      const result = await action();
      if (result.status === "failure") {
        setError(result.message ?? "İşlem başarısız.");
        return;
      }

      setMessage("İhtiyaç kaydı güncellendi.");
      setActionPanel(null);
      refreshOverview(selectedProfileId);
    });
  }

  function renderEvidenceSection() {
    const evidence = requirementAssessment?.evidence ?? [];
    if (evidence.length === 0) {
      return <p className="material-form-hint">İhtiyaç kanıt kaydı bulunmuyor.</p>;
    }

    return (
      <dl className="material-requirement-evidence-grid">
        {evidence.map((item) => (
          <div key={item.field} data-missing={item.isMissing}>
            <dt>{getMaterialAssessmentEvidenceFieldLabel(item.field)}</dt>
            <dd>
              {item.isMissing ? (
                <span className="material-requirement-evidence-missing">Eksik</span>
              ) : (
                (item.value ?? "—")
              )}
              {item.source ? (
                <small>
                  {getMaterialSourceLabel(item.source)} · {formatMaterialDateTime(item.recordedAtUtc)}
                </small>
              ) : null}
            </dd>
          </div>
        ))}
      </dl>
    );
  }

  function renderObservationsList(items: readonly MaterialConsumptionObservation[]) {
    if (items.length === 0) {
      return (
        <div className="material-registry-empty">
          <ClipboardList aria-hidden="true" />
          <p>Kayıtlı tüketim gözlemi bulunamadı.</p>
        </div>
      );
    }

    return (
      <div className="material-registry-table-wrap">
        <table className="material-registry-table">
          <thead>
            <tr>
              <th>Tür</th>
              <th>Miktar</th>
              <th>Virgin/Regrind</th>
              <th>Referans</th>
              <th>Gözlem Zamanı</th>
            </tr>
          </thead>
          <tbody>
            {items.map((observation) => (
              <tr key={observation.id}>
                <td>{getMaterialConsumptionObservationTypeLabel(observation.observationType)}</td>
                <td>{formatMaterialQuantity(observation.quantity, observation.unitOfMeasure)}</td>
                <td>
                  {observation.virginQuantity !== undefined || observation.regrindQuantity !== undefined
                    ? `${formatMaterialQuantity(observation.virginQuantity, observation.unitOfMeasure)} / ${formatMaterialQuantity(observation.regrindQuantity, observation.unitOfMeasure)}`
                    : "—"}
                </td>
                <td>
                  {[observation.productionOrderReference, observation.lotNumber]
                    .filter(Boolean)
                    .join(" · ") || "—"}
                </td>
                <td>{formatMaterialDateTime(observation.observedAtUtc)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    );
  }

  function renderRegrindPolicySection() {
    if (!regrindAssessment) {
      return (
        <div className="material-registry-empty">
          <Scale aria-hidden="true" />
          <p>Regrind politika değerlendirmesi bulunmuyor.</p>
        </div>
      );
    }

    return (
      <>
        <div className="material-requirement-assessment-header">
          <span className={assessmentStatusBadgeClass(regrindAssessment.status)}>
            {getMaterialRequirementAssessmentStatusLabel(regrindAssessment.status)}
          </span>
          <small>{formatMaterialDateTime(regrindAssessment.evaluatedAtUtc)}</small>
        </div>

        {hasInsufficientRegrindData ? (
          <div className="material-requirement-insufficient" role="status">
            <AlertTriangle aria-hidden="true" size={16} />
            Regrind değerlendirmesi için yeterli veri bulunmuyor.
          </div>
        ) : null}

        {regrindAssessment.drivers.length > 0 ? (
          <ul className="material-requirement-driver-list">
            {regrindAssessment.drivers.map((driver) => (
              <li key={driver.code}>
                <strong>{driver.label}</strong>
                <span className={driverStateBadgeClass(driver.state)}>
                  {getMaterialAssessmentDriverStateLabel(driver.state)}
                </span>
                {driver.value ? <em>{driver.value}</em> : null}
              </li>
            ))}
          </ul>
        ) : null}

        {regrindAssessment.evidence.length > 0 ? (
          <dl className="material-requirement-evidence-grid">
            {regrindAssessment.evidence.map((item) => (
              <div key={item.field} data-missing={item.isMissing}>
                <dt>{getMaterialAssessmentEvidenceFieldLabel(item.field)}</dt>
                <dd>
                  {item.isMissing ? (
                    <span className="material-requirement-evidence-missing">Eksik</span>
                  ) : (
                    formatMaterialPercent(item.value ? Number(item.value) : undefined)
                  )}
                </dd>
              </div>
            ))}
          </dl>
        ) : null}
      </>
    );
  }

  return (
    <div className="material-requirement-panel" role="tabpanel">
      {error ? (
        <div className="material-registry-error" role="alert">
          {error}
        </div>
      ) : null}
      {message ? (
        <div className="material-registry-empty" role="status">
          {message}
        </div>
      ) : null}
      {isPending ? (
        <div className="material-registry-status" role="status" aria-live="polite">
          <LoaderCircle aria-hidden="true" size={16} />
          İşleniyor…
        </div>
      ) : null}

      {!loaded && !isPending ? (
        <div className="material-registry-status">
          <LoaderCircle aria-hidden="true" size={16} />
          İhtiyaç verisi yükleniyor…
        </div>
      ) : null}

      {loaded ? (
        <>
          <div className="material-detail-tabs-rail">
            <div className="material-detail-tabs" role="tablist">
              {PANEL_SECTIONS.map(([value, label]) => (
                <button
                  key={value}
                  type="button"
                  role="tab"
                  className="material-detail-tabs__button"
                  aria-selected={panelSection === value}
                  onClick={() => setPanelSection(value)}
                >
                  {label}
                </button>
              ))}
            </div>
          </div>

          <div className="material-requirement-kpis" aria-label="İhtiyaç özet sayıları">
            <article className="material-executive__kpi material-executive__kpi--info">
              <strong>{formatKpiGrams(requirementAssessment?.theoreticalRequirementGrams)}</strong>
              <span>Teorik İhtiyaç</span>
            </article>
            <article className="material-executive__kpi material-executive__kpi--neutral">
              <strong>{formatKpiGrams(varianceAssessment?.observedConsumptionGrams)}</strong>
              <span>Gözlenen Tüketim</span>
            </article>
            <article className="material-executive__kpi material-executive__kpi--warning">
              <strong>
                {varianceAssessment?.varianceGrams !== undefined && varianceAssessment?.varianceGrams !== null
                  ? formatMaterialGrams(varianceAssessment.varianceGrams)
                  : varianceAssessment?.variancePercent !== undefined &&
                      varianceAssessment?.variancePercent !== null
                    ? formatMaterialPercent(varianceAssessment.variancePercent)
                    : "—"}
              </strong>
              <span>Sapma</span>
            </article>
            <article className="material-executive__kpi material-executive__kpi--success">
              <strong>{formatKpiGrams(requirementAssessment?.virginRequirementGrams)}</strong>
              <span>Virgin İhtiyacı</span>
            </article>
            <article className="material-executive__kpi material-executive__kpi--critical">
              <strong>{formatKpiGrams(requirementAssessment?.regrindRequirementGrams)}</strong>
              <span>Regrind İhtiyacı</span>
            </article>
          </div>

          <div className="material-requirement-honesty" aria-label="Veri dürüstlük etiketleri">
            <p className="material-requirement-disclaimer">{theoreticalDisclaimer}</p>
            <p className="material-requirement-disclaimer material-requirement-disclaimer--consumption">
              {consumptionDisclaimer}
            </p>
          </div>

          {requirementCanManage ? (
            <div className="material-requirement-actions">
              <button
                type="button"
                className="material-registry-button compact primary"
                onClick={() => setActionPanel(actionPanel === "create-profile" ? null : "create-profile")}
              >
                İhtiyaç Profili Oluştur
              </button>
              {profileDetail ? (
                <>
                  <button
                    type="button"
                    className="material-registry-button compact"
                    onClick={() => setActionPanel(actionPanel === "update-profile" ? null : "update-profile")}
                  >
                    İhtiyaç Profilini Güncelle
                  </button>
                  <button
                    type="button"
                    className="material-registry-button compact"
                    onClick={() => setActionPanel(actionPanel === "observation" ? null : "observation")}
                  >
                    Tüketim Gözlemi Kaydet
                  </button>
                  <button
                    type="button"
                    className="material-registry-button compact"
                    onClick={() => setActionPanel(actionPanel === "evaluate" ? null : "evaluate")}
                  >
                    Değerlendir
                  </button>
                </>
              ) : null}
            </div>
          ) : (
            <p className="material-readonly-note">
              İhtiyaç yönetim yetkiniz bulunmuyor; kayıtlar salt okunurdur.
            </p>
          )}

          {actionPanel === "create-profile" && requirementCanManage ? (
            <section className="material-detail-action-panel" aria-label="İhtiyaç profili oluştur">
              <h3 className="material-detail-action-panel__title">İhtiyaç profili oluştur</h3>
              <form
                className="material-registry-form-grid"
                action={(formData) => runAction(() => createMaterialRequirementProfileAction(formData))}
              >
                <input type="hidden" name="materialId" value={materialId} />
                <input type="hidden" name="idempotencyKey" value={crypto.randomUUID()} />
                <label className="material-required">
                  İhtiyaç Türü
                  <select name="requirementType" defaultValue="manual-planning">
                    {MATERIAL_REQUIREMENT_TYPE_FORM_OPTIONS.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </select>
                </label>
                <label>
                  Üretim Emri Referansı
                  <input name="productionOrderReference" />
                </label>
                <label>
                  Ürün Referansı
                  <input name="productReference" />
                </label>
                <label>
                  Kalıp Referansı
                  <input name="moldReference" />
                </label>
                <label className="material-required">
                  Hedef Sağlam Parça
                  <input name="goodPartTargetQuantity" type="number" step="1" min="0" required />
                </label>
                <label className="material-required">
                  Parça Ağırlığı (g)
                  <input name="partWeightGrams" type="number" step="0.0001" min="0" required />
                </label>
                <label>
                  Yolluk Ağırlığı/Vuruş (g)
                  <input name="runnerWeightGramsPerShot" type="number" step="0.0001" min="0" />
                </label>
                <label>
                  Etkin Göz Sayısı
                  <input name="effectiveCavityCount" type="number" step="1" min="1" />
                </label>
                <label>
                  Kurulum Payı (g)
                  <input name="setupAllowanceGrams" type="number" step="0.0001" min="0" />
                </label>
                <label>
                  Proses Kaybı (%)
                  <input name="processLossAllowancePercent" type="number" step="0.01" min="0" />
                </label>
                <label>
                  Planlanan Virgin (%)
                  <input name="plannedVirginPercent" type="number" step="0.01" min="0" max="100" />
                </label>
                <label>
                  Planlanan Regrind (%)
                  <input name="plannedRegrindPercent" type="number" step="0.01" min="0" max="100" />
                </label>
                <label>
                  Kaynak
                  <select name="sourceType" defaultValue="manual">
                    {MATERIAL_SOURCE_FORM_OPTIONS.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </select>
                </label>
                <div className="material-registry-actions full-width">
                  <button type="button" className="material-registry-button ghost" onClick={() => setActionPanel(null)}>
                    İptal
                  </button>
                  <button type="submit" className="material-registry-button primary">
                    Kaydet
                  </button>
                </div>
              </form>
            </section>
          ) : null}

          {actionPanel === "update-profile" && profileDetail && requirementCanManage ? (
            <section className="material-detail-action-panel" aria-label="İhtiyaç profili güncelle">
              <h3 className="material-detail-action-panel__title">İhtiyaç profilini güncelle</h3>
              <form
                className="material-registry-form-grid"
                action={(formData) => runAction(() => updateMaterialRequirementProfileAction(formData))}
              >
                <input type="hidden" name="profileId" value={profileDetail.id} />
                <input type="hidden" name="expectedVersion" value={profileDetail.version} />
                <label className="material-required">
                  İhtiyaç Türü
                  <select name="requirementType" defaultValue={profileDetail.requirementType}>
                    {MATERIAL_REQUIREMENT_TYPE_FORM_OPTIONS.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </select>
                </label>
                <label>
                  Üretim Emri Referansı
                  <input name="productionOrderReference" defaultValue={profileDetail.productionOrderReference ?? ""} />
                </label>
                <label>
                  Ürün Referansı
                  <input name="productReference" defaultValue={profileDetail.productReference ?? ""} />
                </label>
                <label>
                  Kalıp Referansı
                  <input name="moldReference" defaultValue={profileDetail.moldReference ?? ""} />
                </label>
                <label>
                  Hedef Sağlam Parça
                  <input
                    name="goodPartTargetQuantity"
                    type="number"
                    step="1"
                    min="0"
                    defaultValue={profileDetail.goodPartTargetQuantity ?? ""}
                  />
                </label>
                <label>
                  Parça Ağırlığı (g)
                  <input
                    name="partWeightGrams"
                    type="number"
                    step="0.0001"
                    min="0"
                    defaultValue={profileDetail.partWeightGrams ?? ""}
                  />
                </label>
                <label>
                  Planlanan Regrind (%)
                  <input
                    name="plannedRegrindPercent"
                    type="number"
                    step="0.01"
                    min="0"
                    max="100"
                    defaultValue={profileDetail.plannedRegrindPercent ?? ""}
                  />
                </label>
                <label>
                  Planlanan Virgin (%)
                  <input
                    name="plannedVirginPercent"
                    type="number"
                    step="0.01"
                    min="0"
                    max="100"
                    defaultValue={profileDetail.plannedVirginPercent ?? ""}
                  />
                </label>
                <div className="material-registry-actions full-width">
                  <button type="button" className="material-registry-button ghost" onClick={() => setActionPanel(null)}>
                    İptal
                  </button>
                  <button type="submit" className="material-registry-button primary">
                    Güncelle
                  </button>
                </div>
              </form>
            </section>
          ) : null}

          {actionPanel === "observation" && profileDetail && requirementCanManage ? (
            <section className="material-detail-action-panel" aria-label="Tüketim gözlemi kaydet">
              <h3 className="material-detail-action-panel__title">Tüketim gözlemi kaydet</h3>
              <form
                className="material-registry-form-grid"
                action={(formData) => runAction(() => createMaterialConsumptionObservationAction(formData))}
              >
                <input type="hidden" name="materialId" value={materialId} />
                <input type="hidden" name="requirementProfileId" value={profileDetail.id} />
                <input type="hidden" name="unitOfMeasure" value={unitOfMeasure} />
                <input type="hidden" name="observedAtUtc" value={new Date().toISOString()} />
                <input type="hidden" name="idempotencyKey" value={crypto.randomUUID()} />
                <label className="material-required">
                  Gözlem Türü
                  <select name="observationType" defaultValue="consumption-observed">
                    {MATERIAL_CONSUMPTION_OBSERVATION_FORM_OPTIONS.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </select>
                </label>
                <label className="material-required">
                  Miktar
                  <input name="quantity" type="number" step="0.001" min="0" required />
                </label>
                <label>
                  Virgin Miktarı
                  <input name="virginQuantity" type="number" step="0.001" min="0" />
                </label>
                <label>
                  Regrind Miktarı
                  <input name="regrindQuantity" type="number" step="0.001" min="0" />
                </label>
                <label className="full-width">
                  Not
                  <textarea name="notes" rows={2} />
                </label>
                <div className="material-registry-actions full-width">
                  <button type="button" className="material-registry-button ghost" onClick={() => setActionPanel(null)}>
                    İptal
                  </button>
                  <button type="submit" className="material-registry-button primary">
                    Kaydet
                  </button>
                </div>
              </form>
            </section>
          ) : null}

          {actionPanel === "evaluate" && profileDetail && requirementCanManage ? (
            <section className="material-detail-action-panel" aria-label="Değerlendir">
              <h3 className="material-detail-action-panel__title">Değerlendir</h3>
              <p className="material-form-hint">
                Seçili profil için ihtiyaç, sapma ve regrind politika değerlendirmeleri çalıştırılır.
              </p>
              <form
                className="material-registry-form-grid"
                action={(formData) => runAction(() => evaluateMaterialRequirementAssessmentsAction(formData))}
              >
                <input type="hidden" name="materialId" value={materialId} />
                <input type="hidden" name="requirementProfileId" value={profileDetail.id} />
                <input type="hidden" name="idempotencyKey" value={crypto.randomUUID()} />
                <div className="material-registry-actions full-width">
                  <button type="button" className="material-registry-button ghost" onClick={() => setActionPanel(null)}>
                    İptal
                  </button>
                  <button type="submit" className="material-registry-button primary">
                    Değerlendirmeyi Başlat
                  </button>
                </div>
              </form>
            </section>
          ) : null}

          {panelSection === "consumption" ? (
            <>
              <section className="material-registry-surface material-requirement-profiles" aria-label="İhtiyaç profilleri">
                <div className="material-registry-surface__header">
                  <h3 className="material-registry-surface__title">İhtiyaç Profilleri</h3>
                  <span className="material-registry-surface__count">{profiles.length} kayıt</span>
                </div>
                {profiles.length === 0 ? (
                  <div className="material-registry-empty">
                    <ClipboardList aria-hidden="true" />
                    <p>Kayıtlı ihtiyaç profili bulunamadı.</p>
                  </div>
                ) : (
                  <div className="material-registry-table-wrap">
                    <table className="material-registry-table">
                      <thead>
                        <tr>
                          <th>Tür</th>
                          <th>Referans</th>
                          <th>Hedef Parça</th>
                          <th>Regrind (%)</th>
                          <th>Güncellik</th>
                          <th>İşlem</th>
                        </tr>
                      </thead>
                      <tbody>
                        {profiles.map((profile) => (
                          <tr
                            key={profile.id}
                            data-profile-id={profile.id}
                            data-selected={profile.id === selectedProfileId}
                            tabIndex={0}
                            onClick={() => selectProfile(profile)}
                            onKeyDown={(event) => {
                              if (event.key === "Enter" || event.key === " ") {
                                event.preventDefault();
                                selectProfile(profile);
                              }
                            }}
                          >
                            <td>{getMaterialRequirementTypeLabel(profile.requirementType)}</td>
                            <td>{profileReferenceText(profile)}</td>
                            <td>{formatMaterialQuantity(profile.goodPartTargetQuantity, "adet")}</td>
                            <td>{formatMaterialPercent(profile.plannedRegrindPercent)}</td>
                            <td>{formatMaterialDateTime(profile.updatedAtUtc)}</td>
                            <td className="material-registry-row-action">
                              <button
                                type="button"
                                className="material-registry-link-action"
                                onClick={(event) => {
                                  event.stopPropagation();
                                  selectProfile(profile);
                                }}
                              >
                                Seç
                              </button>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </section>

              <article className="material-overview-card material-overview-card--wide">
                <h4 className="material-overview-card__title">İhtiyaç Kanıtı</h4>
                {requirementAssessment ? (
                  <div className="material-requirement-assessment-header">
                    <span className={assessmentStatusBadgeClass(requirementAssessment.status)}>
                      {getMaterialRequirementAssessmentStatusLabel(requirementAssessment.status)}
                    </span>
                    <small>{formatMaterialDateTime(requirementAssessment.evaluatedAtUtc)}</small>
                    <small>Kural seti: {requirementAssessment.ruleSetVersion}</small>
                  </div>
                ) : null}
                {hasInsufficientRequirementData ? (
                  <div className="material-requirement-insufficient" role="status">
                    <AlertTriangle aria-hidden="true" size={16} />
                    Teorik ihtiyaç hesabı için yeterli veri bulunmuyor.
                  </div>
                ) : null}
                {renderEvidenceSection()}
              </article>

              <section className="material-registry-surface" aria-label="Tüketim gözlemleri">
                <div className="material-registry-surface__header">
                  <h3 className="material-registry-surface__title">Tüketim Gözlemleri</h3>
                  <span className="material-registry-surface__count">{observations.length} kayıt</span>
                </div>
                {hasInsufficientVarianceData ? (
                  <div className="material-requirement-insufficient" role="status">
                    <AlertTriangle aria-hidden="true" size={16} />
                    Sapma değerlendirmesi için yeterli tüketim verisi bulunmuyor.
                  </div>
                ) : null}
                {renderObservationsList(observations)}
              </section>
            </>
          ) : null}

          {panelSection === "regrind-policy" ? (
            <article className="material-overview-card material-overview-card--wide material-requirement-regrind">
              <h4 className="material-overview-card__title">Regrind Politikası</h4>
              <p className="material-form-hint">
                {materialCode} · {materialName}
              </p>
              <p className="material-form-hint">
                Regrind uygunluk değerlendirmesi otomatik onay sağlamaz; operasyonel karar insan onayı gerektirir.
              </p>
              {renderRegrindPolicySection()}
            </article>
          ) : null}
        </>
      ) : null}
    </div>
  );
}
