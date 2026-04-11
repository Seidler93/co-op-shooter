import { useEffect, useRef, useState } from "react";
import UpdateBanner from "./UpdateBanner";

function DashboardHeader({ authState, launcherRuntime, gameRuntime }) {
  const [isMenuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef(null);
  const profileName = authState.profile?.display_name || authState.session?.user?.email || "Pilot";
  const installPath = gameRuntime.gameState?.installed?.installDir || "Not selected yet";
  const installedVersion = gameRuntime.gameState?.installed?.installedVersion || "Not installed";
  const latestVersion = gameRuntime.gameState?.remote?.version || "Unknown";

  useEffect(() => {
    function handlePointer(event) {
      if (!menuRef.current?.contains(event.target)) {
        setMenuOpen(false);
      }
    }

    window.addEventListener("pointerdown", handlePointer);
    return () => window.removeEventListener("pointerdown", handlePointer);
  }, []);

  return (
    <header className="dashboard-header">
      <div>
        <p className="eyebrow">Co-op Shooter Launcher</p>
        <h1>Project Z</h1>
      </div>

      <div className="header-actions" ref={menuRef}>
        <div className="launcher-version-chip">Launcher v{launcherRuntime.launcherInfo?.version || "..."}</div>
        <div className="launcher-version-chip">Installed {installedVersion}</div>
        <div className="launcher-version-chip">Latest {latestVersion}</div>
        <button className="profile-button" onClick={() => setMenuOpen((value) => !value)} type="button">
          <span className="profile-meta">
            <strong>{profileName}</strong>
            <span>{authState.hasBetaAccess ? "Beta access active" : "No beta access"}</span>
          </span>
          <span className="cog-icon" aria-hidden="true">
            {"\u2699"}
          </span>
        </button>

        {isMenuOpen ? (
          <div className="settings-menu panel">
            <div className="settings-row">
              <span className="label">Profile</span>
              <strong>{profileName}</strong>
            </div>
            <div className="settings-row">
              <span className="label">Beta access</span>
              <strong>{authState.hasBetaAccess ? "Unlocked" : "Locked"}</strong>
            </div>
            <div className="settings-row settings-row-stack">
              <span className="label">Install path</span>
              <strong>{installPath}</strong>
            </div>
            <button className="menu-action" onClick={launcherRuntime.relaunch}>
              Relaunch App
            </button>
            <button className="menu-action" onClick={authState.signOut}>
              Logout
            </button>
          </div>
        ) : null}
      </div>
    </header>
  );
}

function PrimaryActionRail({ gameRuntime, authState, onPrimaryAction, onOpenBetaModal }) {
  const [isGameMenuOpen, setGameMenuOpen] = useState(false);
  const gameMenuRef = useRef(null);
  const installPath = gameRuntime.gameState?.installed?.installDir || "No install folder selected yet.";

  useEffect(() => {
    function handlePointer(event) {
      if (!gameMenuRef.current?.contains(event.target)) {
        setGameMenuOpen(false);
      }
    }

    window.addEventListener("pointerdown", handlePointer);
    return () => window.removeEventListener("pointerdown", handlePointer);
  }, []);

  return (
    <aside className="action-rail">
      <div className="game-action-row" ref={gameMenuRef}>
        <button className="primary mega-button game-action-button" onClick={onPrimaryAction}>
          {gameRuntime.primaryLabel}
        </button>
        <button
          aria-label="Game install settings"
          className="game-action-cog"
          onClick={() => setGameMenuOpen((value) => !value)}
          type="button"
        >
          <span aria-hidden="true">{"\u2699"}</span>
        </button>

        {isGameMenuOpen ? (
          <div className="game-settings-menu panel">
            <div className="settings-row settings-row-stack">
              <span className="label">File location</span>
              <strong>{installPath}</strong>
            </div>
            <button className="menu-action" onClick={gameRuntime.openInstallDirectory}>
              Open File Location
            </button>
            <button className="menu-action" onClick={gameRuntime.chooseInstallDirectory}>
              Change File Location
            </button>
            <button className="menu-action danger" onClick={gameRuntime.uninstallGame}>
              Uninstall Game
            </button>
          </div>
        ) : null}
      </div>

      {!authState.hasBetaAccess ? (
        <button className="text-button" onClick={onOpenBetaModal}>
          Enter beta key instead
        </button>
      ) : null}
    </aside>
  );
}

function GameInfoPanel({ gameRuntime }) {
  return (
    <section className="project-hero panel">
      <div className="project-hero-background" />
      <div className="project-hero-content">
        <p className="eyebrow">Upcoming Intel</p>
        <h2>Operation feed pending</h2>
        <p>
          Reserved for key art, mode previews, event callouts, and beta announcements once the game visuals are ready.
        </p>
      </div>

    </section>
  );
}

function PatchNotesPanel({ gameRuntime }) {
  return (
    <section className="dashboard-panel panel">
      <div className="panel-header spread">
        <div>
          <p className="eyebrow">Patch Notes</p>
          <h2>Latest build notes</h2>
        </div>
        <button className="secondary" onClick={gameRuntime.refreshState}>
          Refresh
        </button>
      </div>

      <div className="patch-notes-list">
        {gameRuntime.patchNotes.map((note, index) => (
          <div className="patch-note" key={`${note}-${index}`}>
            <span className="patch-bullet" />
            <p>{note}</p>
          </div>
        ))}
      </div>

      {gameRuntime.statusMessage ? <div className="notice">{gameRuntime.statusMessage}</div> : null}
    </section>
  );
}

export default function Dashboard({ authState, launcherRuntime, gameRuntime, onPrimaryAction, onOpenBetaModal }) {
  return (
    <section className="dashboard-shell">
      <DashboardHeader
        authState={authState}
        launcherRuntime={launcherRuntime}
        gameRuntime={gameRuntime}
      />

      <UpdateBanner
        launcherUpdate={launcherRuntime.launcherUpdate}
        gameRuntime={gameRuntime}
        onLauncherDownload={launcherRuntime.downloadUpdate}
        onLauncherInstall={launcherRuntime.quitAndInstall}
        onGameRefresh={gameRuntime.refreshState}
      />

      <div className="dashboard-grid">
        <PrimaryActionRail
          gameRuntime={gameRuntime}
          authState={authState}
          onPrimaryAction={onPrimaryAction}
          onOpenBetaModal={onOpenBetaModal}
        />

        <div className="dashboard-center">
          <GameInfoPanel gameRuntime={gameRuntime} />
          <PatchNotesPanel gameRuntime={gameRuntime} />
        </div>
      </div>
    </section>
  );
}
