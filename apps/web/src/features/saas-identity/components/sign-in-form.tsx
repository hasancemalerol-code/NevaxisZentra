"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useState, type FormEvent } from "react";

import { Button } from "@/components/ui/button";
import { signInSaasIdentityBrowser } from "../client/saas-identity-browser-client";
import {
  mapSaasIdentityFailureMessage,
  SAAS_IDENTITY_MESSAGES,
} from "../presentation/saas-identity-messages";
import { AuthFeedback } from "./auth-feedback";

export function SignInForm() {
  const router = useRouter();
  const [organizationContext, setOrganizationContext] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = useCallback(
    async (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();
      setPending(true);
      setError(null);

      const result = await signInSaasIdentityBrowser({
        organizationContext: organizationContext.trim(),
        email: email.trim(),
        password,
      });

      if (result.status === "failure") {
        setError(mapSaasIdentityFailureMessage(result.code));
        setPending(false);
        return;
      }

      if (result.value === "mfa_required") {
        router.push("/mfa-dogrula");
        router.refresh();
        return;
      }

      const returnUrl =
        typeof window !== "undefined"
          ? new URLSearchParams(window.location.search).get("returnUrl")
          : null;
      router.push(
        returnUrl && returnUrl.startsWith("/") && !returnUrl.startsWith("//")
          ? returnUrl
          : "/dashboard",
      );
      router.refresh();
    },
    [email, organizationContext, password, router],
  );

  return (
    <form className="auth-form" onSubmit={handleSubmit} noValidate>
      <div className="auth-form__field">
        <label htmlFor="sign-in-organization">Kuruluş kodu</label>
        <input
          id="sign-in-organization"
          name="organizationContext"
          autoComplete="organization"
          required
          value={organizationContext}
          onChange={(event) => setOrganizationContext(event.target.value)}
          disabled={pending}
        />
      </div>

      <div className="auth-form__field">
        <label htmlFor="sign-in-email">E-posta</label>
        <input
          id="sign-in-email"
          name="email"
          type="email"
          autoComplete="username"
          inputMode="email"
          required
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          disabled={pending}
        />
      </div>

      <div className="auth-form__field">
        <label htmlFor="sign-in-password">Parola</label>
        <input
          id="sign-in-password"
          name="password"
          type="password"
          autoComplete="current-password"
          required
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          disabled={pending}
        />
      </div>

      {error ? <AuthFeedback tone="error" message={error} /> : null}

      <Button
        className="auth-form__submit"
        disabled={pending}
        type="submit"
        variant="primary"
      >
        {pending ? "Oturum açılıyor…" : SAAS_IDENTITY_MESSAGES.signInTitle}
      </Button>

      <p className="auth-form__links">
        <Link href="/sifremi-unuttum">Parolamı unuttum</Link>
      </p>
    </form>
  );
}
