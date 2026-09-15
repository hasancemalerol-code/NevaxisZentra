import Link from "next/link";
import { ArrowLeft } from "lucide-react";

import { PRESENTATION_MODE_ROUTE } from "../domain/presentation-mode-labels";
import "./presentation-mode.css";

export function PresentationModeReturnLink() {
  return (
    <Link
      href={PRESENTATION_MODE_ROUTE}
      className="presentation-mode-return-link"
      data-testid="presentation-mode-return-link"
    >
      <ArrowLeft aria-hidden="true" size={16} />
      Sunum Moduna Dön
    </Link>
  );
}
