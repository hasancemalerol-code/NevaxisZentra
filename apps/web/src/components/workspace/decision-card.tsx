import Link from "next/link";
import type { ReactNode } from "react";
import { ArrowUpRight } from "lucide-react";

export type DecisionCardSection = Readonly<{
  label: string;
  value: ReactNode;
}>;

export type DecisionCardAction = Readonly<{
  label: string;
  href: string;
  testId?: string;
}>;

export type DecisionCardProps = Readonly<{
  eyebrow?: string;
  title: string;
  subtitle?: string;
  statusLabel?: string;
  signal: DecisionCardSection;
  cause?: DecisionCardSection;
  impact?: DecisionCardSection;
  recommendation?: DecisionCardSection;
  expectedResult?: DecisionCardSection;
  actions?: readonly DecisionCardAction[];
  testId?: string;
  children?: ReactNode;
}>;

export function DecisionCard({
  eyebrow,
  title,
  subtitle,
  statusLabel,
  signal,
  cause,
  impact,
  recommendation,
  expectedResult,
  actions,
  testId,
  children,
}: DecisionCardProps) {
  const sections = [
    { key: "signal", section: signal },
    { key: "cause", section: cause },
    { key: "impact", section: impact },
    { key: "recommendation", section: recommendation },
    { key: "expectedResult", section: expectedResult },
  ].filter((entry) => entry.section);

  return (
    <article className="commint-panel" data-testid={testId}>
      <div className="commint-panel__header">
        <div>
          {eyebrow ? <p className="commint-executive__eyebrow">{eyebrow}</p> : null}
          <h3>{title}</h3>
          {subtitle ? <p className="commint-executive__subtitle">{subtitle}</p> : null}
        </div>
        {statusLabel ? <span className="commint-kpi-card__badge">{statusLabel}</span> : null}
      </div>

      <div
        className="commint-kpi-grid"
        style={{ gridTemplateColumns: "repeat(2, minmax(0, 1fr))" }}
      >
        {sections.map(({ key, section }) => (
          <div key={key}>
            <p className="commint-kpi-card__label">{section!.label}</p>
            <p className="commint-kpi-card__detail">{section!.value}</p>
          </div>
        ))}
      </div>

      {children}

      {actions && actions.length > 0 ? (
        <div className="commint-executive__actions" style={{ marginTop: "1.25rem" }}>
          {actions.map((action) => (
            <Link
              key={action.href}
              className="commint-button"
              data-testid={action.testId}
              href={action.href}
            >
              {action.label}
              <ArrowUpRight aria-hidden="true" size={16} />
            </Link>
          ))}
        </div>
      ) : null}
    </article>
  );
}
