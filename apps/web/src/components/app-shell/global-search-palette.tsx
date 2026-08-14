"use client";

import { useRouter } from "next/navigation";
import {
  useCallback,
  useEffect,
  useId,
  useMemo,
  useRef,
  useState,
  type KeyboardEvent,
} from "react";
import { Search } from "lucide-react";

import type { ShellSearchItem } from "./shell-types";

type GlobalSearchPaletteProps = Readonly<{
  items: readonly ShellSearchItem[];
  onClose: () => void;
}>;

function normalizeQuery(value: string): string {
  return value.trim().toLocaleLowerCase("tr-TR");
}

export function GlobalSearchPalette({
  items,
  onClose,
}: GlobalSearchPaletteProps) {
  const router = useRouter();
  const listId = useId();
  const inputRef = useRef<HTMLInputElement>(null);
  const [query, setQuery] = useState("");
  const [activeIndex, setActiveIndex] = useState(0);

  const filteredItems = useMemo(() => {
    const normalizedQuery = normalizeQuery(query);
    if (!normalizedQuery) {
      return items;
    }

    return items.filter((item) => {
      const haystack = `${item.label} ${item.groupLabel}`.toLocaleLowerCase("tr-TR");
      return haystack.includes(normalizedQuery);
    });
  }, [items, query]);

  const safeActiveIndex = Math.min(
    activeIndex,
    Math.max(filteredItems.length - 1, 0),
  );

  useEffect(() => {
    const frame = window.requestAnimationFrame(() => {
      inputRef.current?.focus();
    });

    return () => {
      window.cancelAnimationFrame(frame);
    };
  }, []);

  const navigateTo = useCallback(
    (href: string) => {
      onClose();
      router.push(href);
    },
    [onClose, router],
  );

  function handleInputKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === "ArrowDown") {
      event.preventDefault();
      setActiveIndex((current) => Math.min(current + 1, Math.max(filteredItems.length - 1, 0)));
      return;
    }

    if (event.key === "ArrowUp") {
      event.preventDefault();
      setActiveIndex((current) => Math.max(current - 1, 0));
      return;
    }

    if (event.key === "Enter") {
      event.preventDefault();
      const target = filteredItems[safeActiveIndex];
      if (target) {
        navigateTo(target.href);
      }
    }
  }

  return (
    <div
      className="shell-popover shell-popover--search"
      role="dialog"
      aria-label="Modül veya sayfa ara"
    >
      <label className="shell-search__field" htmlFor={`${listId}-input`}>
        <Search aria-hidden="true" />
        <input
          ref={inputRef}
          autoComplete="off"
          id={`${listId}-input`}
          onChange={(event) => {
            setQuery(event.target.value);
            setActiveIndex(0);
          }}
          onKeyDown={handleInputKeyDown}
          placeholder="Modül veya sayfa ara"
          type="search"
          value={query}
        />
      </label>

      <p className="shell-search__scope">
        Yalnızca erişebildiğiniz modül ve sayfalar listelenir.
      </p>

      {items.length === 0 ? (
        <p className="shell-search__empty" role="status">
          Aranabilir sayfa bulunamadı.
        </p>
      ) : filteredItems.length === 0 ? (
        <p className="shell-search__empty" role="status">
          Sonuç bulunamadı.
        </p>
      ) : (
        <ul
          aria-label="Arama sonuçları"
          className="shell-search__results"
          id={listId}
          role="listbox"
        >
          {filteredItems.map((item, index) => (
            <li key={`${item.href}-${item.label}`} role="presentation">
              <button
                aria-selected={index === safeActiveIndex}
                className="shell-search__result"
                onClick={() => navigateTo(item.href)}
                role="option"
                type="button"
              >
                <span>{item.label}</span>
                <small>{item.groupLabel}</small>
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
