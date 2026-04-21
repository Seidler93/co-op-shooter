import ScreenFrame from "../components/ScreenFrame";
import { Panel, PanelHeading, StatusPill } from "../components/Panel";

const nightmareMaps = [
  {
    id: "district-seven",
    kicker: "Map 01",
    title: "District 7 Depot",
    body: "Tight rail lanes, fast rotation loops, and a rooftop fallback for late-round kiting.",
    status: "featured"
  },
  {
    id: "blacksite-yard",
    kicker: "Map 02",
    title: "Blacksite Yard",
    body: "Large open yard with long sightlines, power gates, and heavier elite pressure.",
    status: "standard"
  },
  {
    id: "mercy-labs",
    kicker: "Map 03",
    title: "Mercy Labs",
    body: "Close interior corridors with quick revive routes and risky trap chokepoints.",
    status: "locked"
  }
];

const nightmareSystems = [
  { label: "Starting Round", value: "1" },
  { label: "Squad Size", value: "4 Players" },
  { label: "Difficulty Ramp", value: "Every 5 Rounds" },
  { label: "Boss Spawn", value: "Round 12+" }
];

const nightmarePerks = ["Rapid Triage", "Ammo Mule", "Dead Wire", "Quick Reload", "Stamina Burst", "Juggernaut"];

export default function NightmareModeScreen({ onBack, playerSlots, topBar }) {
  return (
    <section className="game-screen is-active" id="nightmare-mode">
      <ScreenFrame backdropClassName="backdrop-lobby" gridClassName="screen-grid-campaign" topBar={topBar}>
        <Panel className="campaign-nav-card">
          <PanelHeading kicker="Nightmare Mode" title="Endless Maps" status={<StatusPill tone="warning">Featured</StatusPill>} />
          <div className="campaign-nav-list">
            {nightmareMaps.map((map, index) => (
              <button
                className={`campaign-nav-item ${index === 0 ? "is-selected" : ""} ${map.status === "locked" ? "is-muted" : ""}`.trim()}
                type="button"
                key={map.id}
              >
                <span className="campaign-nav-kicker">{map.kicker}</span>
                <strong>{map.title}</strong>
                <span>{map.status === "locked" ? "Locked" : map.status === "featured" ? "Current Playlist" : "Available"}</span>
              </button>
            ))}
          </div>
        </Panel>

        <Panel className="campaign-briefing-card nightmare-briefing-card">
          <PanelHeading
            kicker="Endless Survival"
            title="District 7 Depot"
            status={<StatusPill tone="ready">Round-Based Co-Op</StatusPill>}
          />
          <div className="campaign-briefing-copy">
            <h4>Hold. Buy. Survive.</h4>
            <p>
              Fight through endless infected rounds, open new sections of the depot, upgrade your weapons, and stay
              alive as elites, bosses, and special waves scale harder with every cycle.
            </p>
          </div>

          <div className="campaign-objective-grid nightmare-info-grid">
            <article className="campaign-objective-card">
              <span className="panel-kicker">Core Loop</span>
              <h4>Endless survival escalation</h4>
              <p>Earn salvage and essence, unlock doors, buy perks, and push your squad deeper into higher rounds.</p>
            </article>
            <article className="campaign-objective-card">
              <span className="panel-kicker">Failure State</span>
              <h4>Full squad wipe ends the run</h4>
              <p>Downed players can still be recovered, but a full team collapse ends the match and posts the final round.</p>
            </article>
          </div>

          <div className="nightmare-level-preview" aria-hidden="true">
            <div className="campaign-level-overlay">
              <span className="panel-kicker">Featured Arena</span>
              <h4>District 7 Depot</h4>
            </div>
          </div>

          <div className="nightmare-systems-grid">
            {nightmareSystems.map((system) => (
              <div className="campaign-check-row" key={system.label}>
                <span>{system.label}</span>
                <strong>{system.value}</strong>
              </div>
            ))}
          </div>

          <div className="action-row">
            <button className="action-button action-button-primary" type="button">
              Start Run
            </button>
            <button className="action-button" type="button">
              Private Match
            </button>
            <button className="action-button" type="button" onClick={onBack}>
              Return to Main Menu
            </button>
          </div>
        </Panel>

        <div className="campaign-side-stack">
          <Panel className="campaign-social-card">
            <PanelHeading title="Squad" status={<StatusPill tone="ready">Ready Check</StatusPill>} />
            <div className="campaign-social-list">
              {playerSlots.slice(0, 4).map((slot) => (
                <div className={`party-row ${slot.tone}`.trim()} key={slot.name}>
                  <strong>{slot.name}</strong>
                  <span>{slot.badge}</span>
                </div>
              ))}
            </div>
          </Panel>

          <Panel className="campaign-loadout-card nightmare-loadout-card">
            <PanelHeading kicker="Run Prep" title="Starting Kit" status={<StatusPill tone="cold">Assault</StatusPill>} />

            <div className="campaign-loadout-stack">
              <article className="campaign-loadout-item is-large">
                <span className="panel-kicker">Starting Weapon</span>
                <h4>MX-12 Carbine</h4>
                <div className="campaign-item-visual" aria-hidden="true">
                  <div className="campaign-item-silhouette weapon-rifle" />
                </div>
              </article>

              <article className="campaign-loadout-item is-large">
                <span className="panel-kicker">Secondary</span>
                <h4>Vandal-19</h4>
                <div className="campaign-item-visual" aria-hidden="true">
                  <div className="campaign-item-silhouette weapon-sidearm" />
                </div>
              </article>

              <article className="campaign-loadout-item is-medium">
                <span className="panel-kicker">Field Device</span>
                <h4>Sentry Turret</h4>
                <div className="campaign-item-visual" aria-hidden="true">
                  <div className="campaign-item-silhouette gear-turret" />
                </div>
              </article>

              <article className="campaign-loadout-item is-medium">
                <span className="panel-kicker">Tactical</span>
                <h4>Shock Mine</h4>
                <div className="campaign-item-visual" aria-hidden="true">
                  <div className="campaign-item-silhouette gear-mine" />
                </div>
              </article>

              <article className="campaign-loadout-item nightmare-perk-card">
                <span className="panel-kicker">Recommended Perks</span>
                <div className="nightmare-perk-list">
                  {nightmarePerks.map((perk) => (
                    <div className="campaign-check-row" key={perk}>
                      <span>Perk</span>
                      <strong>{perk}</strong>
                    </div>
                  ))}
                </div>
              </article>
            </div>
          </Panel>
        </div>
      </ScreenFrame>
    </section>
  );
}
