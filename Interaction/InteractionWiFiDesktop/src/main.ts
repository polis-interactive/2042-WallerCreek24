
import { app, BrowserWindow, ipcMain } from 'electron';
import path from 'path';
import { ActorRefFrom, createActor } from 'xstate';
import { InteractionWiFiMachine } from './state';
import stringify from "safe-stable-stringify";
import { MeasurementRequest } from './measurement';

// Handle creating/removing shortcuts on Windows when installing/uninstalling.
if (require('electron-squirrel-startup')) {
  app.quit();
}

let interactionWiFiActor: ActorRefFrom<typeof InteractionWiFiMachine> | null = null;

const createWindow = () => {
  // Create the browser window.
  const mainWindow = new BrowserWindow({
    width: 800,
    height: 600,
    webPreferences: {
      nodeIntegration: true,
      preload: path.join(__dirname, 'preload.js'),
    },
  });

  // and load the index.html of the app.
  if (MAIN_WINDOW_VITE_DEV_SERVER_URL) {
    mainWindow.loadURL(MAIN_WINDOW_VITE_DEV_SERVER_URL);
  } else {
    mainWindow.loadFile(path.join(__dirname, `../renderer/${MAIN_WINDOW_VITE_NAME}/index.html`));
  }

  // Open the DevTools.
  mainWindow.webContents.openDevTools();

  ipcMain.on("START", () => {
    interactionWiFiActor?.stop();
    interactionWiFiActor = createActor(InteractionWiFiMachine, {
      inspect: (inspectionEvent) => {
        if (inspectionEvent.type === "@xstate.snapshot") {
          const rawSnapshot = stringify(inspectionEvent.snapshot);
          mainWindow.webContents.send("SNAPSHOT", rawSnapshot);
        } else if (inspectionEvent.type === "@xstate.event") {
          const rawEvent = stringify(inspectionEvent.event);
          mainWindow.webContents.send("EVENT", rawEvent);
        }
      }
    })
    interactionWiFiActor.start();
  }),
  ipcMain.on("MEASURE", (_, request: MeasurementRequest) => {
    interactionWiFiActor?.send({
      type: 'MEASUREMENT_REQUEST',
      request
    })
  })
};

// This method will be called when Electron has finished
// initialization and is ready to create browser windows.
// Some APIs can only be used after this event occurs.
app.on('ready', createWindow);

// Quit when all windows are closed, except on macOS. There, it's common
// for applications and their menu bar to stay active until the user quits
// explicitly with Cmd + Q.
app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app.on('activate', () => {
  // On OS X it's common to re-create a window in the app when the
  // dock icon is clicked and there are no other windows open.
  if (BrowserWindow.getAllWindows().length === 0) {
    createWindow();
  }
});

// In this file you can include the rest of your app's specific main process
// code. You can also put them in separate files and import them here.
