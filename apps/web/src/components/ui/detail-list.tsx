import type { ReactNode } from "react";

/** Key/value facts of one record. Items are rendered as a semantic definition list. */
export function DetailList({ items, ...props }: Readonly<{ items: readonly { label: string; value: ReactNode }[]; "data-testid"?: string }>) {
  return (
    <dl className="zx-detail-list" {...props}>
      {items.map((item) => (
        <div key={item.label}>
          <dt>{item.label}</dt>
          <dd>{item.value}</dd>
        </div>
      ))}
    </dl>
  );
}

/** A titled group of content inside a detail panel (explanation, recommendation, evidence …). */
export function DetailBlock({ title, headingId, children }: Readonly<{ title: string; headingId: string; children: ReactNode }>) {
  return (
    <section className="zx-block" aria-labelledby={headingId}>
      <h3 id={headingId} className="zx-block__title">{title}</h3>
      {children}
    </section>
  );
}
