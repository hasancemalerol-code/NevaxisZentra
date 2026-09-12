import type { ReactNode } from "react";

type AuthShellProps = Readonly<{
  title: string;
  description?: string;
  children: ReactNode;
}>;

export function AuthShell({
  title,
  description,
  children,
}: AuthShellProps) {
  return (
    <section className="auth-shell" aria-labelledby="auth-shell-title">
      <header className="auth-shell__header">
        <h1 className="auth-shell__title" id="auth-shell-title">
          {title}
        </h1>
        {description ? (
          <p className="auth-shell__description">{description}</p>
        ) : null}
      </header>
      <div className="auth-shell__body">{children}</div>
    </section>
  );
}
