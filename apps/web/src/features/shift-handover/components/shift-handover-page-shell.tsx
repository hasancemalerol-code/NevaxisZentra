import Link from "next/link";
import { ArrowLeft } from "lucide-react";
import type { ReactNode } from "react";

import { PageFrame, PageHeader } from "@/components/ui/page";
import { SubNav } from "@/components/ui/sub-nav";
import { SHIFT_OPERATIONS_SUBNAV } from "@/features/shift-operations/domain/shift-operations-labels";

import "./shift-handover.css";
import "./shift-brief.css";

type Props = Readonly<{
  title?: string;
  description?: string;
  actions?: ReactNode;
  /** Module tabs belong to list screens; a single record page shows only its own header. */
  showModuleTabs?: boolean;
  backHref?: string;
  backLabel?: string;
  children: ReactNode;
}>;

/** One page frame for every vardiya devir screen: header first, then the module tabs, then the content. */
export function ShiftHandoverPageShell({
  title = "Vardiya devri",
  description = "Vardiya sonunda açık kalemleri, sorumluları ve takip işlerini bir sonraki vardiyaya eksiksiz aktarın.",
  actions,
  showModuleTabs = true,
  backHref,
  backLabel,
  children,
}: Props) {
  return (
    <PageFrame data-testid="shift-handover-page">
      {backHref && backLabel ? (
        <Link className="shift-handover-back-link" href={backHref}>
          <ArrowLeft aria-hidden="true" />
          <span>{backLabel}</span>
        </Link>
      ) : null}
      <PageHeader eyebrow="VARDİYA DEVRİ" title={title} description={description} actions={actions} />
      {showModuleTabs ? (
      <SubNav
        label="Vardiya bölümleri"
        groups={[{
          items: SHIFT_OPERATIONS_SUBNAV.map((item) => ({
            href: item.href,
            label: item.label,
            active: item.href === "/vardiya-devir",
          })),
        }]}
      />
      ) : null}
      <div className="shift-handover-shell">{children}</div>
    </PageFrame>
  );
}
