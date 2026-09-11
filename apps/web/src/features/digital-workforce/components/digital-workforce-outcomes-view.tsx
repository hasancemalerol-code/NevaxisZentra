"use client";

import { DIGITAL_COST_CONTROL, DIGITAL_OUTCOMES, DIGITAL_WORK_CASES } from "../data/digital-workforce-demo-catalog";
import { DIGITAL_WORKFORCE_COST_NOTE, DIGITAL_WORKFORCE_VERIFIED_CLOSED } from "../domain/digital-workforce-labels";
import { DemoBadge, VerifiedBadge } from "./digital-workforce-primitives";

export function DigitalWorkforceOutcomesView() {
  const verified = DIGITAL_WORK_CASES.filter((item) => item.verified);

  return (
    <>
      <header className="dwf-hero">
        <div>
          <p className="dwf-hero__eyebrow">DİJİTAL İŞ GÜCÜ</p>
          <h1>Sonuçlar</h1>
          <p>Birincil KPI doğrulanmış iş kapanışıdır. İşe başlamak değil, sonucu doğrulamak.</p>
        </div>
      </header>

      <section className="dwf-split">
        <article className="dwf-rate">
          <span>Doğrulanmış Kapanış Oranı</span>
          <strong>%92</strong>
          <p>24 tamamlanan iş · 22 doğrulandı · 2 takipte</p>
          <p className="dwf-meta">Demo / tahmini</p>
        </article>
        <section className="dwf-panel">
          <h2>Değer / maliyet</h2>
          <div className="dwf-kpi-grid">
            <article className="dwf-kpi"><span>AI maliyeti</span><strong>$18,40</strong><span>{DIGITAL_WORKFORCE_COST_NOTE}</span></article>
            <article className="dwf-kpi"><span>Tahmini zaman kazancı</span><strong>31,5 sa</strong><span>Tahmini / demo</span></article>
            <article className="dwf-kpi"><span>Doğrulanmış kapanış</span><strong>22</strong></article>
          </div>
        </section>
      </section>

      <section className="dwf-panel">
        <h2>İş Sonuçları</h2>
        <div className="dwf-outcome-grid" style={{ marginTop: "0.65rem" }}>
          {DIGITAL_OUTCOMES.map((item) => (
            <article className="dwf-kpi" key={item.id}>
              <span>{item.label}</span>
              <strong>{item.value}</strong>
              <span>{item.detail}</span>
              {item.verified ? <VerifiedBadge /> : <DemoBadge label="Devam ediyor" />}
            </article>
          ))}
        </div>
      </section>

      <section className="dwf-panel">
        <h2>{DIGITAL_WORKFORCE_VERIFIED_CLOSED}</h2>
        {verified.map((item) => (
          <article key={item.id} style={{ marginTop: "0.8rem" }}>
            <VerifiedBadge />
            <p><strong>{item.title}</strong></p>
            <p>Problem: {item.problem}</p>
            <p>Sonuç: {item.result ?? item.outcome}</p>
            <p className="dwf-meta">{item.businessImpact} · {item.costLabel}</p>
          </article>
        ))}
      </section>

      <section className="dwf-panel">
        <h2>AI kullanım</h2>
        <p>{DIGITAL_WORKFORCE_COST_NOTE}</p>
        <div className="dwf-kpi-grid" style={{ marginTop: "0.65rem" }}>
          <article className="dwf-kpi"><span>Bugün</span><strong>{DIGITAL_COST_CONTROL.todayLabel}</strong></article>
          <article className="dwf-kpi"><span>Aylık tahmin</span><strong>{DIGITAL_COST_CONTROL.monthEstimateLabel}</strong></article>
          <article className="dwf-kpi"><span>Aylık bütçe</span><strong>{DIGITAL_COST_CONTROL.monthlyBudgetLabel}</strong></article>
        </div>
      </section>
    </>
  );
}
