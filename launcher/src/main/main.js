const { app, BrowserWindow, ipcMain, shell } = require("electron");
const { autoUpdater } = require("electron-updater");
const { spawn } = require("child_process");
const fs = require("fs");
const fsp = require("fs/promises");
const path = require("path");
const { pipeline } = require("stream/promises");
const { Readable } = require("stream");

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
    width: 1280,
    height: 820,
    minWidth: 1080,
    minHeight: 720,
    backgroundColor: "#101417",
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
  const installRoot =
    process.env.LAUNCHER_GAME_INSTALL_DIR ||
    path.join(app.getPath("userData"), "game-install");

  return {
    manifestUrl: process.env.LAUNCHER_GAME_MANIFEST_URL || "",
    gameExecutable: process.env.LAUNCHER_GAME_EXECUTABLE || DEFAULT_GAME_EXECUTABLE,
    installRoot,
    metadataPath: path.join(app.getPath("userData"), "game-install.json"),
    downloadsRoot: path.join(app.getPath("userData"), "downloads"),
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
    installDir: config.installRoot,
    executablePath: resolveExecutable(config.installRoot, config.gameExecutable),
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
  if (!response.ok || !response.body) {
    throw new Error(`Download failed with ${response.status} ${response.statusText}`);
  }

  const total = Number(response.headers.get("content-length") || 0);
  let transferred = 0;

  await ensureDir(path.dirname(destinationPath));

  const writeStream = fs.createWriteStream(destinationPath);
  const nodeStream = Readable.fromWeb(response.body);

  nodeStream.on("data", (chunk) => {
    transferred += chunk.length;

    sendToRenderer("game:download-progress", {
      transferred,
      total,
      percent: total > 0 ? Math.round((transferred / total) * 100) : null,
    });
  });

  await pipeline(nodeStream, writeStream);
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

    if (!manifest) {
      throw new Error("A remote game manifest is required before the launcher can install the game.");
    }

    const zipPath = path.join(config.downloadsRoot, manifest.fileName);
    const stagingDir = path.join(app.getPath("userData"), "staging", manifest.version);
    const finalInstallDir = config.installRoot;

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
    await ensureDir(path.dirname(finalInstallDir));
    await fsp.rename(stagingDir, finalInstallDir);

    const executablePath = resolveExecutable(finalInstallDir, manifest.launchExecutable);
    if (!(await pathExists(executablePath))) {
      throw new Error(
        `Install completed, but the launcher could not find ${manifest.launchExecutable} in ${finalInstallDir}.`
      );
    }

    await writeJson(config.metadataPath, {
      version: manifest.version,
      installDir: finalInstallDir,
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
  autoUpdater.autoDownload = false;
  autoUpdater.autoInstallOnAppQuit = true;

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

  ipcMain.handle("launcher:open-website", async () => {
    const { websiteUrl } = getConfig();
    if (!websiteUrl) {
      return { ok: false, message: "No website URL is configured." };
    }

    await shell.openExternal(websiteUrl);
    return { ok: true };
  });

  ipcMain.handle("game:get-state", async () => {
    return getGameState();
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

  ipcMain.handle("game:open-install-directory", async () => {
    const state = await getGameState();
    const target = state.installed.installDir;
    await ensureDir(target);
    await shell.openPath(target);
    return { ok: true };
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
