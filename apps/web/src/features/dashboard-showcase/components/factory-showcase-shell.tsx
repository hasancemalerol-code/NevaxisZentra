import type { ReactNode } from "react";

import "./factory-showcase.css";

type FactoryShowcaseShellProps = Readonly<{
  children: ReactNode;
}>;

export function FactoryShowcaseShell({ children }: FactoryShowcaseShellProps) {
  return (
    <div className="factory-showcase-shell">
      <main className="factory-showcase-shell__main" id="main-content" tabIndex={-1}>
        {children}
      </main>
    </div>
  );
}
