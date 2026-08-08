import Link from "next/link";
import { ArrowLeft, Globe2, Plus, Search } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import { getResearchCaseStatusLabel } from "@/features/export/domain/export-3l";
import { readCountryResearchCases } from "@/features/export/server/export-case-reader";
import { CreateResearchCaseForm } from "./create-research-case-form";

export default async function CountryResearchListPage() {
  const result = await readCountryResearchCases();

  if (result.status === "failure") {
    return (
      <main className="export-page">
        <header className="export-header">
          <p>ÜLKE ARAŞTIRMASI</p>
          <h1>Ülke Araştırma</h1>
        </header>
        <div role="alert" className="state-panel error-state">
          <p>Araştırma dosyaları yüklenemedi.</p>
        </div>
      </main>
    );
  }

  const cases = result.cases;

  return (
    <main className="export-page">
      <div className="export-detail-page__back">
        <Link href="/ihracat">
          <ArrowLeft aria-hidden="true" />
          İhracat ana sayfasına dön
        </Link>
      </div>

      <header className="export-header">
        <p>ÜLKE ARAŞTIRMASI</p>
        <h1>Ülke Araştırma</h1>
        <span>
          Hedef ülke gereksinimlerini araştırın, bulguları kaydedin ve
          doğrulayın
        </span>
      </header>

      <section className="export-workspace" aria-label="Yeni araştırma">
        <div className="export-workspace__heading">
          <div>
            <p>YENİ ARAŞTIRMA</p>
            <h2>Yeni Ülke Araştır</h2>
          </div>
        </div>
        <CreateResearchCaseForm />
      </section>

      <section className="export-workspace" aria-label="Araştırma listesi">
        <div className="export-workspace__heading">
          <div>
            <p>ARAŞTIRMA DOSYALARI</p>
            <h2>Aktif ülke araştırmaları</h2>
          </div>
          <Badge tone="information">{cases.length} dosya</Badge>
        </div>

        {cases.length === 0 ? (
          <div className="export-empty" role="status">
            <Search aria-hidden="true" />
            <p>Henüz ülke araştırması bulunmuyor.</p>
          </div>
        ) : (
          <div className="export-workspace__list">
            {cases.map((researchCase) => (
              <Card key={researchCase.id} className="export-case-card">
                <div className="export-case-card__heading">
                  <Badge tone="information">
                    {getResearchCaseStatusLabel(researchCase.status)}
                  </Badge>
                  <h2>{researchCase.researchNumber}</h2>
                  <p>{researchCase.countryCode}</p>
                </div>
                <div className="export-case-card__summary">
                  <div>
                    <Globe2 aria-hidden="true" />
                    <div>
                      <span>Ülke</span>
                      <strong>{researchCase.countryCode}</strong>
                    </div>
                  </div>
                </div>
                <div className="export-case-card__footer">
                  <Link
                    className="export-case-card__link"
                    href={`/ihracat/ulke-arastirma/${researchCase.id}`}
                  >
                    <Plus aria-hidden="true" />
                    Araştırmayı aç
                  </Link>
                </div>
              </Card>
            ))}
          </div>
        )}
      </section>
    </main>
  );
}
