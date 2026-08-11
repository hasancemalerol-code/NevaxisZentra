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
  'label: "Planlama"',
  'href: "/planlama"',
  'label: "Üretim"',
  'href: "/uretim"',
  'label: "İhracat"',
  'href: "/ihracat"',
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

if (!source.includes('iconKey: "ship"')) {
  fail('İhracat must use iconKey: "ship".');
}

const factoryOperationsGroupPattern =
  /label:\s*"Fabrika operasyonları"\s*,\s*items:\s*\[\s*\{\s*label:\s*"Siparişler"\s*,\s*href:\s*"\/siparisler"\s*,\s*iconKey:\s*"shopping-basket"\s*,?\s*\}\s*,\s*\{\s*label:\s*"Planlama"\s*,\s*href:\s*"\/planlama"\s*,\s*iconKey:\s*"calendar-range"\s*,?\s*\}\s*,\s*\{\s*label:\s*"Üretim"\s*,\s*href:\s*"\/uretim"\s*,\s*iconKey:\s*"factory"\s*,?\s*\}\s*,\s*\{\s*label:\s*"İhracat"\s*,\s*href:\s*"\/ihracat"\s*,\s*iconKey:\s*"ship"\s*,?\s*\}/s;

if (!factoryOperationsGroupPattern.test(source)) {
  fail(
    [
      '"Fabrika operasyonları" group must begin with',
      "Siparişler -> Planlama -> Üretim -> İhracat.",
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

const exportIndex = source.indexOf(
  'label: "İhracat"',
);

const shiftsIndex = source.indexOf(
  'label: "Vardiyalar"',
);

if (
  !(
    ordersIndex < planningIndex &&
    planningIndex < productionIndex &&
    productionIndex < exportIndex &&
    exportIndex < shiftsIndex
  )
) {
  fail(
    "Canonical order must be Siparişler -> Planlama -> Üretim -> İhracat -> Vardiyalar.",
  );
}

console.log(
  "Primary navigation verification passed.",
);
console.log(
  "Locked factory operations order: Siparişler -> Planlama -> Üretim -> İhracat.",
);
