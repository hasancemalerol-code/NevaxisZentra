"use client";

import { useEffect, useMemo, useState, useTransition } from "react";
import { LoaderCircle, Package, Search, X } from "lucide-react";

import type {
  MaterialLotDetail,
  MaterialLotSummary,
  MaterialLotTimelineEntry,
  MaterialLotTraceability,
  MaterialLotWorkspaceSummary,
  MaterialStorageLocation,
} from "../domain/materials";
import {
  formatMaterialDateOnly,
  formatMaterialDateTime,
  formatMaterialLotExpiry,
  formatMaterialLotLocation,
  formatMaterialQuantity,
  getMaterialLotCertificateTypeLabel,
  getMaterialLotEventTypeLabel,
  getMaterialLotQualityStatusLabel,
  getMaterialLotStatusLabel,
  getMaterialSourceLabel,
  MATERIAL_LOT_CERTIFICATE_FORM_OPTIONS,
  MATERIAL_LOT_EXPIRY_FILTER_OPTIONS,
  MATERIAL_LOT_QUALITY_FILTER_OPTIONS,
  MATERIAL_LOT_QUALITY_FORM_OPTIONS,
  MATERIAL_LOT_QUANTITY_DISCLAIMER,
  MATERIAL_LOT_STATUS_FILTER_OPTIONS,
  MATERIAL_LOT_TRANSITION_TARGETS,
} from "../presentation/material-labels";
import { loadMaterialLotDetailAction, loadMaterialLotsAction } from "../server/material-actions";
import {
  createMaterialLotAction,
  linkMaterialLotCertificateAction,
  observeMaterialLotReceiptAction,
  recordMaterialLotQualityStatusAction,
  transitionMaterialLotStatusAction,
  transferMaterialLotAction,
} from "../server/material-writer";
import { MaterialLotTraceabilityPanel } from "./material-lot-traceability-panel";

type MaterialLotPanelProps = Readonly<{
  materialId: string;
  materialCode: string;
  materialName: string;
  unitOfMeasure: string;
  canManage: boolean;
  traceabilityMode?: boolean;
}>;

type LotActionPanel =
  | "create"
  | "receipt"
  | "transfer"
  | "status"
  | "quality"
  | "certificate"
  | null;

type LotDetailSection =
  | "overview"
  | "source"
  | "location"
  | "quality"
  | "documents"
  | "timeline"
  | "traceability";

const LOT_DETAIL_SECTIONS: readonly [LotDetailSection, string][] = [
  ["overview", "Genel Bakış"],
  ["source", "Kaynak ve Kabul"],
  ["location", "Depo/Konum"],
  ["quality", "Kalite Durumu"],
  ["documents", "Belgeler"],
  ["timeline", "Zaman Çizelgesi"],
  ["traceability", "İzlenebilirlik"],
];

function lotStatusBadgeClass(status: string): string {
  switch (status) {
    case "available":
      return "material-registry-badge material-registry-badge--success";
    case "quality-hold":
      return "material-registry-badge material-registry-badge--warning";
    case "blocked":
      return "material-registry-badge material-registry-badge--quarantined";
    case "closed":
      return "material-registry-badge material-registry-badge--draft";
    case "received":
      return "material-registry-badge material-registry-badge--info";
    default:
      return "material-registry-badge material-registry-badge--neutral";
  }
}

function supplierLotText(lot: MaterialLotSummary | MaterialLotDetail): string {
  const parts = [lot.supplierLotNumber, lot.manufacturerLotNumber].filter(Boolean);
  return parts.length > 0 ? parts.join(" · ") : "—";
}

export function MaterialLotPanel({
  materialId,
  materialCode,
  materialName,
  unitOfMeasure,
  canManage,
  traceabilityMode = false,
}: MaterialLotPanelProps) {
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState("");
  const [qualityFilter, setQualityFilter] = useState("");
  const [expiryFilter, setExpiryFilter] = useState("");
  const [warehouseFilter, setWarehouseFilter] = useState("");
  const [summary, setSummary] = useState<MaterialLotWorkspaceSummary | undefined>();
  const [lots, setLots] = useState<readonly MaterialLotSummary[]>([]);
  const [storageLocations, setStorageLocations] = useState<readonly MaterialStorageLocation[]>([]);
  const [lotCanManage, setLotCanManage] = useState(canManage);
  const [selectedLotId, setSelectedLotId] = useState("");
  const [lotDetail, setLotDetail] = useState<MaterialLotDetail | undefined>();
  const [timeline, setTimeline] = useState<readonly MaterialLotTimelineEntry[]>([]);
  const [traceability, setTraceability] = useState<MaterialLotTraceability | undefined>();
  const [lotSection, setLotSection] = useState<LotDetailSection>("overview");
  const [actionPanel, setActionPanel] = useState<LotActionPanel>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loaded, setLoaded] = useState(false);
  const [isPending, startTransition] = useTransition();

  const filtersActive = Boolean(search.trim() || statusFilter || qualityFilter || expiryFilter || warehouseFilter);

  const filteredLots = useMemo(() => {
    return lots.filter((lot) => {
      const matchesSearch =
        !search.trim() ||
        lot.lotNumber.toLowerCase().includes(search.toLowerCase()) ||
        (lot.supplierLotNumber?.toLowerCase().includes(search.toLowerCase()) ?? false) ||
        (lot.manufacturerLotNumber?.toLowerCase().includes(search.toLowerCase()) ?? false);
      const matchesStatus = !statusFilter || lot.status === statusFilter;
      const matchesQuality = !qualityFilter || lot.qualityStatus === qualityFilter;
      const matchesWarehouse =
        !warehouseFilter ||
        (lot.currentWarehouseReference?.toLowerCase().includes(warehouseFilter.toLowerCase()) ?? false);
      const matchesExpiry =
        !expiryFilter ||
        (expiryFilter === "approaching" && lot.isExpiryApproaching) ||
        (expiryFilter === "expired" &&
          Boolean(lot.expiryDate || lot.retestDate) &&
          !lot.isExpiryApproaching);
      return matchesSearch && matchesStatus && matchesQuality && matchesWarehouse && matchesExpiry;
    });
  }, [expiryFilter, lots, qualityFilter, search, statusFilter, warehouseFilter]);

  const selectedSummary = lots.find((lot) => lot.id === selectedLotId);
  const activeLotDetail = lotDetail && lotDetail.id === selectedLotId ? lotDetail : undefined;
  const transitionOptions = activeLotDetail
    ? MATERIAL_LOT_TRANSITION_TARGETS[activeLotDetail.status] ?? []
    : selectedSummary
      ? MATERIAL_LOT_TRANSITION_TARGETS[selectedSummary.status] ?? []
      : [];

  function refreshLots(nextSelectedId = selectedLotId) {
    startTransition(async () => {
      setError(null);
      const result = await loadMaterialLotsAction(materialId, {
        materialId,
        pageSize: 100,
      });

      if (result.status === "failure") {
        setError(result.message);
        return;
      }

      setSummary(result.summary);
      setLots(result.lots);
      setStorageLocations(result.storageLocations);
      setLotCanManage(result.canManage);
      setLoaded(true);

      if (nextSelectedId) {
        const detailResult = await loadMaterialLotDetailAction(nextSelectedId);
        if (detailResult.status === "success") {
          setLotDetail(detailResult.lot);
          setTimeline(detailResult.timeline);
          setTraceability(detailResult.traceability);
          setStorageLocations(detailResult.storageLocations);
          setLotCanManage(detailResult.canManage);
        }
      }
    });
  }

  useEffect(() => {
    if (!loaded) {
      void refreshLots();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- initial load only when materialId changes
  }, [loaded, materialId]);

  function selectLot(lot: MaterialLotSummary) {
    setSelectedLotId(lot.id);
    setLotDetail(undefined);
    setTimeline([]);
    setTraceability(undefined);
    setLotSection(traceabilityMode ? "traceability" : "overview");
    setActionPanel(null);
    setMessage(null);
    setError(null);

    startTransition(async () => {
      const result = await loadMaterialLotDetailAction(lot.id);
      if (result.status === "success") {
        setLotDetail(result.lot);
        setTimeline(result.timeline);
        setTraceability(result.traceability);
        setStorageLocations(result.storageLocations);
        setLotCanManage(result.canManage);
      } else {
        setError(result.message);
      }
    });
  }

  function runAction(action: () => Promise<{ status: string; message?: string }>, lotId?: string) {
    startTransition(async () => {
      setMessage(null);
      setError(null);
      const result = await action();
      if (result.status === "failure") {
        setError(result.message ?? "İşlem başarısız.");
        return;
      }

      setMessage("Lot kaydı güncellendi.");
      setActionPanel(null);
      refreshLots(lotId ?? selectedLotId);
    });
  }

  if (traceabilityMode) {
    return (
      <div className="material-lot-panel" role="tabpanel">
        {!loaded && isPending ? (
          <div className="material-registry-status">
            <LoaderCircle aria-hidden="true" size={16} />
            Lot verisi yükleniyor…
          </div>
        ) : null}
        {error ? (
          <div className="material-registry-error" role="alert">
            {error}
          </div>
        ) : null}
        <div className="material-lot-traceability-picker">
          <label>
            Lot seçin
            <select
              value={selectedLotId}
              onChange={(event) => {
                const lot = lots.find((item) => item.id === event.target.value);
                if (lot) {
                  selectLot(lot);
                } else {
                  setSelectedLotId("");
                  setLotDetail(undefined);
                  setTraceability(undefined);
                }
              }}
            >
              <option value="">Lot seçiniz</option>
              {lots.map((lot) => (
                <option key={lot.id} value={lot.id}>
                  {lot.lotNumber}
                </option>
              ))}
            </select>
          </label>
        </div>
        <MaterialLotTraceabilityPanel traceability={traceability ?? null} lotNumber={activeLotDetail?.lotNumber} />
      </div>
    );
  }

  return (
    <div className="material-lot-panel" role="tabpanel">
      {error ? (
        <div className="material-registry-error" role="alert">
          {error}
        </div>
      ) : null}
      {message ? (
        <div className="material-registry-empty" role="status">
          {message}
        </div>
      ) : null}
      {isPending ? (
        <div className="material-registry-status" role="status" aria-live="polite">
          <LoaderCircle aria-hidden="true" size={16} />
          İşleniyor…
        </div>
      ) : null}

      {!loaded && !isPending ? (
        <div className="material-registry-status">
          <LoaderCircle aria-hidden="true" size={16} />
          Lot verisi yükleniyor…
        </div>
      ) : null}

      {summary ? (
        <div className="material-lot-kpis" aria-label="Lot özet sayıları">
          <article className="material-executive__kpi material-executive__kpi--neutral">
            <strong>{summary.totalCount}</strong>
            <span>Toplam Lot</span>
          </article>
          <article className="material-executive__kpi material-executive__kpi--success">
            <strong>{summary.availableCount}</strong>
            <span>Kullanılabilir</span>
          </article>
          <article className="material-executive__kpi material-executive__kpi--warning">
            <strong>{summary.qualityHoldCount}</strong>
            <span>Kalite Bekliyor</span>
          </article>
          <article className="material-executive__kpi material-executive__kpi--critical">
            <strong>{summary.blockedCount}</strong>
            <span>Bloke</span>
          </article>
          <article className="material-executive__kpi material-executive__kpi--info">
            <strong>{summary.expiryApproachingCount}</strong>
            <span>Süresi Yaklaşan</span>
          </article>
        </div>
      ) : null}

      <p className="material-lot-disclaimer">{MATERIAL_LOT_QUANTITY_DISCLAIMER}</p>

      {lotCanManage ? (
        <div className="material-lot-actions">
          <button
            type="button"
            className="material-registry-button compact primary"
            onClick={() => setActionPanel(actionPanel === "create" ? null : "create")}
          >
            Yeni Lot
          </button>
          {activeLotDetail ? (
            <>
              <button
                type="button"
                className="material-registry-button compact"
                onClick={() => setActionPanel(actionPanel === "receipt" ? null : "receipt")}
              >
                Kabul Gözlemi Kaydet
              </button>
              <button
                type="button"
                className="material-registry-button compact"
                onClick={() => setActionPanel(actionPanel === "transfer" ? null : "transfer")}
              >
                Konum Değiştir
              </button>
              {transitionOptions.length > 0 ? (
                <button
                  type="button"
                  className="material-registry-button compact"
                  onClick={() => setActionPanel(actionPanel === "status" ? null : "status")}
                >
                  Durum Değiştir
                </button>
              ) : null}
              <button
                type="button"
                className="material-registry-button compact"
                onClick={() => setActionPanel(actionPanel === "quality" ? null : "quality")}
              >
                Kalite Durumu Kaydet
              </button>
              <button
                type="button"
                className="material-registry-button compact"
                onClick={() => setActionPanel(actionPanel === "certificate" ? null : "certificate")}
              >
                Belge Referansı Ekle
              </button>
            </>
          ) : null}
        </div>
      ) : (
        <p className="material-readonly-note">Lot yönetim yetkiniz bulunmuyor; kayıtlar salt okunurdur.</p>
      )}

      {actionPanel === "create" && lotCanManage ? (
        <section className="material-detail-action-panel" aria-label="Yeni lot formu">
          <h3 className="material-detail-action-panel__title">Yeni lot oluştur</h3>
          <form
            className="material-registry-form-grid"
            action={(formData) => runAction(() => createMaterialLotAction(formData))}
          >
            <input type="hidden" name="materialId" value={materialId} />
            <input type="hidden" name="unitOfMeasure" value={unitOfMeasure} />
            <input type="hidden" name="idempotencyKey" value={crypto.randomUUID()} />
            <label className="material-required">
              Lot Numarası
              <input name="lotNumber" required />
            </label>
            <label>
              Tedarikçi Lotu
              <input name="supplierLotNumber" />
            </label>
            <label>
              Üretici Lotu
              <input name="manufacturerLotNumber" />
            </label>
            <label>
              Başlangıç Miktarı
              <input name="initialQuantity" type="number" step="0.001" min="0" />
            </label>
            <label>
              Kalite Durumu
              <select name="qualityStatus" defaultValue="pending">
                {MATERIAL_LOT_QUALITY_FORM_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Depo/Konum
              <select name="storageLocationId" defaultValue="">
                <option value="">Seçilmedi</option>
                {storageLocations.map((location) => (
                  <option key={location.id} value={location.id}>
                    {location.warehouseCode} / {location.locationCode} — {location.locationName}
                  </option>
                ))}
              </select>
            </label>
            <label>
              SKT
              <input name="expiryDate" type="date" />
            </label>
            <label className="full-width">
              Not
              <textarea name="notes" rows={2} />
            </label>
            <div className="material-registry-actions full-width">
              <button type="button" className="material-registry-button ghost" onClick={() => setActionPanel(null)}>
                İptal
              </button>
              <button type="submit" className="material-registry-button primary">
                Kaydet
              </button>
            </div>
          </form>
        </section>
      ) : null}

      {actionPanel === "receipt" && activeLotDetail && lotCanManage ? (
        <section className="material-detail-action-panel" aria-label="Kabul gözlemi">
          <h3 className="material-detail-action-panel__title">Kabul gözlemi kaydet</h3>
          <form
            className="material-registry-form-grid"
            action={(formData) =>
              runAction(() => observeMaterialLotReceiptAction(formData), activeLotDetail.id)
            }
          >
            <input type="hidden" name="lotId" value={activeLotDetail.id} />
            <input type="hidden" name="expectedVersion" value={activeLotDetail.version} />
            <input type="hidden" name="receivedAtUtc" value={new Date().toISOString()} />
            <input type="hidden" name="idempotencyKey" value={crypto.randomUUID()} />
            <label className="material-required">
              Gözlenen Miktar
              <input name="observedQuantity" type="number" step="0.001" min="0" required />
            </label>
            <label>
              Mal Kabul Referansı
              <input name="goodsReceiptReference" />
            </label>
            <div className="material-registry-actions full-width">
              <button type="button" className="material-registry-button ghost" onClick={() => setActionPanel(null)}>
                İptal
              </button>
              <button type="submit" className="material-registry-button primary">
                Kaydet
              </button>
            </div>
          </form>
        </section>
      ) : null}

      {actionPanel === "transfer" && activeLotDetail && lotCanManage ? (
        <section className="material-detail-action-panel" aria-label="Konum değiştir">
          <h3 className="material-detail-action-panel__title">Konum değiştir</h3>
          <form
            className="material-registry-form-grid"
            action={(formData) => runAction(() => transferMaterialLotAction(formData), activeLotDetail.id)}
          >
            <input type="hidden" name="lotId" value={activeLotDetail.id} />
            <input type="hidden" name="expectedVersion" value={activeLotDetail.version} />
            <input type="hidden" name="idempotencyKey" value={crypto.randomUUID()} />
            <label className="material-required">
              Hedef Konum
              <select name="targetStorageLocationId" required defaultValue="">
                <option value="">Seçiniz</option>
                {storageLocations.map((location) => (
                  <option key={location.id} value={location.id}>
                    {location.warehouseCode} / {location.locationCode}
                  </option>
                ))}
              </select>
            </label>
            <label className="full-width">
              Gerekçe
              <textarea name="reason" rows={2} />
            </label>
            <div className="material-registry-actions full-width">
              <button type="button" className="material-registry-button ghost" onClick={() => setActionPanel(null)}>
                İptal
              </button>
              <button type="submit" className="material-registry-button primary">
                Transfer Et
              </button>
            </div>
          </form>
        </section>
      ) : null}

      {actionPanel === "status" && activeLotDetail && lotCanManage ? (
        <section className="material-detail-action-panel" aria-label="Durum değiştir">
          <h3 className="material-detail-action-panel__title">Durum değiştir</h3>
          <form
            className="material-registry-form-grid"
            action={(formData) =>
              runAction(() => transitionMaterialLotStatusAction(formData), activeLotDetail.id)
            }
          >
            <input type="hidden" name="lotId" value={activeLotDetail.id} />
            <input type="hidden" name="expectedVersion" value={activeLotDetail.version} />
            <input type="hidden" name="idempotencyKey" value={crypto.randomUUID()} />
            <label className="material-required">
              Hedef Durum
              <select name="targetStatus" required defaultValue="">
                <option value="">Seçiniz</option>
                {transitionOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            <label className="material-required full-width">
              Gerekçe
              <textarea name="reason" rows={2} required />
            </label>
            <div className="material-registry-actions full-width">
              <button type="button" className="material-registry-button ghost" onClick={() => setActionPanel(null)}>
                İptal
              </button>
              <button type="submit" className="material-registry-button primary">
                Durumu Güncelle
              </button>
            </div>
          </form>
        </section>
      ) : null}

      {actionPanel === "quality" && activeLotDetail && lotCanManage ? (
        <section className="material-detail-action-panel" aria-label="Kalite durumu kaydet">
          <h3 className="material-detail-action-panel__title">Kalite durumu kaydet</h3>
          <form
            className="material-registry-form-grid"
            action={(formData) =>
              runAction(() => recordMaterialLotQualityStatusAction(formData), activeLotDetail.id)
            }
          >
            <input type="hidden" name="lotId" value={activeLotDetail.id} />
            <input type="hidden" name="expectedVersion" value={activeLotDetail.version} />
            <input type="hidden" name="idempotencyKey" value={crypto.randomUUID()} />
            <label className="material-required">
              Kalite Durumu
              <select name="qualityStatus" required defaultValue={activeLotDetail.qualityStatus}>
                {MATERIAL_LOT_QUALITY_FORM_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            <label className="full-width">
              Gerekçe
              <textarea name="reason" rows={2} />
            </label>
            <div className="material-registry-actions full-width">
              <button type="button" className="material-registry-button ghost" onClick={() => setActionPanel(null)}>
                İptal
              </button>
              <button type="submit" className="material-registry-button primary">
                Kaydet
              </button>
            </div>
          </form>
        </section>
      ) : null}

      {actionPanel === "certificate" && activeLotDetail && lotCanManage ? (
        <section className="material-detail-action-panel" aria-label="Belge referansı ekle">
          <h3 className="material-detail-action-panel__title">Belge referansı ekle</h3>
          <form
            className="material-registry-form-grid"
            action={(formData) =>
              runAction(() => linkMaterialLotCertificateAction(formData), activeLotDetail.id)
            }
          >
            <input type="hidden" name="lotId" value={activeLotDetail.id} />
            <input type="hidden" name="sourceType" value="manual" />
            <input type="hidden" name="idempotencyKey" value={crypto.randomUUID()} />
            <label className="material-required">
              Belge Türü
              <select name="certificateType" defaultValue="certificate-of-analysis">
                {MATERIAL_LOT_CERTIFICATE_FORM_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            <label className="material-required">
              Belge Referansı
              <input name="documentReference" required />
            </label>
            <label>
              Belge Numarası
              <input name="documentNumber" />
            </label>
            <label>
              Düzenleyen
              <input name="issuer" />
            </label>
            <div className="material-registry-actions full-width">
              <button type="button" className="material-registry-button ghost" onClick={() => setActionPanel(null)}>
                İptal
              </button>
              <button type="submit" className="material-registry-button primary">
                Bağla
              </button>
            </div>
          </form>
        </section>
      ) : null}

      <section className="material-registry-surface material-lot-registry" aria-label="Lot sicili">
        <div className="material-registry-surface__header">
          <h3 className="material-registry-surface__title">Lot Sicili</h3>
          <span className="material-registry-surface__count">
            {filteredLots.length} / {lots.length} kayıt
          </span>
        </div>

        <div className="material-registry-toolbar" aria-label="Lot arama ve filtreler">
          <label className="material-registry-toolbar__search">
            Ara
            <span className="material-registry-search">
              <Search aria-hidden="true" size={14} className="material-registry-search__icon" />
              <input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Lot numarası veya tedarikçi lotu"
              />
            </span>
          </label>
          <div className="material-registry-toolbar__filters">
            <label className="material-registry-toolbar__filter">
              Durum
              <select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)}>
                {MATERIAL_LOT_STATUS_FILTER_OPTIONS.map((option) => (
                  <option key={option.value || "all"} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            <label className="material-registry-toolbar__filter">
              Kalite
              <select value={qualityFilter} onChange={(event) => setQualityFilter(event.target.value)}>
                {MATERIAL_LOT_QUALITY_FILTER_OPTIONS.map((option) => (
                  <option key={option.value || "all"} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            <label className="material-registry-toolbar__filter">
              SKT
              <select value={expiryFilter} onChange={(event) => setExpiryFilter(event.target.value)}>
                {MATERIAL_LOT_EXPIRY_FILTER_OPTIONS.map((option) => (
                  <option key={option.value || "all"} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            {filtersActive ? (
              <button
                type="button"
                className="material-registry-button compact material-registry-toolbar__reset"
                onClick={() => {
                  setSearch("");
                  setStatusFilter("");
                  setQualityFilter("");
                  setExpiryFilter("");
                  setWarehouseFilter("");
                }}
              >
                <X aria-hidden="true" size={14} />
                Filtreleri Temizle
              </button>
            ) : null}
          </div>
        </div>

        <div className="material-registry-table-wrap">
          {filteredLots.length === 0 ? (
            <div className="material-registry-empty">
              <Package aria-hidden="true" />
              <p>Kayıtlı lot bulunamadı.</p>
            </div>
          ) : (
            <table className="material-registry-table">
              <thead>
                <tr>
                  <th>Lot</th>
                  <th>Malzeme</th>
                  <th>Tedarikçi/Üretici Lotu</th>
                  <th>Durum</th>
                  <th>Kalite</th>
                  <th>Depo/Konum</th>
                  <th>Gözlenen Miktar</th>
                  <th>SKT/Yeniden Test</th>
                  <th>Güncellik</th>
                  <th>İşlem</th>
                </tr>
              </thead>
              <tbody>
                {filteredLots.map((lot) => (
                  <tr
                    key={lot.id}
                    data-lot-id={lot.id}
                    data-selected={lot.id === selectedLotId}
                    tabIndex={0}
                    onClick={() => selectLot(lot)}
                    onKeyDown={(event) => {
                      if (event.key === "Enter" || event.key === " ") {
                        event.preventDefault();
                        selectLot(lot);
                      }
                    }}
                  >
                    <td>{lot.lotNumber}</td>
                    <td>
                      <span className="material-identity__name">{lot.materialName}</span>
                      <span className="material-identity__code">{lot.materialCode}</span>
                    </td>
                    <td>{supplierLotText(lot)}</td>
                    <td>
                      <span className={lotStatusBadgeClass(lot.status)}>
                        {getMaterialLotStatusLabel(lot.status)}
                      </span>
                    </td>
                    <td>{getMaterialLotQualityStatusLabel(lot.qualityStatus)}</td>
                    <td>{formatMaterialLotLocation(lot.currentWarehouseReference, lot.currentLocationReference)}</td>
                    <td>{formatMaterialQuantity(lot.currentObservedQuantity, lot.unitOfMeasure)}</td>
                    <td>{formatMaterialLotExpiry(lot.expiryDate, lot.retestDate, lot.isExpiryApproaching)}</td>
                    <td>{formatMaterialDateTime(lot.updatedAtUtc)}</td>
                    <td className="material-registry-row-action">
                      <button
                        type="button"
                        className="material-registry-link-action"
                        onClick={(event) => {
                          event.stopPropagation();
                          selectLot(lot);
                        }}
                      >
                        Detay
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </section>

      {selectedSummary ? (
        <section className="material-registry-panel material-lot-detail" aria-label="Lot detayı">
          <header className="material-detail-header">
            <div className="material-detail-header__identity">
              <div className="material-detail-header__title-row">
                <h3 className="material-detail-header__name">{selectedSummary.lotNumber}</h3>
                <span className={lotStatusBadgeClass(selectedSummary.status)}>
                  {getMaterialLotStatusLabel(selectedSummary.status)}
                </span>
              </div>
              <span className="material-detail-header__code">
                {materialCode} · {materialName}
              </span>
            </div>
          </header>

          {!activeLotDetail && isPending ? (
            <div className="material-registry-status">
              <LoaderCircle aria-hidden="true" size={16} />
              Lot detayı yükleniyor…
            </div>
          ) : null}

          {activeLotDetail ? (
            <>
              <div className="material-detail-tabs-rail">
                <div className="material-detail-tabs" role="tablist">
                  {LOT_DETAIL_SECTIONS.map(([value, label]) => (
                    <button
                      key={value}
                      type="button"
                      role="tab"
                      className="material-detail-tabs__button"
                      aria-selected={lotSection === value}
                      onClick={() => setLotSection(value)}
                    >
                      {label}
                    </button>
                  ))}
                </div>
              </div>

              {lotSection === "overview" ? (
                <article className="material-overview-card material-overview-card--wide">
                  <h4 className="material-overview-card__title">Genel Bakış</h4>
                  <dl className="material-registry-meta-grid">
                    <div>
                      <dt>Lot Numarası</dt>
                      <dd>{activeLotDetail.lotNumber}</dd>
                    </div>
                    <div>
                      <dt>Malzeme</dt>
                      <dd>
                        {activeLotDetail.materialCode} · {activeLotDetail.materialName}
                      </dd>
                    </div>
                    <div>
                      <dt>Durum</dt>
                      <dd>{getMaterialLotStatusLabel(activeLotDetail.status)}</dd>
                    </div>
                    <div>
                      <dt>Kalite</dt>
                      <dd>{getMaterialLotQualityStatusLabel(activeLotDetail.qualityStatus)}</dd>
                    </div>
                    <div>
                      <dt>Gözlenen Miktar</dt>
                      <dd>
                        {formatMaterialQuantity(
                          activeLotDetail.currentObservedQuantity,
                          activeLotDetail.unitOfMeasure,
                        )}
                      </dd>
                    </div>
                    <div>
                      <dt>SKT / Yeniden Test</dt>
                      <dd>
                        {formatMaterialLotExpiry(activeLotDetail.expiryDate, activeLotDetail.retestDate)}
                      </dd>
                    </div>
                    <div>
                      <dt>Son Güncelleme</dt>
                      <dd>{formatMaterialDateTime(activeLotDetail.updatedAtUtc)}</dd>
                    </div>
                  </dl>
                  <p className="material-lot-disclaimer">{MATERIAL_LOT_QUANTITY_DISCLAIMER}</p>
                </article>
              ) : null}

              {lotSection === "source" ? (
                <article className="material-overview-card material-overview-card--wide">
                  <h4 className="material-overview-card__title">Kaynak ve Kabul</h4>
                  <dl className="material-registry-meta-grid">
                    <div>
                      <dt>Tedarikçi Lotu</dt>
                      <dd>{activeLotDetail.supplierLotNumber ?? "—"}</dd>
                    </div>
                    <div>
                      <dt>Üretici Lotu</dt>
                      <dd>{activeLotDetail.manufacturerLotNumber ?? "—"}</dd>
                    </div>
                    <div>
                      <dt>Tedarikçi Referansı</dt>
                      <dd>{activeLotDetail.supplierReference ?? "—"}</dd>
                    </div>
                    <div>
                      <dt>Satın Alma Referansı</dt>
                      <dd>{activeLotDetail.purchaseOrderReference ?? "—"}</dd>
                    </div>
                    <div>
                      <dt>Mal Kabul Referansı</dt>
                      <dd>{activeLotDetail.goodsReceiptReference ?? "—"}</dd>
                    </div>
                    <div>
                      <dt>Kabul Tarihi</dt>
                      <dd>{formatMaterialDateTime(activeLotDetail.receivedAtUtc)}</dd>
                    </div>
                    <div>
                      <dt>Üretim Tarihi</dt>
                      <dd>{formatMaterialDateOnly(activeLotDetail.manufactureDate)}</dd>
                    </div>
                    <div>
                      <dt>Kaynak</dt>
                      <dd>{getMaterialSourceLabel(activeLotDetail.sourceType)}</dd>
                    </div>
                  </dl>
                </article>
              ) : null}

              {lotSection === "location" ? (
                <article className="material-overview-card material-overview-card--wide">
                  <h4 className="material-overview-card__title">Depo/Konum</h4>
                  <dl className="material-registry-meta-grid">
                    <div>
                      <dt>Depo</dt>
                      <dd>{activeLotDetail.currentWarehouseReference ?? "—"}</dd>
                    </div>
                    <div>
                      <dt>Konum</dt>
                      <dd>{activeLotDetail.currentLocationReference ?? "—"}</dd>
                    </div>
                    <div>
                      <dt>Gözlenen Miktar</dt>
                      <dd>
                        {formatMaterialQuantity(
                          activeLotDetail.currentObservedQuantity,
                          activeLotDetail.unitOfMeasure,
                        )}
                      </dd>
                    </div>
                  </dl>
                  <p className="material-lot-disclaimer">{MATERIAL_LOT_QUANTITY_DISCLAIMER}</p>
                </article>
              ) : null}

              {lotSection === "quality" ? (
                <article className="material-overview-card material-overview-card--wide">
                  <h4 className="material-overview-card__title">Kalite Durumu</h4>
                  <dl className="material-registry-meta-grid">
                    <div>
                      <dt>Kalite Durumu</dt>
                      <dd>{getMaterialLotQualityStatusLabel(activeLotDetail.qualityStatus)}</dd>
                    </div>
                    <div>
                      <dt>Lot Durumu</dt>
                      <dd>{getMaterialLotStatusLabel(activeLotDetail.status)}</dd>
                    </div>
                  </dl>
                </article>
              ) : null}

              {lotSection === "documents" ? (
                <article className="material-overview-card material-overview-card--wide">
                  <h4 className="material-overview-card__title">Belgeler</h4>
                  {(traceability?.forward?.certificates.length ?? 0) +
                    (traceability?.backward?.certificates.length ?? 0) >
                  0 ? (
                    <ul className="material-lot-document-list">
                      {[
                        ...(traceability?.backward?.certificates ?? []),
                        ...(traceability?.forward?.certificates ?? []),
                      ]
                        .filter(
                          (certificate, index, items) =>
                            items.findIndex((item) => item.id === certificate.id) === index,
                        )
                        .map((certificate) => (
                          <li key={certificate.id}>
                            <strong>{getMaterialLotCertificateTypeLabel(certificate.certificateType)}</strong>
                            <span>{certificate.documentReference}</span>
                            {certificate.documentNumber ? <em>{certificate.documentNumber}</em> : null}
                          </li>
                        ))}
                    </ul>
                  ) : (
                    <p className="material-form-hint">Bağlı belge referansı bulunmuyor.</p>
                  )}
                </article>
              ) : null}

              {lotSection === "timeline" ? (
                <article className="material-overview-card material-overview-card--wide">
                  <h4 className="material-overview-card__title">Zaman Çizelgesi</h4>
                  {timeline.length === 0 ? (
                    <p className="material-form-hint">Zaman çizelgesi kaydı bulunmuyor.</p>
                  ) : (
                    <div className="material-registry-history">
                      {timeline.map((entry) => (
                        <article key={entry.id} className="material-registry-history-item">
                          <strong>{getMaterialLotEventTypeLabel(entry.eventType)}</strong>
                          {entry.reason ? <p>{entry.reason}</p> : null}
                          {entry.quantity !== undefined ? (
                            <p>{formatMaterialQuantity(entry.quantity, entry.unitOfMeasure)}</p>
                          ) : null}
                          <small>
                            {entry.actorDisplayName} · {formatMaterialDateTime(entry.eventAtUtc)}
                          </small>
                        </article>
                      ))}
                    </div>
                  )}
                </article>
              ) : null}

              {lotSection === "traceability" ? (
                <MaterialLotTraceabilityPanel
                  traceability={traceability ?? null}
                  lotNumber={activeLotDetail.lotNumber}
                />
              ) : null}
            </>
          ) : !isPending ? (
            <div className="material-registry-error" role="alert">
              Lot detayı yüklenemedi.
            </div>
          ) : null}
        </section>
      ) : null}
    </div>
  );
}
