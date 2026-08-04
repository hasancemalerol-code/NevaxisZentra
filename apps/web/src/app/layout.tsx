import type { Metadata } from "next";
import type { ReactNode } from "react";

import { getWebApplicationConfig } from "@/config";

import "./styles.css";

const config = getWebApplicationConfig();

export const metadata: Metadata = {
  title: `${config.applicationName} — Factory Operations`,
  description: "CrevaFactOS desktop-first factory operations workspace.",
};

export default function RootLayout({ children }: Readonly<{ children: ReactNode }>) {
  return (
    <html lang="tr">
      <body>{children}</body>
    </html>
  );
}
