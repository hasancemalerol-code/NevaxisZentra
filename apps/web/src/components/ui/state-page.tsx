import Link from "next/link";
import { Inbox, LockKeyhole, RefreshCw, SearchX, TriangleAlert } from "lucide-react";
import type { ReactNode } from "react";

import { customerMessage } from "@/lib/presentation/customer-message";

import { StatePageLayout } from "./state-page-layout";
import { StateRetryButton } from "./state-retry-button";

export type StatePageKind = "not-found" | "forbidden" | "entitlement" | "error" | "unavailable" | "empty";

const KIND_ICON: Record<StatePageKind, ReactNode> = {
  "not-found": <SearchX />,
  forbidden: <LockKeyhole />,
  entitlement: <LockKeyhole />,
  error: <TriangleAlert />,
  unavailable: <RefreshCw />,
  empty: <Inbox />,
};

type StatePageProps = Readonly<{
  kind: StatePageKind;
  title: string;
  description: string;
  /** Short status label shown above the title (for example "404"). */
  code?: string;
  /** Opaque support reference (error digest). Never an exception message. */
  reference?: string;
  actions?: ReactNode;
  /** True when rendered without the application shell (root-level boundaries). */
  standalone?: boolean;
  /** True when rendered inside a page that already has its own header (a section, not the whole page). */
  inline?: boolean;
  moduleLabel?: string;
}>;

/**
 * One full-page state for not-found, access-denied, service-unavailable and unexpected-error situations.
 * It is the only place those situations are worded, so customers never see framework default pages or raw exception text.
 */
export function StatePage({ kind, title, description, code, reference, actions, standalone = false, inline = false, moduleLabel }: StatePageProps) {
  const sectionClass = `zx-state-page zx-state-page--${kind}${kind === "entitlement" ? " zx-state-page--forbidden" : ""}${standalone ? " zx-state-page--standalone" : ""}${inline ? " zx-state-page--inline" : ""}`;
  const card = (titleInCard: boolean) => (
    <div className="zx-state-card">
      <span className="zx-state-icon" aria-hidden="true">{KIND_ICON[kind]}</span>
      {code ? <p className="zx-state-code">{code}</p> : null}
      {titleInCard ? (inline ? <h2 id="zx-state-title">{title}</h2> : <h1 id="zx-state-title">{title}</h1>) : null}
      <p className="zx-state-description">{description}</p>
      {actions ? <div className="zx-state-actions">{actions}</div> : null}
      {reference ? (
        <p className="zx-state-reference">
          Destek referansı: <code>{reference}</code>
        </p>
      ) : null}
    </div>
  );
  const shared = !standalone && !inline;

  return (
    <section className={sectionClass} aria-labelledby="zx-state-title" data-testid={`state-page-${kind}`}>
      {shared ? (
        <StatePageLayout title={title} moduleLabel={moduleLabel} cardWithTitle={card(true)} cardWithoutTitle={card(false)} />
      ) : (
        card(true)
      )}
    </section>
  );
}

export function StatePageLink({
  href,
  children,
  primary = false,
}: Readonly<{ href: string; children: ReactNode; primary?: boolean }>) {
  return (
    <Link href={href} className={`button button-medium ${primary ? "button-primary" : "button-secondary"}`}>
      {children}
    </Link>
  );
}

/** Skeleton shown while a shipping route streams its first content (route-level loading boundary). */
export function PageLoading({ label = "Sayfa yükleniyor" }: Readonly<{ label?: string }>) {
  return (
    <div className="zx-page-loading" role="status" aria-live="polite" aria-busy="true" data-testid="page-loading">
      <span className="visually-hidden">{label}</span>
      <div className="zx-page-loading__header" aria-hidden="true">
        <span className="skeleton zx-skeleton-eyebrow" />
        <span className="skeleton zx-skeleton-title" />
        <span className="skeleton zx-skeleton-subtitle" />
      </div>
      <div className="zx-page-loading__metrics" aria-hidden="true">
        {[0, 1, 2, 3].map((index) => (
          <span key={index} className="skeleton zx-skeleton-metric" />
        ))}
      </div>
      <div className="zx-page-loading__panel" aria-hidden="true">
        <span className="skeleton zx-skeleton-line" />
        <span className="skeleton zx-skeleton-line zx-skeleton-line--short" />
        <span className="skeleton zx-skeleton-line" />
        <span className="skeleton zx-skeleton-line zx-skeleton-line--short" />
      </div>
    </div>
  );
}

type RecordFailureStateProps = Readonly<{
  /** True when the backend answered "this record does not exist" (as opposed to a load failure). */
  notFound: boolean;
  /** Noun phrase for the missing record, e.g. "üretim hattı". */
  entityLabel: string;
  failedTitle: string;
  /** Raw failure message from the data layer; it is shown only when it is customer-facing Turkish copy. */
  message?: string;
  backHref: string;
  backLabel: string;
}>;

/**
 * Detail screens render this when their record could not be shown. A missing record and a failed load are different
 * truths, and neither exposes raw backend wording.
 */
export function RecordFailureState({ notFound, entityLabel, failedTitle, message, backHref, backLabel }: RecordFailureStateProps) {
  return notFound ? (
    <StatePage
      kind="not-found"
      title="Kayıt Bulunamadı"
      description={`İstenen ${entityLabel} mevcut değil, kaldırılmış ya da yetki alanınızın dışında olabilir.`}
      actions={<StatePageLink href={backHref} primary>{backLabel}</StatePageLink>}
    />
  ) : (
    <StatePage
      kind="unavailable"
      title={failedTitle}
      description={customerMessage(message)}
      actions={<StatePageLink href={backHref} primary>{backLabel}</StatePageLink>}
    />
  );
}

/** A detail pane whose record could not be shown: a calm inline unavailable state, never a bare "yüklenemedi" line. */
export function InlineUnavailable({ subject, testId }: Readonly<{ subject: string; testId?: string }>) {
  return (
    <div data-testid={testId}>
      <StatePage
        kind="unavailable"
        inline
        title={`${subject} şu anda alınamıyor`}
        description="Başka bir kayıt seçebilir ya da bu bilgiyi yeniden isteyebilirsiniz."
        actions={<StateRetryButton primary={false} />}
      />
    </div>
  );
}
