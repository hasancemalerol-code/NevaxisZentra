import Link from "next/link";

import { EmptyState } from "@/components/ui/states";
import {
  PageContent,
  PageHeader,
} from "@/components/ui/page";

export default function AiTaskCreateUnavailablePage() {
  return (
    <PageContent>
      <PageHeader
        description="Yeni bir AI görevi oluşturmak için gerçek görev oluşturma servisi henüz bağlı değil."
        eyebrow="AI Görev Merkezi"
        title="AI görevi oluşturma"
      />

      <EmptyState
        action={
          <Link href="/">AI Görev Merkezi’ne dön</Link>
        }
        description="AI görevleri şu anda yalnızca önizleme kayıtlarından incelenebilir. Oluşturma, onay veya otomatik uygulama bu pakette açılmaz."
        title="AI görev oluşturma henüz kullanıma hazır değil"
      />
    </PageContent>
  );
}
