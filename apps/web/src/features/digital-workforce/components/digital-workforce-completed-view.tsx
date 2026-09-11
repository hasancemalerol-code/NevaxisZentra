"use client";

import Link from "next/link";
import { useMemo, useState } from "react";

import { DIGITAL_WORK_CASES, DIGITAL_WORKERS, getDigitalWorker } from "../data/digital-workforce-demo-catalog";
import { DIGITAL_WORKFORCE_COST_NOTE } from "../domain/digital-workforce-labels";
import { DemoBadge, VerifiedBadge } from "./digital-workforce-primitives";

const RISK_LABELS = {
  critical: "Kritik",
  high: "Yüksek",
  medium: "Orta",
} as const;

export function DigitalWorkforceCompletedView() {
  const [workerId, setWorkerId] = useState("all");
  const [date, setDate] = useState("all");
  const [facility, setFacility] = useState("all");
  const [moduleName, setModuleName] = useState("all");
  const [verified, setVerified] = useState("all");
  const [risk, setRisk] = useState("all");

  const rows = useMemo(
    () =>
      DIGITAL_WORK_CASES.filter((item) => {
        if (item.status !== "closed" && verified === "verified") return false;
        if (workerId !== "all" && item.primaryWorkerId !== workerId && !item.participatingWorkerIds.includes(workerId as typeof item.primaryWorkerId)) {
          return false;
        }
        if (date === "today" && item.closedAtLabel?.startsWith("Dün")) return false;
        if (date === "yesterday" && !item.closedAtLabel?.startsWith("Dün")) return false;
        if (facility !== "all" && facility !== "Pilot Enjeksiyon Tesisi") return false;
        if (moduleName !== "all" && item.source !== moduleName) return false;
        if (verified === "verified" && !item.verified) return false;
        if (verified === "unverified" && item.verified) return false;
        if (risk !== "all" && item.severity !== risk) return false;
        return true;
      }),
    [date, facility, moduleName, risk, verified, workerId],
  );

  const modules = [...new Set(DIGITAL_WORK_CASES.map((item) => item.source))];

  return (
    <>
      <header className="dwf-hero">
        <div>
          <p className="dwf-hero__eyebrow">DİJİTAL İŞ GÜCÜ</p>
          <h1>Tamamlanan İşler</h1>
          <p>Kapanış, doğrulama, süre, iş etkisi ve AI maliyeti birlikte izlenir.</p>
        </div>
      </header>

      <section className="dwf-panel">
        <div className="dwf-filters">
          <select className="dwf-select" onChange={(event) => setWorkerId(event.target.value)} value={workerId}>
            <option value="all">Dijital Çalışan: tümü</option>
            {DIGITAL_WORKERS.map((worker) => (
              <option key={worker.id} value={worker.id}>{worker.name.replace(" Dijital Çalışanı", "")}</option>
            ))}
          </select>
          <select className="dwf-select" onChange={(event) => setDate(event.target.value)} value={date}>
            <option value="all">Tarih: tümü</option>
            <option value="today">Bugün</option>
            <option value="yesterday">Dün</option>
          </select>
          <select className="dwf-select" onChange={(event) => setFacility(event.target.value)} value={facility}>
            <option value="all">Fabrika: tümü</option>
            <option value="Pilot Enjeksiyon Tesisi">Pilot Enjeksiyon Tesisi</option>
          </select>
          <select className="dwf-select" onChange={(event) => setModuleName(event.target.value)} value={moduleName}>
            <option value="all">Modül: tümü</option>
            {modules.map((item) => (
              <option key={item} value={item}>{item}</option>
            ))}
          </select>
          <select className="dwf-select" onChange={(event) => setVerified(event.target.value)} value={verified}>
            <option value="all">Sonuç: tümü</option>
            <option value="verified">Doğrulandı</option>
            <option value="unverified">Doğrulanmadı</option>
          </select>
          <select className="dwf-select" onChange={(event) => setRisk(event.target.value)} value={risk}>
            <option value="all">Risk: tümü</option>
            <option value="critical">Kritik</option>
            <option value="high">Yüksek</option>
            <option value="medium">Orta</option>
          </select>
        </div>

        <div className="dwf-table-wrap">
          <table className="dwf-table">
            <thead>
              <tr>
                <th>Case</th>
                <th>Çalışan</th>
                <th>Problem</th>
                <th>Aksiyon</th>
                <th>Sonuç</th>
                <th>Doğrulama</th>
                <th>Süre</th>
                <th>İş etkisi</th>
                <th>AI maliyeti</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((item) => {
                const worker = getDigitalWorker(item.primaryWorkerId);
                return (
                  <tr key={item.id}>
                    <td>
                      <Link href={`/dijital-is-gucu/${item.primaryWorkerId}?case=${item.id}`}>{item.title}</Link>
                      <div className="dwf-meta">{RISK_LABELS[item.severity]} · {item.source}</div>
                    </td>
                    <td>{worker?.name.replace(" Dijital Çalışanı", "")}</td>
                    <td>{item.problem}</td>
                    <td>{item.actions.join(" · ")}</td>
                    <td>{item.result ?? item.outcome ?? "Devam ediyor"}</td>
                    <td>{item.verified ? <VerifiedBadge /> : <DemoBadge label="Doğrulanmadı" />}</td>
                    <td>{item.startedAtLabel} → {item.closedAtLabel ?? "açık"}</td>
                    <td>{item.businessImpact}</td>
                    <td>
                      {item.costLabel}
                      <div className="dwf-meta">{DIGITAL_WORKFORCE_COST_NOTE}</div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </section>
    </>
  );
}
