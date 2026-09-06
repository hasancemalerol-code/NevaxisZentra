import type { NextConfig } from "next";
import path from "node:path";
import { fileURLToPath } from "node:url";

const apiBaseUrl = process.env.CREVAFACTOS_API_BASE_URL?.replace(/\/$/, "");
const repositoryRoot = path.join(path.dirname(fileURLToPath(import.meta.url)), "..", "..");

const nextConfig: NextConfig = {
  agentRules: false,
  output: "standalone",
  outputFileTracingRoot: repositoryRoot,
  reactStrictMode: true,
  experimental: {
    authInterrupts: true,
  },
  async rewrites() {
    if (!apiBaseUrl) {
      return [];
    }

    return [
      {
        source: "/api/identity/:path*",
        destination: `${apiBaseUrl}/api/identity/:path*`,
      },
      {
        source: "/api/admin-access/:path*",
        destination: `${apiBaseUrl}/api/admin-access/:path*`,
      },
      {
        source: "/api/saas-subscription/:path*",
        destination: `${apiBaseUrl}/api/saas-subscription/:path*`,
      },
    ];
  },
};

export default nextConfig;
