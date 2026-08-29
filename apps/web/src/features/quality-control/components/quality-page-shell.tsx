import type { ReactNode } from "react";

import { QualityBackLink } from "./quality-back-link";
import { QualityWorkspaceNav } from "./quality-workspace-nav";

type QualityNavActive = "overview" | "ncr" | "follow-up" | "reporting" | "none";

export type QualityPageShellProps = Readonly<{
  backHref: string;
  backLabel: string;
  eyebrow: string;
  title: string;
  subtitle?: string;
  actions?: ReactNode;
  nav?: QualityNavActive;
  children: ReactNode;
  footer?: ReactNode;
}>;

export function QualityPageShell({
  backHref,
  backLabel,
  eyebrow,
  title,
  subtitle,
  actions,
  nav = "none",
  children,
  footer,
}: QualityPageShellProps) {
  return (
    <main className="production-execution-page quality-page">
      <header className="quality-page-header production-execution-header">
        <QualityBackLink href={backHref}>{backLabel}</QualityBackLink>
        <div className="quality-page-header__intro">
          <p>{eyebrow}</p>
          <h1>{title}</h1>
          {subtitle ? <span>{subtitle}</span> : null}
        </div>
        {actions ? <div className="quality-page-header__actions">{actions}</div> : null}
      </header>

      {nav !== "none" ? <QualityWorkspaceNav active={nav} /> : null}

      <div className="quality-page-content">{children}</div>

      {footer}
    </main>
  );
}
