import Link from "next/link";
import { ArrowLeft } from "lucide-react";

export type QualityBackLinkProps = Readonly<{
  href: string;
  children: string;
}>;

export function QualityBackLink({ href, children }: QualityBackLinkProps) {
  return (
    <Link href={href} className="quality-back-link">
      <ArrowLeft aria-hidden="true" />
      <span>{children}</span>
    </Link>
  );
}
