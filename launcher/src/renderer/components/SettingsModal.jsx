import { useEffect, useState } from "react";

export default function SettingsModal({
  isOpen,
  authState,
  launcherRuntime,
  gameRuntime,
  onClose,
  onRequestUninstall,
}) {
  const [displayName, setDisplayName] = useState("");
  const [isEditingProfile, setEditingProfile] = useState(false);

  useEffect(() => {
    if (isOpen) {
      setDisplayName(authState.profile?.display_name || authState.session?.user?.email?.split("@")[0] || "");
      setEditingProfile(false);
    }
  }, [isOpen, authState.profile?.display_name, authState.session?.user?.email]);

  if (!isOpen) {
    return null;
  }

  const profileName = authState.profile?.display_name || authState.session?.user?.email || "Pilot";
  const email = authState.session?.user?.email || "Unknown";
  const installPath = gameRuntime.gameState?.installed?.installDir || "No install folder selected yet.";
  const installedVersion = gameRuntime.gameState?.installed?.installedVersion || "Not installed";
  const latestVersion = gameRuntime.gameState?.remote?.version || "Unknown";
  const launcherVersion = launcherRuntime.launcherInfo?.version || "...";
  const launcherStatus = launcherRuntime.launcherUpdate?.message || "Launcher status unavailable.";

  return (
    <div className="modal-backdrop" role="presentation" onClick={onClose}>
      <div
        className="settings-modal panel"
        role="dialog"
        aria-modal="true"
        aria-labelledby="settings-modal-title"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="settings-modal-header">
          <div>
            <p className="eyebrow">Settings</p>
            <h3 id="settings-modal-title">Launcher control center</h3>
          </div>
          <button className="secondary compact-button" onClick={onClose}>
            Close
          </button>
        </div>

        <div className="settings-modal-grid">
          <section className="settings-section">
            <p className="label">Account</p>
            {!isEditingProfile ? (
              <div className="settings-detail profile-edit-row">
                <span>Profile</span>
                <div className="profile-edit-value">
                  <strong>{profileName}</strong>
                  <button
                    aria-label="Edit display name"
                    className="icon-button"
                    onClick={() => setEditingProfile(true)}
                    type="button"
                  >
                    {"\u270E"}
                  </button>
                </div>
              </div>
            ) : (
              <div className="profile-edit-card">
                <label className="field settings-field">
                  <span>Display Name</span>
                  <input
                    value={displayName}
                    onChange={(event) => setDisplayName(event.target.value)}
                    placeholder="Pilot name"
                  />
                </label>
                <div className="button-row">
                  <button
                    className="primary compact-button"
                    disabled={authState.busy || !displayName.trim()}
                    onClick={async () => {
                      const result = await authState.saveProfile(displayName.trim());
                      if (result?.success) {
                        setEditingProfile(false);
                      }
                    }}
                  >
                    Save
                  </button>
                  <button
                    className="secondary compact-button"
                    disabled={authState.busy}
                    onClick={() => {
                      setDisplayName(authState.profile?.display_name || authState.session?.user?.email?.split("@")[0] || "");
                      setEditingProfile(false);
                    }}
                  >
                    Cancel
                  </button>
                </div>
              </div>
            )}
            <div className="settings-detail">
              <span>Email</span>
              <strong>{email}</strong>
            </div>
            <div className="settings-detail">
              <span>Beta access</span>
              <strong>{authState.hasBetaAccess ? "Active" : "Locked"}</strong>
            </div>
            {authState.message ? <div className="notice compact-notice">{authState.message}</div> : null}
            <button className="menu-action" onClick={authState.signOut}>
              Logout
            </button>
          </section>

          <section className="settings-section">
            <p className="label">Launcher</p>
            <div className="settings-detail">
              <span>Version</span>
              <strong>{launcherVersion}</strong>
            </div>
            <div className="settings-detail">
              <span>Status</span>
              <strong>{launcherStatus}</strong>
            </div>
            <button className="menu-action" onClick={launcherRuntime.checkForUpdates}>
              Check for Updates
            </button>
            <button className="menu-action" onClick={launcherRuntime.relaunch}>
              Relaunch App
            </button>
            <button className="menu-action" onClick={launcherRuntime.openDataDirectory}>
              Open Data Folder
            </button>
          </section>

          <section className="settings-section settings-section-wide">
            <p className="label">Game Install</p>
            <div className="settings-detail">
              <span>Installed</span>
              <strong>{installedVersion}</strong>
            </div>
            <div className="settings-detail">
              <span>Latest</span>
              <strong>{latestVersion}</strong>
            </div>
            <div className="settings-detail stack">
              <span>Install path</span>
              <strong>{installPath}</strong>
            </div>
            <div className="settings-action-grid">
              <button className="menu-action" onClick={gameRuntime.openInstallDirectory}>
                Open Folder
              </button>
              <button className="menu-action" onClick={gameRuntime.chooseInstallDirectory}>
                Change Folder
              </button>
              <button className="menu-action" onClick={gameRuntime.repairGame}>
                Repair Install
              </button>
              <button className="menu-action" onClick={gameRuntime.copyDiagnostics}>
                Copy Diagnostics
              </button>
              <button className="menu-action" onClick={gameRuntime.clearDownloadCache}>
                Clear Download Cache
              </button>
              <button className="menu-action danger" onClick={onRequestUninstall}>
                Uninstall Game
              </button>
            </div>
          </section>
        </div>
      </div>
    </div>
  );
}
