import { useEffect, useMemo, useRef, useState } from "react";

export function useLauncherRuntime() {
  const [launcherInfo, setLauncherInfo] = useState(null);
  const [launcherUpdate, setLauncherUpdate] = useState({
    phase: "idle",
    message: "Checking launcher updates on startup...",
  });
  const fallbackTimerRef = useRef(null);

  function clearFallbackTimer() {
    if (fallbackTimerRef.current) {
      window.clearTimeout(fallbackTimerRef.current);
      fallbackTimerRef.current = null;
    }
  }

  useEffect(() => {
    const release = window.desktop.launcher.onUpdateStatus((payload) => {
      setLauncherUpdate(payload);

      if (payload?.phase && payload.phase !== "checking") {
        clearFallbackTimer();
      }
    });

    async function bootstrap() {
      const info = await window.desktop.launcher.getInfo();
      setLauncherInfo(info);
      await checkForUpdates();
    }

    bootstrap();

    return () => {
      clearFallbackTimer();
      release();
    };
  }, []);

  async function checkForUpdates() {
    clearFallbackTimer();
    setLauncherUpdate({
      phase: "checking",
      message: "Checking launcher updates...",
    });

    const result = await window.desktop.launcher.checkForUpdates();
    if (!result?.ok || result?.phase === "dev" || result?.message) {
      clearFallbackTimer();
      setLauncherUpdate({
        phase: result.phase || (result.ok ? "idle" : "error"),
        message: result.message || (result.ok ? "Launcher is up to date." : "Launcher update check failed."),
      });
    } else {
      fallbackTimerRef.current = window.setTimeout(() => {
        setLauncherUpdate((current) =>
          current.phase === "checking"
            ? {
                phase: "idle",
                message: "Launcher update check finished.",
              }
            : current
        );
      }, 10000);
    }

    return result;
  }

  return useMemo(
    () => ({
      launcherInfo,
      launcherUpdate,
      checkForUpdates,
      downloadUpdate: () => window.desktop.launcher.downloadUpdate(),
      quitAndInstall: () => window.desktop.launcher.quitAndInstall(),
      relaunch: () => window.desktop.launcher.relaunch(),
      openDataDirectory: () => window.desktop.launcher.openDataDirectory(),
    }),
    [launcherInfo, launcherUpdate]
  );
}
