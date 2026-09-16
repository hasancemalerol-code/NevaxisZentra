import type { ReactNode } from "react";

type WorkspaceHeaderProps = Readonly<{
  context: string;
  title: string;
  meta?: string;
  breadcrumb?: ReactNode;
  actions?: ReactNode;
}>;

export function WorkspaceHeader({ context, title, meta, breadcrumb, actions }: WorkspaceHeaderProps) {
  return (
    <header className="fact-workspace__header">
      <div>
        {breadcrumb ? <div className="fact-workspace__breadcrumb">{breadcrumb}</div> : null}
        <p className="fact-workspace__context">{context}</p>
        <h1 className="fact-workspace__title">{title}</h1>
        {meta ? <p className="fact-workspace__meta">{meta}</p> : null}
      </div>
      {actions ? <div className="fact-command-bar__group">{actions}</div> : null}
    </header>
  );
}
