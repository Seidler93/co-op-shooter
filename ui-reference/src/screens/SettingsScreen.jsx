import ScreenFrame from "../components/ScreenFrame";
import { Panel, PanelHeading, StatusPill } from "../components/Panel";

export default function SettingsScreen({ topBar }) {
  return (
    <section className="game-screen is-active" id="settings-screen">
      <ScreenFrame backdropClassName="backdrop-loadout" gridClassName="screen-grid-settings" topBar={topBar}>
        <Panel className="category-card">
          <p className="panel-kicker">System Index</p>
          <div className="tab-stack" role="tablist" aria-label="Settings categories">
            {["Gameplay", "Video", "Audio", "Controls"].map((category, index) => (
              <button className={`category-tab ${index === 0 ? "is-active" : ""}`.trim()} type="button" key={category}>
                {category}
              </button>
            ))}
          </div>
        </Panel>

        <Panel className="settings-panel-card">
          <PanelHeading kicker="Gameplay" title="Session Preferences" status={<StatusPill tone="cold">Profile Saved</StatusPill>} />
          <div className="settings-option-list">
            <div className="campaign-check-row">
              <strong>Subtitle Language</strong>
              <span>English</span>
            </div>
            <div className="campaign-check-row">
              <strong>Crosshair Mode</strong>
              <span>Dynamic</span>
            </div>
            <div className="campaign-check-row">
              <strong>Hit Confirm</strong>
              <span>Enabled</span>
            </div>
            <div className="campaign-check-row">
              <strong>Voice Chat</strong>
              <span>Party Only</span>
            </div>
          </div>
        </Panel>

        <Panel className="summary-card">
          <PanelHeading kicker="Quick Preset" title="Current Profile" status={<StatusPill tone="ready">Tournament</StatusPill>} />
          <div className="summary-note">
            <strong>Preset Notes</strong>
            <p>Low-latency input, reduced HUD clutter, subtitles on, and squad-only voice routing.</p>
          </div>
        </Panel>
      </ScreenFrame>
    </section>
  );
}
