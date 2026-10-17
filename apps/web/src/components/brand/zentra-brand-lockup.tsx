import { ZENTRA_BRAND } from "@/lib/brand/zentra-brand";

type ZentraBrandLockupProps = Readonly<{
  className?: string;
}>;

export function ZentraBrandLockup({ className }: ZentraBrandLockupProps) {
  return (
    <div
      aria-label={ZENTRA_BRAND.lockupAlt}
      className={className}
      role="img"
      style={{
        display: "inline-flex",
        flexDirection: "column",
        gap: "0.125rem",
        lineHeight: 1.1,
      }}
    >
      <span
        style={{
          fontSize: "0.75rem",
          fontWeight: 600,
          letterSpacing: "0.28em",
          textTransform: "uppercase",
        }}
      >
        NEVAXIS
      </span>
      <span style={{ fontSize: "1.35rem", fontWeight: 700, letterSpacing: "0.04em" }}>
        ZENTRA
      </span>
    </div>
  );
}

/** @deprecated Use ZentraBrandLockup */
export const FactosBrandLockup = ZentraBrandLockup;
