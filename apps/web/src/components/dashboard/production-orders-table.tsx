import {
  AlertTriangle,
  CheckCircle2,
  Clock3,
  ListChecks,
  Settings2,
  type LucideIcon,
} from "lucide-react";

import type { CSSProperties } from "react";
import Link from "next/link";

import { hasDashboardPermission } from "@/components/dashboard/dashboard-permission";

interface ProductionOrdersTableProps {
  grantedPermissionKeys?: readonly string[];
  orders: readonly unknown[];
}

type ProductionOrderState =
  | "on-track"
  | "attention"
  | "delayed"
  | "setup";

type ProductionOrderRisk =
  | "critical"
  | "high"
  | "medium"
  | "low";

interface NormalizedProductionOrder {
  id: string;
  orderNumber: string;
  productName: string;
  moldCode: string;
  machineCode: string;
  producedQuantity: number;
  plannedQuantity: number;
  dueLabel: string;
  status: ProductionOrderState;
  statusLabel: string;
  riskLevel: ProductionOrderRisk;
}

const fallbackOrders: NormalizedProductionOrder[] = [
  {
    id: "order-preview-1",
    orderNumber: "UE-260804-01",
    productName: "P-104 Kapak Gövdesi",
    moldCode: "KLP-104",
    machineCode: "M-01",
    producedQuantity: 8420,
    plannedQuantity: 10000,
    dueLabel: "Bugün 23:30",
    status: "on-track",
    statusLabel: "Plana Uygun",
    riskLevel: "low",
  },
  {
    id: "order-preview-2",
    orderNumber: "UE-260804-03",
    productName: "P-118 Bağlantı Parçası",
    moldCode: "KLP-118",
    machineCode: "M-02",
    producedQuantity: 6180,
    plannedQuantity: 8500,
    dueLabel: "Yarın 06:00",
    status: "attention",
    statusLabel: "İzleniyor",
    riskLevel: "medium",
  },
  {
    id: "order-preview-3",
    orderNumber: "UE-260804-05",
    productName: "P-205 Teknik Parça",
    moldCode: "KLP-205",
    machineCode: "M-04",
    producedQuantity: 3120,
    plannedQuantity: 9000,
    dueLabel: "6 saat risk",
    status: "delayed",
    statusLabel: "Gecikiyor",
    riskLevel: "critical",
  },
  {
    id: "order-preview-4",
    orderNumber: "UE-260804-07",
    productName: "P-212 Muhafaza",
    moldCode: "KLP-212",
    machineCode: "M-03",
    producedQuantity: 0,
    plannedQuantity: 7200,
    dueLabel: "Yarın 14:00",
    status: "setup",
    statusLabel: "Hazırlık",
    riskLevel: "medium",
  },
  {
    id: "order-preview-5",
    orderNumber: "UE-260804-09",
    productName: "P-220 Taşıyıcı Gövde",
    moldCode: "KLP-220",
    machineCode: "M-06",
    producedQuantity: 5740,
    plannedQuantity: 7600,
    dueLabel: "Yarın 18:30",
    status: "on-track",
    statusLabel: "Plana Uygun",
    riskLevel: "low",
  },
];

const styles: Record<string, CSSProperties> = {
  panel: {
    display: "flex",
    minWidth: 0,
    flexDirection: "column",
    overflow: "hidden",
  },
  wrapper: {
    minWidth: 0,
    minHeight: 0,
    flex: "1 1 auto",
    overflow: "hidden",
  },
  table: {
    width: "100%",
    minWidth: 0,
    borderCollapse: "collapse",
    tableLayout: "fixed",
  },
  headerCell: {
    padding: "7px 7px",
    overflow: "hidden",
    borderBottom: "1px solid #dce5f0",
    background: "#f8fafc",
    color: "#75839a",
    fontSize: "0.39rem",
    fontWeight: 760,
    letterSpacing: "0.02em",
    textAlign: "left",
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  bodyCell: {
    padding: "8px 7px",
    overflow: "hidden",
    borderBottom: "1px solid #dce5f0",
    color: "#52627a",
    fontSize: "0.42rem",
    textAlign: "left",
    textOverflow: "ellipsis",
    verticalAlign: "middle",
    whiteSpace: "nowrap",
  },
  orderNumber: {
    color: "#10213d",
    fontSize: "0.43rem",
    fontWeight: 800,
  },
  product: {
    display: "grid",
    minWidth: 0,
    gap: 2,
  },
  productName: {
    overflow: "hidden",
    color: "#10213d",
    fontSize: "0.43rem",
    fontWeight: 760,
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  moldCode: {
    overflow: "hidden",
    color: "#75839a",
    fontSize: "0.34rem",
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  machine: {
    display: "inline-flex",
    minHeight: 20,
    alignItems: "center",
    justifyContent: "center",
    padding: "3px 6px",
    border: "1px solid #dce5f0",
    borderRadius: 6,
    background: "#f8fafc",
    color: "#10213d",
    fontSize: "0.38rem",
    fontWeight: 760,
  },
  progress: {
    minWidth: 0,
  },
  progressTop: {
    display: "flex",
    minWidth: 0,
    alignItems: "center",
    justifyContent: "space-between",
    gap: 6,
    marginBottom: 5,
  },
  progressLabel: {
    overflow: "hidden",
    color: "#75839a",
    fontSize: "0.35rem",
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  progressValue: {
    color: "#1769e0",
    fontSize: "0.38rem",
    fontWeight: 800,
    whiteSpace: "nowrap",
  },
  track: {
    overflow: "hidden",
    height: 4,
    borderRadius: 999,
    background: "#dce5f0",
  },
  bar: {
    display: "block",
    height: "100%",
    borderRadius: 999,
    background: "#1769e0",
  },
  due: {
    color: "#52627a",
    fontSize: "0.39rem",
    fontWeight: 650,
  },
  badge: {
    display: "inline-flex",
    alignItems: "center",
    gap: 4,
    padding: "4px 5px",
    borderRadius: 6,
    fontSize: "0.35rem",
    fontWeight: 750,
    lineHeight: 1,
    whiteSpace: "nowrap",
  },
  riskDot: {
    width: 5,
    height: 5,
    borderRadius: "50%",
    background: "currentColor",
  },
  action: {
    display: "inline-flex",
    minHeight: 29,
    flex: "0 0 29px",
    alignItems: "center",
    justifyContent: "center",
    padding: "0 10px",
    border: 0,
    borderTop: "1px solid #dce5f0",
    background: "transparent",
    color: "#1769e0",
    font: "inherit",
    fontSize: "0.42rem",
    fontWeight: 750,
    cursor: "not-allowed",
    opacity: 0.9,
  },
};

function asRecord(value: unknown): Record<string, unknown> {
  return typeof value === "object" && value !== null
    ? (value as Record<string, unknown>)
    : {};
}

function readString(
  source: Record<string, unknown>,
  keys: string[],
  fallback: string,
) {
  for (const key of keys) {
    const value = source[key];

    if (typeof value === "string" && value.trim().length > 0) {
      return value;
    }
  }

  return fallback;
}

function readNumber(
  source: Record<string, unknown>,
  keys: string[],
  fallback: number,
) {
  for (const key of keys) {
    const value = source[key];

    if (typeof value === "number" && Number.isFinite(value)) {
      return value;
    }
  }

  return fallback;
}

function normalizeStatus(
  source: Record<string, unknown>,
  fallback: ProductionOrderState,
): ProductionOrderState {
  const rawValue = readString(
    source,
    ["status", "state", "statusCode"],
    fallback,
  ).toLocaleLowerCase("tr-TR");

  if (
    rawValue.includes("delay") ||
    rawValue.includes("gecik")
  ) {
    return "delayed";
  }

  if (
    rawValue.includes("setup") ||
    rawValue.includes("hazırlık") ||
    rawValue.includes("ayar")
  ) {
    return "setup";
  }

  if (
    rawValue.includes("attention") ||
    rawValue.includes("izlen") ||
    rawValue.includes("dikkat")
  ) {
    return "attention";
  }

  return "on-track";
}

function normalizeRisk(
  source: Record<string, unknown>,
  fallback: ProductionOrderRisk,
): ProductionOrderRisk {
  const rawValue = readString(
    source,
    ["riskLevel", "risk", "severity", "priority"],
    fallback,
  ).toLocaleLowerCase("tr-TR");

  if (
    rawValue.includes("critical") ||
    rawValue.includes("kritik")
  ) {
    return "critical";
  }

  if (
    rawValue.includes("high") ||
    rawValue.includes("yüksek")
  ) {
    return "high";
  }

  if (
    rawValue.includes("low") ||
    rawValue.includes("düşük")
  ) {
    return "low";
  }

  return "medium";
}

function getStatusLabel(status: ProductionOrderState) {
  switch (status) {
    case "on-track":
      return "Plana Uygun";
    case "attention":
      return "İzleniyor";
    case "delayed":
      return "Gecikiyor";
    case "setup":
      return "Hazırlık";
  }
}

function getStatusIcon(
  status: ProductionOrderState,
): LucideIcon {
  switch (status) {
    case "on-track":
      return CheckCircle2;
    case "attention":
      return AlertTriangle;
    case "delayed":
      return Clock3;
    case "setup":
      return Settings2;
  }
}

function getStatusStyle(
  status: ProductionOrderState,
): CSSProperties {
  switch (status) {
    case "on-track":
      return {
        background: "#e8f8f1",
        color: "#08945f",
      };
    case "attention":
    case "setup":
      return {
        background: "#fff3df",
        color: "#dc7a00",
      };
    case "delayed":
      return {
        background: "#ffecef",
        color: "#dc3545",
      };
  }
}

function getRiskLabel(risk: ProductionOrderRisk) {
  switch (risk) {
    case "critical":
      return "Kritik";
    case "high":
      return "Yüksek";
    case "medium":
      return "Orta";
    case "low":
      return "Düşük";
  }
}

function getRiskStyle(
  risk: ProductionOrderRisk,
): CSSProperties {
  switch (risk) {
    case "critical":
    case "high":
      return {
        background: "#ffecef",
        color: "#dc3545",
      };
    case "medium":
      return {
        background: "#fff3df",
        color: "#dc7a00",
      };
    case "low":
      return {
        background: "#e8f8f1",
        color: "#08945f",
      };
  }
}

function normalizeOrder(
  value: unknown,
  index: number,
): NormalizedProductionOrder {
  const source = asRecord(value);
  const fallback =
    fallbackOrders[index] ??
    fallbackOrders[fallbackOrders.length - 1];

  const status = normalizeStatus(source, fallback.status);
  const riskLevel = normalizeRisk(
    source,
    fallback.riskLevel,
  );

  return {
    id: readString(
      source,
      ["id", "orderId", "orderNumber", "code"],
      `production-order-${index + 1}`,
    ),
    orderNumber: readString(
      source,
      ["orderNumber", "code", "productionOrderCode"],
      fallback.orderNumber,
    ),
    productName: readString(
      source,
      ["productName", "productLabel", "name"],
      fallback.productName,
    ),
    moldCode: readString(
      source,
      ["moldCode", "toolCode"],
      fallback.moldCode,
    ),
    machineCode: readString(
      source,
      ["machineCode", "assetCode"],
      fallback.machineCode,
    ),
    producedQuantity: Math.max(
      0,
      readNumber(
        source,
        ["producedQuantity", "completedQuantity", "actualQuantity"],
        fallback.producedQuantity,
      ),
    ),
    plannedQuantity: Math.max(
      0,
      readNumber(
        source,
        ["plannedQuantity", "targetQuantity", "quantity"],
        fallback.plannedQuantity,
      ),
    ),
    dueLabel: readString(
      source,
      ["dueLabel", "deadlineLabel", "dueDateLabel"],
      fallback.dueLabel,
    ),
    status,
    statusLabel: readString(
      source,
      ["statusLabel", "stateLabel"],
      getStatusLabel(status),
    ),
    riskLevel,
  };
}

function formatQuantity(value: number) {
  return new Intl.NumberFormat("tr-TR").format(value);
}

function calculateProgress(order: NormalizedProductionOrder) {
  if (order.plannedQuantity <= 0) {
    return 0;
  }

  return Math.min(
    100,
    Math.round(
      (order.producedQuantity / order.plannedQuantity) * 100,
    ),
  );
}

export function ProductionOrdersTable({
  grantedPermissionKeys = [],
  orders,
}: ProductionOrdersTableProps) {
  const canOpenProduction = hasDashboardPermission(
    grantedPermissionKeys,
    "production.execution.read",
  );
  const sourceOrders =
    orders.length > 0 ? orders : fallbackOrders;

  const visibleOrders = sourceOrders
    .slice(0, 5)
    .map(normalizeOrder);

  return (
    <section
      aria-labelledby="production-orders-title"
      className="dashboard-panel dashboard-panel--production-orders"
      style={styles.panel}
    >
      <div className="dashboard-section-heading">
        <div>
          <p className="dashboard-section-heading__eyebrow">
            Devam eden üretim akışı
          </p>

          <h2
            className="dashboard-section-heading__title"
            id="production-orders-title"
          >
            Aktif Üretim Emirleri
          </h2>
        </div>

        <div className="dashboard-section-heading__meta">
          <ListChecks
            aria-hidden="true"
            size={16}
            strokeWidth={1.8}
          />
          {visibleOrders.length} aktif emir
        </div>
      </div>

      <div style={styles.wrapper}>
        <table style={styles.table}>
          <thead>
            <tr>
              <th style={{ ...styles.headerCell, width: "12%" }}>
                Emir
              </th>
              <th style={{ ...styles.headerCell, width: "22%" }}>
                Ürün
              </th>
              <th style={{ ...styles.headerCell, width: "10%" }}>
                Makine
              </th>
              <th style={{ ...styles.headerCell, width: "24%" }}>
                Gerçekleşme
              </th>
              <th style={{ ...styles.headerCell, width: "12%" }}>
                Termin
              </th>
              <th style={{ ...styles.headerCell, width: "12%" }}>
                Durum
              </th>
              <th style={{ ...styles.headerCell, width: "8%" }}>
                Risk
              </th>
            </tr>
          </thead>

          <tbody>
            {visibleOrders.map((order, index) => {
              const progress = calculateProgress(order);
              const StatusIcon = getStatusIcon(order.status);

              return (
                <tr key={order.id}>
                  <td
                    style={{
                      ...styles.bodyCell,
                      borderBottom:
                        index === visibleOrders.length - 1
                          ? 0
                          : styles.bodyCell.borderBottom,
                    }}
                  >
                    <strong style={styles.orderNumber}>
                      {order.orderNumber}
                    </strong>
                  </td>

                  <td
                    style={{
                      ...styles.bodyCell,
                      borderBottom:
                        index === visibleOrders.length - 1
                          ? 0
                          : styles.bodyCell.borderBottom,
                    }}
                  >
                    <div style={styles.product}>
                      <strong style={styles.productName}>
                        {order.productName}
                      </strong>

                      <span style={styles.moldCode}>
                        Kalıp {order.moldCode}
                      </span>
                    </div>
                  </td>

                  <td
                    style={{
                      ...styles.bodyCell,
                      borderBottom:
                        index === visibleOrders.length - 1
                          ? 0
                          : styles.bodyCell.borderBottom,
                    }}
                  >
                    <span style={styles.machine}>
                      {order.machineCode}
                    </span>
                  </td>

                  <td
                    style={{
                      ...styles.bodyCell,
                      borderBottom:
                        index === visibleOrders.length - 1
                          ? 0
                          : styles.bodyCell.borderBottom,
                    }}
                  >
                    <div style={styles.progress}>
                      <div style={styles.progressTop}>
                        <span style={styles.progressLabel}>
                          {formatQuantity(order.producedQuantity)}
                          {" / "}
                          {formatQuantity(order.plannedQuantity)}
                        </span>

                        <strong style={styles.progressValue}>
                          %{progress}
                        </strong>
                      </div>

                      <div
                        aria-label={`${order.orderNumber} üretim ilerlemesi yüzde ${progress}`}
                        aria-valuemax={100}
                        aria-valuemin={0}
                        aria-valuenow={progress}
                        role="progressbar"
                        style={styles.track}
                      >
                        <span
                          style={{
                            ...styles.bar,
                            width: `${progress}%`,
                          }}
                        />
                      </div>
                    </div>
                  </td>

                  <td
                    style={{
                      ...styles.bodyCell,
                      borderBottom:
                        index === visibleOrders.length - 1
                          ? 0
                          : styles.bodyCell.borderBottom,
                    }}
                  >
                    <span style={styles.due}>
                      {order.dueLabel}
                    </span>
                  </td>

                  <td
                    style={{
                      ...styles.bodyCell,
                      borderBottom:
                        index === visibleOrders.length - 1
                          ? 0
                          : styles.bodyCell.borderBottom,
                    }}
                  >
                    <span
                      style={{
                        ...styles.badge,
                        ...getStatusStyle(order.status),
                      }}
                    >
                      <StatusIcon
                        aria-hidden="true"
                        size={11}
                        strokeWidth={1.8}
                      />
                      {order.statusLabel}
                    </span>
                  </td>

                  <td
                    style={{
                      ...styles.bodyCell,
                      borderBottom:
                        index === visibleOrders.length - 1
                          ? 0
                          : styles.bodyCell.borderBottom,
                    }}
                  >
                    <span
                      style={{
                        ...styles.badge,
                        ...getRiskStyle(order.riskLevel),
                      }}
                    >
                      <span
                        aria-hidden="true"
                        style={styles.riskDot}
                      />
                      {getRiskLabel(order.riskLevel)}
                    </span>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      {canOpenProduction ? (
        <Link
          href="/uretim"
          style={{
            ...styles.action,
            cursor: "pointer",
            textDecoration: "none",
          }}
        >
          Tüm üretim emirlerini görüntüle
        </Link>
      ) : (
        <button
          disabled
          style={styles.action}
          title="Üretim emirlerini görüntüleme yetkiniz yok."
          type="button"
        >
          Tüm üretim emirlerini görüntüle
        </button>
      )}
    </section>
  );
}