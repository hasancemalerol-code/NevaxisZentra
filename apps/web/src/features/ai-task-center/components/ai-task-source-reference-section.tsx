import Link from "next/link";
import type { ReactNode } from "react";
import { ExternalLink, Link2 } from "lucide-react";

import { Badge } from "@/components/ui/badge";

type AiTaskSourceReferenceSectionProps = Readonly<{
  sourceModule: string | null;
  sourceEntityId: string | null;
  taskReference: string;
  taskTitle?: string | null;
  teamsMessageSourceLink?: ReactNode;
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
    material: "Malzeme",
    mold: "Kalıp",
    production: "Üretim",
    procurement: "Satın alma",
    "production-planning": "Üretim planlama",
    teams: "Teams mesajı",
    "shift-handover": "Vardiya teslimi",
  };

  return labels[sourceModule] ?? sourceModule;
}

export function AiTaskSourceReferenceSection({
  sourceModule,
  sourceEntityId,
  taskReference,
  taskTitle = null,
  teamsMessageSourceLink = null,
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
  const qualityLink = sourceModule === "quality" && sourceEntityId
    ? {
        href: `/kalite/takip-ve-gorevler?finding=${encodeURIComponent(sourceEntityId)}`,
        label: "Kalite uygunsuzluk kaydını aç",
      }
    : null;
  const materialLink = sourceModule === "material" && sourceEntityId
    ? {
        href: `/stok-ve-depo/malzeme-riskleri?assessment=${encodeURIComponent(sourceEntityId)}`,
        label: "Malzeme risk değerlendirmesini aç",
      }
    : null;
  const moldLink = sourceModule === "mold" && sourceEntityId
    ? {
        href: `/kaliplar/takip-ve-gorevler?mold=${encodeURIComponent(sourceEntityId)}`,
        label: "Kalıp takip kaydını aç",
      }
    : null;
  const productionLink = sourceModule === "production" && sourceEntityId
    ? {
        href: `/uretim/istisnalar-ve-gorevler?exception=${encodeURIComponent(sourceEntityId)}`,
        label: "Üretim istisna kaydını aç",
      }
    : null;
  const procurementLink = sourceModule === "procurement" && sourceEntityId
    ? {
        href: `/satin-alma/tedarik-riskleri?assessment=${encodeURIComponent(sourceEntityId)}`,
        label: "Tedarik risk değerlendirmesini aç",
      }
    : null;
  const sourceLink = commercialIntelligenceLink
    ?? maintenanceLink
    ?? qualityLink
    ?? materialLink
    ?? moldLink
    ?? productionLink
    ?? procurementLink;

  return (
    <section
      className="ai-task-detail-context"
      data-testid="ai-task-source-reference-section"
    >
      <header className="ai-task-detail-context__header">
        <Link2 aria-hidden="true" />
        <div>
          <p className="ai-task-detail-context__eyebrow">Related Context</p>
          <h2 className="ai-task-detail-context__title">İlişkili Bağlam</h2>
        </div>
      </header>

      <dl className="ai-task-detail-context__grid">
        <div>
          <dt>Görev referansı</dt>
          <dd>{taskReference}</dd>
        </div>
        <div>
          <dt>Modül</dt>
          <dd>
            {sourceModule
              ? formatSourceModuleLabel(sourceModule)
              : "Bağlı değil"}
          </dd>
        </div>
        <div>
          <dt>Kaynak kayıt</dt>
          <dd>{sourceEntityId ?? "Tanımlanmadı"}</dd>
        </div>
        <div>
          <dt>Kaynak türü</dt>
          <dd>
            {sourceModule
              ? formatSourceModuleLabel(sourceModule)
              : "Belirtilmemiş"}
          </dd>
        </div>
      </dl>

      {hasSourceLink ? (
        <div className="ai-task-detail-context__footer">
          <Badge tone="neutral">
            {formatSourceModuleLabel(sourceModule!)}
          </Badge>
          <span className="ai-task-detail-context__entity">{sourceEntityId}</span>
          {sourceLink ? (
            <Link className="ai-task-detail-context__link" href={sourceLink.href}>
              <ExternalLink aria-hidden="true" />
              {sourceLink.label}
            </Link>
          ) : null}
          {sourceModule === "teams" ? teamsMessageSourceLink : null}
        </div>
      ) : (
        <p className="ai-task-detail-context__empty">
          Bu görev henüz bir kaynak modülüne bağlanmadı.
        </p>
      )}
    </section>
  );
}
