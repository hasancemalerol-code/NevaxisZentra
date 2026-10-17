"use client";

import { useCallback, useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import {
  AlertTriangle,
  Boxes,
  CheckCircle2,
  PackagePlus,
  ShieldCheck,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";

import type {
  ExportPackingReconciliation,
  ExportPackingUnitSummary,
  ExportPackingWorkspace,
  PackingStatus,
} from "../domain/export-2l";
import {
  getPackingReconciliationStatusLabel,
  getPackingStatusLabel,
  getPackingUnitTypeLabel,
  PACKING_UNIT_TYPE_OPTIONS,
} from "../domain/export-2l";
import type {
  addPackingItemAction,
  completePackingAction,
  createPackingUnitAction,
  verifyPackingAction,
} from "../server/export-case-writer";
import { customerMessage } from "@/lib/presentation/customer-message";

type BadgeTone =
  | "neutral"
  | "information"
  | "success"
  | "warning"
  | "danger"
  | "ai";

function packingTone(status: PackingStatus): BadgeTone {
  switch (status) {
    case "verified":
      return "success";
    case "complete":
      return "information";
    case "packing":
      return "warning";
    case "draft":
    default:
      return "neutral";
  }
}

type PanelState =
  | { kind: "idle" }
  | { kind: "pending"; action: string }
  | { kind: "error"; message: string }
  | { kind: "success"; message: string };

type ExportPackingPanelProps = Readonly<{
  exportCaseId: string;
  caseVersion: number;
  packing: ExportPackingWorkspace | null;
  reconciliation: ExportPackingReconciliation | null;
  onCreateUnit: typeof createPackingUnitAction;
  onAddItem: typeof addPackingItemAction;
  onComplete: typeof completePackingAction;
  onVerify: typeof verifyPackingAction;
}>;

export function ExportPackingPanel({
  exportCaseId,
  caseVersion,
  packing,
  reconciliation,
  onCreateUnit,
  onAddItem,
  onComplete,
  onVerify,
}: ExportPackingPanelProps) {
  const router = useRouter();
  const [state, setState] = useState<PanelState>({ kind: "idle" });
  const [unitCode, setUnitCode] = useState("");
  const [unitType, setUnitType] = useState("pallet");
  const [grossWeight, setGrossWeight] = useState("");
  const [itemUnitId, setItemUnitId] = useState("");
  const [productCode, setProductCode] = useState("");
  const [productName, setProductName] = useState("");
  const [quantity, setQuantity] = useState("");
  const [unitOfMeasure, setUnitOfMeasure] = useState("adet");

  const isPending = state.kind === "pending";
  const units = packing?.units ?? [];
  const packingStatus = packing?.packingStatus ?? "draft";

  const showReconciliationWarning =
    reconciliation !== null &&
    (reconciliation.reconciliationStatus === "incomplete" ||
      reconciliation.reconciliationStatus === "overpacked" ||
      reconciliation.reconciliationStatus === "mismatch");

  const handleCreateUnit = useCallback(
    async (event: FormEvent) => {
      event.preventDefault();
      setState({ kind: "pending", action: "create-unit" });

      const result = await onCreateUnit(exportCaseId, {
        unitCode,
        unitType,
        grossWeight: grossWeight.trim()
          ? Number(grossWeight)
          : undefined,
      });

      if (result.status === "failure") {
        setState({ kind: "error", message: result.message });
        return;
      }

      setUnitCode("");
      setGrossWeight("");
      setState({ kind: "success", message: "Paket birimi eklendi." });
      router.refresh();
    },
    [exportCaseId, grossWeight, onCreateUnit, router, unitCode, unitType],
  );

  const handleAddItem = useCallback(
    async (event: FormEvent) => {
      event.preventDefault();
      if (!itemUnitId) {
        setState({ kind: "error", message: "Paket birimi seçin." });
        return;
      }

      setState({ kind: "pending", action: "add-item" });

      const result = await onAddItem(exportCaseId, itemUnitId, {
        productCode,
        productName,
        quantity: Number(quantity),
        unitOfMeasure,
      });

      if (result.status === "failure") {
        setState({ kind: "error", message: result.message });
        return;
      }

      setProductCode("");
      setProductName("");
      setQuantity("");
      setState({ kind: "success", message: "Kalem eklendi." });
      router.refresh();
    },
    [
      exportCaseId,
      itemUnitId,
      onAddItem,
      productCode,
      productName,
      quantity,
      router,
      unitOfMeasure,
    ],
  );

  const handleComplete = useCallback(async () => {
    const expectedCaseVersion = caseVersion;
    setState({ kind: "pending", action: "complete" });
    const result = await onComplete(exportCaseId, expectedCaseVersion);
    if (result.status === "failure") {
      setState({ kind: "error", message: result.message });
      return;
    }
    setState({ kind: "success", message: "Paketleme tamamlandı." });
    router.refresh();
  }, [caseVersion, exportCaseId, onComplete, router]);

  const handleVerify = useCallback(async () => {
    const expectedCaseVersion = caseVersion;
    setState({ kind: "pending", action: "verify" });
    const result = await onVerify(exportCaseId, expectedCaseVersion);
    if (result.status === "failure") {
      setState({ kind: "error", message: result.message });
      return;
    }
    setState({ kind: "success", message: "Paketleme doğrulandı." });
    router.refresh();
  }, [caseVersion, exportCaseId, onVerify, router]);

  return (
    <section className="export-packing-panel">
      <div className="export-packing-panel__heading">
        <div>
          <p>PAKETLEME</p>
          <h2>Paketleme çalışma alanı</h2>
          <span>
            Birimleri ve kalemleri yönetin. Tamamlama ve doğrulama dosya sürümü
            ile korunur.
          </span>
        </div>
        <Badge tone={packingTone(packingStatus)}>
          {getPackingStatusLabel(packingStatus)}
        </Badge>
      </div>

      <div className="export-packing-panel__summary">
        <div>
          <span>Birim</span>
          <strong>{units.length}</strong>
        </div>
        <div>
          <span>Kalem</span>
          <strong>
            {units.reduce((sum, unit) => sum + unit.items.length, 0)}
          </strong>
        </div>
        <div>
          <span>Dosya sürümü</span>
          <strong>v{caseVersion}</strong>
        </div>
        {reconciliation ? (
          <div>
            <span>Mutabakat</span>
            <strong>
              {getPackingReconciliationStatusLabel(
                reconciliation.reconciliationStatus,
              )}
            </strong>
          </div>
        ) : null}
      </div>

      {showReconciliationWarning && reconciliation ? (
        <div className="export-packing-panel__warning" role="status">
          <AlertTriangle aria-hidden="true" />
          <div>
            <strong>Mutabakat uyarısı</strong>
            <p>
              {reconciliation.explanation ??
                "Sipariş miktarı ile paketlenen miktar uyuşmuyor."}
            </p>
          </div>
        </div>
      ) : null}

      {state.kind === "error" && (
        <div
          className="export-packing-panel__flash export-packing-panel__flash--error"
          role="alert"
        >
          {customerMessage(state.message)}
        </div>
      )}
      {state.kind === "success" && (
        <div
          className="export-packing-panel__flash export-packing-panel__flash--success"
          role="status"
        >
          {customerMessage(state.message)}
        </div>
      )}

      <form className="export-packing-panel__form" onSubmit={handleCreateUnit}>
        <div className="export-packing-panel__form-title">
          <PackagePlus aria-hidden="true" />
          <strong>Paket birimi ekle</strong>
        </div>
        <div className="export-packing-panel__fields">
          <label>
            Birim kodu <span className="zx-required" aria-hidden="true">*</span>
            <input
              className="input"
              value={unitCode}
              onChange={(e) => setUnitCode(e.target.value)}
              disabled={isPending}
              required
            />
          </label>
          <label>
            Tip
            <select
              className="input"
              value={unitType}
              onChange={(e) => setUnitType(e.target.value)}
              disabled={isPending}
            >
              {PACKING_UNIT_TYPE_OPTIONS.map((type) => (
                <option key={type} value={type}>
                  {getPackingUnitTypeLabel(type)}
                </option>
              ))}
            </select>
          </label>
          <label>
            Brüt ağırlık
            <input
              className="input"
              type="number"
              step="any"
              value={grossWeight}
              onChange={(e) => setGrossWeight(e.target.value)}
              disabled={isPending}
            />
          </label>
        </div>
        <button
          type="submit"
          className="button button-primary button-small"
          disabled={isPending || !unitCode.trim()}
        >
          Birim ekle
        </button>
      </form>

      {units.length > 0 ? (
        <form className="export-packing-panel__form" onSubmit={handleAddItem}>
          <div className="export-packing-panel__form-title">
            <Boxes aria-hidden="true" />
            <strong>Kalem ekle</strong>
          </div>
          <div className="export-packing-panel__fields">
            <label>
              Birim <span className="zx-required" aria-hidden="true">*</span>
              <select
                className="input"
                value={itemUnitId}
                onChange={(e) => setItemUnitId(e.target.value)}
                disabled={isPending}
                required
              >
                <option value="">Seçin</option>
                {units.map((unit) => (
                  <option key={unit.id} value={unit.id}>
                    {unit.unitCode}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Ürün kodu <span className="zx-required" aria-hidden="true">*</span>
              <input
                className="input"
                value={productCode}
                onChange={(e) => setProductCode(e.target.value)}
                disabled={isPending}
                required
              />
            </label>
            <label>
              Ürün adı <span className="zx-required" aria-hidden="true">*</span>
              <input
                className="input"
                value={productName}
                onChange={(e) => setProductName(e.target.value)}
                disabled={isPending}
                required
              />
            </label>
            <label>
              Miktar <span className="zx-required" aria-hidden="true">*</span>
              <input
                className="input"
                type="number"
                step="any"
                value={quantity}
                onChange={(e) => setQuantity(e.target.value)}
                disabled={isPending}
                required
              />
            </label>
            <label>
              Birim <span className="zx-required" aria-hidden="true">*</span>
              <input
                className="input"
                value={unitOfMeasure}
                onChange={(e) => setUnitOfMeasure(e.target.value)}
                disabled={isPending}
                required
              />
            </label>
          </div>
          <button
            type="submit"
            className="button button-primary button-small"
            disabled={isPending}
          >
            Kalem ekle
          </button>
        </form>
      ) : null}

      <div className="export-packing-panel__actions">
        {(packingStatus === "draft" || packingStatus === "packing") && (
          <button
            type="button"
            className="button button-primary button-small"
            disabled={isPending || units.length === 0}
            onClick={() => void handleComplete()}
          >
            <CheckCircle2 aria-hidden="true" />
            Paketlemeyi tamamla
          </button>
        )}
        {packingStatus === "complete" && (
          <button
            type="button"
            className="button button-primary button-small"
            disabled={isPending}
            onClick={() => void handleVerify()}
          >
            <ShieldCheck aria-hidden="true" />
            Paketlemeyi doğrula
          </button>
        )}
      </div>

      {units.length === 0 ? (
        <div className="export-packing-panel__empty" role="status">
          <Boxes aria-hidden="true" />
          <p>Henüz paket birimi yok.</p>
        </div>
      ) : (
        <ul className="export-packing-panel__units">
          {units.map((unit) => (
            <PackingUnitRow key={unit.id} unit={unit} />
          ))}
        </ul>
      )}
    </section>
  );
}

function PackingUnitRow({ unit }: { unit: ExportPackingUnitSummary }) {
  return (
    <li className="export-packing-panel__unit">
      <div className="export-packing-panel__unit-main">
        <Boxes aria-hidden="true" />
        <div>
          <strong>{unit.unitCode}</strong>
          <span>
            {getPackingUnitTypeLabel(unit.unitType)} · v{unit.version}
            {unit.grossWeight != null
              ? ` · ${unit.grossWeight}${unit.weightUnit ? ` ${unit.weightUnit}` : ""}`
              : ""}
          </span>
        </div>
        <Badge tone={packingTone(unit.status)}>
          {getPackingStatusLabel(unit.status)}
        </Badge>
      </div>
      {unit.items.length > 0 ? (
        <ul className="export-packing-panel__items">
          {unit.items.map((item) => (
            <li key={item.id}>
              <strong>
                {item.productCode} — {item.productName}
              </strong>
              <span>
                {item.quantity} {item.unitOfMeasure}
                {item.lotBatchReference ? ` · Lot ${item.lotBatchReference}` : ""}
              </span>
            </li>
          ))}
        </ul>
      ) : (
        <p className="export-packing-panel__no-items">Kalem yok</p>
      )}
    </li>
  );
}
