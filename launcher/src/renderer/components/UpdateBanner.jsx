export default function UpdateBanner({ launcherUpdate, gameRuntime, onLauncherDownload, onLauncherInstall, onGameRefresh }) {
  const showLauncherBanner =
    launcherUpdate?.phase === "available" ||
    launcherUpdate?.phase === "downloaded" ||
    launcherUpdate?.phase === "downloading";

  const showGameUpdateBanner = gameRuntime.gameState?.updateAvailable;

  if (!showLauncherBanner && !showGameUpdateBanner) {
    return null;
  }

  return (
    <div className="banner-stack">
      {showLauncherBanner ? (
        <section className="update-banner panel">
          <div>
            <p className="eyebrow">Launcher Update</p>
            <h3>{launcherUpdate.message}</h3>
          </div>
          <div className="button-row">
            {launcherUpdate.phase === "available" ? (
              <button className="secondary" onClick={onLauncherDownload}>
                Download Update
              </button>
            ) : null}
            {launcherUpdate.phase === "downloaded" ? (
              <button className="primary" onClick={onLauncherInstall}>
                Restart to Install
              </button>
            ) : null}
          </div>
        </section>
      ) : null}

      {showGameUpdateBanner ? (
        <section className="update-banner panel game-update-banner">
          <div>
            <p className="eyebrow">Game Update</p>
            <h3>A newer build is available for this install.</h3>
          </div>
          <button className="secondary" onClick={onGameRefresh}>
            Recheck Status
          </button>
        </section>
      ) : null}
    </div>
  );
}
