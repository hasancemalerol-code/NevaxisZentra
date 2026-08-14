import {
  ArrowUpRight,
  BrainCircuit,
  CheckCircle2,
  Clock3,
  PackageSearch,
  Sparkles,
  Wrench,
  type LucideIcon,
} from "lucide-react";
import Link from "next/link";
import type { CSSProperties } from "react";

import { getAiTaskPreviewById } from "@/features/ai-task-center/data/ai-task-preview-data";

interface AiTaskCenterSummaryProps {
  tasks: readonly unknown[];
}

interface NormalizedTask {
  id: string;
  category: string;
  title: string;
  description: string;
  status: string;
  priority: "critical" | "high" | "medium" | "low";
  icon: LucideIcon;
}

const fallbackTasks: NormalizedTask[] = [
  {
    id: "preview-material-risk",
    category: "Malzeme Risk Analizi",
    title: "PP GF30 malzeme riskini incele",
    description:
      "Mevcut kapsama süresi planlanan üretim tüketimini karşılamıyor.",
    status: "Onay bekliyor",
    priority: "critical",
    icon: PackageSearch,
  },
  {
    id: "preview-production-plan",
    category: "AI Üretim Planlayıcı",
    title: "M-07 üretim sırasını yeniden değerlendir",
    description:
      "Termin baskısı nedeniyle alternatif üretim sırası önerildi.",
    status: "İnceleniyor",
    priority: "high",
    icon: BrainCircuit,
  },
  {
    id: "preview-mold-maintenance",
    category: "Kalıp Yaşam Döngüsü",
    title: "KLP-204 bakım önerisini değerlendir",
    description:
      "Bakım eşiği yüzde 96 seviyesine ulaştı.",
    status: "İnceleme gerekli",
    priority: "high",
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
  list: {
    display: "grid",
    minWidth: 0,
    minHeight: 0,
    gridTemplateRows: "repeat(3, minmax(0, 1fr))",
    flex: "1 1 auto",
    padding: "0 14px",
    overflow: "hidden",
  },
  item: {
    display: "grid",
    minWidth: 0,
    minHeight: 0,
    gridTemplateColumns: "34px minmax(0, 1fr) 58px",
    alignItems: "center",
    gap: 10,
    padding: "10px 0",
    overflow: "hidden",
    borderBottom: "1px solid #dce5f0",
  },
  icon: {
    display: "grid",
    width: 34,
    height: 34,
    flex: "0 0 auto",
    placeItems: "center",
    borderRadius: 9,
    background: "#eaf2ff",
    color: "#1769e0",
  },
  content: {
    display: "grid",
    minWidth: 0,
    alignContent: "center",
    gap: 5,
    overflow: "hidden",
  },
  metaRow: {
    display: "flex",
    minWidth: 0,
    alignItems: "center",
    justifyContent: "space-between",
    gap: 8,
  },
  category: {
    overflow: "hidden",
    color: "#75839a",
    fontSize: 10,
    fontWeight: 700,
    lineHeight: 1.2,
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  status: {
    display: "inline-flex",
    flex: "0 0 auto",
    alignItems: "center",
    gap: 4,
    color: "#75839a",
    fontSize: 9,
    fontWeight: 650,
    lineHeight: 1,
    whiteSpace: "nowrap",
  },
  title: {
    overflow: "hidden",
    margin: 0,
    color: "#10213d",
    fontSize: 13,
    fontWeight: 790,
    lineHeight: 1.25,
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  description: {
    overflow: "hidden",
    margin: 0,
    color: "#5f6f86",
    fontSize: 10,
    lineHeight: 1.3,
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  actionArea: {
    display: "grid",
    minWidth: 58,
    justifyItems: "end",
    alignContent: "center",
    gap: 8,
  },
  priority: {
    display: "inline-flex",
    minWidth: 52,
    alignItems: "center",
    justifyContent: "center",
    padding: "6px 7px",
    borderRadius: 7,
    fontSize: 10,
    fontWeight: 780,
    lineHeight: 1,
    whiteSpace: "nowrap",
  },
  inspectButton: {
    display: "grid",
    width: 34,
    height: 34,
    placeItems: "center",
    border: "1px solid #cfdbea",
    borderRadius: 8,
    background: "#ffffff",
    color: "#1769e0",
    cursor: "not-allowed",
    opacity: 1,
    textDecoration: "none",
  },
  inspectButtonActive: {
    cursor: "pointer",
  },
  footer: {
    display: "flex",
    minHeight: 34,
    flex: "0 0 34px",
    alignItems: "center",
    justifyContent: "center",
    gap: 6,
    borderTop: "1px solid #dce5f0",
    color: "#1769e0",
    fontSize: 10,
    fontWeight: 750,
    textDecoration: "none",
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

    if (
      typeof value === "string" &&
      value.trim().length > 0
    ) {
      return value;
    }
  }

  return fallback;
}

function normalizePriority(
  source: Record<string, unknown>,
): NormalizedTask["priority"] {
  const value = readString(
    source,
    ["priority", "priorityLevel", "riskLevel", "severity"],
    "medium",
  ).toLocaleLowerCase("tr-TR");

  if (
    value.includes("critical") ||
    value.includes("kritik")
  ) {
    return "critical";
  }

  if (
    value.includes("high") ||
    value.includes("yüksek")
  ) {
    return "high";
  }

  if (
    value.includes("low") ||
    value.includes("düşük")
  ) {
    return "low";
  }

  return "medium";
}

function getIcon(category: string): LucideIcon {
  const value = category.toLocaleLowerCase("tr-TR");

  if (value.includes("malzeme")) {
    return PackageSearch;
  }

  if (
    value.includes("kalıp") ||
    value.includes("bakım")
  ) {
    return Wrench;
  }

  return BrainCircuit;
}

function normalizeTask(
  value: unknown,
  index: number,
): NormalizedTask {
  const source = asRecord(value);
  const fallback =
    fallbackTasks[index] ??
    fallbackTasks[fallbackTasks.length - 1];

  const category = readString(
    source,
    [
      "categoryLabel",
      "category",
      "sourceLabel",
      "typeLabel",
      "agentLabel",
    ],
    fallback.category,
  );

  return {
    id: readString(
      source,
      ["id", "taskId", "code"],
      `ai-task-${index + 1}`,
    ),
    category,
    title: readString(
      source,
      ["title", "taskTitle", "name", "recommendation"],
      fallback.title,
    ),
    description: readString(
      source,
      ["description", "summary", "reason", "detail"],
      fallback.description,
    ),
    status: readString(
      source,
      ["statusLabel", "status", "reviewStatusLabel"],
      fallback.status,
    ),
    priority: normalizePriority(source),
    icon: getIcon(category),
  };
}

function getPriorityLabel(
  priority: NormalizedTask["priority"],
) {
  switch (priority) {
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

function getPriorityStyle(
  priority: NormalizedTask["priority"],
): CSSProperties {
  switch (priority) {
    case "critical":
      return {
        background: "#ffecef",
        color: "#dc3545",
      };
    case "high":
      return {
        background: "#fff2df",
        color: "#d97706",
      };
    case "medium":
      return {
        background: "#eaf2ff",
        color: "#1769e0",
      };
    case "low":
      return {
        background: "#e8f8f1",
        color: "#08945f",
      };
  }
}

export function AiTaskCenterSummary({
  tasks,
}: AiTaskCenterSummaryProps) {
  const sourceTasks =
    tasks.length > 0 ? tasks : fallbackTasks;

  const visibleTasks = sourceTasks
    .slice(0, 3)
    .map(normalizeTask);

  return (
    <section
      aria-labelledby="ai-task-center-title"
      className="dashboard-panel dashboard-panel--ai-task-center"
      style={styles.panel}
    >
      <div className="dashboard-section-heading">
        <div>
          <p className="dashboard-section-heading__eyebrow">
            İnsan denetimli öneriler
          </p>

          <h2
            className="dashboard-section-heading__title"
            id="ai-task-center-title"
          >
            AI Görev Merkezi
          </h2>
        </div>

        <div className="dashboard-section-heading__meta">
          <Sparkles
            aria-hidden="true"
            size={16}
            strokeWidth={1.8}
          />
          {visibleTasks.length} görev bekliyor
        </div>
      </div>

      <div style={styles.list}>
        {visibleTasks.map((task, index) => {
          const Icon = task.icon;
          const openableTask = getAiTaskPreviewById(task.id);
          const detailHref = openableTask
            ? `/ai-gorevleri/${openableTask.id}`
            : null;

          return (
            <article
              key={task.id}
              style={{
                ...styles.item,
                borderBottom:
                  index === visibleTasks.length - 1
                    ? 0
                    : styles.item.borderBottom,
              }}
            >
              <span
                aria-hidden="true"
                style={styles.icon}
              >
                <Icon size={17} strokeWidth={1.8} />
              </span>

              <div style={styles.content}>
                <div style={styles.metaRow}>
                  <span style={styles.category}>
                    {task.category}
                  </span>

                  <span style={styles.status}>
                    {task.status
                      .toLocaleLowerCase("tr-TR")
                      .includes("tamam") ? (
                      <CheckCircle2
                        aria-hidden="true"
                        size={11}
                        strokeWidth={1.8}
                      />
                    ) : (
                      <Clock3
                        aria-hidden="true"
                        size={11}
                        strokeWidth={1.8}
                      />
                    )}

                    {task.status}
                  </span>
                </div>

                <h3 style={styles.title}>
                  {task.title}
                </h3>

                <p style={styles.description}>
                  {task.description}
                </p>
              </div>

              <div style={styles.actionArea}>
                <span
                  style={{
                    ...styles.priority,
                    ...getPriorityStyle(task.priority),
                  }}
                >
                  {getPriorityLabel(task.priority)}
                </span>

                {detailHref ? (
                  <Link
                    aria-label={`${task.title} görevini incele`}
                    href={detailHref}
                    style={{
                      ...styles.inspectButton,
                      ...styles.inspectButtonActive,
                    }}
                  >
                    <ArrowUpRight
                      aria-hidden="true"
                      size={16}
                      strokeWidth={1.8}
                    />
                  </Link>
                ) : (
                  <button
                    aria-label={`${task.title} görevini incele. Bu görev için açılabilir bir kayıt bulunmuyor.`}
                    disabled
                    style={styles.inspectButton}
                    title="Bu görev için açılabilir bir kayıt bulunmuyor."
                    type="button"
                  >
                    <ArrowUpRight
                      aria-hidden="true"
                      size={16}
                      strokeWidth={1.8}
                    />
                  </button>
                )}
              </div>
            </article>
          );
        })}
      </div>

      <Link href="/" style={styles.footer}>
        Tüm AI görevlerini görüntüle
        <ArrowUpRight
          aria-hidden="true"
          size={14}
          strokeWidth={1.8}
        />
      </Link>
    </section>
  );
}