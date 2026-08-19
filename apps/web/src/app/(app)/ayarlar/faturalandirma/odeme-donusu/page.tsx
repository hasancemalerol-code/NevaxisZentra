import { CheckoutReturnView } from "@/features/saas-billing/components/checkout-return-view";

type PageProps = Readonly<{
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}>;

function readParam(value: string | string[] | undefined): string | null {
  if (Array.isArray(value)) {
    return value[0] ?? null;
  }
  return value ?? null;
}

export default async function BillingCheckoutReturnPage({ searchParams }: PageProps) {
  const params = await searchParams;
  const paymentAttemptId = readParam(params.paymentAttemptId);
  const checkoutSessionToken = readParam(params.token);

  return (
    <CheckoutReturnView
      paymentAttemptId={paymentAttemptId}
      checkoutSessionToken={checkoutSessionToken}
    />
  );
}
