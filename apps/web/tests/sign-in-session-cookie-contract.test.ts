import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import test from "node:test";

const WEB_ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "..");

function readSource(relativePath: string): string {
  return readFileSync(resolve(WEB_ROOT, relativePath), "utf8");
}

// Regression: the browser sign-in fetch uses credentials:"omit", so only the
// server-side /giris/oturum route can set the HttpOnly session cookie. A
// successful "authenticated" sign-in that skipped the repost left the user
// without a cookie and looped back to /giris ("Oturum açılıyor..." forever).
test("sign-in form reposts to the server session route for every successful outcome", () => {
  const source = readSource(
    "src/features/saas-identity/components/sign-in-form.tsx",
  );

  assert.match(source, /action\s*=\s*"\/giris\/oturum"/);
  assert.match(source, /fallbackForm\.submit\(\)/);

  // The repost must not be gated on a specific outcome (e.g. mfa_required).
  assert.doesNotMatch(source, /result\.value\s*===\s*"mfa_required"/);
  assert.doesNotMatch(source, /result\.value\s*!==\s*"mfa_required"/);

  // The client-side redirect that bypassed the cookie must not return.
  assert.doesNotMatch(source, /router\.push\(/);
  assert.doesNotMatch(source, /router\.refresh\(/);
});

test("server session route sets the session cookie and redirects", () => {
  const source = readSource("src/app/(auth)/giris/oturum/route.ts");

  assert.match(source, /Set-Cookie/);
  assert.match(source, /NextResponse\.redirect/);
});
