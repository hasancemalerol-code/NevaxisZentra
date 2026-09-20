import Module from "node:module";

const originalLoad = Module._load;

Module._load = function patchedLoad(request, parent, isMain) {
  if (request.endsWith(".css")) {
    return {};
  }

  if (request === "next/navigation") {
    return {
      usePathname: () => "/",
      useRouter: () => ({
        push: () => undefined,
        replace: () => undefined,
        refresh: () => undefined,
        back: () => undefined,
        forward: () => undefined,
        prefetch: async () => undefined,
      }),
      useSearchParams: () => new URLSearchParams(),
      useParams: () => ({}),
    };
  }

  return originalLoad.call(this, request, parent, isMain);
};
