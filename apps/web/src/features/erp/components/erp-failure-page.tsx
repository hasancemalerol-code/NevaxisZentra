import { FailureStatePage } from "@/components/ui/failure-state";

import { ErpPageHeader } from "./erp-ui-primitives";
import Link from "next/link";

import { ErpWorkspaceNav, getErpSectionLabel } from "./erp-workspace-nav";
import "./erp-workspace.css";

type ErpFailurePageProps = Readonly<{
  title: string;
  description: string;
  /** Section the user came from; keeps one ERP pill active and powers the explicit list link. */
  listHref: string;
  listLabel: string;
  failedTitle: string;
  entityLabel?: string;
  code?: unknown;
  message?: string | null;
}>;

/** One ERP failure / not-found page: same header and sub-navigation as the working pages, explicit way back to the list. */
export function ErpFailurePage({
  title,
  description,
  listHref,
  listLabel,
  failedTitle,
  entityLabel,
  code,
  message,
}: ErpFailurePageProps) {
  const sectionLabel = getErpSectionLabel(listHref);
  return (
    <div className="production-execution-page erp-workspace-page erp-workspace-page--dense">
      <nav aria-label="Konum">
        <ol className="erp-breadcrumb">
          <li>
            <Link href="/erp-entegrasyonlari/baglanti">ERP Entegrasyonları</Link>
          </li>
          {sectionLabel ? (
            <li>
              <span aria-hidden="true">/</span> <Link href={listHref}>{sectionLabel}</Link>
            </li>
          ) : null}
          <li>
            <span aria-hidden="true">/</span> <span aria-current="page">{title}</span>
          </li>
        </ol>
      </nav>
      <ErpPageHeader title={title} description={description} />
      <ErpWorkspaceNav activeHref={listHref} />
      <FailureStatePage
        code={code}
        message={message}
        failedTitle={failedTitle}
        entityLabel={entityLabel}
        backHref={listHref}
        backLabel={listLabel}
        inline
      />
    </div>
  );
}
