"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useState, type FormEvent } from "react";

import { Button } from "@/components/ui/button";
import { acceptSaasIdentityInvitationBrowser } from "../client/saas-identity-browser-client";
import { stripSensitiveQueryParam } from "../lib/saas-identity-csrf";
import {
  mapSaasIdentityFailureMessage,
  SAAS_IDENTITY_MESSAGES,
} from "../presentation/saas-identity-messages";
import { AuthFeedback } from "./auth-feedback";

type InvitationAcceptFormProps = Readonly<{
  token: string | null;
}>;

export function InvitationAcceptForm({ token }: InvitationAcceptFormProps) {
  const router = useRouter();
  const [displayName, setDisplayName] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [expired, setExpired] = useState(false);

  useEffect(() => {
    stripSensitiveQueryParam("token");
  }, []);

  const handleSubmit = useCallback(
    async (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();

      if (!token) {
        setExpired(true);
        return;
      }

      if (password !== confirmPassword) {
        setError("Parolalar eşleşmiyor.");
        return;
      }

      setPending(true);
      setError(null);

      const result = await acceptSaasIdentityInvitationBrowser({
        token,
        displayName: displayName.trim(),
        password,
      });

      setPending(false);

      if (result.status === "failure") {
        if (
          result.code === "invalid-token" ||
          result.code === "token-expired"
        ) {
          setExpired(true);
          return;
        }

        setError(mapSaasIdentityFailureMessage(
          result.code,
          SAAS_IDENTITY_MESSAGES.unexpectedError,
        ));
        return;
      }

      router.push("/giris");
      router.refresh();
    },
    [confirmPassword, displayName, password, router, token],
  );

  if (!token || expired) {
    return (
      <>
        <AuthFeedback
          tone="error"
          message={SAAS_IDENTITY_MESSAGES.invitationExpired}
        />
        <p className="auth-form__links">
          <Link href="/giris">Oturum açmaya dön</Link>
        </p>
      </>
    );
  }

  return (
    <form className="auth-form" onSubmit={handleSubmit} noValidate>
      <div className="auth-form__field">
        <label htmlFor="invitation-display-name">Görünen ad</label>
        <input
          id="invitation-display-name"
          name="displayName"
          autoComplete="name"
          required
          value={displayName}
          onChange={(event) => setDisplayName(event.target.value)}
          disabled={pending}
        />
      </div>

      <div className="auth-form__field">
        <label htmlFor="invitation-password">Parola</label>
        <input
          id="invitation-password"
          name="password"
          type="password"
          autoComplete="new-password"
          required
          minLength={12}
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          disabled={pending}
        />
      </div>

      <div className="auth-form__field">
        <label htmlFor="invitation-password-confirm">Parola (tekrar)</label>
        <input
          id="invitation-password-confirm"
          name="confirmPassword"
          type="password"
          autoComplete="new-password"
          required
          minLength={12}
          value={confirmPassword}
          onChange={(event) => setConfirmPassword(event.target.value)}
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
        {pending ? "Kaydediliyor…" : "Daveti kabul et"}
      </Button>
    </form>
  );
}
