import type { Metadata } from "next";
import type { ReactNode } from "react";

import { getWebApplicationConfig } from "@/config";

import "./styles.css";

const config = getWebApplicationConfig();

export const metadata: Metadata = {
  title: config.applicationName,
  description: "CrevaFactOS executable web foundation.",
};

export default function RootLayout({ children }: Readonly<{ children: ReactNode }>) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
