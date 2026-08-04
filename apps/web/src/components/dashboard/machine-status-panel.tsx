import {
  Activity,
  AlertTriangle,
  Clock3,
  Factory,
  Gauge,
  Settings2,
  Wrench,
  type LucideIcon,
} from "lucide-react";

import type { CSSProperties } from "react";

interface MachineStatusPanelProps {
  machines: readonly unknown[];
}

type MachineState =
  | "producing"
  | "waiting"
  | "setup"
  | "maintenance"
  | "fault"
  | "offline";

interface NormalizedMachine {
  id: string;
  code: string;
  name: string;
  state: MachineState;
  stateLabel: string;
  jobLabel: string;
  productLabel: string;
  utilization: number;
  cycleLabel: string;
  outputLabel: string;
  icon: LucideIcon;
}

const fallbackMachines: NormalizedMachine[] = [
  {
    id: "machine-m01",
    code: "M-01",
    name: "Haitian Mars III",
    state: "producing",
    stateLabel: "Üretimde",
    jobLabel: "UE-260804-01",
    productLabel: "P-104 Kapak Gövdesi",
    utilization: 92,
    cycleLabel: "31,8 sn",
    outputLabel: "8.420 adet",
    icon: Activity,
  },
  {
    id: "machine-m02",
    code: "M-02",
    name: "Engel Victory",
    state: "producing",
    stateLabel: "Üretimde",
    jobLabel: "UE-260804-03",
    productLabel: "P-118 Bağlantı Parçası",
    utilization: 87,
    cycleLabel: "42,1 sn",
    outputLabel: "6.180 adet",
    icon: Activity,
  },
  {
    id: "machine-m03",
    code: "M-03",
    name: "Arburg Allrounder",
    state: "setup",
    stateLabel: "Ayar",
    jobLabel: "UE-260804-07",
    productLabel: "Kalıp değişimi sürüyor",
    utilization: 64,
    cycleLabel: "18 dk",
    outputLabel: "Hazırlanıyor",
    icon: Settings2,
  },
  {
    id: "machine-m04",
    code: "M-04",
    name: "KraussMaffei CX",
    state: "offline",
    stateLabel: "Bağlantı Yok",
    jobLabel: "UE-260804-05",
    productLabel: "PLC iletişimi kesildi",
    utilization: 0,
    cycleLabel: "—",
    outputLabel: "Duruşta",
    icon: AlertTriangle,
  },
  {
    id: "machine-m05",
    code: "M-05",
    name: "Haitian Jupiter",
    state: "fault",
    stateLabel: "Arızalı",
    jobLabel: "UE-260804-09",
    productLabel: "Hidrolik sıcaklık alarmı",
    utilization: 38,
    cycleLabel: "—",
    outputLabel: "Bakım çağrıldı",
    icon: AlertTriangle,
  },
  {
    id: "machine-m06",
    code: "M-06",
    name: "Borche BU",
    state: "producing",
    stateLabel: "Üretimde",
    jobLabel: "UE-260804-11",
    productLabel: "P-205 Teknik Parça",
    utilization: 81,
    cycleLabel: "36,4 sn",
    outputLabel: "5.740 adet",
    icon: Gauge,
  },
  {
    id: "machine-m07",
    code: "M-07",
    name: "Fanuc Roboshot",
    state: "waiting",
    stateLabel: "Beklemede",
    jobLabel: "UE-260804-12",
    productLabel: "Malzeme beslemesi bekleniyor",
    utilization: 54,
    cycleLabel: "24 dk",
    outputLabel: "Sırada",
    icon: Clock3,
  },
  {
    id: "machine-m08",
    code: "M-08",
    name: "Sumitomo Demag",
    state: "maintenance",
    stateLabel: "Bakımda",
    jobLabel: "BK-260804-04",
    productLabel: "Periyodik bakım işlemi",
    utilization: 22,
    cycleLabel: "45 dk",
    outputLabel: "Teknik ekip",
    icon: Wrench,
  },
];

const styles: Record<string, CSSProperties> = {
  panel: {
    display: "flex",
    minWidth: 0,
    flexDirection: "column",
    overflow: "hidden",
  },
  grid: {
    display: "grid",
    minWidth: 0,
    minHeight: 0,
    gridTemplateColumns: "repeat(4, minmax(0, 1fr))",
    gap: 7,
    flex: "1 1 auto",
    padding: 10,
    overflow: "hidden",
  },
  card: {
    display: "flex",
    minWidth: 0,
    minHeight: 128,
    flexDirection: "column",
    padding: 9,
    overflow: "hidden",
    border: "1px solid #dce5f0",
    borderRadius: 9,
    background: "#f8fafc",
  },
  top: {
    display: "flex",
    minWidth: 0,
    alignItems: "flex-start",
    justifyContent: "space-between",
    gap: 7,
  },
  identity: {
    minWidth: 0,
  },
  code: {
    margin: 0,
    color: "#10213d",
    fontSize: "0.51rem",
    fontWeight: 820,
    lineHeight: 1.1,
  },
  name: {
    overflow: "hidden",
    margin: "3px 0 0",
    color: "#75839a",
    fontSize: "0.39rem",
    fontWeight: 620,
    lineHeight: 1.2,
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  state: {
    display: "inline-flex",
    flex: "0 0 auto",
    alignItems: "center",
    gap: 4,
    padding: "4px 5px",
    borderRadius: 6,
    fontSize: "0.36rem",
    fontWeight: 760,
    lineHeight: 1,
    whiteSpace: "nowrap",
  },
  job: {
    display: "grid",
    minWidth: 0,
    gap: 2,
    marginTop: 8,
  },
  jobLabel: {
    color: "#75839a",
    fontSize: "0.35rem",
    fontWeight: 650,
  },
  product: {
    overflow: "hidden",
    color: "#10213d",
    fontSize: "0.43rem",
    fontWeight: 750,
    lineHeight: 1.2,
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  utilization: {
    marginTop: "auto",
    paddingTop: 9,
  },
  utilizationTop: {
    display: "flex",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 6,
    marginBottom: 5,
    color: "#75839a",
    fontSize: "0.35rem",
  },
  utilizationValue: {
    fontSize: "0.39rem",
    fontWeight: 800,
  },
  track: {
    overflow: "hidden",
    height: 4,
    borderRadius: 999,
    background: "#dce5f0",
  },
  progress: {
    display: "block",
    height: "100%",
    borderRadius: 999,
  },
  footer: {
    display: "flex",
    minWidth: 0,
    alignItems: "center",
    justifyContent: "space-between",
    gap: 6,
    marginTop: 7,
    paddingTop: 6,
    borderTop: "1px solid #dce5f0",
    color: "#75839a",
    fontSize: "0.33rem",
  },
  footerValue: {
    overflow: "hidden",
    fontWeight: 740,
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
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

function normalizeState(
  source: Record<string, unknown>,
  fallback: MachineState,
): MachineState {
  const rawValue = readString(
    source,
    ["state", "status", "operationalStatus"],
    fallback,
  ).toLocaleLowerCase("tr-TR");

  if (
    rawValue.includes("fault") ||
    rawValue.includes("arıza") ||
    rawValue.includes("critical")
  ) {
    return "fault";
  }

  if (
    rawValue.includes("offline") ||
    rawValue.includes("bağlantı")
  ) {
    return "offline";
  }

  if (
    rawValue.includes("maintenance") ||
    rawValue.includes("bakım")
  ) {
    return "maintenance";
  }

  if (
    rawValue.includes("setup") ||
    rawValue.includes("ayar")
  ) {
    return "setup";
  }

  if (
    rawValue.includes("waiting") ||
    rawValue.includes("bekle")
  ) {
    return "waiting";
  }

  return "producing";
}

function getStateLabel(state: MachineState) {
  switch (state) {
    case "producing":
      return "Üretimde";
    case "waiting":
      return "Beklemede";
    case "setup":
      return "Ayar";
    case "maintenance":
      return "Bakımda";
    case "fault":
      return "Arızalı";
    case "offline":
      return "Bağlantı Yok";
  }
}

function getStateTone(state: MachineState): {
  accent: string;
  soft: string;
} {
  switch (state) {
    case "producing":
      return {
        accent: "#08945f",
        soft: "#e8f8f1",
      };
    case "waiting":
    case "setup":
      return {
        accent: "#dc7a00",
        soft: "#fff3df",
      };
    case "maintenance":
      return {
        accent: "#7c3aed",
        soft: "#f2ebff",
      };
    case "fault":
    case "offline":
      return {
        accent: "#dc3545",
        soft: "#ffecef",
      };
  }
}

function getStateIcon(state: MachineState): LucideIcon {
  switch (state) {
    case "producing":
      return Activity;
    case "waiting":
      return Clock3;
    case "setup":
      return Settings2;
    case "maintenance":
      return Wrench;
    case "fault":
    case "offline":
      return AlertTriangle;
  }
}

function normalizeMachine(
  value: unknown,
  index: number,
): NormalizedMachine {
  const source = asRecord(value);
  const fallback =
    fallbackMachines[index] ??
    fallbackMachines[fallbackMachines.length - 1];

  const state = normalizeState(source, fallback.state);

  return {
    id: readString(
      source,
      ["id", "machineId", "code"],
      `machine-${index + 1}`,
    ),
    code: readString(
      source,
      ["code", "machineCode", "assetCode"],
      fallback.code,
    ),
    name: readString(
      source,
      ["name", "machineName", "model"],
      fallback.name,
    ),
    state,
    stateLabel: readString(
      source,
      ["stateLabel", "statusLabel"],
      getStateLabel(state),
    ),
    jobLabel: readString(
      source,
      ["jobLabel", "productionOrderCode", "orderCode"],
      fallback.jobLabel,
    ),
    productLabel: readString(
      source,
      ["productLabel", "productName", "currentJobLabel"],
      fallback.productLabel,
    ),
    utilization: Math.max(
      0,
      Math.min(
        100,
        readNumber(
          source,
          ["utilization", "utilizationRate", "oee"],
          fallback.utilization,
        ),
      ),
    ),
    cycleLabel: readString(
      source,
      ["cycleLabel", "cycleTimeLabel"],
      fallback.cycleLabel,
    ),
    outputLabel: readString(
      source,
      ["outputLabel", "productionLabel"],
      fallback.outputLabel,
    ),
    icon: getStateIcon(state),
  };
}

export function MachineStatusPanel({
  machines,
}: MachineStatusPanelProps) {
  const sourceMachines =
    machines.length > 0 ? machines : fallbackMachines;

  const visibleMachines = Array.from(
    { length: 8 },
    (_, index) =>
      normalizeMachine(
        sourceMachines[index] ?? fallbackMachines[index],
        index,
      ),
  );

  return (
    <section
      aria-labelledby="machine-status-title"
      className="dashboard-panel dashboard-panel--machine-status"
      style={styles.panel}
    >
      <div className="dashboard-section-heading">
        <div>
          <p className="dashboard-section-heading__eyebrow">
            Canlı üretim görünümü
          </p>

          <h2
            className="dashboard-section-heading__title"
            id="machine-status-title"
          >
            Makine Durumu
          </h2>
        </div>

        <div className="dashboard-section-heading__meta">
          <Factory
            aria-hidden="true"
            size={16}
            strokeWidth={1.8}
          />
          {visibleMachines.length} makine
        </div>
      </div>

      <div
        className="locked-machine-grid"
        style={styles.grid}
      >
        {visibleMachines.map((machine) => {
          const Icon = machine.icon;
          const tone = getStateTone(machine.state);

          return (
            <article
              className={`locked-machine-card locked-machine-card--${machine.state}`}
              key={machine.id}
              style={styles.card}
            >
              <div style={styles.top}>
                <div style={styles.identity}>
                  <p style={styles.code}>{machine.code}</p>
                  <p style={styles.name}>{machine.name}</p>
                </div>

                <span
                  style={{
                    ...styles.state,
                    background: tone.soft,
                    color: tone.accent,
                  }}
                >
                  <Icon
                    aria-hidden="true"
                    size={11}
                    strokeWidth={1.8}
                  />
                  {machine.stateLabel}
                </span>
              </div>

              <div style={styles.job}>
                <span style={styles.jobLabel}>
                  {machine.jobLabel}
                </span>

                <strong style={styles.product}>
                  {machine.productLabel}
                </strong>
              </div>

              <div style={styles.utilization}>
                <div style={styles.utilizationTop}>
                  <span>Kullanım oranı</span>

                  <strong
                    style={{
                      ...styles.utilizationValue,
                      color: tone.accent,
                    }}
                  >
                    %{Math.round(machine.utilization)}
                  </strong>
                </div>

                <div style={styles.track}>
                  <span
                    style={{
                      ...styles.progress,
                      width: `${machine.utilization}%`,
                      background: tone.accent,
                    }}
                  />
                </div>
              </div>

              <div style={styles.footer}>
                <span>{machine.cycleLabel}</span>

                <strong
                  style={{
                    ...styles.footerValue,
                    color: tone.accent,
                  }}
                >
                  {machine.outputLabel}
                </strong>
              </div>
            </article>
          );
        })}
      </div>
    </section>
  );
}