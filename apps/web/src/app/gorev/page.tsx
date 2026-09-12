import type { Metadata } from "next";

import { PublicSecureTaskView } from "@/features/ai-task-center/components/public-secure-task-view";

export const metadata: Metadata = {
  title: "Güvenli Görev | FactOS",
  robots: { index: false, follow: false },
};

export default function PublicSecureTaskPage() {
  return <PublicSecureTaskView />;
}
