import Image from "next/image";

import { FACTOS_BRAND } from "@/lib/brand/factos-brand";

type FactosBrandLockupProps = Readonly<{
  className?: string;
  priority?: boolean;
}>;

export function FactosBrandLockup({
  className,
  priority = false,
}: FactosBrandLockupProps) {
  return (
    <Image
      alt={FACTOS_BRAND.lockupAlt}
      className={className}
      height={56}
      priority={priority}
      src={FACTOS_BRAND.lockupImagePath}
      width={200}
    />
  );
}
