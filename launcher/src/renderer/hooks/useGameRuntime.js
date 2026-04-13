import { useEffect, useMemo, useState } from "react";

const GAME_DESCRIPTION =
  "Drop into a cooperative sci-fi firefight built around smooth action, session-based progression, and private beta playtests.";

export function useGameRuntime({ isAuthenticated, hasBetaAccess }) {
  const [gameState, setGameState] = useState(null);
  const [downloadProgress, setDownloadProgress] = useState(null);
  const [installPhase, setInstallPhase] = useState("idle");
  const [statusMessage, setStatusMessage] = useState("");
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    const releaseInstall = window.desktop.game.onInstallStatus((payload) => {
      setInstallPhase(payload.phase || "idle");
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
    if (installPhase !== "complete") {
      return undefined;
    }

    const timeoutId = window.setTimeout(() => {
      setInstallPhase("idle");
    }, 1800);

    return () => window.clearTimeout(timeoutId);
  }, [installPhase]);

  useEffect(() => {
    if (statusMessage !== "Launching game...") {
      return undefined;
    }

    const timeoutId = window.setTimeout(() => {
      setStatusMessage("");
    }, 2500);

    return () => window.clearTimeout(timeoutId);
  }, [statusMessage]);

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
    setInstallPhase("downloading");
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

  async function launchGame(context = {}) {
    const result = await window.desktop.game.launch(context);
    if (!result.ok) {
      setStatusMessage(result.message || "Launch failed.");
      return result;
    }

    setStatusMessage("Launching game...");
    return result;
  }

  async function uninstallGame() {
    setBusy(true);
    setInstallPhase("idle");

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

  async function repairGame() {
    setBusy(true);
    setInstallPhase("repairing");
    setDownloadProgress(null);

    try {
      const result = await window.desktop.game.repair();
      if (!result.ok) {
        setStatusMessage(result.message || "Repair failed.");
        return result;
      }

      setGameState(result.state);
      setStatusMessage("Repair complete.");
      return result;
    } finally {
      setBusy(false);
    }
  }

  async function copyDiagnostics() {
    const result = await window.desktop.game.copyDiagnostics({
      isAuthenticated,
      hasBetaAccess,
    });

    if (!result.ok) {
      setStatusMessage(result.message || "Could not copy diagnostics.");
      return result;
    }

    setStatusMessage("Diagnostics copied to clipboard.");
    return result;
  }

  async function clearDownloadCache() {
    const result = await window.desktop.game.clearDownloadCache();

    if (!result.ok) {
      setStatusMessage(result.message || "Could not clear downloaded cache.");
      return result;
    }

    setGameState(result.state);
    setStatusMessage("Downloaded cache cleared.");
    return result;
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

  const progressPercent = downloadProgress?.percent ?? null;
  const phaseLabelMap = {
    downloading: progressPercent != null ? `Downloading ${progressPercent}%` : "Downloading",
    extracting: "Extracting",
    installing: "Installing",
    repairing: "Repairing",
    complete: "Ready",
  };

  const primaryLabel = phaseLabelMap[installPhase] || primaryLabelMap[primaryAction];
  const progressState = (() => {
    if (installPhase === "downloading") {
      return {
        visible: true,
        label: primaryLabel,
        percent: progressPercent ?? 8,
        indeterminate: progressPercent == null,
      };
    }

    if (installPhase === "extracting") {
      return { visible: true, label: "Extracting", percent: 72, indeterminate: false };
    }

    if (installPhase === "installing" || installPhase === "repairing") {
      return { visible: true, label: primaryLabel, percent: 88, indeterminate: false };
    }

    if (installPhase === "complete") {
      return { visible: true, label: "Ready", percent: 100, indeterminate: false };
    }

    return { visible: false, label: "", percent: 0, indeterminate: false };
  })();

  const patchNotes = (() => {
    const remote = gameState?.remote;

    if (Array.isArray(remote?.noteSections) && remote.noteSections.length > 0) {
      return remote.noteSections.flatMap((section) => {
        const items = Array.isArray(section.items) ? section.items : [];
        return items.map((item) => `${section.title}: ${item}`);
      });
    }

    if (Array.isArray(remote?.notes)) {
      return remote.notes.map((note) => String(note).trim()).filter(Boolean);
    }

    if (remote?.notes) {
      return String(remote.notes)
        .split(/\r?\n/)
        .map((note) => note.trim())
        .filter(Boolean);
    }

    return [
      "Automatic update checks on startup.",
      "Invite-only beta access gating.",
      "Launcher-aware install and play states.",
    ];
  })();

  return useMemo(
    () => ({
      gameState,
      downloadProgress,
      installPhase,
      progressState,
      statusMessage,
      busy,
      installState,
      primaryAction,
      primaryLabel,
      canTriggerPrimary:
        !busy && isAuthenticated,
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
      repairGame,
      uninstallGame,
      copyDiagnostics,
      clearDownloadCache,
      openInstallDirectory: () => window.desktop.game.openInstallDirectory(),
    }),
    [
      gameState,
      downloadProgress,
      installPhase,
      progressState,
      statusMessage,
      busy,
      installState,
      primaryAction,
      primaryLabel,
      isAuthenticated,
      hasBetaAccess,
      patchNotes,
    ]
  );
}
