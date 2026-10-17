import type { ComponentPropsWithoutRef, ReactNode } from "react";

type PageHeaderProps = Readonly<{
  eyebrow?: string;
  title: string;
  description?: string;
  actions?: ReactNode;
  preview?: boolean;
  /** Use 2 when the header introduces a section embedded in another screen that already owns the h1. */
  headingLevel?: 1 | 2;
}>;

export function PageHeader({ eyebrow, title, description, actions, preview: _preview = false, headingLevel = 1 }: PageHeaderProps) {
  const Heading = headingLevel === 2 ? "h2" : "h1";
  return (
    <header className="zx-page-header">
      <div className="zx-page-header__copy">
        {eyebrow ? (
          <p className="zx-page-header__eyebrow">
            {eyebrow}
            
          </p>
        ) : null}
        <Heading>{title}</Heading>
        {description ? <p className="zx-page-header__description">{description}</p> : null}
      </div>
      {actions ? <div className="zx-page-header__actions">{actions}</div> : null}
    </header>
  );
}

/** Page frame shared by the shipping screens: max width, rhythm and responsive gutters. */
export function PageFrame({ children, ...props }: Readonly<{ children: ReactNode } & ComponentPropsWithoutRef<"div">>) {
  return (
    <div className="zx-page" {...props}>
      {children}
    </div>
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
