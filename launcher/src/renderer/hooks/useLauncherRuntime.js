import { useEffect, useMemo, useState } from "react";

export function useLauncherRuntime() {
  const [launcherInfo, setLauncherInfo] = useState(null);
  const [launcherUpdate, setLauncherUpdate] = useState({
    phase: "idle",
    message: "Checking launcher updates on startup...",
  });

  useEffect(() => {
    const release = window.desktop.launcher.onUpdateStatus((payload) => {
      setLauncherUpdate(payload);
    });

    async function bootstrap() {
      const info = await window.desktop.launcher.getInfo();
      setLauncherInfo(info);
      await checkForUpdates();
    }

    bootstrap();

    return () => {
      release();
    };
  }, []);

  async function checkForUpdates() {
    const result = await window.desktop.launcher.checkForUpdates();
    if (result?.message || result?.phase) {
      setLauncherUpdate({
        phase: result.phase || (result.ok ? "idle" : "error"),
        message: result.message || (result.ok ? "Launcher is up to date." : "Launcher update check failed."),
      });
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
