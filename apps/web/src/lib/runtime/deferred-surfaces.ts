/**
 * Deferred and demo-only surfaces.
 *
 * The Digital Workforce module is deferred, the Golden Demo / presentation entry cards are sales-demo material and the
 * Intelligence Runtime control centre (routing decisions, dataset versions, model registry, evaluation runs) is internal ML
 * tooling. None of them may appear on a normal Production installation. They are visible only in an explicitly allowed context:
 *   - a Demo or Presentation deployment channel (NEVAXIS_FACTOS_DEPLOYMENT_CHANNEL = DEMO | PRESENTATION),
 *   - a local development server (NODE_ENV = development),
 *   - or an explicit opt-in (NEVAXIS_ZENTRA_ENABLE_DEFERRED_SURFACES = 1).
 * Anything else — including an unset channel in a production build — is OFF (fail closed).
 */

/** Route prefix of the deferred Digital Workforce module. */
export const DEFERRED_WORKFORCE_PREFIX = "/dijital-is-gucu";

type Env = Readonly<Record<string, string | undefined>>;

export function deferredSurfacesEnabled(env: Env = process.env): boolean {
  if (env.NEVAXIS_ZENTRA_ENABLE_DEFERRED_SURFACES === "1") {
    return true;
  }

  const channel = env.NEVAXIS_FACTOS_DEPLOYMENT_CHANNEL?.trim().toUpperCase();
  if (channel === "DEMO" || channel === "PRESENTATION") {
    return true;
  }

  return env.NODE_ENV === "development";
}

/** Every route prefix that exists only for deferred / demo deployments. */
/** Internal ML operations tooling (routing, datasets, model registry, evaluations): never a customer Production surface. */
export const INTERNAL_INTELLIGENCE_RUNTIME_PREFIX = "/intelligence-runtime";

export const DEFERRED_SURFACE_PREFIXES: readonly string[] = [
  DEFERRED_WORKFORCE_PREFIX,
  "/sunum-modu",
  "/golden-demo",
  INTERNAL_INTELLIGENCE_RUNTIME_PREFIX,
];

export function isDeferredSurfaceHref(href: string): boolean {
  const path = href.split(/[?#]/)[0];
  return DEFERRED_SURFACE_PREFIXES.some((prefix) => path === prefix || path.startsWith(`${prefix}/`));
}

/** True when a link to `href` may be shown to the current deployment. */
export function isSurfaceHrefVisible(href: string, env: Env = process.env): boolean {
  return !isDeferredSurfaceHref(href) || deferredSurfacesEnabled(env);
}
