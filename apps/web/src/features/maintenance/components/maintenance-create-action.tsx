import Link from "next/link";
import type { ReactNode } from "react";

type MaintenanceCreateActionProps = Readonly<{
  href: string;
  label: string;
  icon: ReactNode;
  variant: "primary" | "secondary" | "tertiary";
  canManage: boolean;
  deniedReason: string;
}>;

export function MaintenanceCreateAction({
  href,
  label,
  icon,
  variant,
  canManage,
  deniedReason,
}: MaintenanceCreateActionProps) {
  const className = `maintenance-dashboard-action maintenance-dashboard-action--${variant}`;

  if (!canManage) {
    return (
      <button
        aria-label={`${label}. ${deniedReason}`}
        className={className}
        disabled
        title={deniedReason}
        type="button"
      >
        {icon}
        <span>
          <strong>{label}</strong>
          <small>{deniedReason}</small>
        </span>
      </button>
    );
  }

  return (
    <Link className={className} href={href}>
      {icon}
      {label}
    </Link>
  );
}
