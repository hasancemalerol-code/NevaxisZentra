import assert from "node:assert/strict";
import test from "node:test";
import { renderToStaticMarkup } from "react-dom/server";

import HomePage from "../src/app/page";

test("renders the minimal application page", () => {
  const markup = renderToStaticMarkup(<HomePage />);

  assert.match(markup, /CrevaFactOS Web/);
  assert.match(markup, /Product capabilities are not part of this package/);
});
