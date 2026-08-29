import Link from "next/link";
import { ExternalLink, Link2 } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";

type AiTaskSourceReferenceSectionProps = Readonly<{
  sourceModule: string | null;
  sourceEntityId: string | null;
  taskReference: string;
  taskTitle?: string | null;
}>;

function resolveCommercialIntelligenceSourceLink(
  sourceEntityId: string,
  taskTitle?: string | null,
): { href: string; label: string } | null {
  if (taskTitle?.startsWith("Fiyat senaryosu")) {
    return {
      href: `/ticari-zeka/fiyat-koruma-ve-onay?scenario=${encodeURIComponent(sourceEntityId)}`,
      label: "Fiyat senaryosunu aç",
    };
  }

  return {
    href: `/ticari-zeka/urun-karliligi?comparison=${encodeURIComponent(sourceEntityId)}`,
    label: "Karlılık karşılaştırmasını aç",
  };
}

function formatSourceModuleLabel(sourceModule: string): string {
  const labels: Record<string, string> = {
    "commercial-intelligence": "Ticari zekâ",
    "operational-risk": "Operasyonel risk",
    export: "Dışa aktarım",
    maintenance: "Bakım",
    quality: "Kalite",
    "production-planning": "Üretim planlama",
  };

  return labels[sourceModule] ?? sourceModule;
}

export function AiTaskSourceReferenceSection({
  sourceModule,
  sourceEntityId,
  taskReference,
  taskTitle = null,
}: AiTaskSourceReferenceSectionProps) {
  const hasSourceLink = Boolean(sourceModule && sourceEntityId);
  const commercialIntelligenceLink = sourceModule === "commercial-intelligence" && sourceEntityId
    ? resolveCommercialIntelligenceSourceLink(sourceEntityId, taskTitle)
    : null;
  const maintenanceLink = sourceModule === "maintenance" && sourceEntityId
    ? {
        href: `/bakim/kestirimci-riskler?assessment=${encodeURIComponent(sourceEntityId)}`,
        label: "Kestirimci bakım riskini aç",
      }
    : null;
  const sourceLink = commercialIntelligenceLink ?? maintenanceLink;

  return (
    <Card
      className="ai-task-source-reference"
      data-testid="ai-task-source-reference-section"
    >
      <div className="ai-task-source-reference__heading">
        <Link2 aria-hidden="true" />
        <div>
          <h3>Kaynak referansı</h3>
          <p>
            Bu görev, gelecekteki modül entegrasyonları için genel kaynak
            bağlantısı taşır.
          </p>
        </div>
      </div>

      <dl className="ai-task-source-reference__grid">
        <div>
          <dt>Görev referansı</dt>
          <dd>{taskReference}</dd>
        </div>

        <div>
          <dt>Kaynak modül</dt>
          <dd>
            {sourceModule
              ? formatSourceModuleLabel(sourceModule)
              : "Bağlı değil"}
          </dd>
        </div>

        <div>
          <dt>Kaynak varlık</dt>
          <dd>
            {sourceEntityId ?? "Tanımlanmadı"}
          </dd>
        </div>
      </dl>

      {hasSourceLink ? (
        <div className="ai-task-source-reference__link-row">
          <Badge tone="neutral">
            {formatSourceModuleLabel(sourceModule!)}
          </Badge>

          <span>
            {sourceEntityId}
          </span>

          {sourceLink ? (
            <Link href={sourceLink.href}>
              <ExternalLink aria-hidden="true" />
              {sourceLink.label}
            </Link>
          ) : null}
        </div>
      ) : (
        <p className="ai-task-source-reference__empty">
          Bu görev henüz bir kaynak modülüne bağlanmadı. Taslak görevler
          manuel oluşturulduğunda kaynak referansı boş kalabilir.
        </p>
      )}
    </Card>
  );
}
