import type { Metadata } from "next";
import type { ReactNode } from "react";

import { buildZentraDocumentTitle } from "@/lib/brand/zentra-brand";

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
import "../design-system/zentra-premium.css";
import "../design-system/zentra-surfaces.css";
import "../design-system/zentra-surfaces-features.css";
import "../design-system/zentra-surfaces-modules.css";

export const metadata: Metadata = {
  title: buildZentraDocumentTitle(),
  description:
    "Nevaxis Zentra — Fabrikanızın Akıllı Operasyon Sistemi: üretim, kalite, bakım ve doğrulanmış operasyonel yürütme.",
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
