// launcher/src/main/main.js
const { app, BrowserWindow, ipcMain, dialog } = require("electron");
const path = require("path");
const fs = require("fs");
const { execFile, spawn } = require("child_process");

let win;

// --------------------
// Game Launch Constants
// --------------------
const GAME_NAME = "CoopShooter";
const GAME_EXE_NAME = `${GAME_NAME}.exe`;

// --------------------
// Window
// --------------------
function createWindow() {
  win = new BrowserWindow({
    width: 1100,
    height: 700,
    webPreferences: {
      preload: path.join(__dirname, "preload.js"),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: true,
    },
  });

  const isDev = !app.isPackaged;

  if (isDev) {
    win.loadURL("http://localhost:5173");
  } else {
    win.loadFile(path.join(__dirname, "../renderer/index.html"));
  }
}

// --------------------
// Path + Validation
// --------------------

function getGameExePath() {
  const override = process.env.GAME_EXE_PATH;
  if (override && override.trim()) return path.resolve(override.trim());

  if (!app.isPackaged) {
    // __dirname ~ co-op-shooter/launcher/src/main
    const repoRoot = path.resolve(__dirname, "..", "..", "..");
    return path.join(repoRoot, "builds", "windows", GAME_EXE_NAME);
  }

  // Packaged: extraResources land here
  return path.join(process.resourcesPath, "builds", "windows", GAME_EXE_NAME);
}

function verifyUnityBuild(exePath) {
  const dir = path.dirname(exePath);
  const dataDir = path.join(dir, path.basename(exePath, ".exe") + "_Data");
  const unityPlayer = path.join(dir, "UnityPlayer.dll");

  const missing = [];
  if (!fs.existsSync(exePath)) missing.push(exePath);
  if (!fs.existsSync(dataDir)) missing.push(dataDir);
  if (!fs.existsSync(unityPlayer)) missing.push(unityPlayer);

  return { ok: missing.length === 0, missing };
}

// --------------------
// Running / Launch
// --------------------

function isGameRunning() {
  return new Promise((resolve, reject) => {
    execFile(
      "tasklist",
      ["/FI", `IMAGENAME eq ${GAME_EXE_NAME}`],
      { windowsHide: true },
      (err, stdout) => {
        if (err) return reject(err);
        const out = String(stdout || "").toLowerCase();
        resolve(out.includes(GAME_EXE_NAME.toLowerCase()));
      }
    );
  });
}

function launchGame(exePath) {
  return new Promise((resolve, reject) => {
    const child = spawn(exePath, [], {
      cwd: path.dirname(exePath),
      detached: true,
      windowsHide: false,
      stdio: "ignore",
    });

    child.on("error", reject);

    // If spawn succeeded, we can detach and consider it launched.
    child.unref();
    resolve();
  });
}

// --------------------
// IPC
// --------------------

ipcMain.handle("game:status", async () => {
  try {
    if (process.platform !== "win32") return { ok: true, running: false };
    const running = await isGameRunning();
    return { ok: true, running };
  } catch (e) {
    return { ok: false, code: "PROCESS_CHECK_FAILED", message: e?.message || "Process check failed." };
  }
});

ipcMain.handle("game:launch", async (event) => {
  try {
    if (process.platform !== "win32") {
      return { ok: false, code: "UNSUPPORTED_PLATFORM", message: "Windows only (for now)." };
    }

    const exePath = getGameExePath();

    // 1) Verify build presence (exe + Data + UnityPlayer)
    const check = verifyUnityBuild(exePath);
    if (!check.ok) {
      const parentWin = BrowserWindow.fromWebContents(event.sender) || win;

      const detail =
        `Expected Unity build files next to the exe.\n\n` +
        `Missing:\n- ${check.missing.join("\n- ")}\n\n` +
        `Resolved exe path:\n${exePath}`;

      if (parentWin) {
        dialog.showMessageBox(parentWin, {
          type: "error",
          title: "Game Build Incomplete",
          message: "Unity build files are missing or incomplete.",
          detail,
        });
      }

      return { ok: false, code: "BUILD_INCOMPLETE", message: detail };
    }

    // 2) Prevent double-launch
    const running = await isGameRunning();
    if (running) {
      return { ok: false, code: "ALREADY_RUNNING", message: "Game is already running." };
    }

    // 3) Launch
    await launchGame(exePath);
    return { ok: true };
  } catch (e) {
    const msg = e?.message || String(e);
    const parentWin = BrowserWindow.fromWebContents(event.sender) || win;

    if (parentWin) {
      dialog.showMessageBox(parentWin, {
        type: "error",
        title: "Launch Failed",
        message: "Failed to launch the game.",
        detail: msg,
      });
    }

    return { ok: false, code: "LAUNCH_FAILED", message: msg };
  }
});

// --------------------
// App Lifecycle
// --------------------
app.whenReady().then(() => {
  createWindow();

  app.on("activate", () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow();
  });
});

app.on("window-all-closed", () => {
  if (process.platform !== "darwin") app.quit();
});