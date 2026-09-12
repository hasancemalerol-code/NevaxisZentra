import Image from "next/image";

import { FACTOS_BRAND } from "@/lib/brand/factos-brand";

export function FactosAuthHero() {
  return (
    <div aria-hidden="true" className="auth-layout__hero">
      <Image
        alt={FACTOS_BRAND.heroAlt}
        className="auth-layout__hero-image"
        fill
        priority
        sizes="(max-width: 960px) 100vw, 60vw"
        src={FACTOS_BRAND.heroImagePath}
      />
    </div>
  );
}
