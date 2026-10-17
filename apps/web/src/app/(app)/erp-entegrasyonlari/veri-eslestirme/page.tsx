import { redirect } from "next/navigation";

/** "Veri Eşleştirme" is an alias of the Eşleştirme page; one canonical route keeps one active pill. */
export default function ErpVeriEslestirmePage(): never {
  redirect("/erp-entegrasyonlari/eslestirme");
}
