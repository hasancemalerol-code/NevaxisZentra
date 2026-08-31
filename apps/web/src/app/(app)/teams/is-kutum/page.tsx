import { redirect } from "next/navigation";

export const dynamic = "force-dynamic";

type TeamsWorkInboxRedirectPageProps = Readonly<{
  searchParams?: Promise<Readonly<Record<string, string | string[] | undefined>>>;
}>;

export default async function TeamsWorkInboxRedirectPage({
  searchParams,
}: TeamsWorkInboxRedirectPageProps) {
  const params = await searchParams;
  const view = typeof params?.gorunum === "string" ? params.gorunum : "assigned";
  const origin = typeof params?.kaynak === "string" ? params.kaynak : "all";
  redirect(`/?gorunum=${encodeURIComponent(view)}&kaynak=${encodeURIComponent(origin)}`);
}
