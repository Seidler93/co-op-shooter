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
      await window.desktop.launcher.checkForUpdates();
    }

    bootstrap();

    return () => {
      release();
    };
  }, []);

  return useMemo(
    () => ({
      launcherInfo,
      launcherUpdate,
      checkForUpdates: () => window.desktop.launcher.checkForUpdates(),
      downloadUpdate: () => window.desktop.launcher.downloadUpdate(),
      quitAndInstall: () => window.desktop.launcher.quitAndInstall(),
      relaunch: () => window.desktop.launcher.relaunch(),
    }),
    [launcherInfo, launcherUpdate]
  );
}
