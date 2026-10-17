import type { ReactNode } from "react";
import { notFound } from "next/navigation";

import { deferredSurfacesEnabled } from "@/lib/runtime/deferred-surfaces";

export default function IntelligenceRuntimeLayout({ children }: Readonly<{ children: ReactNode }>) {
  // Internal ML operations tooling: Production answers 404 (fail closed, nothing is fetched); permission alone never exposes it.
  if (!deferredSurfacesEnabled()) {
    notFound();
  }
  return children;
}
