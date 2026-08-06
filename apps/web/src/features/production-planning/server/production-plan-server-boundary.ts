declare const window: unknown;

if (typeof window !== "undefined") {
  throw new Error(
    "Production Planning server modules cannot be imported from the client.",
  );
}

export {};
