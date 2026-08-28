import type { ComponentPropsWithoutRef, ReactNode } from "react";

import { Badge } from "./badge";

type PageHeaderProps = Readonly<{
  eyebrow?: string;
  title: string;
  description?: string;
  actions?: ReactNode;
  preview?: boolean;
}>;

export function PageHeader({ eyebrow, title, description, actions, preview = false }: PageHeaderProps) {
  return (
    <header className="page-header">
      <div className="page-header-copy">
        <div className="page-header-eyebrow">
          {eyebrow ? <span>{eyebrow}</span> : null}
          {preview ? <Badge tone="information">UI önizleme</Badge> : null}
        </div>
        <h1>{title}</h1>
        {description ? <p>{description}</p> : null}
      </div>
      {actions ? <div className="page-header-actions">{actions}</div> : null}
    </header>
  );
}

type SectionProps = Readonly<{
  title: string;
  description?: string;
  action?: ReactNode;
  children: ReactNode;
  className?: string;
}>;

export function Section({ title, description, action, children, className = "" }: SectionProps) {
  return (
    <section className={`section ${className}`.trim()}>
      <div className="section-heading">
        <div>
          <h2>{title}</h2>
          {description ? <p>{description}</p> : null}
        </div>
        {action ? <div className="section-action">{action}</div> : null}
      </div>
      <div className="section-content">{children}</div>
    </section>
  );
}

export function PageContent({
  children,
  ...props
}: Readonly<{ children: ReactNode } & ComponentPropsWithoutRef<"div">>) {
  return (
    <div className="page-content" {...props}>
      {children}
    </div>
  );
}

export function Divider() {
  return <hr className="divider" />;
}
