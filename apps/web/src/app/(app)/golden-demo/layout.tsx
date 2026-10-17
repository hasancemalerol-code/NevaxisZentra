import type { ReactNode } from "react";
import { notFound } from "next/navigation";

import { deferredSurfacesEnabled } from "@/lib/runtime/deferred-surfaces";

export default function DeferredSurfaceLayout({ children }: Readonly<{ children: ReactNode }>) {
  // Demo / presentation surface: Production answers 404 (fail closed), permission alone never exposes it.
  if (!deferredSurfacesEnabled()) {
    notFound();
  }
  return children;
}
