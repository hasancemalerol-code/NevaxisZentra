"use client";

import { useCallback, useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { ArrowRightLeft, Ship, Truck } from "lucide-react";

import { Badge } from "@/components/ui/badge";

import type {
  ExportShipment,
  ExportShipmentReadiness,
  ShipmentStatus,
} from "../domain/export-2l";
import {
  getShipmentStatusLabel,
  getTransportModeLabel,
  SHIPMENT_TRANSITION_OPTIONS,
  TRANSPORT_MODE_OPTIONS,
} from "../domain/export-2l";
import type {
  createShipmentAction,
  transitionShipmentAction,
  updateShipmentAction,
} from "../server/export-case-writer";

type BadgeTone =
  | "neutral"
  | "information"
  | "success"
  | "warning"
  | "danger"
  | "ai";

function shipmentTone(status: ShipmentStatus): BadgeTone {
  switch (status) {
    case "ready":
    case "cleared-for-dispatch":
    case "dispatched":
      return "success";
    case "handed-to-broker":
    case "preparing":
      return "information";
    case "cancelled":
      return "danger";
    case "closed":
      return "neutral";
    case "draft":
    default:
      return "warning";
  }
}

type PanelState =
  | { kind: "idle" }
  | { kind: "pending"; action: string }
  | { kind: "error"; message: string }
  | { kind: "success"; message: string };

type ExportShipmentPanelProps = Readonly<{
  exportCaseId: string;
  defaultDestinationCountryCode: string;
  defaultIncotermCode?: string;
  shipments: readonly ExportShipment[];
  shipmentReadinessById: Readonly<Record<string, ExportShipmentReadiness>>;
  onCreate: typeof createShipmentAction;
  onUpdate: typeof updateShipmentAction;
  onTransition: typeof transitionShipmentAction;
}>;

export function ExportShipmentPanel({
  exportCaseId,
  defaultDestinationCountryCode,
  defaultIncotermCode,
  shipments,
  shipmentReadinessById,
  onCreate,
  onUpdate,
  onTransition,
}: ExportShipmentPanelProps) {
  const router = useRouter();
  const [state, setState] = useState<PanelState>({ kind: "idle" });
  const [selectedId, setSelectedId] = useState<string | null>(
    shipments[0]?.id ?? null,
  );
  const [shipmentReference, setShipmentReference] = useState("");
  const [transportMode, setTransportMode] = useState("road");
  const [destinationCountryCode, setDestinationCountryCode] = useState(
    defaultDestinationCountryCode,
  );
  const [destinationCity, setDestinationCity] = useState("");
  const [forwarderName, setForwarderName] = useState("");
  const [carrierName, setCarrierName] = useState("");
  const [bookingReference, setBookingReference] = useState("");
  const [trackingReference, setTrackingReference] = useState(
    shipments[0]?.trackingReference ?? "",
  );
  const [editTransportMode, setEditTransportMode] = useState(
    shipments[0]?.transportMode ?? "road",
  );
  const [cancellationReason, setCancellationReason] = useState("");

  const isPending = state.kind === "pending";
  const selected =
    shipments.find((s) => s.id === selectedId) ?? shipments[0] ?? null;
  const readiness = selected
    ? (shipmentReadinessById[selected.id] ?? null)
    : null;

  const selectShipment = useCallback((shipment: ExportShipment) => {
    setSelectedId(shipment.id);
    setEditTransportMode(shipment.transportMode ?? "road");
    setTrackingReference(shipment.trackingReference ?? "");
  }, []);

  const handleCreate = useCallback(
    async (event: FormEvent) => {
      event.preventDefault();
      setState({ kind: "pending", action: "create" });

      const result = await onCreate(exportCaseId, {
        shipmentReference,
        transportMode,
        incotermCode: defaultIncotermCode,
        destinationCountryCode,
        destinationCity: destinationCity || undefined,
        forwarderName: forwarderName || undefined,
        carrierName: carrierName || undefined,
        bookingReference: bookingReference || undefined,
      });

      if (result.status === "failure") {
        setState({ kind: "error", message: result.message });
        return;
      }

      setShipmentReference("");
      setDestinationCity("");
      setForwarderName("");
      setCarrierName("");
      setBookingReference("");
      setSelectedId(result.value.shipmentId);
      setState({ kind: "success", message: "Sevkiyat oluşturuldu." });
      router.refresh();
    },
    [
      bookingReference,
      carrierName,
      defaultIncotermCode,
      destinationCity,
      destinationCountryCode,
      exportCaseId,
      forwarderName,
      onCreate,
      router,
      shipmentReference,
      transportMode,
    ],
  );

  const handleUpdate = useCallback(async () => {
    if (!selected) return;
    setState({ kind: "pending", action: "update" });

    const result = await onUpdate(exportCaseId, selected.id, {
      expectedVersion: selected.version,
      transportMode: editTransportMode,
      trackingReference: trackingReference || undefined,
      destinationCountryCode: selected.destinationCountryCode,
      destinationCity: selected.destinationCity,
      forwarderName: selected.forwarderName,
      carrierName: selected.carrierName,
      bookingReference: selected.bookingReference,
      incotermCode: selected.incotermCode,
    });

    if (result.status === "failure") {
      setState({ kind: "error", message: result.message });
      return;
    }

    setState({ kind: "success", message: "Sevkiyat güncellendi." });
    router.refresh();
  }, [
    editTransportMode,
    exportCaseId,
    onUpdate,
    router,
    selected,
    trackingReference,
  ]);

  const handleTransition = useCallback(
    async (targetStatus: ShipmentStatus) => {
      if (!selected) return;

      if (targetStatus === "cancelled" && !cancellationReason.trim()) {
        setState({
          kind: "error",
          message: "İptal nedeni zorunludur.",
        });
        return;
      }

      setState({ kind: "pending", action: `transition-${targetStatus}` });

      const result = await onTransition(exportCaseId, selected.id, {
        expectedVersion: selected.version,
        targetStatus,
        cancellationReason:
          targetStatus === "cancelled"
            ? cancellationReason.trim()
            : undefined,
      });

      if (result.status === "failure") {
        setState({ kind: "error", message: result.message });
        return;
      }

      setCancellationReason("");
      setState({
        kind: "success",
        message: `Durum: ${getShipmentStatusLabel(targetStatus)}`,
      });
      router.refresh();
    },
    [cancellationReason, exportCaseId, onTransition, router, selected],
  );

  const transitions = selected
    ? SHIPMENT_TRANSITION_OPTIONS[selected.status]
    : [];

  return (
    <section className="export-shipment-panel">
      <div className="export-shipment-panel__heading">
        <div>
          <p>SEVKİYAT</p>
          <h2>Sevkiyat planı</h2>
          <span>
            Sevkiyat oluşturun, alanları güncelleyin ve durum geçişlerini
            yönetin.
          </span>
        </div>
      </div>

      {state.kind === "error" && (
        <div
          className="export-shipment-panel__flash export-shipment-panel__flash--error"
          role="alert"
        >
          {state.message}
        </div>
      )}
      {state.kind === "success" && (
        <div
          className="export-shipment-panel__flash export-shipment-panel__flash--success"
          role="status"
        >
          {state.message}
        </div>
      )}

      <form className="export-shipment-panel__form" onSubmit={handleCreate}>
        <div className="export-shipment-panel__form-title">
          <Ship aria-hidden="true" />
          <strong>Yeni sevkiyat</strong>
        </div>
        <div className="export-shipment-panel__fields">
          <label>
            Referans
            <input
              className="input"
              value={shipmentReference}
              onChange={(e) => setShipmentReference(e.target.value)}
              disabled={isPending}
              required
            />
          </label>
          <label>
            Taşıma modu
            <select
              className="input"
              value={transportMode}
              onChange={(e) => setTransportMode(e.target.value)}
              disabled={isPending}
            >
              {TRANSPORT_MODE_OPTIONS.map((mode) => (
                <option key={mode} value={mode}>
                  {getTransportModeLabel(mode)}
                </option>
              ))}
            </select>
          </label>
          <label>
            Hedef ülke
            <input
              className="input"
              value={destinationCountryCode}
              onChange={(e) => setDestinationCountryCode(e.target.value)}
              disabled={isPending}
              required
            />
          </label>
          <label>
            Şehir
            <input
              className="input"
              value={destinationCity}
              onChange={(e) => setDestinationCity(e.target.value)}
              disabled={isPending}
            />
          </label>
          <label>
            Forwarder
            <input
              className="input"
              value={forwarderName}
              onChange={(e) => setForwarderName(e.target.value)}
              disabled={isPending}
            />
          </label>
          <label>
            Taşıyıcı
            <input
              className="input"
              value={carrierName}
              onChange={(e) => setCarrierName(e.target.value)}
              disabled={isPending}
            />
          </label>
          <label>
            Rezervasyon
            <input
              className="input"
              value={bookingReference}
              onChange={(e) => setBookingReference(e.target.value)}
              disabled={isPending}
            />
          </label>
        </div>
        <button
          type="submit"
          className="button button-primary button-small"
          disabled={isPending || !shipmentReference.trim()}
        >
          Sevkiyat oluştur
        </button>
      </form>

      {shipments.length === 0 ? (
        <div className="export-shipment-panel__empty" role="status">
          <Truck aria-hidden="true" />
          <p>Henüz sevkiyat yok.</p>
        </div>
      ) : (
        <>
          <ul className="export-shipment-panel__list">
            {shipments.map((shipment) => (
              <li key={shipment.id}>
                <button
                  type="button"
                  className={
                    selected?.id === shipment.id
                      ? "export-shipment-panel__list-item export-shipment-panel__list-item--active"
                      : "export-shipment-panel__list-item"
                  }
                  onClick={() => selectShipment(shipment)}
                >
                  <div>
                    <strong>{shipment.shipmentReference}</strong>
                    <span>
                      {shipment.transportMode
                        ? getTransportModeLabel(shipment.transportMode)
                        : "Mod yok"}{" "}
                      · {shipment.destinationCountryCode}
                    </span>
                  </div>
                  <Badge tone={shipmentTone(shipment.status)}>
                    {getShipmentStatusLabel(shipment.status)}
                  </Badge>
                </button>
              </li>
            ))}
          </ul>

          {selected ? (
            <div className="export-shipment-panel__detail">
              <div className="export-shipment-panel__detail-header">
                <h3>{selected.shipmentReference}</h3>
                <Badge tone={shipmentTone(selected.status)}>
                  {getShipmentStatusLabel(selected.status)} · v
                  {selected.version}
                </Badge>
              </div>

              <div className="export-shipment-panel__fields">
                <label>
                  Taşıma modu
                  <select
                    className="input"
                    value={editTransportMode}
                    onChange={(e) => setEditTransportMode(e.target.value)}
                    disabled={isPending}
                  >
                    {TRANSPORT_MODE_OPTIONS.map((mode) => (
                      <option key={mode} value={mode}>
                        {getTransportModeLabel(mode)}
                      </option>
                    ))}
                  </select>
                </label>
                <label>
                  Takip no
                  <input
                    className="input"
                    value={trackingReference}
                    onChange={(e) => setTrackingReference(e.target.value)}
                    disabled={isPending}
                  />
                </label>
              </div>

              <button
                type="button"
                className="button button-secondary button-small"
                disabled={isPending}
                onClick={() => void handleUpdate()}
              >
                Alanları kaydet
              </button>

              {readiness ? (
                <div className="export-shipment-panel__readiness">
                  <strong>
                    Sevkiyat hazırlığı · %{readiness.readinessPercent}
                  </strong>
                  <span>
                    {readiness.blockingIssueCount} blokaj ·{" "}
                    {readiness.missingRequirementCount} eksik
                  </span>
                  <ul>
                    {readiness.requirements.map((req) => (
                      <li key={req.key}>
                        <span>{req.label}</span>
                        <em>{req.isSatisfied ? "Tamam" : "Eksik"}</em>
                      </li>
                    ))}
                  </ul>
                </div>
              ) : null}

              {transitions.length > 0 ? (
                <div className="export-shipment-panel__transitions">
                  <div className="export-shipment-panel__form-title">
                    <ArrowRightLeft aria-hidden="true" />
                    <strong>Durum geçişi</strong>
                  </div>
                  {transitions.includes("cancelled") ? (
                    <label>
                      İptal nedeni
                      <input
                        className="input"
                        value={cancellationReason}
                        onChange={(e) => setCancellationReason(e.target.value)}
                        disabled={isPending}
                        placeholder="İptal için zorunlu"
                      />
                    </label>
                  ) : null}
                  <div className="export-shipment-panel__transition-buttons">
                    {transitions.map((target) => (
                      <button
                        key={target}
                        type="button"
                        className={
                          target === "cancelled"
                            ? "button button-destructive button-small"
                            : "button button-primary button-small"
                        }
                        disabled={isPending}
                        onClick={() => void handleTransition(target)}
                      >
                        {getShipmentStatusLabel(target)}
                      </button>
                    ))}
                  </div>
                </div>
              ) : null}
            </div>
          ) : null}
        </>
      )}
    </section>
  );
}
