import assert from "node:assert/strict";
import test from "node:test";

import { pickCatalogColumns } from "../src/features/apps-catalog/domain/catalog-columns";

test("catalog columns close every row where possible", () => {
  assert.equal(pickCatalogColumns(1, 4), 1);
  assert.equal(pickCatalogColumns(3, 4), 3);
  assert.equal(pickCatalogColumns(6, 4), 3);
  assert.equal(pickCatalogColumns(8, 4), 4);
  assert.equal(pickCatalogColumns(6, 3), 3);
  assert.equal(pickCatalogColumns(6, 2), 2);
  assert.equal(pickCatalogColumns(7, 4), 4);
});
