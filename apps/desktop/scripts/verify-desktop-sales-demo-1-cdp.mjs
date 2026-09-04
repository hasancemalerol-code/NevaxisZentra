#!/usr/bin/env node
import { setTimeout as delay } from "node:timers/promises";

const PORT = Number(process.env.CREVAFACTOS_DESKTOP_CDP_PORT ?? "9233");
const WEB = (process.env.CREVAFACTOS_WEB_BASE_URL ?? "http://127.0.0.1:3001").replace(/\/$/, "");

export async function waitForCdp(timeoutMs = 120000) {
  const deadline = Date.now() + timeoutMs;
  const endpoints = ["/json/list", "/json"];
  while (Date.now() < deadline) {
    for (const endpoint of endpoints) {
      try {
        const targets = await fetch(`http://127.0.0.1:${PORT}${endpoint}`).then((r) => r.json());
        const page = targets.find((t) => t.type === "page" && (t.url?.startsWith(WEB) || t.url?.startsWith("http://localhost:3001")));
        if (page?.webSocketDebuggerUrl) return page;
      } catch {
        // not ready
      }
    }
    await delay(1000);
  }
  throw new Error("WebView2 CDP target not available");
}

export async function navigateInWebView(path) {
  const target = await waitForCdp();
  const ws = new WebSocket(target.webSocketDebuggerUrl);
  await new Promise((res, rej) => {
    ws.onopen = res;
    ws.onerror = rej;
  });
  let nextId = 1;
  const pending = new Map();
  ws.onmessage = (event) => {
    const message = JSON.parse(String(event.data));
    if (message.id && pending.has(message.id)) {
      const { resolve, reject } = pending.get(message.id);
      pending.delete(message.id);
      message.error ? reject(new Error(message.error.message)) : resolve(message.result);
    }
  };
  const send = (method, params = {}) =>
    new Promise((resolve, reject) => {
      const id = nextId++;
      pending.set(id, { resolve, reject });
      ws.send(JSON.stringify({ id, method, params }));
    });

  await send("Page.enable");
  await send("Page.navigate", { url: `${WEB}${path}` });
  await delay(2500);
  const snapshot = await send("Runtime.evaluate", {
    returnByValue: true,
    expression: `({ href: location.href, title: document.title, textLen: (document.body?.innerText || '').length })`,
  });
  ws.close();
  return snapshot?.result?.value ?? {};
}

if (process.argv[2]) {
  const result = await navigateInWebView(process.argv[2]);
  console.log(JSON.stringify(result));
}
