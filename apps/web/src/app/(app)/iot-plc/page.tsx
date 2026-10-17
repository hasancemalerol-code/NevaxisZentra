

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { IotPlcWorkspaceView } from "@/features/iot-plc/components/iot-plc-workspace-view";
import "@/features/iot-plc/components/iot-plc-workspace.css";
import { readIotPlcWorkspace } from "@/features/iot-plc/server/iot-plc-queries";

import { FailureStatePage } from "@/components/ui/failure-state";

export default async function IotPlcPage() {
  await forbidUnlessGranted("iot-plc.read");

  const result = await readIotPlcWorkspace();

  if (result.status === "failure") {
    return (
      <div className="iot-plc-page">
        <header className="iot-plc-executive">
          <div className="iot-plc-executive__intro">
            <p className="iot-plc-executive__eyebrow">ENDÜSTRİYEL BAĞLANTI</p>
            <h1>IoT ve PLC</h1>
            <p className="iot-plc-executive__subtitle">
              Endüstriyel bağlantıları, sinyal akışını ve veri kalitesi kanıtlarını tek çalışma alanında izleyin.
            </p>
          </div>
        </header>
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="IoT/PLC verisi yüklenemedi" inline />
      </div>
    );
  }

  return (
    <IotPlcWorkspaceView
      workspace={result.workspace}
      canManage={result.canManage}
    />
  );
}
