import Link from "next/link";
import { ArrowLeft, FileWarning, ShieldCheck } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import {
  getQualityNcrSeverityTone,
  getQualityStatusLabel,
} from "@/features/quality-control/domain/quality-control";
import { readQualityNcrList } from "@/features/quality-control/server/quality-control-reader";

type PageProps = Readonly<{
  searchParams: Promise<{
    status?: string;
    severity?: string;
    hold?: string;
    productionOrderId?: string;
  }>;
}>;

export default async function QualityNcrListPage({ searchParams }: PageProps) {
  const filters = await searchParams;
  const holdActive =
    filters.hold === "active" ? true : filters.hold === "none" ? false : undefined;

  const result = await readQualityNcrList({
    status: filters.status,
    severity: filters.severity,
    holdActive,
    productionOrderId: filters.productionOrderId,
  });

  if (result.status === "failure") {
    return (
      <main className="production-execution-page">
        <div role="alert" className="state-panel error-state">
          <ShieldCheck aria-hidden="true" />
          <p>{result.message}</p>
        </div>
      </main>
    );
  }

  const ncrs = [...result.ncrs].sort((a, b) => b.updatedAtUtc.localeCompare(a.updatedAtUtc));

  return (
    <main className="production-execution-page">
      <header className="production-execution-header">
        <Link href="/kalite" className="production-execution-header__link">
          <ArrowLeft aria-hidden="true" />
          Kalite ana sayfası
        </Link>
        <p>KALİTE KONTROL</p>
        <h1>Uygunsuzluk Raporları</h1>
        <span>Açık NCR, kalite blokesi ve düzeltici aksiyon durumlarını izleyin.</span>
      </header>

      <Card className="production-execution-panel">
        <header>
          <h2>Filtreler</h2>
        </header>
        <form className="production-execution-form-row" method="get">
          <select name="status" defaultValue={filters.status ?? ""}>
            <option value="">Tüm durumlar</option>
            <option value="open">Açık</option>
            <option value="under-review">İncelemede</option>
            <option value="action-required">Aksiyon gerekli</option>
            <option value="resolved">Çözüldü</option>
            <option value="closed">Kapatıldı</option>
          </select>
          <select name="severity" defaultValue={filters.severity ?? ""}>
            <option value="">Tüm şiddetler</option>
            <option value="minor">Minör</option>
            <option value="major">Majör</option>
            <option value="critical">Kritik</option>
          </select>
          <select name="hold" defaultValue={filters.hold ?? ""}>
            <option value="">Tüm blokesi durumları</option>
            <option value="active">Aktif blokesi</option>
            <option value="none">Blokesi yok</option>
          </select>
          <input
            name="productionOrderId"
            type="text"
            placeholder="Üretim emri ID"
            defaultValue={filters.productionOrderId ?? ""}
          />
          <button type="submit" className="production-execution-action-button">
            Uygula
          </button>
        </form>
      </Card>

      <Card className="production-execution-panel production-execution-panel--wide">
        <header>
          <h2>NCR listesi</h2>
        </header>
        {ncrs.length === 0 ? (
          <p>Kayıt bulunamadı.</p>
        ) : (
          <table className="production-execution-table">
            <thead>
              <tr>
                <th>NCR No</th>
                <th>Üretim emri</th>
                <th>Ürün</th>
                <th>Şiddet</th>
                <th>Durum</th>
                <th>Blokesi</th>
                <th>Disposition</th>
                <th>Sorumlu</th>
                <th>Oluşturulma</th>
              </tr>
            </thead>
            <tbody>
              {ncrs.map((ncr) => (
                <tr key={ncr.id}>
                  <td>
                    <Link href={`/kalite/ncr/${ncr.id}`}>
                      <strong>{ncr.ncrNumber}</strong>
                    </Link>
                    <span>{ncr.title}</span>
                  </td>
                  <td>{ncr.productionOrderReference ?? ncr.productionOrderId ?? "—"}</td>
                  <td>{ncr.productReference ?? "—"}</td>
                  <td>
                    <Badge tone={getQualityNcrSeverityTone(ncr.severity)}>
                      {getQualityStatusLabel(ncr.severity)}
                    </Badge>
                  </td>
                  <td>{getQualityStatusLabel(ncr.status)}</td>
                  <td>{getQualityStatusLabel(ncr.holdStatus)}</td>
                  <td>
                    {ncr.dispositionType
                      ? getQualityStatusLabel(ncr.dispositionType)
                      : getQualityStatusLabel(ncr.dispositionStatus)}
                  </td>
                  <td>{ncr.ownerSubjectId ?? "—"}</td>
                  <td>{new Date(ncr.createdAtUtc).toLocaleString("tr-TR")}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Card>

      <footer className="production-execution-footer-note">
        <FileWarning aria-hidden="true" />
        Kalite blokesi üretim emri durumundan ayrı izlenir; üretim durumu salt okunurdur.
      </footer>
    </main>
  );
}
