// src/main/preload.js
const { contextBridge, ipcRenderer } = require("electron");

// Expose only what you need to the renderer
contextBridge.exposeInMainWorld("launcher", {
  checkForUpdates: () => ipcRenderer.invoke("update:check"),
  onUpdateAvailable: (cb) => ipcRenderer.on("update:available", () => cb()),
  onUpdateDownloaded: (cb) => ipcRenderer.on("update:downloaded", () => cb())
});

contextBridge.exposeInMainWorld("game", {
  /**
   * Launch the Unity game.
   * @returns {Promise<{ ok: true } | { ok: false, code: string, message: string }>}
   */
  launch: () => ipcRenderer.invoke("game:launch"),

  /**
   * Optional helper: query running state.
   * @returns {Promise<{ ok: true, running: boolean } | { ok: false, code: string, message: string }>}
   */
  status: () => ipcRenderer.invoke("game:status"),
});