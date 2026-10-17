"use client";

import Link from "next/link";
import { useEffect, useRef } from "react";

export type SubNavItem = Readonly<{ href: string; label: string; active: boolean }>;
export type SubNavGroup = Readonly<{ label?: string; items: readonly SubNavItem[] }>;

/**
 * The one module sub-navigation (tabs) for Bakım, Kalite, Vardiyalar, ERP, Teams and the other module homes.
 * Groups render as separate clusters on one row and wrap on narrow screens instead of overflowing.
 */
export function SubNav({ label, groups }: Readonly<{ label: string; groups: readonly SubNavGroup[] }>) {
  const navRef = useRef<HTMLElement | null>(null);

  // On narrow screens the tabs scroll horizontally: bring the current tab into view.
  useEffect(() => {
    const nav = navRef.current;
    const current = nav?.querySelector<HTMLElement>('[aria-current="page"]');
    if (!nav || !current || nav.scrollWidth <= nav.clientWidth) return;
    nav.scrollLeft = Math.max(0, current.offsetLeft - nav.clientWidth / 2 + current.offsetWidth / 2);
  }, [groups]);

  return (
    <nav ref={navRef} className="zx-subnav" aria-label={label}>
      {groups.map((group, groupIndex) => (
        <div className="zx-subnav__group" key={group.label ?? groupIndex} role="group" aria-label={group.label}>
          {group.label ? <span className="zx-subnav__group-label" aria-hidden="true">{group.label}</span> : null}
          {group.items.map((item) => (
            <Link
              key={item.href}
              href={item.href}
              className="zx-subnav__link"
              aria-current={item.active ? "page" : undefined}
            >
              {item.label}
            </Link>
          ))}
        </div>
      ))}
    </nav>
  );
}
