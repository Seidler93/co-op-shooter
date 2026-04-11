import { useEffect, useMemo, useState } from "react";

const GAME_DESCRIPTION =
  "Drop into a cooperative sci-fi firefight built around smooth action, session-based progression, and private beta playtests.";

export function useGameRuntime({ isAuthenticated, hasBetaAccess }) {
  const [gameState, setGameState] = useState(null);
  const [downloadProgress, setDownloadProgress] = useState(null);
  const [statusMessage, setStatusMessage] = useState("");
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    const releaseInstall = window.desktop.game.onInstallStatus((payload) => {
      setStatusMessage(payload.message);
    });

    const releaseDownload = window.desktop.game.onDownloadProgress((payload) => {
      setDownloadProgress(payload);
    });

    async function bootstrap() {
      const state = await window.desktop.game.getState();
      setGameState(state);
    }

    bootstrap();

    return () => {
      releaseInstall();
      releaseDownload();
    };
  }, []);

  useEffect(() => {
    if (!isAuthenticated) {
      return;
    }

    refreshState();
  }, [isAuthenticated]);

  async function refreshState() {
    const state = await window.desktop.game.checkForUpdates();
    setGameState(state);
    return state;
  }

  async function installOrUpdateGame() {
    setBusy(true);
    setDownloadProgress(null);

    try {
      const result = await window.desktop.game.installOrUpdate();
      if (!result.ok) {
        setStatusMessage(result.message || "Install failed.");
        return result;
      }

      setGameState(result.state);
      return result;
    } finally {
      setBusy(false);
    }
  }

  async function launchGame() {
    const result = await window.desktop.game.launch();
    if (!result.ok) {
      setStatusMessage(result.message || "Launch failed.");
      return result;
    }

    setStatusMessage("Launching game...");
    return result;
  }

  async function uninstallGame() {
    setBusy(true);

    try {
      const result = await window.desktop.game.uninstall();
      if (!result.ok) {
        setStatusMessage(result.message || "Uninstall failed.");
        return result;
      }

      setGameState(result.state);
      setStatusMessage("Game uninstalled.");
      return result;
    } finally {
      setBusy(false);
    }
  }

  const installState = gameState?.installed?.canLaunch ? "installed" : "not-installed";
  const primaryAction = gameState?.updateAvailable
    ? "update"
    : gameState?.installed?.canLaunch
      ? "play"
      : "download";

  const primaryLabelMap = {
    download: "Download",
    update: "Update",
    play: "Play",
  };

  const patchNotes = gameState?.remote?.notes
    ? gameState.remote.notes
        .split(/\r?\n/)
        .map((note) => note.trim())
        .filter(Boolean)
    : [
        "Automatic update checks on startup.",
        "Invite-only beta access gating.",
        "Launcher-aware install and play states.",
      ];

  return useMemo(
    () => ({
      gameState,
      downloadProgress,
      statusMessage,
      busy,
      installState,
      primaryAction,
      primaryLabel: primaryLabelMap[primaryAction],
      canTriggerPrimary:
        !busy &&
        isAuthenticated &&
        (primaryAction !== "download" || hasBetaAccess || !gameState?.config?.manifestConfigured),
      gameDescription: GAME_DESCRIPTION,
      patchNotes,
      chooseInstallDirectory: async () => {
        const result = await window.desktop.game.chooseInstallDirectory();
        if (result.ok) {
          setStatusMessage(`Install folder set to ${result.path}`);
          await refreshState();
        }

        return result;
      },
      refreshState,
      installOrUpdateGame,
      launchGame,
      uninstallGame,
      openInstallDirectory: () => window.desktop.game.openInstallDirectory(),
    }),
    [gameState, downloadProgress, statusMessage, busy, installState, primaryAction, isAuthenticated, hasBetaAccess, patchNotes]
  );
}
