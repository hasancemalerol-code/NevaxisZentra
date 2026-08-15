declare const window: unknown;

if (typeof window !== "undefined") {
  throw new Error("SaaS admin server modules cannot be imported from the client.");
}

export {};
