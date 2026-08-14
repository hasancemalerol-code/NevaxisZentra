import Link from "next/link";
import type { CSSProperties, ReactNode } from "react";

import {
  canOpenDashboardTarget,
  type DashboardNavTarget,
} from "./dashboard-navigation";

type DashboardNavigationActionProps = Readonly<{
  target: DashboardNavTarget;
  grantedPermissionKeys?: readonly string[];
  className?: string;
  style?: CSSProperties;
  ariaLabel: string;
  children: ReactNode;
}>;

export function DashboardNavigationAction({
  target,
  grantedPermissionKeys = [],
  className,
  style,
  ariaLabel,
  children,
}: DashboardNavigationActionProps) {
  const canNavigate = canOpenDashboardTarget(
    grantedPermissionKeys,
    target,
  );

  if (!canNavigate) {
    return (
      <button
        aria-label={`${ariaLabel}. ${target.deniedReason}`}
        className={className}
        disabled
        style={{
          ...style,
          cursor: "not-allowed",
        }}
        title={target.deniedReason}
        type="button"
      >
        {children}
      </button>
    );
  }

  return (
    <Link
      aria-label={ariaLabel}
      className={className}
      href={target.href}
      style={{
        ...style,
        cursor: "pointer",
        textDecoration: "none",
      }}
    >
      {children}
    </Link>
  );
}
