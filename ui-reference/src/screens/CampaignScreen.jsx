import ScreenFrame from "../components/ScreenFrame";
import { Panel, PanelHeading, StatusPill } from "../components/Panel";

const campaignOperations = [
  {
    id: "op-blackout",
    kicker: "Act 1",
    title: "Blackout Corridor",
    body: "Push through the dead district, restore emergency power, and reopen the civilian evacuation route.",
    status: "active"
  },
  {
    id: "op-mercy",
    kicker: "Act 2",
    title: "St. Mercy Collapse",
    body: "Sweep the hospital perimeter, secure the surgical wing, and recover trapped responders.",
    status: "locked"
  },
  {
    id: "op-riverline",
    kicker: "Act 3",
    title: "Riverline Extraction",
    body: "Escort the final convoy to the river barrier while infected waves breach the lower streets.",
    status: "locked"
  }
];

export default function CampaignScreen({ onBack, playerSlots, topBar }) {
  return (
    <section className="game-screen is-active" id="campaign-screen">
      <ScreenFrame backdropClassName="backdrop-lobby" gridClassName="screen-grid-campaign" topBar={topBar}>
        <Panel className="campaign-nav-card">
          <PanelHeading kicker="Campaign" title="Operations" status={<StatusPill tone="warning">1 Active</StatusPill>} />
          <div className="campaign-nav-list">
            {campaignOperations.map((operation, index) => (
              <button
                className={`campaign-nav-item ${index === 0 ? "is-selected" : ""} ${operation.status === "locked" ? "is-muted" : ""}`.trim()}
                type="button"
                key={operation.id}
              >
                <span className="campaign-nav-kicker">{operation.kicker}</span>
                <strong>{operation.title}</strong>
                <span>{operation.status === "locked" ? "Locked" : "Current Objective"}</span>
              </button>
            ))}
          </div>
        </Panel>

        <Panel className="campaign-briefing-card">
          <PanelHeading
            kicker="Operation Briefing"
            title="Blackout Corridor"
            status={<StatusPill tone="ready">Squad Recommended</StatusPill>}
          />
          <div className="campaign-briefing-copy">
            <h4>Downtown Power Relay</h4>
            <p>
              The lower commercial zone is dark, flooded, and overrun. Restore the relay line, reactivate rooftop
              beacons, and guide survivors to the rail extraction point before the horde collapses the block.
            </p>
          </div>

          <div className="campaign-objective-grid">
            <article className="campaign-objective-card">
              <span className="panel-kicker">Primary</span>
              <h4>Restart the district relay</h4>
              <p>Fight through the transit tunnel and reactivate three breaker stations before power loss becomes permanent.</p>
            </article>
            <article className="campaign-objective-card">
              <span className="panel-kicker">Secondary</span>
              <h4>Recover triage supplies</h4>
              <p>Search the clinic annex for medical cases that improve survival bonuses in later campaign operations.</p>
            </article>
          </div>

          <div className="campaign-level-preview" aria-hidden="true">
            <div className="campaign-level-overlay">
              <span className="panel-kicker">Mission Preview</span>
              <h4>Downtown Power Relay</h4>
            </div>
          </div>

          <div className="action-row">
            <button className="action-button action-button-primary" type="button">
              Start Operation
            </button>
            <button className="action-button" type="button" onClick={onBack}>
              Return to Main Menu
            </button>
          </div>
        </Panel>

        <div className="campaign-side-stack">
          <Panel className="campaign-social-card">
            <PanelHeading title="Social" status={<StatusPill tone="ready">Squad Status</StatusPill>} />
            <div className="campaign-social-list">
              {playerSlots.slice(0, 4).map((slot) => (
                <div className={`party-row ${slot.tone}`.trim()} key={slot.name}>
                  <strong>{slot.name}</strong>
                  <span>{slot.badge}</span>
                </div>
              ))}
            </div>
          </Panel>

          <Panel className="campaign-loadout-card">
            <PanelHeading kicker="Deployment" title="Loadout" status={<StatusPill tone="cold">Assault</StatusPill>} />

            <div className="campaign-loadout-stack">
              <article className="campaign-loadout-item is-large">
                <span className="panel-kicker">Primary</span>
                <h4>MX-12 Carbine</h4>
                <div className="campaign-item-visual" aria-hidden="true">
                  <div className="campaign-item-silhouette weapon-rifle" />
                </div>
              </article>

              <article className="campaign-loadout-item is-large">
                <span className="panel-kicker">Secondary</span>
                <h4>Rook-9 Sidearm</h4>
                <div className="campaign-item-visual" aria-hidden="true">
                  <div className="campaign-item-silhouette weapon-sidearm" />
                </div>
              </article>

              <article className="campaign-loadout-item is-medium">
                <span className="panel-kicker">Equipment</span>
                <h4>Shock Mine</h4>
                <div className="campaign-item-visual" aria-hidden="true">
                  <div className="campaign-item-silhouette gear-mine" />
                </div>
              </article>

              <article className="campaign-loadout-item is-medium">
                <span className="panel-kicker">Field Upgrade</span>
                <h4>Sentry Turret</h4>
                <div className="campaign-item-visual" aria-hidden="true">
                  <div className="campaign-item-silhouette gear-turret" />
                </div>
              </article>

              <article className="campaign-loadout-item is-perks">
                <span className="panel-kicker">Perks</span>
                <div className="campaign-perk-list">
                  <div className="campaign-perk-icon perk-triage" aria-label="Rapid Triage" title="Rapid Triage" />
                  <div className="campaign-perk-icon perk-ammo" aria-label="Ammo Mule" title="Ammo Mule" />
                  <div className="campaign-perk-icon perk-cold" aria-label="Cold Blooded" title="Cold Blooded" />
                </div>
              </article>
            </div>
          </Panel>
        </div>
      </ScreenFrame>
    </section>
  );
}
