declare const window: unknown;

if (typeof window !== "undefined") {
  throw new Error(
    "Admin access server modules cannot be imported from the client.",
  );
}

export {};
