import assert from "node:assert/strict";
import test from "node:test";
import { renderToStaticMarkup } from "react-dom/server";

import {
  formatFactoryCopilotContextHeader,
  isFactoryCopilotSurface,
  resolveFactoryCopilotContext,
} from "../src/features/factory-copilot/application/factory-copilot-context";
import { FactoryCopilotDrawer } from "../src/features/factory-copilot/components/factory-copilot-drawer";
import { FactoryCopilotHeaderAction } from "../src/features/factory-copilot/components/factory-copilot-header-action";
import { FactoryCopilotShellProvider } from "../src/features/factory-copilot/components/factory-copilot-shell-context";

test("factory copilot surfaces include operational workspaces", () => {
  assert.equal(isFactoryCopilotSurface("/"), true);
  assert.equal(isFactoryCopilotSurface("/fabrika"), true);
  assert.equal(isFactoryCopilotSurface("/planlama/plan-1"), true);
  assert.equal(isFactoryCopilotSurface("/isler"), true);
  assert.equal(isFactoryCopilotSurface("/senaryo-kurtarma"), true);
  assert.equal(isFactoryCopilotSurface("/ayarlar"), false);
});

test("factory copilot inherits line and work context from url", () => {
  const params = new URLSearchParams("alan=INJ&kaynak=res-1&hat=line-1&is=case-1&run=run-1");
  const context = resolveFactoryCopilotContext("/fabrika", params);

  assert.equal(context.entryPoint, "FABRIKA");
  assert.equal(context.areaCode, "INJ");
  assert.equal(context.lineId, "line-1");
  assert.equal(context.resourceId, "res-1");
  assert.equal(context.digitalWorkCaseId, "case-1");
  assert.equal(context.scenarioRunId, "run-1");
});

test("factory copilot context header renders operational scope", () => {
  const label = formatFactoryCopilotContextHeader({
    entryPoint: "FABRIKA",
    factoryLabel: "Pilot tesis",
    lineLabel: "MIX-LINE-01",
    resourceLabel: "ST-30",
  });

  assert.match(label, /MIX-LINE-01/);
  assert.match(label, /ST-30/);
});

test("factory copilot header action is visible when access is granted", () => {
  const html = renderToStaticMarkup(
    <FactoryCopilotShellProvider canAccess onOpen={() => undefined} registerHeaderAction={() => () => undefined}>
      <FactoryCopilotHeaderAction />
    </FactoryCopilotShellProvider>,
  );

  assert.match(html, /data-testid="factory-copilot-header-action"/);
  assert.match(html, /Zentra Copilot/);
});

test("factory copilot drawer renders context header and suggestions area", () => {
  const html = renderToStaticMarkup(
    <FactoryCopilotDrawer
      open
      onClose={() => undefined}
      pathname="/fabrika"
      searchParams={new URLSearchParams("hat=line-1")}
    />,
  );

  assert.match(html, /Zentra Copilot/);
  assert.match(html, /data-testid="factory-copilot-drawer"/);
  assert.match(html, /Operasyonel sorunuzu yazın/);
});
