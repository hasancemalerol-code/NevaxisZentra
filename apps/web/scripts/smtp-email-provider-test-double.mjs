#!/usr/bin/env node
/**
 * Deterministic SMTP test double for Email delivery adapter runtime closure.
 * NOT a real provider — development/closure only.
 */
import { createServer as createNetServer } from "node:net";
import { createServer as createHttpServer } from "node:http";
import { appendFileSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const MODE = process.env.NEVAXIS_FACTOS_EMAIL_TEST_DOUBLE_MODE ?? "accept";
const SMTP_PORT = Number(process.env.NEVAXIS_FACTOS_EMAIL_TEST_DOUBLE_PORT ?? 19025);
const HTTP_PORT = Number(process.env.NEVAXIS_FACTOS_EMAIL_TEST_DOUBLE_HTTP_PORT ?? 19026);
const CAPTURE_DIR = process.env.NEVAXIS_FACTOS_EMAIL_CAPTURE_DIR ?? resolve(ROOT, "artifacts/email-closure");
const CAPTURE_FILE = resolve(CAPTURE_DIR, "captured-messages.jsonl");
const EXPECTED_USER = process.env.NEVAXIS_FACTOS_EMAIL_TEST_DOUBLE_USERNAME ?? "closure-smtp-user";
const EXPECTED_PASSWORD = process.env.NEVAXIS_FACTOS_EMAIL_TEST_DOUBLE_PASSWORD ?? "closure-smtp-password";

mkdirSync(CAPTURE_DIR, { recursive: true });

let lastCaptured = null;
let acceptSequence = 0;

function sanitizeForEvidence(value) {
  if (!value) return value;
  return String(value)
    .replace(/#access=[^\s"'<>]+/gi, "#access=[REDACTED]")
    .replace(/access=[^\s"'<>]+/gi, "access=[REDACTED]")
    .replace(/gorev[^\s]*access[^\s]*/gi, "gorev#access=[REDACTED]");
}

function maskEmail(email) {
  const at = email.indexOf("@");
  if (at <= 0) return "***";
  const local = email.slice(0, at);
  const domain = email.slice(at + 1);
  const maskedLocal = local.length <= 1 ? "*" : `${local[0]}***${local[local.length - 1]}`;
  return `${maskedLocal}@${domain}`;
}

function recordCapture(entry) {
  const safe = {
    ...entry,
    toMasked: entry.to ? maskEmail(entry.to) : null,
    htmlBody: entry.htmlBody ? sanitizeForEvidence(entry.htmlBody) : null,
    plainTextBody: entry.plainTextBody ? sanitizeForEvidence(entry.plainTextBody) : null,
    subject: entry.subject ? sanitizeForEvidence(entry.subject) : null,
  };
  delete safe.to;
  lastCaptured = safe;
  appendFileSync(CAPTURE_FILE, `${JSON.stringify(safe)}\n`, "utf8");
}

function parseAddresses(line) {
  const match = line.match(/<([^>]+)>/);
  return match ? match[1] : line.split(/\s+/).pop()?.replace(/[<>]/g, "") ?? "";
}

function parseMime(raw) {
  const headerEnd = raw.indexOf("\r\n\r\n");
  const headers = headerEnd >= 0 ? raw.slice(0, headerEnd) : raw;
  const body = headerEnd >= 0 ? raw.slice(headerEnd + 4) : "";
  const subjectMatch = headers.match(/^Subject:\s*(.+)$/im);
  const fromMatch = headers.match(/^From:\s*(.+)$/im);
  const toMatch = headers.match(/^To:\s*(.+)$/im);
  const messageIdMatch = headers.match(/^Message-ID:\s*(.+)$/im);
  let plainTextBody = null;
  let htmlBody = null;
  const boundaryMatch = headers.match(/boundary="?([^"\r\n;]+)"?/i) ?? body.match(/boundary="?([^"\r\n;]+)"?/i);
  if (boundaryMatch) {
    const boundary = boundaryMatch[1];
    const parts = body.split(`--${boundary}`);
    for (const part of parts) {
      if (!part || part.startsWith("--")) continue;
      const partHeaderEnd = part.indexOf("\r\n\r\n");
      const partHeaders = partHeaderEnd >= 0 ? part.slice(0, partHeaderEnd) : "";
      const partBody = partHeaderEnd >= 0 ? part.slice(partHeaderEnd + 4).replace(/\r\n--$/, "").trim() : "";
      if (/text\/plain/i.test(partHeaders)) plainTextBody = decodeQuotedPrintable(partBody);
      if (/text\/html/i.test(partHeaders)) htmlBody = decodeQuotedPrintable(partBody);
    }
  } else if (/text\/html/i.test(headers)) {
    htmlBody = decodeQuotedPrintable(body.trim());
  } else {
    plainTextBody = decodeQuotedPrintable(body.trim());
  }
  return {
    subject: subjectMatch?.[1]?.trim() ?? null,
    from: fromMatch?.[1]?.trim() ?? null,
    to: toMatch?.[1]?.trim() ?? null,
    messageId: messageIdMatch?.[1]?.trim() ?? null,
    plainTextBody,
    htmlBody,
  };
}

function decodeQuotedPrintable(value) {
  const cleaned = value.replace(/=\r\n/g, "");
  const bytes = [];
  for (let i = 0; i < cleaned.length; i++) {
    if (cleaned[i] === "=" && i + 2 < cleaned.length) {
      bytes.push(parseInt(cleaned.slice(i + 1, i + 3), 16));
      i += 2;
    } else {
      bytes.push(cleaned.charCodeAt(i));
    }
  }
  return Buffer.from(bytes).toString("utf8");
}

function createSmtpServer() {
  return createNetServer((socket) => {
    socket.on("error", () => {});
    let buffer = "";
    let mailFrom = null;
    let rcptTo = null;
    let dataBuffer = "";
    let inData = false;
    let authenticated = false;
    let authStep = 0;
    let authUser = "";
    let shouldHang = false;

    const send = (line) => {
      if (shouldHang || socket.destroyed || socket.writableEnded) return;
      try {
        socket.write(`${line}\r\n`);
      } catch {
        // Client disconnected during scripted failure modes.
      }
    };

    send("220 NevaxisZentra-email-test-double ESMTP");

    socket.on("data", (chunk) => {
      if (shouldHang) return;
      buffer += chunk.toString("binary");

      while (true) {
        if (inData) {
          const endIdx = buffer.indexOf("\r\n.\r\n");
          if (endIdx < 0) break;
          dataBuffer += buffer.slice(0, endIdx);
          buffer = buffer.slice(endIdx + 5);
          inData = false;

          if (MODE === "temporary-failure") {
            send("451 4.3.0 Temporary server error");
            socket.destroy();
            return;
          }

          acceptSequence += 1;
          const parsed = parseMime(dataBuffer);
          const messageId = parsed.messageId ?? `<test-double-${Date.now()}.${acceptSequence}@closure.NevaxisZentra.local>`;
          recordCapture({
            capturedAtUtc: new Date().toISOString(),
            mode: MODE,
            messageId,
            from: parsed.from,
            to: parsed.to ?? rcptTo,
            subject: parsed.subject,
            plainTextBody: parsed.plainTextBody,
            htmlBody: parsed.htmlBody,
            mailFrom,
            rcptTo,
          });
          send(`250 2.0.0 Ok: queued as ${messageId}`);
          dataBuffer = "";
          continue;
        }

        const lineEnd = buffer.indexOf("\r\n");
        if (lineEnd < 0) break;
        const line = buffer.slice(0, lineEnd);
        buffer = buffer.slice(lineEnd + 2);
        const upper = line.toUpperCase();

        if (upper.startsWith("EHLO") || upper.startsWith("HELO")) {
          if (MODE === "timeout") {
            send("421 4.4.2 Connection timed out");
            socket.destroy();
            return;
          }
          const extensions = ["250-NevaxisZentra-email-test-double Hello", "250-AUTH LOGIN PLAIN", "250 STARTTLS"];
          extensions.forEach((ext, idx) => {
            if (idx === extensions.length - 1) send(ext);
            else send(ext);
          });
          continue;
        }

        if (upper === "STARTTLS") {
          if (MODE === "tls-config") {
            send("454 4.7.0 TLS not available");
            socket.destroy();
            return;
          }
          send("454 4.7.0 TLS not available");
          continue;
        }

        if (upper.startsWith("AUTH LOGIN")) {
          send("334 VXNlcm5hbWU=");
          authStep = 1;
          continue;
        }

        if (authStep === 1) {
          authUser = Buffer.from(line, "base64").toString("utf8");
          send("334 UGFzc3dvcmQ=");
          authStep = 2;
          continue;
        }

        if (authStep === 2) {
          const password = Buffer.from(line, "base64").toString("utf8");
          authStep = 0;
          if (MODE === "authentication-failure") {
            send("535 5.7.8 Authentication credentials invalid");
            continue;
          }
          authenticated = true;
          send("235 2.7.0 Authentication successful");
          continue;
        }

        if (upper.startsWith("MAIL FROM")) {
          if (MODE === "authentication-failure" && !authenticated) {
            send("530 5.7.0 Authentication required");
            continue;
          }
          if (MODE === "temporary-failure") {
            send("451 4.3.0 Temporary server error");
            socket.destroy();
            return;
          }
          mailFrom = parseAddresses(line);
          send("250 2.1.0 Ok");
          continue;
        }

        if (upper.startsWith("RCPT TO")) {
          rcptTo = parseAddresses(line);
          if (MODE === "permanent-recipient") {
            send("550 5.1.1 User unknown");
            continue;
          }
          send("250 2.1.5 Ok");
          continue;
        }

        if (upper === "DATA") {
          inData = true;
          dataBuffer = "";
          send("354 End data with <CR><LF>.<CR><LF>");
          continue;
        }

        if (upper === "RSET") {
          mailFrom = null;
          rcptTo = null;
          dataBuffer = "";
          inData = false;
          send("250 2.0.0 Ok");
          continue;
        }

        if (upper === "QUIT") {
          send("221 2.0.0 Bye");
          socket.end();
          return;
        }

        if (upper.startsWith("NOOP")) {
          send("250 2.0.0 Ok");
          continue;
        }

        send("502 5.5.1 Command not implemented");
      }
    });
  });
}

const smtpServer = createSmtpServer();
smtpServer.on("error", (error) => {
  console.error(`SMTP server error: ${error.message}`);
});
smtpServer.listen(SMTP_PORT, "127.0.0.1", () => {
  console.log(`SMTP email test double listening on 127.0.0.1:${SMTP_PORT} mode=${MODE}`);
});

const httpServer = createHttpServer((req, res) => {
  if (req.url === "/health") {
    res.writeHead(200, { "Content-Type": "application/json" });
    res.end(JSON.stringify({ ok: true, mode: MODE }));
    return;
  }
  if (req.url === "/last-captured") {
    res.writeHead(200, { "Content-Type": "application/json" });
    res.end(JSON.stringify(lastCaptured ?? {}));
    return;
  }
  if (req.url === "/preview" && lastCaptured?.htmlBody) {
    const html = sanitizeForEvidence(lastCaptured.htmlBody);
    res.writeHead(200, { "Content-Type": "text/html; charset=utf-8" });
    res.end(html);
    return;
  }
  if (req.url === "/reset-captures") {
    writeFileSync(CAPTURE_FILE, "", "utf8");
    lastCaptured = null;
    res.writeHead(200);
    res.end("ok");
    return;
  }
  res.writeHead(404);
  res.end();
});

httpServer.listen(HTTP_PORT, "127.0.0.1", () => {
  console.log(`SMTP email test double HTTP on 127.0.0.1:${HTTP_PORT}`);
});

// Load last capture if file exists
try {
  const lines = readFileSync(CAPTURE_FILE, "utf8").trim().split("\n").filter(Boolean);
  if (lines.length > 0) {
    lastCaptured = JSON.parse(lines[lines.length - 1]);
  }
} catch {
  // ignore
}
