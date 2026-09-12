"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState, useTransition } from "react";

import {
  getProductionManagerAutonomyLabel,
  getProductionManagerConfidenceLabel,
  getProductionManagerDecisionTypeLabel,
  getProductionManagerStatusLabel,
} from "../domain/production-manager-labels";
import type {
  ProductionManagerDecisionApi,
  ProductionManagerSimulationRowApi,
} from "../server/production-manager-api-client";
import {
  productionManagerApproveAction,
  productionManagerExecuteAction,
  productionManagerSimulateAction,
  productionManagerVerifyAction,
} from "../server/production-manager-actions";

const TABS = [
  "Problem",
  "Mevcut Durum",
  "Tahmin",
  "Alternatifler",
  "Simülasyon",
  "Karar",
  "Uygulama",
  "Doğrulama",
  "Sonuç",
] as const;

type FactoryDecisionDetailViewProps = Readonly<{
  decision: ProductionManagerDecisionApi;
}>;

export function FactoryDecisionDetailView({ decision: initialDecision }: FactoryDecisionDetailViewProps) {
  const router = useRouter();
  const [decision, setDecision] = useState(initialDecision);
  const [activeTab, setActiveTab] = useState<typeof TABS[number]>("Problem");
  const [message, setMessage] = useState<string | null>(null);
  const [simulationRows, setSimulationRows] = useState<readonly ProductionManagerSimulationRowApi[]>([]);
  const [isPending, startTransition] = useTransition();
  const selectedOption = decision.options.find((option) => option.id === decision.selectedOptionId);
  const topOption = decision.options.find((option) => !option.isRejected && option.rank === 1)
    ?? decision.options.find((option) => !option.isRejected);

  function runAction(action: () => Promise<{ status: "success" | "failure"; message?: string; value?: ProductionManagerDecisionApi }>) {
    setMessage(null);
    startTransition(async () => {
      const result = await action();
      if (result.status === "failure") {
        setMessage(result.message ?? "İşlem başarısız.");
        return;
      }
      if (result.value) {
        setDecision(result.value);
      }
      router.refresh();
    });
  }

  function runSimulate() {
    setMessage(null);
    startTransition(async () => {
      const scenarioType =
        decision.decisionType === "machine-risk"
          ? "machine-stop"
          : decision.decisionType === "capacity-overload"
            ? "capacity-shift"
            : "order-delay";
      const result = await productionManagerSimulateAction({
        scenarioType,
        machineId: decision.decisionType === "machine-risk" ? "IM-07" : undefined,
        delayHours: 3,
      });
      if (result.status === "failure") {
        setMessage(result.message ?? "Simülasyon başarısız.");
        return;
      }
      setSimulationRows(result.value.rows);
    });
  }

  return (
    <main className="commint-page" data-testid="factory-decision-detail-view">
      <header className="commint-executive">
        <div className="commint-executive__intro">
          <p className="commint-executive__eyebrow">KARAR DETAYI</p>
          <h1>{decision.title}</h1>
          <p className="commint-executive__subtitle">
            {getProductionManagerDecisionTypeLabel(decision.decisionType)} ·{" "}
            {getProductionManagerStatusLabel(decision.status)}
          </p>
        </div>
        <div className="commint-executive__actions">
          {decision.status === "pending-approval" ? (
            <button
              type="button"
              className="commint-button"
              disabled={isPending}
              onClick={() =>
                runAction(() =>
                  productionManagerApproveAction(decision.id, true, decision.concurrencyToken),
                )
              }
              data-testid="factory-decision-approve"
            >
              Onayla
            </button>
          ) : null}
          {decision.status === "pending-approval" || decision.status === "approved" || decision.status === "proposed" ? (
            <button
              type="button"
              className="commint-button"
              disabled={isPending}
              onClick={() =>
                runAction(() =>
                  productionManagerExecuteAction(
                    decision.id,
                    decision.concurrencyToken,
                    decision.selectedOptionId ?? topOption?.id,
                  ),
                )
              }
              data-testid="factory-decision-execute"
            >
              Uygula
            </button>
          ) : null}
          {decision.executedAtUtc && decision.status !== "verified" ? (
            <button
              type="button"
              className="commint-button commint-button--ghost"
              disabled={isPending}
              onClick={() =>
                runAction(() =>
                  productionManagerVerifyAction(
                    decision.id,
                    decision.concurrencyToken,
                    JSON.stringify({ onTimeProbability: 0.82 }),
                  ),
                )
              }
              data-testid="factory-decision-verify"
            >
              Doğrula
            </button>
          ) : null}
          {decision.factoryCaseId ? (
            <Link
              className="commint-button commint-button--ghost"
              href={`/operasyon/yonetim-merkezi/vakalar/${decision.factoryCaseId}`}
            >
              Bağlı vaka
            </Link>
          ) : null}
          <Link className="commint-button commint-button--ghost" href="/operasyon/yonetim-merkezi">
            Operasyon Merkezi
          </Link>
          <Link className="commint-button commint-button--ghost" href="/dijital-is-gucu">
            Dijital İş Gücü
          </Link>
        </div>
      </header>

      {message ? (
        <p role="alert" className="commint-error-banner" data-testid="factory-decision-action-message">
          {message}
        </p>
      ) : null}

      <nav className="commint-filter-rail" aria-label="Karar sekmeleri">
        {TABS.map((tab) => (
          <button
            key={tab}
            type="button"
            className={`commint-button ${activeTab === tab ? "" : "commint-button--ghost"}`}
            onClick={() => setActiveTab(tab)}
            data-testid={`factory-decision-tab-${tab}`}
          >
            {tab}
          </button>
        ))}
      </nav>

      <section className="commint-panel">
        {activeTab === "Problem" ? (
          <div>
            <h2>Problem</h2>
            <p>{decision.problemSummary}</p>
          </div>
        ) : null}

        {activeTab === "Tahmin" ? (
          <div>
            <h2>Tahmin</h2>
            <p>{getProductionManagerConfidenceLabel(decision.confidenceBand)}</p>
            <p>Güven skoru: {decision.confidenceScore}</p>
          </div>
        ) : null}

        {activeTab === "Alternatifler" || activeTab === "Karar" ? (
          <div data-testid="factory-decision-option-comparison">
            <h2>Alternatifler</h2>
            <table className="commint-table">
              <thead>
                <tr>
                  <th>Seçenek</th>
                  <th>Beklenen sonuç</th>
                  <th>Skor</th>
                  <th>Onay</th>
                  <th>Durum</th>
                </tr>
              </thead>
              <tbody>
                {decision.options.map((option) => (
                  <tr key={option.id}>
                    <td>{option.optionCode}: {option.title}</td>
                    <td>{option.description}</td>
                    <td>{option.score}</td>
                    <td>{option.requiresApproval ? "Gerekli" : "Gerekmez"}</td>
                    <td>{option.isRejected ? option.rejectionReason ?? "Reddedildi" : "Geçerli"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : null}

        {activeTab === "Karar" && selectedOption ? (
          <div>
            <h3>Önerilen aksiyon</h3>
            <p>{selectedOption.title}</p>
            <p>{getProductionManagerAutonomyLabel(decision.autonomyMode)}</p>
            <p>Onay durumu: {decision.approvalState}</p>
          </div>
        ) : null}

        {activeTab === "Uygulama" ? (
          <div>
            <h2>Uygulama</h2>
            <p>{decision.executedAtUtc ? `Uygulandı: ${decision.executedAtUtc}` : "Henüz uygulanmadı"}</p>
          </div>
        ) : null}

        {activeTab === "Doğrulama" || activeTab === "Sonuç" ? (
          <div data-testid="factory-decision-effectiveness">
            <h2>{activeTab === "Doğrulama" ? "Doğrulama" : "Sonuç"}</h2>
            <p>{decision.verifiedAtUtc ? `Doğrulandı: ${decision.verifiedAtUtc}` : "Doğrulama bekleniyor"}</p>
            {decision.effectivenessScore != null ? (
              <p>Decision Effectiveness: {decision.effectivenessScore}</p>
            ) : null}
            <p>Expected vs Actual karşılaştırması doğrulama adımında kaydedilir.</p>
          </div>
        ) : null}

        {activeTab === "Simülasyon" ? (
          <div data-testid="factory-decision-simulator">
            <h2>Senaryo Simülasyonu</h2>
            <p>Karar tipine göre what-if senaryosu çalıştırın. Simülasyon iş verisini değiştirmez.</p>
            <button
              type="button"
              className="commint-button"
              disabled={isPending}
              onClick={runSimulate}
            >
              Simülasyonu çalıştır
            </button>
            {simulationRows.length > 0 ? (
              <table className="commint-table">
                <thead>
                  <tr>
                    <th>Seçenek</th>
                    <th>Zamanında tamamlama</th>
                    <th>Kapasite</th>
                    <th>Onay</th>
                  </tr>
                </thead>
                <tbody>
                  {simulationRows.map((row) => (
                    <tr key={row.optionCode}>
                      <td>{row.label}</td>
                      <td>{Math.round(row.onTimeProbability * 100)}%</td>
                      <td>{Math.round(row.capacityUtilizationPercent)}%</td>
                      <td>{row.requiresApproval ? "Gerekli" : "Hayır"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            ) : null}
          </div>
        ) : null}

        {activeTab === "Mevcut Durum" ? (
          <div>
            <h2>Mevcut Durum</h2>
            <p>Karar anındaki fabrika durumu snapshot ile bağlıdır.</p>
            <p>Otonomi modu: {getProductionManagerAutonomyLabel(decision.autonomyMode)}</p>
          </div>
        ) : null}
      </section>
    </main>
  );
}
