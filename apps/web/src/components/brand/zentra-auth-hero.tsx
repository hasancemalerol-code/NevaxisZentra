import { ZENTRA_BRAND } from "@/lib/brand/zentra-brand";

export function ZentraAuthHero() {
  return (
    <section
      aria-label={ZENTRA_BRAND.heroAlt}
      className="auth-hero"
      style={{ backgroundColor: ZENTRA_BRAND.heroBackgroundColor }}
    >
      <div className="auth-hero__content">
        <ZentraBrandWordmark />
        <p className="auth-hero__descriptor">{ZENTRA_BRAND.descriptor}</p>
      </div>
    </section>
  );
}

function ZentraBrandWordmark() {
  return (
    <div aria-label={ZENTRA_BRAND.lockupAlt} className="auth-hero__wordmark" role="img">
      <span className="auth-hero__wordmark-endorsement">{ZENTRA_BRAND.endorsement}</span>
      <span className="auth-hero__wordmark-product">{ZENTRA_BRAND.fullProductName}</span>
    </div>
  );
}

/** @deprecated Use ZentraAuthHero */
export const FactosAuthHero = ZentraAuthHero;
