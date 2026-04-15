import ScreenFrame from "../components/ScreenFrame";
import { Panel, PanelHeading, StatusPill } from "../components/Panel";

export default function OnlineLobbyScreen({ playerSlots, missionDifficulties, isReady, onToggleReady }) {
  return (
    <section className="game-screen is-active" id="online-lobby">
      <ScreenFrame backdropClassName="backdrop-lobby" gridClassName="screen-grid-lobby">
        <Panel className="party-card">
          <PanelHeading kicker="Strike Team" title="Party Panel" status={<StatusPill tone="ready">3/4 Online</StatusPill>} />
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
            <p className="panel-kicker">Session Control</p>
            <h3>Host or Join</h3>
            <div className="action-row">
              <button className="action-button action-button-primary" type="button">
                Host Match
              </button>
              <button className="action-button" type="button">
                Join by Code
              </button>
            </div>
            <div className="session-code">
              <span>Squad Code</span>
              <strong>RAVN-317</strong>
            </div>
          </Panel>

          <Panel className="mission-card">
            <PanelHeading
              kicker="Operation Select"
              title="Mission and Difficulty"
              status={<StatusPill tone="warning">Veteran</StatusPill>}
            />
            <div className="mission-summary">
              <h4>St. Mercy Evacuation Route</h4>
              <p>Escort survivors through the quarantine corridor and secure rooftop extraction.</p>
            </div>
            <div className="difficulty-row">
              {missionDifficulties.map((difficulty) => (
                <button
                  className={`difficulty-chip ${difficulty === "Veteran" ? "is-active" : ""}`.trim()}
                  type="button"
                  key={difficulty}
                >
                  {difficulty}
                </button>
              ))}
            </div>
          </Panel>

          <Panel className="ready-card">
            <p className="panel-kicker">Deployment</p>
            <h3>Final Check</h3>
            <p>All players must confirm loadouts before the host can launch the mission.</p>
            <button
              className={`ready-button ${isReady ? "is-confirmed" : ""}`.trim()}
              type="button"
              aria-pressed={isReady}
              onClick={onToggleReady}
            >
              {isReady ? "Ready Confirmed" : "Ready Up"}
            </button>
          </Panel>
        </div>
      </ScreenFrame>
    </section>
  );
}
