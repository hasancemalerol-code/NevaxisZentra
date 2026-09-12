#!/usr/bin/env node
import { createServer } from "node:http";

const PORT = Number(process.env.NEVAXIS_FACTOS_WHATSAPP_TEST_DOUBLE_PORT ?? 18999);
const MODE = process.env.NEVAXIS_FACTOS_WHATSAPP_TEST_DOUBLE_MODE ?? "accept";
let acceptMessageSequence = 0;

const server = createServer((req, res) => {
  if (req.method !== "POST" || !req.url?.includes("/messages")) {
    res.writeHead(404);
    res.end();
    return;
  }

  let body = "";
  req.on("data", (chunk) => { body += chunk; });
  req.on("end", () => {
    const handlers = {
      accept: () => {
        acceptMessageSequence += 1;
        const messageId = `wamid.TEST.DOUBLE.${Date.now()}.${acceptMessageSequence}`;
        res.writeHead(200, { "Content-Type": "application/json" });
        res.end(JSON.stringify({ messages: [{ id: messageId }] }));
      },
      "invalid-recipient": () => {
        res.writeHead(400, { "Content-Type": "application/json" });
        res.end(JSON.stringify({ error: { code: 100, message: "invalid recipient" } }));
      },
      "invalid-template": () => {
        res.writeHead(400, { "Content-Type": "application/json" });
        res.end(JSON.stringify({ error: { code: 132000, message: "template not approved" } }));
      },
      auth: () => {
        res.writeHead(401, { "Content-Type": "application/json" });
        res.end(JSON.stringify({ error: { code: 190, message: "invalid token" } }));
      },
      "rate-limit": () => {
        res.writeHead(429, { "Retry-After": "30", "Content-Type": "application/json" });
        res.end(JSON.stringify({ error: { code: 4, message: "rate limit" } }));
      },
      "server-error": () => {
        res.writeHead(503, { "Content-Type": "application/json" });
        res.end(JSON.stringify({ error: { code: 1, message: "temporary" } }));
      },
      timeout: () => {
        // Never respond — client timeout path
      },
    };

    const handler = handlers[MODE] ?? handlers.accept;
    handler();
  });
});

server.listen(PORT, "127.0.0.1", () => {
  console.log(`Meta WhatsApp test double listening on http://127.0.0.1:${PORT} mode=${MODE}`);
});
