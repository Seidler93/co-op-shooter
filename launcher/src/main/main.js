const { app, BrowserWindow, ipcMain, dialog } = require("electron");
const path = require("path");
const { autoUpdater } = require("electron-updater");

let win;

function createWindow() {
  win = new BrowserWindow({
    width: 1100,
    height: 700,
    webPreferences: {
      preload: path.join(__dirname, "preload.js"),
      contextIsolation: true,
      nodeIntegration: false
    }
  });

  const isDev = !app.isPackaged;

  if (isDev) {
    win.loadURL("http://localhost:5173");
    win.webContents.openDevTools({ mode: "detach" });
  } else {
    win.loadFile(path.join(__dirname, "../renderer/index.html"));
  }
}

app.whenReady().then(() => {
  createWindow();

  // Auto update checks in production only
  if (app.isPackaged) {
    autoUpdater.checkForUpdatesAndNotify();
  }
});

autoUpdater.on("update-available", () => {
  win?.webContents.send("update:available");
});

autoUpdater.on("update-downloaded", async () => {
  win?.webContents.send("update:downloaded");
  const result = await dialog.showMessageBox(win, {
    type: "info",
    buttons: ["Restart Now", "Later"],
    message: "Update downloaded. Restart to apply?"
  });
  if (result.response === 0) autoUpdater.quitAndInstall();
});

ipcMain.handle("update:check", async () => {
  if (!app.isPackaged) return { skipped: true, reason: "dev" };
  const res = await autoUpdater.checkForUpdates();
  return { ok: true, res: !!res };
});