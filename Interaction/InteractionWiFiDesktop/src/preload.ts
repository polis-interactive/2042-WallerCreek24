import { contextBridge, ipcRenderer } from "electron";
import { InteractionEvent, InteractionEventCallback, InteractionSnapshot, InteractionSnapshotCallback } from "./state";
import { MeasurementRequest } from "./measurement";


export const InteractionWiFiApi = {
  startActor: (snapshotCallback: InteractionSnapshotCallback, eventCallback: InteractionEventCallback) => {
    ipcRenderer.removeAllListeners('SNAPSHOT');
    ipcRenderer.removeAllListeners('EVENT');
    ipcRenderer.on('SNAPSHOT', (_, rawSnapshot: string) => {
      const snapshot = JSON.parse(rawSnapshot) as InteractionSnapshot;
      snapshotCallback(snapshot);
    });
    ipcRenderer.on('EVENT', (_, rawEvent: string) => {
      const event = JSON.parse(rawEvent) as InteractionEvent;
      eventCallback(event);
    });
    ipcRenderer.send('START');
  },
  sendMeasurementRequest: (request: MeasurementRequest) => {
    ipcRenderer.send('MEASURE', request);
  }
}

process.once("loaded", () => {
  contextBridge.exposeInMainWorld('InteractionWiFiApi', InteractionWiFiApi)
});