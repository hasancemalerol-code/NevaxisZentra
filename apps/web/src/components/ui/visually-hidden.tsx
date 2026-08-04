import type { ReactNode } from "react";

export function VisuallyHidden({ children }: Readonly<{ children: ReactNode }>) {
  return <span className="visually-hidden">{children}</span>;
}
