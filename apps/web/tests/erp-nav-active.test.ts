import assert from "node:assert/strict";
import test from "node:test";

import { isErpNavItemActive } from "../src/features/erp/presentation/erp-nav-active";

const items = [
  "/erp-entegrasyonlari/baglanti",
  "/erp-entegrasyonlari/senkronizasyonlar",
  "/erp-entegrasyonlari/baglanti#erp-musteriler",
  "/erp-entegrasyonlari/baglanti#erp-urunler",
  "/erp-entegrasyonlari/eslestirme",
  "/erp-entegrasyonlari/siparisler",
  "/erp-entegrasyonlari/satin-alma",
];

test("exactly one ERP pill is active on the connection page (anchors never claim the state)", () => {
  const active = items.filter((href) => isErpNavItemActive(href, "/erp-entegrasyonlari/baglanti"));
  assert.deepEqual(active, ["/erp-entegrasyonlari/baglanti"]);
});

test("a detail route keeps its list pill active and nothing else", () => {
  const active = items.filter((href) => isErpNavItemActive(href, "/erp-entegrasyonlari/siparisler/abc"));
  assert.deepEqual(active, ["/erp-entegrasyonlari/siparisler"]);
});

test("a shared prefix does not activate a sibling", () => {
  assert.equal(isErpNavItemActive("/erp-entegrasyonlari/satin-alma", "/erp-entegrasyonlari/satin-alma-x"), false);
});
