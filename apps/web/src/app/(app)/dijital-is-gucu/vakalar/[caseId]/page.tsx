import { redirect } from "next/navigation";

type VakalarRedirectPageProps = Readonly<{
  params: Promise<{ caseId: string }>;
}>;

export default async function DigitalWorkforceCaseRedirectPage({ params }: VakalarRedirectPageProps) {
  const { caseId } = await params;
  redirect(`/isler?is=${encodeURIComponent(caseId)}`);
}
