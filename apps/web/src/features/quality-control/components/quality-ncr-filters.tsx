import Link from "next/link";

import { Button } from "@/components/ui/button";

export type QualityNcrFiltersProps = Readonly<{
  status?: string;
  severity?: string;
  hold?: string;
  productionOrderId?: string;
}>;

export function QualityNcrFilters({
  status,
  severity,
  hold,
  productionOrderId,
}: QualityNcrFiltersProps) {
  const hasFilters = Boolean(status || severity || hold || productionOrderId);

  return (
    <section className="quality-section" aria-label="NCR filtreleri">
      <header className="quality-section__header">
        <h2>Filtreler</h2>
      </header>
      <form className="quality-filter-panel" method="get">
        <div className="quality-filter-panel__field">
          <label htmlFor="ncr-filter-status">Durum</label>
          <select
            id="ncr-filter-status"
            name="status"
            className="quality-form-select"
            defaultValue={status ?? ""}
          >
            <option value="">Tüm durumlar</option>
            <option value="open">Açık</option>
            <option value="under-review">İncelemede</option>
            <option value="action-required">Aksiyon gerekli</option>
            <option value="resolved">Çözüldü</option>
            <option value="closed">Kapatıldı</option>
          </select>
        </div>
        <div className="quality-filter-panel__field">
          <label htmlFor="ncr-filter-severity">Kritiklik</label>
          <select
            id="ncr-filter-severity"
            name="severity"
            className="quality-form-select"
            defaultValue={severity ?? ""}
          >
            <option value="">Tüm kritiklik</option>
            <option value="minor">Minör</option>
            <option value="major">Majör</option>
            <option value="critical">Kritik</option>
          </select>
        </div>
        <div className="quality-filter-panel__field">
          <label htmlFor="ncr-filter-hold">Kalite Blokesi</label>
          <select
            id="ncr-filter-hold"
            name="hold"
            className="quality-form-select"
            defaultValue={hold ?? ""}
          >
            <option value="">Tüm kalite blokesi</option>
            <option value="active">Aktif</option>
            <option value="none">Yok</option>
          </select>
        </div>
        <div className="quality-filter-panel__field">
          <label htmlFor="ncr-filter-order">Üretim Emri</label>
          <input
            id="ncr-filter-order"
            name="productionOrderId"
            type="text"
            className="quality-form-input"
            placeholder="Emir referansı"
            defaultValue={productionOrderId ?? ""}
          />
        </div>
        <div className="quality-filter-panel__actions">
          <Button type="submit" variant="primary" size="medium">
            Uygula
          </Button>
          {hasFilters ? (
            <Link href="/kalite/ncr" className="quality-filter-panel__clear">
              Temizle
            </Link>
          ) : null}
        </div>
      </form>
    </section>
  );
}
