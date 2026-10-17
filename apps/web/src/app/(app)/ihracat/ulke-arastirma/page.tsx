import Link from "next/link";
import { ArrowLeft, Globe2, Plus, Search } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import { getResearchCaseStatusLabel } from "@/features/export/domain/export-3l";
import { readCountryResearchCases } from "@/features/export/server/export-case-reader";
import { CreateResearchCaseForm } from "./create-research-case-form";
import { FailureStatePage } from "@/components/ui/failure-state";

export default async function CountryResearchListPage() {
  const result = await readCountryResearchCases();

  if (result.status === "failure") {
    return (
      <div className="export-page">
        <header className="export-header">
          <p>İHRACAT</p>
          <h1>Ülke Araştırma</h1>
        </header>
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Araştırma dosyaları yüklenemedi" inline />
      </div>
    );
  }

  const cases = result.cases;

  return (
    <div className="export-page">
      <header className="export-header">
        <div className="export-header__top">
          <div className="export-header__intro">
            <p>İHRACAT</p>
            <h1>Ülke Araştırma</h1>
          </div>
          <div className="export-header__actions">
            <Link href="/ihracat" className="button button-secondary button-medium">
              <ArrowLeft aria-hidden="true" />
              İhracat ana sayfası
            </Link>
          </div>
        </div>
        <span>
          Hedef ülke gereksinimlerini araştırın, bulguları kaydedin ve
          doğrulayın
        </span>
      </header>

      <div className="export-split">
      <section className="export-workspace" aria-label="Yeni araştırma">
        <div className="export-workspace__heading">
          <div>
            <div className="export-workspace__title-row">
              <h2>Yeni araştırma</h2>
            </div>
            <span>Hedef ülke ve ürün bilgisini girin; araştırma dosyası otomatik açılır.</span>
          </div>
        </div>
        <CreateResearchCaseForm />
      </section>

      <section className="export-workspace" aria-label="Araştırma listesi">
        <div className="export-workspace__heading">
          <div>
            <div className="export-workspace__title-row">
              <h2>Aktif ülke araştırmaları</h2>
              <Badge tone="information">{cases.length} dosya</Badge>
            </div>
            <span>Başlatılan araştırma dosyaları burada listelenir.</span>
          </div>
        </div>

        {cases.length === 0 ? (
          <div className="export-empty" role="status" aria-label="Boş liste">
            <div className="export-empty__icon">
              <Search aria-hidden="true" />
            </div>
            <div>
              <p>ARAŞTIRMA YOK</p>
              <h2>Henüz ülke araştırması bulunmuyor</h2>
            </div>
            <p className="export-empty__desc">
              Formu doldurarak bir hedef ülke için ilk araştırma dosyasını başlatın.
            </p>
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
      </div>
    </div>
  );
}
