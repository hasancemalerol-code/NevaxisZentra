"use client";

import { useEffect } from "react";
import { usePathname } from "next/navigation";

/**
 * Native date/time inputs show the browser's own mask ("gg.aa.yyyy --:--") while they are empty, which looks unstyled next to the
 * product's other controls. This only tags empty fields (data-zx-empty) and gives them a Turkish placeholder; the CSS in
 * zentra-premium.css hides the browser mask while a field is empty and not focused. The input type and its value are untouched,
 * so every form keeps submitting exactly the same wire value.
 */
const PLACEHOLDERS: Readonly<Record<string, string>> = {
  date: "Tarih seçin",
  "datetime-local": "Tarih ve saat seçin",
  time: "Saat seçin",
};

function tag(input: HTMLInputElement): void {
  const placeholder = PLACEHOLDERS[input.type];
  if (!placeholder) return;
  if (input.dataset.zxPlaceholder !== placeholder) input.dataset.zxPlaceholder = placeholder;
  const empty = input.value === "" ? "true" : "false";
  if (input.dataset.zxEmpty !== empty) input.dataset.zxEmpty = empty;
}

function tagAll(): void {
  document.querySelectorAll<HTMLInputElement>('input[type="date"], input[type="datetime-local"], input[type="time"]').forEach(tag);
}

export function DateFieldEnhancer() {
  const pathname = usePathname();

  useEffect(() => {
    tagAll();
    const onChange = (event: Event) => {
      if (event.target instanceof HTMLInputElement) tag(event.target);
    };
    document.addEventListener("input", onChange, true);
    document.addEventListener("change", onChange, true);
    document.addEventListener("focusout", onChange, true);

    let frame = 0;
    const observer = new MutationObserver(() => {
      cancelAnimationFrame(frame);
      frame = requestAnimationFrame(tagAll);
    });
    observer.observe(document.querySelector("main") ?? document.body, { childList: true, subtree: true });
    // Values set programmatically (controlled inputs) fire no events, so a slow re-scan keeps the flag honest.
    const interval = window.setInterval(tagAll, 1000);

    return () => {
      document.removeEventListener("input", onChange, true);
      document.removeEventListener("change", onChange, true);
      document.removeEventListener("focusout", onChange, true);
      cancelAnimationFrame(frame);
      observer.disconnect();
      window.clearInterval(interval);
    };
  }, [pathname]);

  return null;
}
