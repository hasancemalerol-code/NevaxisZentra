export function parseMembershipIdFromMemberReference(
  memberReference: string | undefined,
): string | null {
  if (!memberReference) {
    return null;
  }

  const match = /^mem_([0-9a-f]{32})$/i.exec(memberReference.trim());
  if (!match) {
    return null;
  }

  const hex = match[1].toLowerCase();
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}
