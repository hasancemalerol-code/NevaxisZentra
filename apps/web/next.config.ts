import type { NextConfig } from "next";
import path from "node:path";
import { fileURLToPath } from "node:url";

const apiBaseUrl = process.env.CREVAFACTOS_API_BASE_URL?.replace(/\/$/, "");
const repositoryRoot = path.join(path.dirname(fileURLToPath(import.meta.url)), "..", "..");

const nextConfig: NextConfig = {
  agentRules: false,
  allowedDevOrigins: ["127.0.0.1", "localhost"],
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
      {
        source: "/api/public/ai-task-access/:path*",
        destination: `${apiBaseUrl}/api/public/ai-task-access/:path*`,
      },
    ];
  },
  async headers() {
    return [
      {
        source: "/gorev",
        headers: [
          { key: "Cache-Control", value: "no-store" },
          { key: "Referrer-Policy", value: "no-referrer" },
          { key: "X-Robots-Tag", value: "noindex, nofollow" },
        ],
      },
    ];
  },
};

export default nextConfig;
