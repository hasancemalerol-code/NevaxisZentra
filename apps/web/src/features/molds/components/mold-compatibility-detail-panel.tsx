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

type MoldCompatibilityDetailPanelProps = Readonly<{
  moldId: string;
  overview?: MoldCompatibilityOverview;
  canManage: boolean;
  isPending: boolean;
  loadError?: string | null;
  onAction: (action: () => Promise<{ status: string; message?: string; compatibilityOverview?: MoldCompatibilityOverview }>) => void;
}>;

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
    return <div className="mold-registry-error" role="alert">{loadError}</div>;
  }

  if (!overview && isPending) {
    return <div className="mold-registry-empty" role="status">Makine uyumluluğu yükleniyor…</div>;
  }

  if (!overview && !isPending) {
    return <div className="mold-registry-empty">Makine uyumluluğu verisi yüklenemedi.</div>;
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
      <section className="mold-registry-kpis" aria-label="Makine uyumluluğu özeti">
        <article className="mold-registry-kpi">
          <strong>{overview.compatibleCount}</strong>
          <span>Uyumlu</span>
        </article>
        <article className="mold-registry-kpi">
          <strong>{overview.conditionalCount}</strong>
          <span>Koşullu</span>
        </article>
        <article className="mold-registry-kpi">
          <strong>{overview.incompatibleCount}</strong>
          <span>Uyumsuz</span>
        </article>
        <article className="mold-registry-kpi">
          <strong>{overview.insufficientDataCount}</strong>
          <span>Yetersiz Veri</span>
        </article>
      </section>

      {!overview.requirement ? (
        <div className="mold-registry-empty">
          Bu kalıp için henüz uyumluluk gereksinim profili tanımlanmamış.
        </div>
      ) : null}

      {canManage ? (
        <div className="mold-registry-actions">
          <button
            type="button"
            className="mold-registry-button primary"
            disabled={isPending}
            onClick={handleEvaluateAll}
          >
            Tüm Makineleri Değerlendir
          </button>
        </div>
      ) : (
        <div className="mold-registry-empty">Makine uyumluluğu için yalnızca görüntüleme yetkiniz var.</div>
      )}

      <table className="mold-registry-table">
        <thead>
          <tr>
            <th>Makine</th>
            <th>Teknik Durum</th>
            <th>Kritik Kısıt</th>
            <th>Veri Güncelliği</th>
            <th>Teorik Kapasite</th>
            <th>İncele</th>
          </tr>
        </thead>
        <tbody>
          {overview.machines.length === 0 ? (
            <tr><td colSpan={6}>Henüz makine uyumluluk değerlendirmesi yok.</td></tr>
          ) : overview.machines.map((machine) => (
            <tr
              key={machine.assessmentId}
              data-selected={machine.machineAssetId === selectedMachineAssetId}
              onClick={() => handleSelectMachine(machine)}
            >
              <td>
                <strong>{machine.machineCode}</strong>
                <div>{machine.machineDisplayName}</div>
              </td>
              <td>
                <span className="mold-registry-badge">
                  {getMoldCompatibilityStatusLabel(machine.status)}
                </span>
              </td>
              <td>{machine.criticalConstraintLabel ?? "—"}</td>
              <td>
                {formatCompatibilityTimestamp(machine.evaluatedAtUtc)}
                {machine.requirementStale || machine.machineProfileStale ? (
                  <div>
                    {machine.requirementStale ? <span className="mold-registry-badge">Gereksinim eski</span> : null}
                    {machine.machineProfileStale ? <span className="mold-registry-badge">Makine profili eski</span> : null}
                  </div>
                ) : null}
              </td>
              <td>
                {formatTheoreticalPartsPerHour(machine.theoreticalPartsPerHour)}
                {machine.theoreticalPartsPerHour !== undefined ? (
                  <small>{MOLD_COMPATIBILITY_THEORETICAL_CAPACITY_DISCLAIMER}</small>
                ) : null}
              </td>
              <td>
                <button
                  type="button"
                  className="mold-registry-button"
                  onClick={(event) => {
                    event.stopPropagation();
                    handleSelectMachine(machine);
                  }}
                >
                  İncele
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      <section aria-label="Kanıt paneli">
        <h3>Kanıt ve Kural Sonuçları</h3>
        {!selectedSummary ? (
          <div className="mold-registry-empty">Kural sonuçlarını görmek için tablodan bir makine seçin.</div>
        ) : (
          <>
            <dl className="mold-registry-meta-grid">
              <div>
                <dt>Makine</dt>
                <dd>{selectedSummary.machineCode} · {selectedSummary.machineDisplayName}</dd>
              </div>
              <div>
                <dt>Uyumluluk Durumu</dt>
                <dd>{getMoldCompatibilityStatusLabel(selectedSummary.status)}</dd>
              </div>
              <div>
                <dt>Teorik Kapasite</dt>
                <dd>
                  {formatTheoreticalPartsPerHour(selectedSummary.theoreticalPartsPerHour)}
                  {selectedSummary.theoreticalPartsPerHour !== undefined ? (
                    <small>{MOLD_COMPATIBILITY_THEORETICAL_CAPACITY_DISCLAIMER}</small>
                  ) : null}
                </dd>
              </div>
              <div>
                <dt>Son Değerlendirme</dt>
                <dd>{formatCompatibilityTimestamp(selectedSummary.evaluatedAtUtc)}</dd>
              </div>
            </dl>

            {selectedAssessment && selectedAssessment.reasons.length > 0 ? (
              <table className="mold-registry-table">
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
                        <span className="mold-registry-badge">
                          {getMoldCompatibilityRuleStatusLabel(reason.status)}
                        </span>
                      </td>
                      <td>{reason.requiredValue ?? "—"}</td>
                      <td>{reason.availableValue ?? "—"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
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
                    <dt>Gözenek Sayısı</dt>
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
      </section>
    </>
  );
}
