"use client";

import Link from "next/link";
import {
  FileStack,
  GitBranch,
  Package,
  Scale,
  Search,
  Truck,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";

import type { ExportTraceability } from "../domain/export-3l";
import { getComplianceStatusLabel } from "../domain/export-3l";

type ExportTraceabilityPanelProps = Readonly<{
  traceability: ExportTraceability | null;
}>;

export function ExportTraceabilityPanel({
  traceability,
}: ExportTraceabilityPanelProps) {
  if (!traceability) {
    return (
      <section className="export-traceability-panel" role="status">
        <p>İZLENEBİLİRLİK</p>
        <h2>İzlenebilirlik verisi alınamadı</h2>
        <span>Dosya zinciri şu an görüntülenemiyor.</span>
      </section>
    );
  }

  return (
    <section className="export-traceability-panel">
      <header className="export-traceability-panel__header">
        <div>
          <p>İZLENEBİLİRLİK</p>
          <h2>İhracat dosyası zinciri</h2>
          <span>
            {traceability.caseNumber} · {traceability.customerName} ·{" "}
            {traceability.destinationCountryCode}
          </span>
        </div>
        <Badge tone="information">{traceability.activityCount} aktivite</Badge>
      </header>

      <div className="export-traceability-panel__chain">
        <article>
          <FileStack aria-hidden="true" />
          <div>
            <h3>Belgeler</h3>
            <ul>
              {traceability.documents.map((doc) => (
                <li key={doc.id}>
                  <span>{doc.documentType}</span>
                  <em>{doc.status}</em>
                </li>
              ))}
              {traceability.documents.length === 0 ? (
                <li className="export-traceability-panel__empty">Kayıt yok</li>
              ) : null}
            </ul>
          </div>
        </article>

        <article>
          <Package aria-hidden="true" />
          <div>
            <h3>Paketleme</h3>
            <ul>
              {traceability.packingUnits.map((unit) => (
                <li key={unit.id}>
                  <span>{unit.unitCode}</span>
                  <em>{unit.status}</em>
                </li>
              ))}
              {traceability.packingUnits.length === 0 ? (
                <li className="export-traceability-panel__empty">Kayıt yok</li>
              ) : null}
            </ul>
          </div>
        </article>

        <article>
          <Truck aria-hidden="true" />
          <div>
            <h3>Sevkiyat</h3>
            <ul>
              {traceability.shipments.map((shipment) => (
                <li key={shipment.id}>
                  <span>{shipment.shipmentReference}</span>
                  <em>{shipment.status}</em>
                </li>
              ))}
              {traceability.shipments.length === 0 ? (
                <li className="export-traceability-panel__empty">Kayıt yok</li>
              ) : null}
            </ul>
          </div>
        </article>

        <article>
          <Scale aria-hidden="true" />
          <div>
            <h3>Uygunluk</h3>
            {traceability.compliance ? (
              <p>
                {getComplianceStatusLabel(
                  traceability.compliance.result as "ready" | "attention" | "blocked" | "unable-to-evaluate",
                )}
                {traceability.compliance.evaluationNumber
                  ? ` · #${traceability.compliance.evaluationNumber}`
                  : ""}
              </p>
            ) : (
              <p className="export-traceability-panel__empty">Değerlendirme yok</p>
            )}
          </div>
        </article>

        <article>
          <Search aria-hidden="true" />
          <div>
            <h3>Ülke araştırması</h3>
            {traceability.research?.researchCaseId ? (
              <Link
                href={`/ihracat/ulke-arastirma/${traceability.research.researchCaseId}`}
              >
                {traceability.research.researchNumber ?? "Araştırma"}
              </Link>
            ) : (
              <p className="export-traceability-panel__empty">Bağlı araştırma yok</p>
            )}
          </div>
        </article>

        {traceability.broker ? (
          <article>
            <GitBranch aria-hidden="true" />
            <div>
              <h3>Broker</h3>
              <p>
                {traceability.broker.status}
                {traceability.broker.packageVersionNumber
                  ? ` · v${traceability.broker.packageVersionNumber}`
                  : ""}
              </p>
            </div>
          </article>
        ) : null}
      </div>
    </section>
  );
}
