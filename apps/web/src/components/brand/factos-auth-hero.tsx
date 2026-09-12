import Image from "next/image";

import { FACTOS_BRAND } from "@/lib/brand/factos-brand";

export function FactosAuthHero() {
  return (
    <div
      aria-hidden="true"
      className="auth-layout__hero"
      style={{ backgroundColor: FACTOS_BRAND.heroBackgroundColor }}
    >
      <div className="auth-layout__hero-frame auth-layout__hero-frame--full">
        <Image
          alt={FACTOS_BRAND.heroAlt}
          className="auth-layout__hero-image"
          fill
          priority
          sizes="(max-width: 1100px) 0px, 70vw"
          src={FACTOS_BRAND.heroImagePath}
        />
      </div>

      <div className="auth-layout__hero-frame auth-layout__hero-frame--compact">
        <Image
          alt={FACTOS_BRAND.lockupAlt}
          className="auth-layout__hero-lockup"
          height={84}
          priority
          src={FACTOS_BRAND.lockupImagePath}
          width={320}
        />
      </div>
    </div>
  );
}
