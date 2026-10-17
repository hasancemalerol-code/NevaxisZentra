/**
 * Exactly one ERP sub-navigation pill is current.
 * A pill is current for its own route or for a detail route below it; section anchors
 * (for example "#erp-musteriler") never claim the current state, so the page they live on
 * keeps a single active pill.
 */
export function isErpNavItemActive(itemHref: string, activeHref: string): boolean {
  if (itemHref.includes("#")) {
    return false;
  }
  return activeHref === itemHref || activeHref.startsWith(`${itemHref}/`);
}
