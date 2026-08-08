import type { Metadata } from "next";
import type { ReactNode } from "react";

import { getWebApplicationConfig } from "@/config";

import "./styles.css";
import "./globals.css";
import "./dashboard.css";
import "./dashboard-layout-fix.css";
import "./command-center.css";
import "./production-execution.css";
import "./export.css";

const config = getWebApplicationConfig();

export const metadata: Metadata = {
  title: `${config.applicationName} — Factory Operations`,
  description:
    "CrevaFactOS desktop-first factory operations management platform.",
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