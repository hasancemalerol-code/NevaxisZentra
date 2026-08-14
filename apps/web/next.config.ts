import type { NextConfig } from "next";

const apiBaseUrl = process.env.CREVAFACTOS_API_BASE_URL?.replace(/\/$/, "");

const nextConfig: NextConfig = {
  agentRules: false,
  output: "standalone",
  reactStrictMode: true,
  async rewrites() {
    if (!apiBaseUrl) {
      return [];
    }

    return [
      {
        source: "/api/identity/:path*",
        destination: `${apiBaseUrl}/api/identity/:path*`,
      },
    ];
  },
};

export default nextConfig;
