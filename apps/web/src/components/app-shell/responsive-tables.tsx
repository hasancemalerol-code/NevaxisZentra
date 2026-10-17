"use client";

import { useEffect } from "react";
import { usePathname } from "next/navigation";

/**
 * Wide data tables reflow into labelled cards on narrow screens (CSS in zentra-premium.css, max-width 820px).
 * The cell labels come from the table's own column headers, so no table has to be rewritten: this only tags the DOM
 * and keeps the table semantics (roles) intact when the layout changes.
 */
const MIN_COLUMNS = 4;

function tagTable(table: HTMLTableElement): void {
  if (table.dataset.zxStack !== undefined) return;
  const headerCells = Array.from(table.querySelectorAll<HTMLTableCellElement>("thead tr:last-child > th"));
  if (headerCells.length < MIN_COLUMNS) return;
  const labels = headerCells.map((cell) => (cell.textContent ?? "").replace(/\s+/g, " ").trim());

  table.dataset.zxStack = "true";
  table.setAttribute("role", "table");
  for (const group of Array.from(table.tHead ? [table.tHead] : [])) group.setAttribute("role", "rowgroup");
  for (const body of Array.from(table.tBodies)) body.setAttribute("role", "rowgroup");
  for (const row of Array.from(table.rows)) {
    row.setAttribute("role", "row");
    Array.from(row.cells).forEach((cell, index) => {
      if (cell.tagName === "TD") {
        cell.setAttribute("role", "cell");
        const label = cell.colSpan > 1 ? "" : labels[index] ?? "";
        cell.dataset.label = label;
      } else {
        cell.setAttribute("role", "columnheader");
      }
    });
  }
}

function tagAll(): void {
  document.querySelectorAll<HTMLTableElement>("main table").forEach(tagTable);
}

export function ResponsiveTables() {
  const pathname = usePathname();

  useEffect(() => {
    tagAll();
    const target = document.querySelector("main") ?? document.body;
    let frame = 0;
    const observer = new MutationObserver(() => {
      cancelAnimationFrame(frame);
      frame = requestAnimationFrame(tagAll);
    });
    observer.observe(target, { childList: true, subtree: true });
    return () => {
      cancelAnimationFrame(frame);
      observer.disconnect();
    };
  }, [pathname]);

  return null;
}
