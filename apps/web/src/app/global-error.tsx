"use client";

import "./styles.css";
import "../design-system/zentra-premium.css";
import "../design-system/zentra-surfaces.css";
import "../design-system/zentra-surfaces-features.css";
import "../design-system/zentra-surfaces-modules.css";

import { StatePage } from "@/components/ui/state-page";

/** Last-resort boundary: renders its own document, so it must not depend on the application shell. */
export default function GlobalError({ error, reset }: Readonly<{ error: Error & { digest?: string }; reset: () => void }>) {
  return (
    <html lang="tr">
      <body>
        <StatePage
          standalone
          kind="error"
          title="Uygulama şu anda açılamadı"
          description="Beklenmeyen bir sorun oluştu. Sayfayı yeniden deneyin; sorun sürerse destek ekibine aşağıdaki referansı iletin."
          reference={error.digest}
          actions={
            <button type="button" className="button button-medium button-primary" onClick={() => reset()}>
              Yeniden Dene
            </button>
          }
        />
      </body>
    </html>
  );
}
