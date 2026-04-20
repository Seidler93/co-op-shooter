import ScreenFrame from "../components/ScreenFrame";
import { Panel, PanelHeading, StatusPill } from "../components/Panel";

const playerLoadouts = {
  "Ajax Seventy": {
    primary: "MX-12 Carbine",
    secondary: "Rook-9 Sidearm",
    equipment: "Shock Mine",
    perk: "Rapid Triage"
  },
  "Vera North": {
    primary: "Viper CQC",
    secondary: "Medic-7",
    equipment: "Stim Pack",
    perk: "Field Surgeon"
  },
  Reyes: {
    primary: "Bastion 58",
    secondary: "Hammer-45",
    equipment: "Breach Charge",
    perk: "Armor Mule"
  },
  "Invite Slot": {
    primary: "Open Slot",
    secondary: "Awaiting Player",
    equipment: "No Selection",
    perk: "No Perk"
  }
};

export default function OnlineLobbyScreen({ playerSlots, topBar }) {
  return (
    <section className="game-screen is-active" id="online-lobby">
      <ScreenFrame backdropClassName="backdrop-lobby" gridClassName="screen-grid-lobby" topBar={topBar}>
        <div className="lobby-player-grid">
          {playerSlots.map((slot) => {
            const loadout = playerLoadouts[slot.name] ?? playerLoadouts["Invite Slot"];

            return (
              <Panel className={`lobby-player-card ${slot.tone}`.trim()} key={slot.name}>
                <PanelHeading
                  kicker={slot.role}
                  title={slot.name}
                  status={<StatusPill tone={slot.tone === "is-ready" ? "ready" : slot.tone === "is-host" ? "warning" : "cold"}>{slot.badge}</StatusPill>}
                />
                <div className="lobby-player-loadout">
                  <div className="lobby-loadout-row">
                    <span>Primary</span>
                    <strong>{loadout.primary}</strong>
                  </div>
                  <div className="lobby-loadout-row">
                    <span>Secondary</span>
                    <strong>{loadout.secondary}</strong>
                  </div>
                  <div className="lobby-loadout-row">
                    <span>Equipment</span>
                    <strong>{loadout.equipment}</strong>
                  </div>
                  <div className="lobby-loadout-row">
                    <span>Perk</span>
                    <strong>{loadout.perk}</strong>
                  </div>
                </div>
              </Panel>
            );
          })}
        </div>
      </ScreenFrame>
    </section>
  );
}
