import type { ReactNode } from "react";
import { redirect } from "next/navigation";

import { FactosAuthHero } from "@/components/brand/factos-auth-hero";
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
      <FactosAuthHero />
      <div className="auth-layout__panel">
        <p className="visually-hidden">{config.applicationName}</p>
        <div className="auth-layout__frame">{children}</div>
      </div>
    </div>
  );
}
