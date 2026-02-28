// src/main/preload.js
const { contextBridge, ipcRenderer } = require("electron");

// Expose only what you need to the renderer
contextBridge.exposeInMainWorld("launcher", {
  checkForUpdates: () => ipcRenderer.invoke("update:check"),
  onUpdateAvailable: (cb) => ipcRenderer.on("update:available", () => cb()),
  onUpdateDownloaded: (cb) => ipcRenderer.on("update:downloaded", () => cb())
});