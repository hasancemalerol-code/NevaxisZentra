import { ChevronDown } from "lucide-react";
import type { ReactNode } from "react";

type CollapsibleSectionProps = Readonly<{
  title: string;
  /** Short state shown beside the title while collapsed, e.g. "Henüz veri yok". */
  hint?: string;
  defaultOpen?: boolean;
  children: ReactNode;
}>;

/**
 * A section that may become relevant later: it takes one compact row until opened, so empty schema-supported blocks never
 * create dead space.
 */
export function CollapsibleSection({ title, hint, defaultOpen = false, children }: CollapsibleSectionProps) {
  return (
    <details className="zx-collapsible" open={defaultOpen || undefined}>
      <summary>
        <span className="zx-collapsible__title">{title}</span>
        {hint ? <span className="zx-collapsible__hint">{hint}</span> : null}
        <ChevronDown aria-hidden="true" className="zx-collapsible__chevron" size={16} />
      </summary>
      <div className="zx-collapsible__body">{children}</div>
    </details>
  );
}
