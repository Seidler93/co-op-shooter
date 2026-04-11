const { app, BrowserWindow, clipboard, dialog, ipcMain, shell } = require("electron");
const { autoUpdater } = require("electron-updater");
const { spawn } = require("child_process");
const fs = require("fs");
const fsp = require("fs/promises");
const os = require("os");
const path = require("path");

const APP_CHANNEL = "stable";
const DEFAULT_GAME_EXECUTABLE = "CoopShooter.exe";

let mainWindow = null;
let gameInstallInFlight = null;

function loadEnvFile() {
  const envPath = path.resolve(__dirname, "..", "..", ".env");

  if (!fs.existsSync(envPath)) {
    return;
  }

  const contents = fs.readFileSync(envPath, "utf8");
  const lines = contents.split(/\r?\n/);

  for (const line of lines) {
    const trimmed = line.trim();
    if (!trimmed || trimmed.startsWith("#")) {
      continue;
    }

    const separatorIndex = trimmed.indexOf("=");
    if (separatorIndex <= 0) {
      continue;
    }

    const key = trimmed.slice(0, separatorIndex).trim();
    const rawValue = trimmed.slice(separatorIndex + 1).trim();
    const value = rawValue.replace(/^"(.*)"$/, "$1").replace(/^'(.*)'$/, "$1");

    if (!(key in process.env)) {
      process.env[key] = value;
    }
  }
}

function isDev() {
  return !app.isPackaged;
}

function sendToRenderer(channel, payload) {
  if (mainWindow && !mainWindow.isDestroyed()) {
    mainWindow.webContents.send(channel, payload);
  }
}

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1060,
    height: 680,
    minWidth: 860,
    minHeight: 560,
    backgroundColor: "#101417",
    autoHideMenuBar: true,
    menuBarVisible: false,
    titleBarStyle: "hiddenInset",
    webPreferences: {
      preload: path.join(__dirname, "preload.js"),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: true,
    },
  });

  if (isDev()) {
    mainWindow.loadURL("http://localhost:5173");
    mainWindow.webContents.openDevTools({ mode: "detach" });
  } else {
    mainWindow.loadFile(path.join(__dirname, "../../dist-renderer/index.html"));
  }
}

function getLauncherInfo() {
  return {
    version: app.getVersion(),
    name: app.getName(),
    channel: APP_CHANNEL,
    packaged: app.isPackaged,
    platform: process.platform,
  };
}

function getConfig() {
  const localDataRoot =
    process.env.LAUNCHER_LOCAL_DATA_DIR ||
    path.join(process.env.LOCALAPPDATA || path.join(os.homedir(), "AppData", "Local"), "coop-shooter-launcher");

  const installRoot =
    process.env.LAUNCHER_GAME_INSTALL_DIR ||
    path.join(localDataRoot, "game-install");

  return {
    manifestUrl: process.env.LAUNCHER_GAME_MANIFEST_URL || "",
    launcherUpdateBaseUrl: process.env.LAUNCHER_UPDATE_BASE_URL || "",
    gameExecutable: process.env.LAUNCHER_GAME_EXECUTABLE || DEFAULT_GAME_EXECUTABLE,
    localDataRoot,
    installRoot,
    metadataPath: path.join(localDataRoot, "game-install.json"),
    downloadsRoot: path.join(localDataRoot, "downloads"),
    stagingRoot: path.join(localDataRoot, "staging"),
    websiteUrl: process.env.LAUNCHER_WEBSITE_URL || "",
  };
}

async function ensureDir(dirPath) {
  await fsp.mkdir(dirPath, { recursive: true });
}

async function readJson(filePath, fallback = null) {
  try {
    const raw = await fsp.readFile(filePath, "utf8");
    return JSON.parse(raw);
  } catch (error) {
    return fallback;
  }
}

async function getUserPreferences(config) {
  return readJson(config.metadataPath, {});
}

async function getPreferredInstallRoot(config) {
  const metadata = await getUserPreferences(config);
  return metadata.preferredInstallDir || config.installRoot;
}

function normalizeInstallChoice(selectedDir) {
  const folderName = path.basename(selectedDir).toLowerCase();
  if (folderName === "co-op shooter" || folderName === "coopshooter" || folderName === "game-install") {
    return selectedDir;
  }

  return path.join(selectedDir, "Co-op Shooter");
}

function isSafeInstallDeleteTarget(config, targetDir) {
  const resolvedTarget = path.resolve(targetDir);
  const resolvedDefaultInstall = path.resolve(config.installRoot);
  const resolvedDataRoot = path.resolve(config.localDataRoot);

  return resolvedTarget === resolvedDefaultInstall || resolvedTarget.startsWith(`${resolvedDataRoot}${path.sep}`);
}

async function writeJson(filePath, value) {
  await ensureDir(path.dirname(filePath));
  await fsp.writeFile(filePath, JSON.stringify(value, null, 2), "utf8");
}

async function pathExists(targetPath) {
  try {
    await fsp.access(targetPath, fs.constants.F_OK);
    return true;
  } catch (error) {
    return false;
  }
}

async function findFileRecursive(rootDir, fileName) {
  if (!(await pathExists(rootDir))) {
    return null;
  }

  const entries = await fsp.readdir(rootDir, { withFileTypes: true });

  for (const entry of entries) {
    const fullPath = path.join(rootDir, entry.name);

    if (entry.isFile() && entry.name.toLowerCase() === fileName.toLowerCase()) {
      return fullPath;
    }
  }

  for (const entry of entries) {
    if (!entry.isDirectory()) {
      continue;
    }

    const nestedMatch = await findFileRecursive(path.join(rootDir, entry.name), fileName);
    if (nestedMatch) {
      return nestedMatch;
    }
  }

  return null;
}

function getDevBuildPath() {
  const repoRoot = path.resolve(__dirname, "..", "..", "..");
  return path.join(repoRoot, "builds", "windows");
}

function resolveExecutable(baseDir, relativeExecutable) {
  return path.join(baseDir, relativeExecutable || DEFAULT_GAME_EXECUTABLE);
}

async function getInstalledGameRecord() {
  const config = getConfig();
  const metadata = await readJson(config.metadataPath, null);
  const preferredInstallRoot = await getPreferredInstallRoot(config);

  if (metadata && metadata.installDir) {
    const exePath = resolveExecutable(
      metadata.installDir,
      metadata.launchExecutable || config.gameExecutable
    );

    return {
      source: "managed",
      installDir: metadata.installDir,
      executablePath: exePath,
      installedVersion: metadata.version || null,
      launchExecutable: metadata.launchExecutable || config.gameExecutable,
      installedAt: metadata.installedAt || null,
    };
  }

  if (isDev()) {
    const devExe = path.join(getDevBuildPath(), config.gameExecutable);
    if (await pathExists(devExe)) {
      return {
        source: "dev-build",
        installDir: getDevBuildPath(),
        executablePath: devExe,
        installedVersion: "dev-local",
        launchExecutable: config.gameExecutable,
        installedAt: null,
      };
    }
  }

  return {
    source: "none",
    installDir: preferredInstallRoot,
    executablePath: resolveExecutable(preferredInstallRoot, config.gameExecutable),
    installedVersion: null,
    launchExecutable: config.gameExecutable,
    installedAt: null,
  };
}

async function fetchJson(url) {
  const response = await fetch(url, {
    headers: {
      "Cache-Control": "no-cache",
    },
  });

  if (!response.ok) {
    throw new Error(`Request failed with ${response.status} ${response.statusText}`);
  }

  return response.json();
}

function normalizeManifest(manifest) {
  if (!manifest || typeof manifest !== "object") {
    throw new Error("Invalid game manifest.");
  }

  const platformRelease =
    manifest.platforms?.[process.platform] ||
    manifest.platforms?.windows ||
    manifest.windows;

  if (!platformRelease?.downloadUrl) {
    throw new Error(`Manifest is missing a ${process.platform} download.`);
  }

  return {
    version: manifest.version,
    notes: manifest.notes || "",
    noteSections: manifest.noteSections || manifest.changelog || [],
    summary: manifest.summary || "",
    publishedAt: manifest.publishedAt || null,
    launchExecutable:
      platformRelease.launchExecutable ||
      manifest.launchExecutable ||
      DEFAULT_GAME_EXECUTABLE,
    downloadUrl: platformRelease.downloadUrl,
    fileName:
      platformRelease.fileName ||
      `${app.getName().toLowerCase().replace(/\s+/g, "-")}-${manifest.version}.zip`,
  };
}

async function getRemoteManifest() {
  const config = getConfig();
  if (!config.manifestUrl) {
    return null;
  }

  const manifest = await fetchJson(config.manifestUrl);
  return normalizeManifest(manifest);
}

async function getGameState() {
  const config = getConfig();
  const installed = await getInstalledGameRecord();
  const executableExists = await pathExists(installed.executablePath);

  let remote = null;
  let updateError = null;

  try {
    remote = await getRemoteManifest();
  } catch (error) {
    updateError = error.message;
  }

  const needsInstall = !executableExists;
  const updateAvailable =
    !!remote &&
    executableExists &&
    installed.installedVersion !== "dev-local" &&
    installed.installedVersion !== remote.version;

  return {
    config: {
      manifestConfigured: !!config.manifestUrl,
      websiteUrl: config.websiteUrl,
      installRoot: installed.installDir,
    },
    installed: {
      ...installed,
      executableExists,
      canLaunch: executableExists,
    },
    remote,
    needsInstall,
    updateAvailable,
    installInProgress: !!gameInstallInFlight,
    updateError,
  };
}

function isGameRunning(executablePath) {
  return new Promise((resolve) => {
    if (process.platform !== "win32") {
      resolve(false);
      return;
    }

    const imageName = path.basename(executablePath);
    const tasklist = spawn("tasklist", ["/FI", `IMAGENAME eq ${imageName}`], {
      windowsHide: true,
      stdio: ["ignore", "pipe", "ignore"],
    });

    let stdout = "";
    tasklist.stdout.on("data", (chunk) => {
      stdout += chunk.toString();
    });

    tasklist.on("close", () => {
      resolve(stdout.toLowerCase().includes(imageName.toLowerCase()));
    });

    tasklist.on("error", () => {
      resolve(false);
    });
  });
}

async function downloadFile(url, destinationPath) {
  const response = await fetch(url);
  if (!response.ok) {
    throw new Error(`Download failed with ${response.status} ${response.statusText}`);
  }

  const total = Number(response.headers.get("content-length") || 0);
  await ensureDir(path.dirname(destinationPath));
  const chunks = [];
  let transferred = 0;

  if (response.body?.getReader) {
    const reader = response.body.getReader();

    while (true) {
      const { done, value } = await reader.read();
      if (done) {
        break;
      }

      const chunk = Buffer.from(value);
      chunks.push(chunk);
      transferred += chunk.length;

      sendToRenderer("game:download-progress", {
        transferred,
        total,
        percent: total ? Math.round((transferred / total) * 100) : null,
      });
    }
  } else {
    const fallbackBuffer = Buffer.from(await response.arrayBuffer());
    chunks.push(fallbackBuffer);
    transferred = fallbackBuffer.length;
  }

  const buffer = Buffer.concat(chunks);

  await fsp.writeFile(destinationPath, buffer);

  sendToRenderer("game:download-progress", {
    transferred: buffer.length,
    total: total || buffer.length,
    percent: 100,
  });
}

function extractZip(zipPath, destinationDir) {
  return new Promise((resolve, reject) => {
    const args = [
      "-NoProfile",
      "-NonInteractive",
      "-Command",
      `Expand-Archive -LiteralPath '${zipPath.replace(/'/g, "''")}' -DestinationPath '${destinationDir.replace(/'/g, "''")}' -Force`,
    ];

    const child = spawn("powershell.exe", args, {
      windowsHide: true,
      stdio: "ignore",
    });

    child.on("close", (code) => {
      if (code === 0) {
        resolve();
        return;
      }

      reject(new Error(`Expand-Archive failed with exit code ${code}.`));
    });

    child.on("error", reject);
  });
}

async function installGame() {
  if (gameInstallInFlight) {
    return gameInstallInFlight;
  }

  gameInstallInFlight = (async () => {
    const config = getConfig();
    const manifest = await getRemoteManifest();
    const preferredInstallRoot = await getPreferredInstallRoot(config);
    const existingMetadata = await readJson(config.metadataPath, {});

    if (!manifest) {
      throw new Error("A remote game manifest is required before the launcher can install the game.");
    }

    const zipPath = path.join(config.downloadsRoot, manifest.fileName);
    const stagingDir = path.join(config.stagingRoot, manifest.version);
    const finalInstallDir = preferredInstallRoot;

    sendToRenderer("game:install-status", {
      phase: "downloading",
      message: `Downloading ${manifest.version}...`,
    });

    await downloadFile(manifest.downloadUrl, zipPath);

    sendToRenderer("game:install-status", {
      phase: "extracting",
      message: "Extracting game files...",
    });

    await fsp.rm(stagingDir, { recursive: true, force: true });
    await ensureDir(stagingDir);
    await extractZip(zipPath, stagingDir);

    sendToRenderer("game:install-status", {
      phase: "installing",
      message: "Installing game files...",
    });

    await fsp.rm(finalInstallDir, { recursive: true, force: true });
    await ensureDir(finalInstallDir);
    await extractZip(zipPath, finalInstallDir);

    const extractedExecutablePath = await findFileRecursive(finalInstallDir, manifest.launchExecutable);
    if (!extractedExecutablePath) {
      throw new Error(
        `Install completed, but the launcher could not find ${manifest.launchExecutable} in ${finalInstallDir}.`
      );
    }

    const resolvedInstallDir = path.dirname(extractedExecutablePath);
    const executablePath = extractedExecutablePath;
    if (!(await pathExists(executablePath))) {
      throw new Error(
        `Install completed, but the launcher could not find ${manifest.launchExecutable} in ${finalInstallDir}.`
      );
    }

    await writeJson(config.metadataPath, {
      ...existingMetadata,
      version: manifest.version,
      installDir: resolvedInstallDir,
      launchExecutable: manifest.launchExecutable,
      installedAt: new Date().toISOString(),
    });

    sendToRenderer("game:install-status", {
      phase: "complete",
      message: `Installed version ${manifest.version}.`,
    });

    return await getGameState();
  })();

  try {
    return await gameInstallInFlight;
  } finally {
    gameInstallInFlight = null;
  }
}

async function launchInstalledGame() {
  const state = await getGameState();

  if (!state.installed.canLaunch) {
    return {
      ok: false,
      code: "GAME_NOT_INSTALLED",
      message: "The game is not installed yet.",
    };
  }

  if (await isGameRunning(state.installed.executablePath)) {
    return {
      ok: false,
      code: "ALREADY_RUNNING",
      message: "The game is already running.",
    };
  }

  const child = spawn(state.installed.executablePath, [], {
    cwd: path.dirname(state.installed.executablePath),
    detached: true,
    windowsHide: false,
    stdio: "ignore",
  });

  child.unref();

  return { ok: true };
}

function setupAutoUpdater() {
  const { launcherUpdateBaseUrl } = getConfig();

  autoUpdater.autoDownload = false;
  autoUpdater.autoInstallOnAppQuit = true;

  if (launcherUpdateBaseUrl) {
    autoUpdater.setFeedURL({
      provider: "generic",
      url: launcherUpdateBaseUrl,
    });
  }

  autoUpdater.on("checking-for-update", () => {
    sendToRenderer("launcher:update-status", {
      phase: "checking",
      message: "Checking launcher updates...",
    });
  });

  autoUpdater.on("update-available", (info) => {
    sendToRenderer("launcher:update-status", {
      phase: "available",
      message: `Launcher ${info.version} is available.`,
      info,
    });
  });

  autoUpdater.on("update-not-available", (info) => {
    sendToRenderer("launcher:update-status", {
      phase: "idle",
      message: "Launcher is up to date.",
      info,
    });
  });

  autoUpdater.on("download-progress", (progress) => {
    sendToRenderer("launcher:update-status", {
      phase: "downloading",
      message: `Downloading launcher update ${Math.round(progress.percent || 0)}%`,
      progress,
    });
  });

  autoUpdater.on("update-downloaded", (info) => {
    sendToRenderer("launcher:update-status", {
      phase: "downloaded",
      message: `Launcher ${info.version} is ready to install.`,
      info,
    });
  });

  autoUpdater.on("error", (error) => {
    sendToRenderer("launcher:update-status", {
      phase: "error",
      message: error.message,
    });
  });
}

function registerIpc() {
  ipcMain.handle("launcher:get-info", async () => {
    return getLauncherInfo();
  });

  ipcMain.handle("launcher:check-for-updates", async () => {
    if (!app.isPackaged) {
      return {
        ok: true,
        phase: "dev",
        message: "Launcher auto-update checks only run in packaged builds.",
      };
    }

    try {
      const result = await autoUpdater.checkForUpdates();
      return {
        ok: true,
        phase: result?.updateInfo ? "checking" : "idle",
      };
    } catch (error) {
      return {
        ok: false,
        phase: "error",
        message: error.message,
      };
    }
  });

  ipcMain.handle("launcher:download-update", async () => {
    if (!app.isPackaged) {
      return { ok: false, message: "Launcher updates require a packaged build." };
    }

    try {
      await autoUpdater.downloadUpdate();
      return { ok: true };
    } catch (error) {
      return { ok: false, message: error.message };
    }
  });

  ipcMain.handle("launcher:quit-and-install", async () => {
    if (!app.isPackaged) {
      return { ok: false, message: "Launcher updates require a packaged build." };
    }

    setImmediate(() => {
      autoUpdater.quitAndInstall();
    });

    return { ok: true };
  });

  ipcMain.handle("launcher:relaunch", async () => {
    app.relaunch();
    app.exit(0);
    return { ok: true };
  });

  ipcMain.handle("launcher:open-website", async () => {
    const { websiteUrl } = getConfig();
    if (!websiteUrl) {
      return { ok: false, message: "No website URL is configured." };
    }

    await shell.openExternal(websiteUrl);
    return { ok: true };
  });

  ipcMain.handle("launcher:open-data-directory", async () => {
    const { localDataRoot } = getConfig();
    await ensureDir(localDataRoot);
    await shell.openPath(localDataRoot);
    return { ok: true };
  });

  ipcMain.handle("game:get-state", async () => {
    return getGameState();
  });

  ipcMain.handle("game:choose-install-directory", async () => {
    const config = getConfig();
    const preferences = await getUserPreferences(config);
    const defaultPath = preferences.preferredInstallDir || config.installRoot;

    const result = await dialog.showOpenDialog(mainWindow, {
      title: "Choose Game Install Folder",
      buttonLabel: "Use Folder",
      defaultPath,
      properties: ["openDirectory", "createDirectory"],
    });

    if (result.canceled || result.filePaths.length === 0) {
      return { ok: false, canceled: true };
    }

    const selectedInstallDir = normalizeInstallChoice(result.filePaths[0]);
    const nextPreferences = {
      ...preferences,
      preferredInstallDir: selectedInstallDir,
    };

    await writeJson(config.metadataPath, {
      ...preferences,
      ...nextPreferences,
    });

    return {
      ok: true,
      path: selectedInstallDir,
    };
  });

  ipcMain.handle("game:check-for-updates", async () => {
    return getGameState();
  });

  ipcMain.handle("game:install-or-update", async () => {
    try {
      return {
        ok: true,
        state: await installGame(),
      };
    } catch (error) {
      sendToRenderer("game:install-status", {
        phase: "error",
        message: error.message,
      });

      return {
        ok: false,
        message: error.message,
      };
    }
  });

  ipcMain.handle("game:launch", async () => {
    return launchInstalledGame();
  });

  ipcMain.handle("game:repair", async () => {
    try {
      sendToRenderer("game:install-status", {
        phase: "repairing",
        message: "Repairing game install...",
      });

      return {
        ok: true,
        state: await installGame(),
      };
    } catch (error) {
      sendToRenderer("game:install-status", {
        phase: "error",
        message: error.message,
      });

      return {
        ok: false,
        message: error.message,
      };
    }
  });

  ipcMain.handle("game:open-install-directory", async () => {
    const state = await getGameState();
    const target = state.installed.installDir;
    await ensureDir(target);
    await shell.openPath(target);
    return { ok: true };
  });

  ipcMain.handle("game:uninstall", async () => {
    const config = getConfig();
    const metadata = await readJson(config.metadataPath, {});
    const installDir = metadata.installDir;
    const executablePath = installDir
      ? resolveExecutable(installDir, metadata.launchExecutable || config.gameExecutable)
      : null;

    if (installDir && (await pathExists(executablePath))) {
      await fsp.rm(installDir, { recursive: true, force: true });
    }

    await writeJson(config.metadataPath, {
      preferredInstallDir: metadata.preferredInstallDir || installDir || config.installRoot,
    });

    sendToRenderer("game:install-status", {
      phase: "complete",
      message: "Game uninstalled.",
    });

    return {
      ok: true,
      state: await getGameState(),
    };
  });

  ipcMain.handle("game:copy-diagnostics", async (_event, context = {}) => {
    const config = getConfig();
    const state = await getGameState();
    const diagnostics = {
      generatedAt: new Date().toISOString(),
      launcher: getLauncherInfo(),
      game: {
        installed: state.installed,
        needsInstall: state.needsInstall,
        updateAvailable: state.updateAvailable,
        installInProgress: state.installInProgress,
        updateError: state.updateError,
        remote: state.remote
          ? {
              version: state.remote.version,
              publishedAt: state.remote.publishedAt,
              fileName: state.remote.fileName,
            }
          : null,
      },
      config: {
        manifestConfigured: !!config.manifestUrl,
        manifestUrl: config.manifestUrl,
        updateFeedConfigured: !!config.launcherUpdateBaseUrl,
        updateFeedUrl: config.launcherUpdateBaseUrl,
        websiteUrl: config.websiteUrl,
      },
      context,
    };

    const text = JSON.stringify(diagnostics, null, 2);
    clipboard.writeText(text);

    return {
      ok: true,
      text,
    };
  });

  ipcMain.handle("game:clear-download-cache", async () => {
    const config = getConfig();
    await fsp.rm(config.downloadsRoot, { recursive: true, force: true });
    await fsp.rm(config.stagingRoot, { recursive: true, force: true });
    await ensureDir(config.downloadsRoot);
    await ensureDir(config.stagingRoot);

    sendToRenderer("game:install-status", {
      phase: "complete",
      message: "Downloaded cache cleared.",
    });

    return {
      ok: true,
      state: await getGameState(),
    };
  });
}

app.whenReady().then(() => {
  loadEnvFile();
  app.setName("Co-op Shooter");
  createWindow();
  registerIpc();
  setupAutoUpdater();

  app.on("activate", () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
});

app.on("window-all-closed", () => {
  if (process.platform !== "darwin") {
    app.quit();
  }
});
