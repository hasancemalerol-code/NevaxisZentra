/**
 * Canonical runtime env: NEVAXIS_ZENTRA_* with legacy fallbacks.
 * Precedence: Zentra → FactOS → CrevaFactOS (no merge on conflict).
 */
export function readZentraEnv(
  suffix: string,
  environment: NodeJS.ProcessEnv = process.env,
): string | undefined {
  const zentra = environment[`NEVAXIS_ZENTRA_${suffix}`]?.trim();
  if (zentra) {
    return zentra;
  }

  const factos = environment[`NEVAXIS_FACTOS_${suffix}`]?.trim();
  if (factos) {
    return factos;
  }

  const creva = environment[`CREVAFACTOS_${suffix}`]?.trim();
  return creva || undefined;
}
