import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

const navigationFileUrl = new URL(
  "../src/lib/navigation/primary-navigation.ts",
  import.meta.url,
);

const navigationFilePath =
  fileURLToPath(navigationFileUrl);

const source = readFileSync(
  navigationFilePath,
  "utf8",
).replaceAll("\r\n", "\n");

function fail(message) {
  console.error(
    `Primary navigation verification failed: ${message}`,
  );

  process.exit(1);
}

function countOccurrences(value, searchValue) {
  return value.split(searchValue).length - 1;
}

const requiredUniqueValues = [
  'label: "Siparişler"',
  'href: "/siparisler"',
  'icon: ShoppingBasket',
  'label: "Planlama"',
  'href: "/planlama"',
  'icon: CalendarRange',
  'label: "Üretim"',
  'href: "/uretim"',
  'icon: Factory',
];

for (const requiredValue of requiredUniqueValues) {
  const occurrenceCount = countOccurrences(
    source,
    requiredValue,
  );

  if (occurrenceCount !== 1) {
    fail(
      `"${requiredValue}" exactly once expected, found ${occurrenceCount}.`,
    );
  }
}

const factoryOperationsGroupPattern =
  /label:\s*"Fabrika Operasyonları"\s*,\s*items:\s*\[\s*\{\s*label:\s*"Siparişler"\s*,\s*href:\s*"\/siparisler"\s*,\s*icon:\s*ShoppingBasket\s*,?\s*\}\s*,\s*\{\s*label:\s*"Planlama"\s*,\s*href:\s*"\/planlama"\s*,\s*icon:\s*CalendarRange\s*,?\s*\}\s*,\s*\{\s*label:\s*"Üretim"\s*,\s*href:\s*"\/uretim"\s*,\s*icon:\s*Factory\s*,?\s*\}/s;

if (!factoryOperationsGroupPattern.test(source)) {
  fail(
    [
      '"Fabrika Operasyonları" group must begin with',
      "Siparişler -> Planlama -> Üretim.",
      "Labels, routes, icons, or canonical order changed.",
    ].join(" "),
  );
}

const ordersIndex = source.indexOf(
  'label: "Siparişler"',
);

const planningIndex = source.indexOf(
  'label: "Planlama"',
);

const productionIndex = source.indexOf(
  'label: "Üretim"',
);

if (
  !(
    ordersIndex <
      planningIndex &&
    planningIndex <
      productionIndex
  )
) {
  fail(
    "Canonical order must be Siparişler -> Planlama -> Üretim.",
  );
}

console.log(
  "Primary navigation verification passed.",
);
console.log(
  "Locked factory operations order: Siparişler -> Planlama -> Üretim.",
);