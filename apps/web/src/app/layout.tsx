import type { Metadata } from "next";
import type { ReactNode } from "react";

import { buildFactosDocumentTitle } from "@/lib/brand/factos-brand";

import "./styles.css";
import "../design-system/industrial-foundation.css";
import "../design-system/industrial-hardening.css";
import "./globals.css";
import "./dashboard.css";
import "./dashboard-layout-fix.css";
import "./command-center.css";
import "./production-execution.css";
import "./export.css";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";

export const metadata: Metadata = {
  title: buildFactosDocumentTitle(),
  description:
    "FactOS by NEVAXIS — factory operations software for production, quality, maintenance, and verified operational execution.",
};

interface RootLayoutProps {
  children: ReactNode;
}

export default function RootLayout({
  children,
}: Readonly<RootLayoutProps>) {
  return (
    <html lang="tr">
      <body>{children}</body>
    </html>
  );
}
