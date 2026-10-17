import Link from "next/link";
import { ChevronRight } from "lucide-react";

import "@/features/bugun/components/bugun-workspace.css";

export type FactoryContextBreadcrumbSegment = Readonly<{
  label: string;
  href?: string;
}>;

type FactoryContextBreadcrumbProps = Readonly<{
  segments: readonly FactoryContextBreadcrumbSegment[];
  testId?: string;
}>;

export function FactoryContextBreadcrumb({ segments, testId }: FactoryContextBreadcrumbProps) {
  return (
    <nav
      aria-label="Fabrika bağlam yolu"
      className="fact-workspace__breadcrumb"
      data-testid={testId ?? "factory-context-breadcrumb"}
    >
      <ol className="factory-context-breadcrumb">
        <li>
          <Link href="/fabrika">Fabrika</Link>
        </li>
        {segments.map((segment, index) => {
          const isLast = index === segments.length - 1;

          return (
            <li key={`${segment.label}-${index}`}>
              <ChevronRight aria-hidden="true" size={12} />
              {segment.href && !isLast ? (
                <Link href={segment.href}>{segment.label}</Link>
              ) : (
                <span aria-current={isLast ? "page" : undefined}>{segment.label}</span>
              )}
            </li>
          );
        })}
      </ol>
    </nav>
  );
}
