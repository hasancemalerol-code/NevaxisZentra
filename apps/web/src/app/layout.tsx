import type { Metadata } from "next";
import type { ReactNode } from "react";

import { buildFactosDocumentTitle } from "@/lib/brand/factos-brand";

import "./styles.css";
import "./globals.css";
import "./dashboard.css";
import "./dashboard-layout-fix.css";
import "./command-center.css";
import "./production-execution.css";
import "./export.css";

export const metadata: Metadata = {
  title: buildFactosDocumentTitle(),
  description:
    "FactOS by NEVAXIS — AI factory operations platform for real-time visibility, planning, quality, and human-in-the-loop execution.",
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
