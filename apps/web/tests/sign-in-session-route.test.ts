import assert from "node:assert/strict";
import test from "node:test";

import { POST } from "../src/app/(auth)/giris/oturum/route";
import { isSafeAppReturnUrl } from "../src/features/saas-identity/lib/saas-identity-return-url";

const ORIGINAL_FETCH = globalThis.fetch;
const ORIGINAL_API_URL = process.env.NEVAXIS_FACTOS_API_BASE_URL;
const SESSION_COOKIE = "zentra_session=opaque; Path=/; HttpOnly; SameSite=Lax";

test.beforeEach(() => {
  process.env.NEVAXIS_FACTOS_API_BASE_URL = "http://api.example.test";
});

test.afterEach(() => {
  globalThis.fetch = ORIGINAL_FETCH;
  if (ORIGINAL_API_URL === undefined) {
    delete process.env.NEVAXIS_FACTOS_API_BASE_URL;
  } else {
    process.env.NEVAXIS_FACTOS_API_BASE_URL = ORIGINAL_API_URL;
  }
});

function mockApi(status: number, body: unknown, cookies: string[] = []): void {
  globalThis.fetch = (async () => {
    const headers = new Headers({ "content-type": "application/json" });
    for (const cookie of cookies) {
      headers.append("set-cookie", cookie);
    }
    return new Response(JSON.stringify(body), { status, headers });
  }) as typeof fetch;
}

function signInRequest(fields: Record<string, string>): Request {
  const form = new FormData();
  for (const [key, value] of Object.entries(fields)) {
    form.set(key, value);
  }
  return new Request("http://localhost:3002/giris/oturum", {
    method: "POST",
    body: form,
  });
}

const CREDENTIALS = {
  organizationContext: "dev-pilot-tenant",
  email: "user@example.test",
  password: "not-a-real-password",
};

// Regression for the P0: a direct (non-MFA) sign-in must end with the session
// cookie set by this route and a redirect into the application.
test("authenticated sign-in forwards the session cookie and redirects into the app", async () => {
  mockApi(200, { outcome: "authenticated" }, [SESSION_COOKIE]);

  const response = await POST(signInRequest({ ...CREDENTIALS, returnUrl: "/fabrika" }));

  assert.equal(response.status, 303);
  assert.equal(new URL(response.headers.get("location")!).pathname, "/fabrika");
  assert.deepEqual(response.headers.getSetCookie(), [SESSION_COOKIE]);
});

test("mfa_required redirects to MFA verification with the cookie", async () => {
  mockApi(200, { outcome: "mfa_required" }, [SESSION_COOKIE]);

  const response = await POST(signInRequest(CREDENTIALS));

  assert.equal(response.status, 303);
  assert.equal(new URL(response.headers.get("location")!).pathname, "/mfa-dogrula");
  assert.deepEqual(response.headers.getSetCookie(), [SESSION_COOKIE]);
});

test("failed authentication issues no cookie and returns to sign-in", async () => {
  mockApi(401, { code: "invalid-credentials" });

  const response = await POST(signInRequest(CREDENTIALS));

  assert.equal(response.status, 303);
  const location = new URL(response.headers.get("location")!);
  assert.equal(location.pathname, "/giris");
  assert.equal(location.searchParams.get("auth"), "denied");
  assert.deepEqual(response.headers.getSetCookie(), []);
});

test("hostile returnUrl values never redirect off-origin after sign-in", async () => {
  for (const hostile of ["//evil.test", "/\\evil.test", "/\t/evil.test", "/\n/evil.test", "https://evil.test"]) {
    mockApi(200, { outcome: "authenticated" }, [SESSION_COOKIE]);

    const response = await POST(signInRequest({ ...CREDENTIALS, returnUrl: hostile }));

    const location = new URL(response.headers.get("location")!);
    assert.equal(location.origin, "http://localhost:3002", `redirected off-origin for ${JSON.stringify(hostile)}`);
  }
});

test("isSafeAppReturnUrl rejects control characters and off-site forms", () => {
  for (const bad of ["", "//x", "/\\x", "/\t/x", "/\r/x", "/\n/x", "/\u0000x", "https://x", "x"]) {
    assert.equal(isSafeAppReturnUrl(bad), false, JSON.stringify(bad));
  }
  for (const good of ["/", "/fabrika", "/giris?returnUrl=%2Ffabrika"]) {
    assert.equal(isSafeAppReturnUrl(good), true, good);
  }
});
