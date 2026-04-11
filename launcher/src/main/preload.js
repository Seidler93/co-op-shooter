const { contextBridge, ipcRenderer } = require("electron");

function subscribe(channel, callback) {
  const listener = (_event, payload) => callback(payload);
  ipcRenderer.on(channel, listener);

  return () => {
    ipcRenderer.removeListener(channel, listener);
  };
}

contextBridge.exposeInMainWorld("desktop", {
  launcher: {
    getInfo: () => ipcRenderer.invoke("launcher:get-info"),
    checkForUpdates: () => ipcRenderer.invoke("launcher:check-for-updates"),
    downloadUpdate: () => ipcRenderer.invoke("launcher:download-update"),
    quitAndInstall: () => ipcRenderer.invoke("launcher:quit-and-install"),
    relaunch: () => ipcRenderer.invoke("launcher:relaunch"),
    openDataDirectory: () => ipcRenderer.invoke("launcher:open-data-directory"),
    openWebsite: () => ipcRenderer.invoke("launcher:open-website"),
    onUpdateStatus: (callback) => subscribe("launcher:update-status", callback),
  },
  game: {
    getState: () => ipcRenderer.invoke("game:get-state"),
    checkForUpdates: () => ipcRenderer.invoke("game:check-for-updates"),
    installOrUpdate: () => ipcRenderer.invoke("game:install-or-update"),
    launch: () => ipcRenderer.invoke("game:launch"),
    repair: () => ipcRenderer.invoke("game:repair"),
    chooseInstallDirectory: () => ipcRenderer.invoke("game:choose-install-directory"),
    openInstallDirectory: () => ipcRenderer.invoke("game:open-install-directory"),
    uninstall: () => ipcRenderer.invoke("game:uninstall"),
    clearDownloadCache: () => ipcRenderer.invoke("game:clear-download-cache"),
    copyDiagnostics: (context) => ipcRenderer.invoke("game:copy-diagnostics", context),
    onDownloadProgress: (callback) => subscribe("game:download-progress", callback),
    onInstallStatus: (callback) => subscribe("game:install-status", callback),
  },
});
