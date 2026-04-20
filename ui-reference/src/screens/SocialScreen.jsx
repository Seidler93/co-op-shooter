import ScreenFrame from "../components/ScreenFrame";
import { Panel, PanelHeading, StatusPill } from "../components/Panel";

export default function SocialScreen({ topBar, playerSlots }) {
  return (
    <section className="game-screen is-active" id="social-screen">
      <ScreenFrame backdropClassName="backdrop-lobby" gridClassName="screen-grid-social" topBar={topBar}>
        <Panel className="party-card">
          <PanelHeading kicker="Current Squad" title="Party" status={<StatusPill tone="ready">3/4 Online</StatusPill>} />
          <div className="player-slot-list">
            {playerSlots.map((slot) => (
              <button className={`player-slot ${slot.tone}`.trim()} type="button" key={slot.name}>
                <span className="slot-badge">{slot.badge}</span>
                <strong>{slot.name}</strong>
                <span>{slot.role}</span>
              </button>
            ))}
          </div>
        </Panel>

        <div className="lobby-actions">
          <Panel className="action-card">
            <PanelHeading kicker="Friends" title="Online Contacts" status={<StatusPill tone="ready">7 Online</StatusPill>} />
            <div className="campaign-social-list">
              {["Harrow", "Mako", "Solace", "Knox"].map((name, index) => (
                <div className={`party-row ${index < 2 ? "is-ready" : "is-empty"}`.trim()} key={name}>
                  <strong>{name}</strong>
                  <span>{index < 2 ? "Joinable" : "In Match"}</span>
                </div>
              ))}
            </div>
          </Panel>

          <Panel className="ready-card">
            <PanelHeading kicker="Social Actions" title="Quick Invite" status={<StatusPill tone="cold">Party Open</StatusPill>} />
            <div className="action-row">
              <button className="action-button action-button-primary" type="button">
                Invite Squad
              </button>
              <button className="action-button" type="button">
                Find Players
              </button>
            </div>
          </Panel>
        </div>
      </ScreenFrame>
    </section>
  );
}
