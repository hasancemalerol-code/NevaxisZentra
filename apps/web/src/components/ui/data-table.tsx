import type { ComponentPropsWithoutRef, ReactNode } from "react";

type DataTableProps = Readonly<{
  /** Accessible name of the table (announced by screen readers and used as the scroll region label). */
  label: string;
  children: ReactNode;
  minWidth?: number;
}> & Omit<ComponentPropsWithoutRef<"div">, "children">;

/**
 * Dense-data table shell: horizontal scroll stays inside the card (never the page), the header is sticky, the scroll
 * region is keyboard reachable and the table itself carries an accessible name.
 */
export function DataTable({ label, children, minWidth, className = "", ...props }: DataTableProps) {
  return (
    <div className={`zx-table-wrap ${className}`.trim()} role="region" aria-label={label} tabIndex={0} {...props}>
      <table className="zx-table" aria-label={label} style={minWidth ? { minWidth } : undefined}>
        {children}
      </table>
    </div>
  );
}
