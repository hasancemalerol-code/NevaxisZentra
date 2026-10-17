/** Picks a column count that closes every row (no trailing stretched or lone tile) up to the given maximum. */
export function pickCatalogColumns(count: number, max: number): number {
  if (count <= max) {
    return Math.max(count, 1);
  }
  for (let columns = max; columns >= 2; columns -= 1) {
    if (count % columns === 0) {
      return columns;
    }
  }
  return max;
}

/**
 * Spreads catalogue groups over a fixed number of columns so every column ends at about the same height.
 * A group weighs its tile count plus its header; the first column starts with the extra weight of a section rendered outside the list.
 */
export function balanceCatalogColumns<T extends Readonly<{ items: readonly unknown[] }>>(
  groups: readonly T[],
  columnCount: number,
  firstColumnHeadStart = 0,
): T[][] {
  const columns: T[][] = Array.from({ length: columnCount }, () => []);
  const weights = Array.from({ length: columnCount }, (_, index) => (index === 0 ? firstColumnHeadStart : 0));
  const ordered = groups
    .map((group, index) => ({ group, index }))
    .sort((left, right) => right.group.items.length - left.group.items.length || left.index - right.index);
  for (const { group } of ordered) {
    let target = 0;
    for (let column = 1; column < columnCount; column += 1) {
      if (weights[column] < weights[target]) {
        target = column;
      }
    }
    columns[target].push(group);
    weights[target] += group.items.length + 1.5;
  }
  return columns;
}
