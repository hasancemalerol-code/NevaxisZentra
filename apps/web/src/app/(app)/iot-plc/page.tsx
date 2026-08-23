import { Cable } from "lucide-react";

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { IotPlcWorkspaceView } from "@/features/iot-plc/components/iot-plc-workspace-view";
import "@/features/iot-plc/components/iot-plc-workspace.css";
import { readIotPlcWorkspace } from "@/features/iot-plc/server/iot-plc-queries";

export default async function IotPlcPage() {
  await forbidUnlessGranted("iot-plc.read");

  const result = await readIotPlcWorkspace();

  if (result.status === "failure") {
    const isUnauthorized =
      result.code === "permission-denied" || result.code === "unauthenticated";

    return (
      <main className="iot-plc-page">
        <header className="iot-plc-executive">
          <div className="iot-plc-executive__intro">
            <p className="iot-plc-executive__eyebrow">ENDÜSTRİYEL BAĞLANTI</p>
            <h1>IoT & PLC</h1>
            <p className="iot-plc-executive__subtitle">
              Endüstriyel bağlantıları, sinyal akışını ve veri kalitesi kanıtlarını tek çalışma alanında izleyin.
            </p>
          </div>
        </header>
        <div
          role="alert"
          className={isUnauthorized ? "iot-plc-error-banner" : "iot-plc-partial-error"}
        >
          <Cable aria-hidden="true" />
          <div>
            <h3>{isUnauthorized ? "Bu modüle erişim yetkiniz yok" : "IoT/PLC verisi yüklenemedi"}</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  return (
    <IotPlcWorkspaceView
      workspace={result.workspace}
      canManage={result.canManage}
    />
  );
}
