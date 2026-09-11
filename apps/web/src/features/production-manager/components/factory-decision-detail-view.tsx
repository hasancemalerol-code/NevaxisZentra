"use client";

import Link from "next/link";
import { useState } from "react";

import {
  getProductionManagerAutonomyLabel,
  getProductionManagerConfidenceLabel,
  getProductionManagerDecisionTypeLabel,
  getProductionManagerStatusLabel,
} from "../domain/production-manager-labels";
import type { ProductionManagerDecisionApi } from "../server/production-manager-api-client";

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

export function FactoryDecisionDetailView({ decision }: FactoryDecisionDetailViewProps) {
  const [activeTab, setActiveTab] = useState<typeof TABS[number]>("Problem");
  const selectedOption = decision.options.find((option) => option.id === decision.selectedOptionId);

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
        </div>
      </header>

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
          <div>
            <h2>Sonuç</h2>
            <p>{decision.verifiedAtUtc ? `Doğrulandı: ${decision.verifiedAtUtc}` : "Doğrulama bekleniyor"}</p>
            {decision.effectivenessScore != null ? (
              <p>Etkinlik skoru: {decision.effectivenessScore}</p>
            ) : null}
          </div>
        ) : null}

        {activeTab === "Simülasyon" ? (
          <div data-testid="factory-decision-simulator-note">
            <h2>Senaryo Simülasyonu</h2>
            <p>Simülasyon sonuçları karar oluşturma sırasında değerlendirildi. Ayrıntılı senaryo karşılaştırması Operasyon Merkezi simülasyon akışından çalıştırılabilir.</p>
          </div>
        ) : null}

        {activeTab === "Mevcut Durum" ? (
          <div>
            <h2>Mevcut Durum</h2>
            <p>Karar anındaki fabrika durumu snapshot ile bağlıdır.</p>
          </div>
        ) : null}
      </section>
    </main>
  );
}
