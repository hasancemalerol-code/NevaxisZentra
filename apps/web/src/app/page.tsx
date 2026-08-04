import { getWebApplicationConfig } from "@/config";

export default function HomePage() {
  const config = getWebApplicationConfig();

  return (
    <main>
      <section aria-labelledby="page-title">
        <p className="eyebrow">Executable foundation</p>
        <h1 id="page-title">{config.applicationName}</h1>
        <p>The web application is running. Product capabilities are not part of this package.</p>
      </section>
    </main>
  );
}
