"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import {
  useCallback,
  useEffect,
  useRef,
  useState,
  type FormEvent,
  type KeyboardEvent,
} from "react";

import { Button } from "@/components/ui/button";
import { DEVELOPMENT_DEMO_CREDENTIALS } from "@/lib/brand/factos-brand";
import { signInSaasIdentityBrowser } from "../client/saas-identity-browser-client";
import { stripSensitiveQueryParam } from "../lib/saas-identity-csrf";
import { isSafeAppReturnUrl } from "../lib/saas-identity-return-url";
import {
  mapSaasIdentityFailureMessage,
  SAAS_IDENTITY_MESSAGES,
} from "../presentation/saas-identity-messages";
import { AuthFeedback } from "./auth-feedback";

type SignInFormProps = Readonly<{
  initialError?: string | null;
  initialNotice?: string | null;
  returnUrl?: string | null;
}>;

type SignInCredentials = Readonly<{
  organizationContext: string;
  email: string;
  password: string;
}>;

function readSignInCredentials(
  form: HTMLFormElement,
): SignInCredentials {
  const formData = new FormData(form);
  return {
    organizationContext: String(formData.get("organizationContext") ?? "").trim(),
    email: String(formData.get("email") ?? "").trim(),
    password: String(formData.get("password") ?? ""),
  };
}

export function SignInForm({
  initialError = null,
  initialNotice = null,
  returnUrl = null,
}: SignInFormProps) {
  const router = useRouter();
  const formRef = useRef<HTMLFormElement>(null);
  const submittingRef = useRef(false);
  const [organizationContext, setOrganizationContext] = useState(
    process.env.NODE_ENV === "development"
      ? DEVELOPMENT_DEMO_CREDENTIALS.organizationContext
      : "",
  );
  const [email, setEmail] = useState(
    process.env.NODE_ENV === "development"
      ? DEVELOPMENT_DEMO_CREDENTIALS.email
      : "",
  );
  const [password, setPassword] = useState("");
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(initialError);
  const [clientReady, setClientReady] = useState(false);

  useEffect(() => {
    stripSensitiveQueryParam("password");
    stripSensitiveQueryParam("email");
    stripSensitiveQueryParam("organizationContext");
    setClientReady(true);
  }, []);

  const authenticate = useCallback(
    async (credentials: SignInCredentials) => {
      if (submittingRef.current) {
        return;
      }

      if (
        !credentials.organizationContext ||
        !credentials.email ||
        !credentials.password
      ) {
        setError(SAAS_IDENTITY_MESSAGES.signInMissingFields);
        return;
      }

      submittingRef.current = true;
      setPending(true);
      setError(null);

      try {
        const result = await signInSaasIdentityBrowser({
          organizationContext: credentials.organizationContext,
          email: credentials.email,
          password: credentials.password,
        });

        if (result.status === "failure") {
          setError(mapSaasIdentityFailureMessage(result.code));
          submittingRef.current = false;
          setPending(false);
          return;
        }

        if (result.value === "mfa_required") {
          router.push("/mfa-dogrula");
          router.refresh();
          return;
        }

        const requestedReturnUrl =
          typeof window !== "undefined"
            ? new URLSearchParams(window.location.search).get("returnUrl")
            : returnUrl;
        router.push(
          isSafeAppReturnUrl(requestedReturnUrl)
            ? requestedReturnUrl
            : "/dashboard",
        );
        router.refresh();
      } catch {
        setError(SAAS_IDENTITY_MESSAGES.unexpectedError);
        submittingRef.current = false;
        setPending(false);
      }
    },
    [returnUrl, router],
  );

  const handleSubmit = useCallback(
    (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();
      event.stopPropagation();
      event.nativeEvent.stopImmediatePropagation();
      void authenticate(readSignInCredentials(event.currentTarget));
    },
    [authenticate],
  );

  const handleSubmitClick = useCallback(() => {
    const form = formRef.current;
    if (!form) {
      return;
    }

    void authenticate(readSignInCredentials(form));
  }, [authenticate]);

  const handleFieldKeyDown = useCallback(
    (event: KeyboardEvent<HTMLInputElement>) => {
      if (event.key !== "Enter") {
        return;
      }

      event.preventDefault();
      const form = formRef.current;
      if (!form) {
        return;
      }

      void authenticate(readSignInCredentials(form));
    },
    [authenticate],
  );

  return (
    <form
      ref={formRef}
      action="/giris/oturum"
      autoComplete="on"
      className="auth-form"
      method="post"
      noValidate
      onSubmit={handleSubmit}
    >
      {isSafeAppReturnUrl(returnUrl) ? (
        <input name="returnUrl" type="hidden" value={returnUrl} />
      ) : null}

      <div className="auth-form__field">
        <label htmlFor="sign-in-organization">Kuruluş kodu</label>
        <input
          id="sign-in-organization"
          name="organizationContext"
          autoComplete="organization"
          required
          value={organizationContext}
          onChange={(event) => setOrganizationContext(event.target.value)}
          onKeyDown={handleFieldKeyDown}
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
          onKeyDown={handleFieldKeyDown}
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
          onKeyDown={handleFieldKeyDown}
          disabled={pending}
        />
      </div>

      {initialNotice ? <AuthFeedback tone="info" message={initialNotice} /> : null}

      {error ? <AuthFeedback tone="error" message={error} /> : null}

      <Button
        className="auth-form__submit"
        disabled={pending}
        aria-busy={pending}
        type={clientReady ? "button" : "submit"}
        variant="primary"
        onClick={clientReady ? handleSubmitClick : undefined}
      >
        {pending ? "Oturum açılıyor…" : SAAS_IDENTITY_MESSAGES.signInTitle}
      </Button>

      <p className="auth-form__links">
        <Link href="/sifremi-unuttum">Parolamı unuttum</Link>
      </p>
    </form>
  );
}
