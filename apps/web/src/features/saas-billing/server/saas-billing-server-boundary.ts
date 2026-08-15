declare const window: unknown;

if (typeof window !== "undefined") {
  throw new Error("SaaS billing server modules cannot be imported from the client.");
}

export {};
