export function buildCanonicalTaskCenterRedirect(
  searchParams: Readonly<Record<string, string | string[] | undefined>> | undefined,
): string {
  const query = new URLSearchParams();

  if (typeof searchParams?.gorunum === "string") {
    query.set("gorunum", searchParams.gorunum);
  }

  if (typeof searchParams?.kaynak === "string") {
    query.set("kaynak", searchParams.kaynak);
  }

  const suffix = query.size > 0 ? `?${query.toString()}` : "";
  return `/${suffix}`;
}
