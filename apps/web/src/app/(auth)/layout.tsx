import type { ReactNode } from "react";
import { redirect } from "next/navigation";

import { getWebApplicationConfig } from "@/config";
import { resolveSaasIdentitySession } from "@/features/saas-identity/server/saas-identity-session";

import "./auth.css";

type AuthLayoutProps = Readonly<{
  children: ReactNode;
}>;

export default async function AuthLayout({ children }: AuthLayoutProps) {
  const session = await resolveSaasIdentitySession();

  if (session.status === "authenticated") {
    redirect("/dashboard");
  }

  const config = getWebApplicationConfig();

  return (
    <div className="auth-layout">
      <div className="auth-layout__frame">
        <p className="visually-hidden">{config.applicationName}</p>
        {children}
      </div>
    </div>
  );
}
