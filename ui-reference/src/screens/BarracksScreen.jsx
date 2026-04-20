import ScreenFrame from "../components/ScreenFrame";
import { Panel, PanelHeading, StatusPill } from "../components/Panel";

export default function BarracksScreen({ topBar, profileStats, summaryStats }) {
  return (
    <section className="game-screen is-active" id="barracks-screen">
      <ScreenFrame backdropClassName="backdrop-loadout" gridClassName="screen-grid-barracks" topBar={topBar}>
        <Panel className="barracks-operator-card">
          <PanelHeading kicker="Operator File" title="Ajax Seventy" status={<StatusPill tone="ready">Level 28</StatusPill>} />
          <div className="barracks-avatar-card">
            <div className="profile-avatar barracks-avatar">AZ</div>
            <div className="barracks-tag-list">
              <span className="slot-meta">Assault</span>
              <span className="slot-meta">Veteran Squad</span>
            </div>
          </div>
        </Panel>

        <div className="barracks-main-column">
          <Panel className="barracks-record-card">
            <PanelHeading kicker="Service Record" title="Career Snapshot" status={<StatusPill tone="cold">Updated</StatusPill>} />
            <div className="barracks-stat-grid">
              {profileStats.map((stat) => (
                <div className="barracks-stat-tile" key={stat.label}>
                  <span>{stat.label}</span>
                  <strong>{stat.value}</strong>
                </div>
              ))}
            </div>
          </Panel>

          <Panel className="barracks-record-card">
            <PanelHeading kicker="Combat Profile" title="Capability Spread" status={<StatusPill tone="warning">Ranked</StatusPill>} />
            <div className="summary-bars">
              {summaryStats.map((stat) => (
                <div className="summary-row" key={stat.label}>
                  <span>{stat.label}</span>
                  <div className="bar">
                    <i style={{ width: `${stat.value}%` }}></i>
                  </div>
                </div>
              ))}
            </div>
          </Panel>
        </div>

        <Panel className="barracks-side-card">
          <PanelHeading kicker="Commendations" title="Recent Unlocks" status={<StatusPill tone="ready">3 New</StatusPill>} />
          <div className="barracks-unlock-list">
            <div className="campaign-check-row">
              <strong>Weapon Charm</strong>
              <span>Extraction Fang</span>
            </div>
            <div className="campaign-check-row">
              <strong>Armor Tint</strong>
              <span>Urban Ash</span>
            </div>
            <div className="campaign-check-row">
              <strong>Title</strong>
              <span>Night Cleaner</span>
            </div>
          </div>
        </Panel>
      </ScreenFrame>
    </section>
  );
}
