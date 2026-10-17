"use client";

import { useEffect, useRef } from "react";

/**
 * Marks a horizontally scrolling strip with `data-fade-start` / `data-fade-end` so CSS can show an edge fade
 * where more items are hidden, and scrolls the active item (aria-current / aria-selected) into view.
 * State is written straight to the element so scrolling never re-renders React.
 */
export function useScrollFade<T extends HTMLElement>(activeKey?: string) {
  const ref = useRef<T | null>(null);

  useEffect(() => {
    const element = ref.current;
    if (!element) {
      return undefined;
    }

    const update = () => {
      const maxScroll = element.scrollWidth - element.clientWidth;
      element.dataset.fadeStart = element.scrollLeft > 2 ? "true" : "false";
      element.dataset.fadeEnd = element.scrollLeft < maxScroll - 2 ? "true" : "false";
    };

    const active = element.querySelector<HTMLElement>('[aria-current="page"], [aria-selected="true"]');
    if (active && element.scrollWidth > element.clientWidth) {
      const elementBox = element.getBoundingClientRect();
      const activeBox = active.getBoundingClientRect();
      if (activeBox.right > elementBox.right - 24) {
        element.scrollLeft += activeBox.right - elementBox.right + 48;
      } else if (activeBox.left < elementBox.left + 24) {
        element.scrollLeft -= elementBox.left - activeBox.left + 48;
      }
    }

    update();
    element.addEventListener("scroll", update, { passive: true });
    const observer = typeof ResizeObserver === "undefined" ? null : new ResizeObserver(update);
    observer?.observe(element);
    // The strip's own box does not change when its content grows (fonts, counts), so watch the items too.
    Array.from(element.children).forEach((child) => observer?.observe(child));
    window.addEventListener("resize", update);
    let cancelled = false;
    if (typeof document !== "undefined" && document.fonts?.ready) {
      void document.fonts.ready.then(() => {
        if (!cancelled) {
          update();
        }
      });
    }
    const frame = typeof requestAnimationFrame === "function" ? requestAnimationFrame(update) : 0;

    return () => {
      cancelled = true;
      if (frame && typeof cancelAnimationFrame === "function") {
        cancelAnimationFrame(frame);
      }
      element.removeEventListener("scroll", update);
      observer?.disconnect();
      window.removeEventListener("resize", update);
    };
  }, [activeKey]);

  return ref;
}
