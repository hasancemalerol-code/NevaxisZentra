import Link from "next/link";
import type { ReactNode } from "react";

import { displayDataName } from "@/lib/presentation/display-name";
import { getMaintenanceAssetTypeLabel } from "../presentation/maintenance-labels";

type MaintenanceAssetCellProps = Readonly<{
  assetReference: string;
  assetType?: string;
  assetName?: string;
  href?: string;
}>;

export function MaintenanceAssetCell({
  assetReference,
  assetType,
  assetName,
  href,
}: MaintenanceAssetCellProps) {
  const typeLabel = assetType ? getMaintenanceAssetTypeLabel(assetType) : undefined;
  const secondaryParts = [typeLabel, assetName ? displayDataName(assetName) : undefined].filter(Boolean);
  const secondary = secondaryParts.length > 0 ? secondaryParts.join(" · ") : undefined;

  return (
    <div className="maintenance-asset-cell">
      {href ? (
        <Link href={href} className="maintenance-asset-cell__reference">
          {assetReference}
        </Link>
      ) : (
        <span className="maintenance-asset-cell__reference">{assetReference}</span>
      )}
      {secondary ? (
        <span className="maintenance-asset-cell__meta" title={secondary}>
          {secondary}
        </span>
      ) : null}
    </div>
  );
}

type MaintenanceTableIdentifierCellProps = Readonly<{
  primary: ReactNode;
  secondary?: ReactNode;
  href?: string;
}>;

export function MaintenanceTableIdentifierCell({
  primary,
  secondary,
  href,
}: MaintenanceTableIdentifierCellProps) {
  return (
    <div className="maintenance-table__identifier">
      {href ? <Link href={href}>{primary}</Link> : primary}
      {secondary ? <small>{secondary}</small> : null}
    </div>
  );
}

type MaintenanceTableActionLinkProps = Readonly<{
  href: string;
  children: ReactNode;
  title?: string;
}>;

export function MaintenanceTableActionLink({
  href,
  children,
  title,
}: MaintenanceTableActionLinkProps) {
  return (
    <Link href={href} className="maintenance-table-action" title={title}>
      {children}
    </Link>
  );
}

type MaintenanceTableActionGroupProps = Readonly<{
  children: ReactNode;
}>;

export function MaintenanceTableActionGroup({ children }: MaintenanceTableActionGroupProps) {
  return <div className="maintenance-table-action-group">{children}</div>;
}

type MaintenanceSectionHeaderProps = Readonly<{
  title: string;
  subtitle?: string;
  actionHref?: string;
  actionLabel?: string;
  actionDisabledReason?: string;
  className?: string;
}>;

export function MaintenanceSectionHeader({
  title,
  subtitle,
  actionHref,
  actionLabel,
  actionDisabledReason,
  className = "maintenance-dashboard-section__header",
}: MaintenanceSectionHeaderProps) {
  return (
    <header className={className}>
      <div className="maintenance-section-header__titles">
        <h2>{title}</h2>
        {subtitle ? <p>{subtitle}</p> : null}
      </div>
      {actionLabel && actionDisabledReason ? (
        <button
          aria-label={`${actionLabel}. ${actionDisabledReason}`}
          className="maintenance-section-action"
          disabled
          title={actionDisabledReason}
          type="button"
        >
          {actionLabel}
        </button>
      ) : actionHref && actionLabel ? (
        <Link href={actionHref} className="maintenance-section-action">
          {actionLabel}
        </Link>
      ) : null}
    </header>
  );
}

type MaintenanceEmptyStateProps = Readonly<{
  icon?: ReactNode;
  title: string;
  description?: string;
}>;

export function MaintenanceEmptyState({ icon, title, description }: MaintenanceEmptyStateProps) {
  return (
    <div className="maintenance-empty-state maintenance-empty-state--premium">
      {icon ? <div className="maintenance-empty-state__icon">{icon}</div> : null}
      <strong>{title}</strong>
      {description ? <p>{description}</p> : null}
    </div>
  );
}
