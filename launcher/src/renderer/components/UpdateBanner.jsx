export default function UpdateBanner({ launcherUpdate, gameRuntime, onLauncherDownload, onLauncherInstall }) {
  const previewLauncherUpdate =
    import.meta.env.DEV && !launcherUpdate
      ? {
          phase: "available",
          message: "Launcher 0.1.16 is available.",
        }
      : import.meta.env.DEV && launcherUpdate?.phase === "idle"
        ? {
            ...launcherUpdate,
            phase: "available",
            message: "Launcher 0.1.16 is available.",
          }
        : launcherUpdate;

  const previewGameState =
    import.meta.env.DEV && !gameRuntime.gameState
      ? {
          updateAvailable: true,
        }
      : import.meta.env.DEV && !gameRuntime.gameState?.updateAvailable
        ? {
            ...gameRuntime.gameState,
            updateAvailable: true,
          }
        : gameRuntime.gameState;

  const showLauncherBanner =
    previewLauncherUpdate?.phase === "available" ||
    previewLauncherUpdate?.phase === "downloaded" ||
    previewLauncherUpdate?.phase === "downloading";

  const showGameUpdateBanner = previewGameState?.updateAvailable;

  if (!showLauncherBanner && !showGameUpdateBanner) {
    return null;
  }

  return (
    <div className="update-card-stack">
      {showLauncherBanner ? (
        <section className="update-card panel">
          <div className="update-card-copy">
            <p className="eyebrow">Launcher Update</p>
            <h3>{previewLauncherUpdate.message || "A launcher update is available."}</h3>
          </div>
          <div className="update-card-actions">
            {previewLauncherUpdate.phase === "available" ? (
              <button className="secondary compact-button" onClick={onLauncherDownload}>
                Download Update
              </button>
            ) : null}
            {previewLauncherUpdate.phase === "downloading" ? (
              <span className="update-card-status">{previewLauncherUpdate.message || "Downloading update..."}</span>
            ) : null}
            {previewLauncherUpdate.phase === "downloaded" ? (
              <button className="primary compact-button" onClick={onLauncherInstall}>
                Restart to Install
              </button>
            ) : null}
          </div>
        </section>
      ) : null}

      {showGameUpdateBanner ? (
        <section className="update-card panel game-update-card">
          <div className="update-card-copy">
            <p className="eyebrow">Game Update</p>
            <h3>A newer build is available for this install.</h3>
          </div>
        </section>
      ) : null}
    </div>
  );
}
