import type { ComponentPropsWithoutRef, ReactNode } from "react";

type PanelProps = Readonly<{
  title: string;
  subtitle?: string;
  actions?: ReactNode;
  /** Removes inner padding so a table can run edge to edge. */
  flush?: boolean;
  headingId?: string;
  children: ReactNode;
}> & Omit<ComponentPropsWithoutRef<"section">, "title" | "children">;

/** Titled content card. Its heading is an h2 so every screen keeps the h1 → h2 → h3 outline. */
export function Panel({ title, subtitle, actions, flush = false, headingId, children, className = "", ...props }: PanelProps) {
  const labelledBy = headingId ?? `panel-${title.toLowerCase().replace(/[^a-z0-9ğüşöçıİ]+/gi, "-")}`;
  return (
    <section className={`zx-panel${flush ? " zx-panel--flush" : ""} ${className}`.trim()} aria-labelledby={labelledBy} {...props}>
      <header className="zx-panel__header">
        <div>
          <h2 id={labelledBy} className="zx-panel__title">{title}</h2>
          {subtitle ? <p className="zx-panel__subtitle">{subtitle}</p> : null}
        </div>
        {actions ? <div className="zx-panel__actions">{actions}</div> : null}
      </header>
      {children}
    </section>
  );
}
