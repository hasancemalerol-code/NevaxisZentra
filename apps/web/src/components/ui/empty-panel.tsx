import type { ReactNode } from "react";

/** Shared empty state for lists, tables and detail panels. */
export function EmptyPanel({
  icon,
  title,
  description,
  action,
  compact = false,
  ...props
}: Readonly<{
  icon?: ReactNode;
  title: string;
  description?: string;
  action?: ReactNode;
  compact?: boolean;
  "data-testid"?: string;
}>) {
  return (
    <div className={`zx-empty${compact ? " zx-empty--compact" : ""}`} {...props}>
      {icon ? <span className="zx-empty__icon" aria-hidden="true">{icon}</span> : null}
      <strong className="zx-empty__title">{title}</strong>
      {description ? <p>{description}</p> : null}
      {action ? <div>{action}</div> : null}
    </div>
  );
}
