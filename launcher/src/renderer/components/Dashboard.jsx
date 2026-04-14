import { useEffect, useRef, useState } from "react";
import UpdateBanner from "./UpdateBanner";

function BetaAccessBadge({ active }) {
  return (
    <span className={active ? "beta-badge active" : "beta-badge locked"}>
      <span className="beta-dot" />
      {active ? "Beta Active" : "Beta Locked"}
    </span>
  );
}

function LauncherStatusFooter({ launcherRuntime, gameRuntime }) {
  const phase = launcherRuntime.launcherUpdate?.phase || "idle";
  const launcherVersion = launcherRuntime.launcherInfo?.version || "...";
  const installedVersion = gameRuntime.gameState?.installed?.installedVersion || "Not installed";
  const labelMap = {
    idle: "Launcher up to date",
    checking: "Checking launcher update",
    available: "Launcher update available",
    downloading: launcherRuntime.launcherUpdate?.message || "Downloading launcher update",
    downloaded: "Launcher update ready",
    error: launcherRuntime.launcherUpdate?.message || "Launcher update check failed",
    dev: "Launcher updates disabled in dev mode",
  };

  return (
    <footer className="dashboard-footer">
      <div className={`launcher-status-footer ${phase}`}>
        <span className="footer-status-dot" />
        <span>{labelMap[phase] || launcherRuntime.launcherUpdate?.message || "Launcher status unavailable"}</span>
      </div>
      <div className="footer-version-meta">
        <span>Launcher v{launcherVersion}</span>
        <span>Game {installedVersion}</span>
      </div>
    </footer>
  );
}

function DashboardHeader({ authState, gameRuntime, onOpenSettings }) {
  const [isMenuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef(null);
  const profileName =
    authState.profile?.display_name ||
    authState.session?.user?.email?.split("@")[0] ||
    "Pilot";
  const installPath = gameRuntime.gameState?.installed?.installDir || "Not selected yet";

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
        <BetaAccessBadge active={authState.hasBetaAccess} />
        <button className="profile-button" onClick={() => setMenuOpen((value) => !value)} type="button">
          <span className="profile-meta">
            <strong>{profileName}</strong>
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
            <button className="menu-action" onClick={onOpenSettings}>
              Open Settings
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

function PrimaryActionRail({
  gameRuntime,
  authState,
  launcherRuntime,
  launcherPlayBlockMessage,
  onPrimaryAction,
  onOpenBetaModal,
  onRequestUninstall,
}) {
  const [isGameMenuOpen, setGameMenuOpen] = useState(false);
  const gameMenuRef = useRef(null);
  const installPath = gameRuntime.gameState?.installed?.installDir || "No install folder selected yet.";
  const isPlayBlockedByLauncher = gameRuntime.primaryAction === "play" && !!launcherPlayBlockMessage;

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
      <UpdateBanner
        launcherUpdate={launcherRuntime.launcherUpdate}
        gameRuntime={gameRuntime}
        onLauncherDownload={launcherRuntime.downloadUpdate}
        onLauncherInstall={launcherRuntime.quitAndInstall}
      />

      {gameRuntime.progressState.visible ? (
        <div className="install-progress-card">
          <div className="install-progress-meta">
            <span>{gameRuntime.progressState.label}</span>
            {!gameRuntime.progressState.indeterminate ? <strong>{gameRuntime.progressState.percent}%</strong> : null}
          </div>
          {gameRuntime.statusMessage ? (
            <p className="install-progress-message">{gameRuntime.statusMessage}</p>
          ) : null}
          <div className="install-progress-track">
            <span
              className={gameRuntime.progressState.indeterminate ? "install-progress-bar indeterminate" : "install-progress-bar"}
              style={
                gameRuntime.progressState.indeterminate
                  ? undefined
                  : { width: `${Math.max(0, Math.min(100, gameRuntime.progressState.percent))}%` }
              }
            />
          </div>
        </div>
      ) : null}

      <div className="game-action-row" ref={gameMenuRef}>
        <button
          className="primary mega-button game-action-button"
          disabled={!gameRuntime.canTriggerPrimary || isPlayBlockedByLauncher}
          onClick={onPrimaryAction}
        >
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
            <button className="menu-action" onClick={gameRuntime.repairGame}>
              Repair Install
            </button>
            <button className="menu-action" onClick={() => gameRuntime.refreshState({ reason: "manual" })}>
              Check for Game Update
            </button>
            <button className="menu-action" onClick={gameRuntime.copyDiagnostics}>
              Copy Diagnostics
            </button>
            <button className="menu-action danger" onClick={onRequestUninstall}>
              Uninstall Game
            </button>
          </div>
        ) : null}
      </div>

      {isPlayBlockedByLauncher ? (
        <p className="launcher-play-block-message">{launcherPlayBlockMessage}</p>
      ) : null}

      {!authState.hasBetaAccess ? (
        <button className="text-button" onClick={onOpenBetaModal}>
          Enter beta key instead
        </button>
      ) : null}
    </aside>
  );
}

function OperationTab() {
  return (
    <div className="operation-panel">
      <div className="project-hero-background" />
      <div className="operation-panel-content">
        <p className="eyebrow">Upcoming Intel</p>
        <h2>Operation feed pending</h2>
        <p>
          Reserved for key art, mode previews, event callouts, and beta announcements once the game visuals are ready.
        </p>
      </div>
    </div>
  );
}

function PatchNotesTab({ gameRuntime }) {
  return (
    <div className="patch-panel">
      <div className="panel-header spread">
        <div>
          <p className="eyebrow">Patch Notes</p>
          <h2>Latest build notes</h2>
        </div>
        <button className="secondary" onClick={gameRuntime.refreshState} type="button">
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
    </div>
  );
}

function DashboardContentCard({ gameRuntime }) {
  const [activeTab, setActiveTab] = useState("operation");

  return (
    <section className="dashboard-content-card panel">
      <div className="content-card-tabs" role="tablist" aria-label="Launcher information">
        <button
          aria-selected={activeTab === "operation"}
          className={activeTab === "operation" ? "content-tab active" : "content-tab"}
          onClick={() => setActiveTab("operation")}
          role="tab"
          type="button"
        >
          Operation
        </button>
        <button
          aria-selected={activeTab === "patch-notes"}
          className={activeTab === "patch-notes" ? "content-tab active" : "content-tab"}
          onClick={() => setActiveTab("patch-notes")}
          role="tab"
          type="button"
        >
          Patch Notes
        </button>
      </div>

      <div className="content-card-body">
        {activeTab === "operation" ? (
          <OperationTab />
        ) : (
          <PatchNotesTab gameRuntime={gameRuntime} />
        )}
      </div>
    </section>
  );
}

export default function Dashboard({
  authState,
  launcherRuntime,
  gameRuntime,
  launcherPlayBlockMessage,
  onPrimaryAction,
  onOpenBetaModal,
  onRequestUninstall,
  onOpenSettings,
}) {
  return (
    <section className="dashboard-shell">
      <DashboardHeader
        authState={authState}
        gameRuntime={gameRuntime}
        onOpenSettings={onOpenSettings}
      />

      <div className="dashboard-grid">
        <PrimaryActionRail
          gameRuntime={gameRuntime}
          authState={authState}
          launcherRuntime={launcherRuntime}
          launcherPlayBlockMessage={launcherPlayBlockMessage}
          onPrimaryAction={onPrimaryAction}
          onOpenBetaModal={onOpenBetaModal}
          onRequestUninstall={onRequestUninstall}
        />

        <div className="dashboard-center">
          <DashboardContentCard gameRuntime={gameRuntime} />
        </div>
      </div>

      <LauncherStatusFooter launcherRuntime={launcherRuntime} gameRuntime={gameRuntime} />
    </section>
  );
}
