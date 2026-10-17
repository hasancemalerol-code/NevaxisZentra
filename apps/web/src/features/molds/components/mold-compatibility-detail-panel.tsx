"use client";

import { useMemo, useState } from "react";

import type {
  MoldCompatibilityAssessment,
  MoldCompatibilityAssessmentSummary,
  MoldCompatibilityOverview,
} from "../domain/molds";
import { formatMoldNumber } from "../presentation/mold-labels";
import {
  formatCompatibilityTimestamp,
  formatTheoreticalPartsPerHour,
  getMoldCompatibilityRuleStatusLabel,
  getMoldCompatibilityStatusLabel,
  MOLD_COMPATIBILITY_THEORETICAL_CAPACITY_DISCLAIMER,
} from "../presentation/mold-compatibility-labels";
import { evaluateMoldCompatibilityAllAction } from "../server/mold-actions";
import { customerMessage } from "@/lib/presentation/customer-message";
import { InlineUnavailable } from "@/components/ui/state-page";

type MoldCompatibilityDetailPanelProps = Readonly<{
  moldId: string;
  overview?: MoldCompatibilityOverview;
  canManage: boolean;
  isPending: boolean;
  loadError?: string | null;
  onAction: (action: () => Promise<{ status: string; message?: string; compatibilityOverview?: MoldCompatibilityOverview }>) => void;
}>;

function compatibilityBadgeClass(status: string): string {
  switch (status) {
    case "compatible":
    case "pass":
      return "mold-registry-badge mold-registry-badge--compatible";
    case "conditional":
    case "warn":
      return "mold-registry-badge mold-registry-badge--conditional";
    case "incompatible":
    case "fail":
      return "mold-registry-badge mold-registry-badge--incompatible";
    case "insufficient-data":
    case "unknown":
      return "mold-registry-badge mold-registry-badge--insufficient-data";
    default:
      return "mold-registry-badge mold-registry-badge--neutral";
  }
}

export function MoldCompatibilityDetailPanel({
  moldId,
  overview,
  canManage,
  isPending,
  loadError,
  onAction,
}: MoldCompatibilityDetailPanelProps) {
  const [selectedMachineAssetId, setSelectedMachineAssetId] = useState<string | null>(null);
  const [evaluationDetails, setEvaluationDetails] = useState<readonly MoldCompatibilityAssessment[]>([]);

  const selectedSummary = useMemo(
    () => overview?.machines.find((machine) => machine.machineAssetId === selectedMachineAssetId),
    [overview?.machines, selectedMachineAssetId],
  );

  const selectedAssessment = useMemo(
    () => evaluationDetails.find((assessment) => assessment.machineAssetId === selectedMachineAssetId),
    [evaluationDetails, selectedMachineAssetId],
  );

  if (loadError) {
    return (
      <div className="mold-registry-error" role="alert">
        {customerMessage(loadError)}
      </div>
    );
  }

  if (!overview && isPending) {
    return (
      <div className="mold-registry-empty" role="status" aria-live="polite">
        Makine uyumluluğu yükleniyor…
      </div>
    );
  }

  if (!overview && !isPending) {
    return <InlineUnavailable subject="Makine uyumluluğu verisi" />;
  }

  if (!overview) {
    return null;
  }

  function handleSelectMachine(machine: MoldCompatibilityAssessmentSummary) {
    setSelectedMachineAssetId(machine.machineAssetId);
  }

  function handleEvaluateAll() {
    onAction(async () => {
      const result = await evaluateMoldCompatibilityAllAction(moldId);
      if (result.status === "success") {
        setEvaluationDetails(result.assessments);
        if (result.compatibilityOverview) {
          return {
            status: "success",
            compatibilityOverview: result.compatibilityOverview,
          };
        }
      }

      return result;
    });
  }

  return (
    <>
      <section className="mold-registry-kpis mold-registry-kpis--compact" aria-label="Makine uyumluluğu özeti">
        <article className="mold-registry-kpi mold-registry-kpi--success">
          <strong>{overview.compatibleCount}</strong>
          <span>Uyumlu</span>
        </article>
        <article className="mold-registry-kpi mold-registry-kpi--warning">
          <strong>{overview.conditionalCount}</strong>
          <span>Koşullu</span>
        </article>
        <article className="mold-registry-kpi mold-registry-kpi--critical">
          <strong>{overview.incompatibleCount}</strong>
          <span>Uyumsuz</span>
        </article>
        <article className="mold-registry-kpi mold-registry-kpi--neutral">
          <strong>{overview.insufficientDataCount}</strong>
          <span>Yetersiz Veri</span>
        </article>
      </section>

      {!overview.requirement ? (
        <div className="mold-registry-empty">
          Bu kalıp için henüz uyumluluk gereksinim profili tanımlanmamış.
        </div>
      ) : null}

      <div className="mold-registry-actions mold-registry-actions--start">
        {canManage ? (
          <button
            type="button"
            className="mold-registry-button primary"
            disabled={isPending}
            onClick={handleEvaluateAll}
          >
            Tüm Makineleri Değerlendir
          </button>
        ) : (
          <div className="mold-readonly-note">Makine uyumluluğu için yalnızca görüntüleme yetkiniz var.</div>
        )}
      </div>

      <div className="mold-compat-layout">
        <div className="mold-compat-table-wrap" aria-label="Makine karşılaştırma tablosu" tabIndex={0} role="region">
          <table aria-label="Makine karşılaştırma tablosu" className="mold-registry-table">
            <thead>
              <tr>
                <th>Makine</th>
                <th>Teknik Durum</th>
                <th>Kritik Kısıt</th>
                <th>Veri Güncelliği</th>
                <th>Teorik Kapasite</th>
                <th>İşlem</th>
              </tr>
            </thead>
            <tbody>
              {overview.machines.length === 0 ? (
                <tr>
                  <td colSpan={6}>Henüz makine uyumluluk değerlendirmesi yok.</td>
                </tr>
              ) : (
                overview.machines.map((machine) => (
                  <tr
                    key={machine.assessmentId}
                    data-selected={machine.machineAssetId === selectedMachineAssetId}
                    tabIndex={0}
                    onClick={() => handleSelectMachine(machine)}
                    onKeyDown={(event) => {
                      if (event.key === "Enter" || event.key === " ") {
                        event.preventDefault();
                        handleSelectMachine(machine);
                      }
                    }}
                  >
                    <td>
                      <div className="mold-identity">
                        <span className="mold-identity__name">{machine.machineDisplayName}</span>
                        <span className="mold-identity__code">{machine.machineCode}</span>
                      </div>
                    </td>
                    <td>
                      <span className={compatibilityBadgeClass(machine.status)}>
                        {getMoldCompatibilityStatusLabel(machine.status)}
                      </span>
                    </td>
                    <td>
                      <span className="mold-cell-truncate" title={machine.criticalConstraintLabel ?? undefined}>
                        {machine.criticalConstraintLabel ?? "—"}
                      </span>
                    </td>
                    <td>
                      {formatCompatibilityTimestamp(machine.evaluatedAtUtc)}
                      {machine.requirementStale || machine.machineProfileStale ? (
                        <div>
                          {machine.requirementStale ? (
                            <span className="mold-registry-badge mold-registry-badge--warning">Gereksinim eski</span>
                          ) : null}{" "}
                          {machine.machineProfileStale ? (
                            <span className="mold-registry-badge mold-registry-badge--warning">Makine profili eski</span>
                          ) : null}
                        </div>
                      ) : null}
                    </td>
                    <td>
                      {formatTheoreticalPartsPerHour(machine.theoreticalPartsPerHour)}
                      {machine.theoreticalPartsPerHour !== undefined ? (
                        <small className="mold-compat-disclaimer">
                          {MOLD_COMPATIBILITY_THEORETICAL_CAPACITY_DISCLAIMER}
                        </small>
                      ) : null}
                    </td>
                    <td>
                      <button
                        type="button"
                        className="mold-registry-button compact"
                        onClick={(event) => {
                          event.stopPropagation();
                          handleSelectMachine(machine);
                        }}
                      >
                        İncele
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        <aside className="mold-compat-evidence" aria-label="Kanıt paneli">
          <h3>Kanıt ve Kural Sonuçları</h3>
          {!selectedSummary ? (
            <div className="mold-registry-empty">Kural sonuçlarını görmek için tablodan bir makine seçin.</div>
          ) : (
            <>
              <dl className="mold-registry-meta-grid">
                <div>
                  <dt>Makine</dt>
                  <dd>
                    {selectedSummary.machineCode} · {selectedSummary.machineDisplayName}
                  </dd>
                </div>
                <div>
                  <dt>Uyumluluk Durumu</dt>
                  <dd>
                    <span className={compatibilityBadgeClass(selectedSummary.status)}>
                      {getMoldCompatibilityStatusLabel(selectedSummary.status)}
                    </span>
                  </dd>
                </div>
                <div>
                  <dt>Teorik Kapasite</dt>
                  <dd>
                    {formatTheoreticalPartsPerHour(selectedSummary.theoreticalPartsPerHour)}
                    {selectedSummary.theoreticalPartsPerHour !== undefined ? (
                      <small className="mold-compat-disclaimer">
                        {MOLD_COMPATIBILITY_THEORETICAL_CAPACITY_DISCLAIMER}
                      </small>
                    ) : null}
                  </dd>
                </div>
                <div>
                  <dt>Son Değerlendirme</dt>
                  <dd>{formatCompatibilityTimestamp(selectedSummary.evaluatedAtUtc)}</dd>
                </div>
              </dl>

              {selectedAssessment && selectedAssessment.reasons.length > 0 ? (
                <div className="mold-compat-table-wrap" tabIndex={0} role="region" aria-label="Kanıt ve Kural Sonuçları (kaydırılabilir)">
                  <table aria-label="Kanıt ve Kural Sonuçları" className="mold-registry-table">
                    <thead>
                      <tr>
                        <th>Kural</th>
                        <th>Durum</th>
                        <th>Gerekli</th>
                        <th>Mevcut</th>
                      </tr>
                    </thead>
                    <tbody>
                      {selectedAssessment.reasons.map((reason) => (
                        <tr key={reason.ruleCode}>
                          <td>{reason.labelTr}</td>
                          <td>
                            <span className={compatibilityBadgeClass(reason.status)}>
                              {getMoldCompatibilityRuleStatusLabel(reason.status)}
                            </span>
                          </td>
                          <td>{reason.requiredValue ?? "—"}</td>
                          <td>{reason.availableValue ?? "—"}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : (
                <div className="mold-registry-empty">
                  {selectedSummary.criticalConstraintLabel
                    ? `Özet kısıt: ${selectedSummary.criticalConstraintLabel}`
                    : "Detaylı kural sonuçları için değerlendirme çalıştırın."}
                </div>
              )}

              {overview.requirement ? (
                <section aria-label="Kalıp gereksinim kanıtı">
                  <h4>Kalıp Gereksinim Profili</h4>
                  <dl className="mold-registry-meta-grid">
                    <div>
                      <dt>Kıskaç Kuvveti (ton)</dt>
                      <dd>{formatMoldNumber(overview.requirement.requiredClampForceTon)}</dd>
                    </div>
                    <div>
                      <dt>Göz Sayısı</dt>
                      <dd>{formatMoldNumber(overview.requirement.effectiveCavityCount)}</dd>
                    </div>
                    <div>
                      <dt>Kalıp Boyutları (mm)</dt>
                      <dd>
                        {formatMoldNumber(overview.requirement.moldWidthMm)} ×{" "}
                        {formatMoldNumber(overview.requirement.moldHeightMm)} ×{" "}
                        {formatMoldNumber(overview.requirement.moldDepthMm)}
                      </dd>
                    </div>
                    <div>
                      <dt>Malzeme Grubu</dt>
                      <dd>{overview.requirement.supportedMaterialGroup ?? "—"}</dd>
                    </div>
                  </dl>
                </section>
              ) : null}
            </>
          )}
        </aside>
      </div>
    </>
  );
}
