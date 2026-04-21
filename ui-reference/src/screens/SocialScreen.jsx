import { useEffect, useMemo, useRef, useState } from "react";
import ScreenFrame from "../components/ScreenFrame";
import { Panel, PanelHeading, StatusPill } from "../components/Panel";

const socialMissionBriefs = {
  Joinable: {
    mission: "Extraction Point",
    difficulty: "Veteran",
    zone: "District 7 Transit",
    objective: "Secure the relay hub and escort the team to rooftop extraction.",
    squad: "Mission in staging with one open slot."
  },
  "In Match": {
    mission: "Dead Signal",
    difficulty: "Nightmare",
    zone: "Blacksite Comms",
    objective: "Restart the uplink arrays and hold the final defense room.",
    squad: "Squad is already deployed in an active mission."
  }
};

const friendRoster = [
  {
    id: "harrow",
    name: "Harrow",
    status: "Joinable",
    rank: 42,
    role: "Scout Controller",
    favoriteWeapon: "Longwatch 7",
    kd: "2.14",
    revives: "118",
    clearRate: "87%",
    specialty: "Precision overwatch and elite callouts",
    perks: ["Awareness", "Steady Hands", "Threat Ping"],
    note: "Prefers marksman support on Veteran and Nightmare runs."
  },
  {
    id: "mako",
    name: "Mako",
    status: "Joinable",
    rank: 37,
    role: "Breach Vanguard",
    favoriteWeapon: "Bastion 58",
    kd: "1.91",
    revives: "76",
    clearRate: "81%",
    specialty: "Frontline pressure and horde lane control",
    perks: ["Recoil Control", "Damage Resistance", "Fast Hands"],
    note: "Usually hosts four-player defense runs after 8 PM."
  },
  {
    id: "solace",
    name: "Solace",
    status: "In Match",
    rank: 33,
    role: "Field Medic",
    favoriteWeapon: "Viper CQC",
    kd: "1.47",
    revives: "203",
    clearRate: "89%",
    specialty: "Recovery loops and team sustain",
    perks: ["Revive Specialist", "Quick Recovery", "Interaction Speed"],
    note: "Best with objective-heavy missions and recovery builds."
  },
  {
    id: "knox",
    name: "Knox",
    status: "In Match",
    rank: 29,
    role: "Demolitions",
    favoriteWeapon: "MX-12 Carbine",
    kd: "1.72",
    revives: "54",
    clearRate: "74%",
    specialty: "Trap setup and choke-point defense",
    perks: ["Equipment Efficiency", "Extra Charge", "Supply Specialist"],
    note: "Runs utility-heavy kits and rotates field upgrades often."
  },
  { id: "atlas", name: "Atlas", status: "Joinable", rank: 31, role: "LMG Anchor", favoriteWeapon: "Bastion 58", kd: "1.66", revives: "65", clearRate: "78%", specialty: "Sustained lane denial", perks: ["Ammo Efficiency", "Recoil Control", "Field Battery"], note: "Prefers longer holdout maps." },
  { id: "echo", name: "Echo", status: "Joinable", rank: 44, role: "Recon Flanker", favoriteWeapon: "Longwatch 7", kd: "2.33", revives: "48", clearRate: "91%", specialty: "Fast target marking", perks: ["Threat Ping", "Awareness", "Quick ADS"], note: "Often joins late-night Nightmare clears." },
  { id: "rivet", name: "Rivet", status: "In Match", rank: 26, role: "Trap Engineer", favoriteWeapon: "MX-12 Carbine", kd: "1.39", revives: "40", clearRate: "72%", specialty: "Choke setups and mine placement", perks: ["Equipment Efficiency", "Extra Charge", "Deep Pockets"], note: "Utility-first player." },
  { id: "bishop", name: "Bishop", status: "Joinable", rank: 51, role: "Support Lead", favoriteWeapon: "Medic-7", kd: "1.58", revives: "244", clearRate: "93%", specialty: "Squad sustain and rotations", perks: ["Revive Specialist", "Quick Recovery", "Supply Specialist"], note: "Reliable anchor for four-player teams." },
  { id: "nyx", name: "Nyx", status: "Joinable", rank: 22, role: "SMG Runner", favoriteWeapon: "Viper CQC", kd: "1.84", revives: "29", clearRate: "69%", specialty: "Fast objective pressure", perks: ["Sprint Recovery", "Fast Hands", "Quick ADS"], note: "Likes speed-heavy builds." },
  { id: "cinder", name: "Cinder", status: "In Match", rank: 39, role: "Area Control", favoriteWeapon: "MX-12 Carbine", kd: "1.77", revives: "82", clearRate: "84%", specialty: "Crossfire setup", perks: ["Awareness", "Interaction Speed", "Reserve Boost"], note: "Great with layered defenses." },
  { id: "ash", name: "Ash", status: "Joinable", rank: 35, role: "Flex Assault", favoriteWeapon: "MX-12 Carbine", kd: "1.69", revives: "71", clearRate: "80%", specialty: "Balanced squad filler", perks: ["Fast Hands", "Quick ADS", "Minor Armor"], note: "Comfortable in almost any role." },
  { id: "onyx", name: "Onyx", status: "In Match", rank: 48, role: "Heavy Breaker", favoriteWeapon: "Bastion 58", kd: "2.01", revives: "88", clearRate: "85%", specialty: "Boss stagger windows", perks: ["Damage Resistance", "Elite Damage", "Recoil Control"], note: "Best against elite-heavy waves." },
  { id: "wisp", name: "Wisp", status: "Joinable", rank: 27, role: "Intel Runner", favoriteWeapon: "Longwatch 7", kd: "1.54", revives: "37", clearRate: "76%", specialty: "Threat tagging and scouting", perks: ["Threat Ping", "Awareness", "Reserve Boost"], note: "Likes recon drone support." },
  { id: "flare", name: "Flare", status: "Joinable", rank: 30, role: "Shock Specialist", favoriteWeapon: "Viper CQC", kd: "1.63", revives: "46", clearRate: "75%", specialty: "Close-range burst pressure", perks: ["Quick ADS", "Fast Hands", "Equipment Efficiency"], note: "Good in short corridor fights." },
  { id: "gale", name: "Gale", status: "In Match", rank: 41, role: "Field Support", favoriteWeapon: "Medic-7", kd: "1.42", revives: "197", clearRate: "88%", specialty: "Recovery and deployables", perks: ["Quick Recharge", "Field Battery", "Revive Specialist"], note: "Strong utility support player." },
  { id: "rook", name: "Rook", status: "Joinable", rank: 24, role: "Defensive Rifleman", favoriteWeapon: "MX-12 Carbine", kd: "1.35", revives: "58", clearRate: "70%", specialty: "Holding narrow lanes", perks: ["Minor Armor", "Steady Hands", "Interaction Speed"], note: "Prefers slower, safer play." },
  { id: "vex", name: "Vex", status: "In Match", rank: 46, role: "Precision Hunter", favoriteWeapon: "Longwatch 7", kd: "2.48", revives: "61", clearRate: "92%", specialty: "Elite deletion", perks: ["Elite Damage", "Steady Hands", "Quick ADS"], note: "Excellent with specialist targets." },
  { id: "ember", name: "Ember", status: "Joinable", rank: 28, role: "Crowd Control", favoriteWeapon: "Bastion 58", kd: "1.57", revives: "49", clearRate: "73%", specialty: "Wave thinning and chokepoints", perks: ["Ammo Efficiency", "Recoil Control", "Equipment Efficiency"], note: "Prefers lane-control builds." },
  { id: "sable", name: "Sable", status: "Joinable", rank: 34, role: "Objective Escort", favoriteWeapon: "MX-12 Carbine", kd: "1.61", revives: "93", clearRate: "82%", specialty: "Escort pacing and revives", perks: ["Interaction Speed", "Quick Recovery", "Fast Hands"], note: "Strong in mission-focused squads." },
  { id: "titan", name: "Titan", status: "In Match", rank: 53, role: "Juggernaut", favoriteWeapon: "Bastion 58", kd: "1.95", revives: "102", clearRate: "86%", specialty: "Frontline soak and suppression", perks: ["Damage Resistance", "Minor Armor", "Supply Specialist"], note: "Works best as the squad anchor." }
];

export default function SocialScreen({ topBar, playerSlots, customLoadouts = [] }) {
  const [selectedFriendId, setSelectedFriendId] = useState(friendRoster[0]?.id ?? "");
  const [joinModalFriendId, setJoinModalFriendId] = useState("");
  const [selectedLoadoutId, setSelectedLoadoutId] = useState(customLoadouts[0]?.id ?? "");
  const [inviteToast, setInviteToast] = useState("");
  const [isInviteToastClosing, setIsInviteToastClosing] = useState(false);
  const inviteToastTimeoutRef = useRef(null);
  const selectedFriend = useMemo(
    () => friendRoster.find((friend) => friend.id === selectedFriendId) ?? friendRoster[0],
    [selectedFriendId]
  );
  const joinModalFriend = useMemo(
    () => friendRoster.find((friend) => friend.id === joinModalFriendId) ?? null,
    [joinModalFriendId]
  );
  const joinMission = socialMissionBriefs[joinModalFriend?.status] ?? socialMissionBriefs.Joinable;

  useEffect(() => {
    if (!inviteToast) {
      setIsInviteToastClosing(false);
      return undefined;
    }

    if (inviteToastTimeoutRef.current) {
      window.clearTimeout(inviteToastTimeoutRef.current);
    }

    const closeTimeoutId = window.setTimeout(() => {
      setIsInviteToastClosing(true);
    }, 1800);

    const clearTimeoutId = window.setTimeout(() => {
      setInviteToast("");
      setIsInviteToastClosing(false);
    }, 2450);

    inviteToastTimeoutRef.current = clearTimeoutId;

    return () => {
      window.clearTimeout(closeTimeoutId);
      window.clearTimeout(clearTimeoutId);
    };
  }, [inviteToast]);

  return (
    <section className="game-screen is-active" id="social-screen">
      <ScreenFrame backdropClassName="backdrop-lobby" gridClassName="screen-grid-social" topBar={topBar}>
        <Panel className="social-friends-panel">
          <PanelHeading kicker="Friends" title="Online Contacts" status={<StatusPill tone="ready">20 Online</StatusPill>} />
          <div className="social-friend-list">
            {friendRoster.map((friend) => (
              <div
                className={`social-friend-card ${friend.id === selectedFriend?.id ? "is-selected" : ""}`.trim()}
                key={friend.id}
              >
                <button className="social-friend-select" type="button" onClick={() => setSelectedFriendId(friend.id)}>
                  <div className="social-friend-main">
                    <div className="social-friend-head">
                      <div className="social-friend-identity">
                        <div className="social-friend-name-row">
                          <strong>{friend.name}</strong>
                          <span className="social-friend-rank">Rank {friend.rank}</span>
                        </div>
                        <span>{friend.role}</span>
                      </div>
                      <em className={`social-friend-status ${friend.status === "Joinable" ? "is-ready" : "is-busy"}`.trim()}>
                        {friend.status}
                      </em>
                    </div>
                  </div>
                </button>
                <div className="social-friend-actions">
                  {friend.status === "Joinable" ? (
                    <button
                      className="action-button action-button-confirm social-join-button"
                      type="button"
                      onClick={() => {
                        setSelectedFriendId(friend.id);
                        setJoinModalFriendId(friend.id);
                      }}
                    >
                      Join
                    </button>
                  ) : null}
                  <button
                    className="action-button action-button-confirm social-invite-button"
                    type="button"
                    onClick={() => {
                      setSelectedFriendId(friend.id);
                      setIsInviteToastClosing(false);
                      setInviteToast(`Invite sent to ${friend.name}`);
                    }}
                  >
                    Invite
                  </button>
                </div>
              </div>
            ))}
          </div>
        </Panel>

        <div className="social-right-column">
          <Panel className="party-card social-party-panel">
            <PanelHeading kicker="Current Squad" title="Party" status={<StatusPill tone="ready">3/4</StatusPill>} />
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

          <Panel className="social-barracks-panel">
            <PanelHeading kicker="Barracks" title={selectedFriend?.name ?? "Operator"} status={<StatusPill tone="cold">Selected Friend</StatusPill>} />
            <div className="social-barracks-hero">
              <div className="profile-avatar barracks-avatar">{selectedFriend?.name?.slice(0, 2).toUpperCase() ?? "OP"}</div>
              <div>
                <h3>{selectedFriend?.role}</h3>
                <p>{selectedFriend?.specialty}</p>
              </div>
            </div>

            <div className="barracks-stat-grid social-barracks-stats">
              <div className="barracks-stat-tile">
                <span>Rank</span>
                <strong>{selectedFriend?.rank ?? "--"}</strong>
              </div>
              <div className="barracks-stat-tile">
                <span>K/D</span>
                <strong>{selectedFriend?.kd ?? "--"}</strong>
              </div>
              <div className="barracks-stat-tile">
                <span>Clear Rate</span>
                <strong>{selectedFriend?.clearRate ?? "--"}</strong>
              </div>
            </div>

            <div className="social-barracks-note">
              <strong>Favorite Weapon</strong>
              <p>{selectedFriend?.favoriteWeapon}</p>
            </div>

            <div className="social-barracks-note">
              <strong>Revives</strong>
              <p>{selectedFriend?.revives} clutch revives this season.</p>
            </div>

            <div className="social-barracks-note">
              <strong>Preferred Perks</strong>
              <div className="equipped-perk-chip-list">
                {selectedFriend?.perks.map((perk) => (
                  <div className="campaign-perk-chip" key={perk}>
                    {perk}
                  </div>
                ))}
              </div>
            </div>

            <div className="social-barracks-note">
              <strong>Operator Note</strong>
              <p>{selectedFriend?.note}</p>
            </div>
          </Panel>
        </div>

        {joinModalFriend ? (
          <div className="perk-modal-backdrop" role="presentation">
            <div className="perk-swap-modal social-join-modal" role="dialog" aria-modal="true" aria-labelledby="social-join-title">
              <div className="perk-modal-head">
                <div>
                  <span className="panel-kicker">Join Squad</span>
                  <h3 id="social-join-title">{joinModalFriend.name}'s Session</h3>
                </div>
                <StatusPill tone="ready">{joinMission.difficulty}</StatusPill>
              </div>

              <div className="social-join-section">
                <strong>Mission Brief</strong>
                <div className="campaign-check-row">
                  <span>Operation</span>
                  <strong>{joinMission.mission}</strong>
                </div>
                <div className="campaign-check-row">
                  <span>Zone</span>
                  <strong>{joinMission.zone}</strong>
                </div>
                <div className="campaign-check-row">
                  <span>Objective</span>
                  <strong>{joinMission.objective}</strong>
                </div>
                <div className="campaign-check-row">
                  <span>Squad Status</span>
                  <strong>{joinMission.squad}</strong>
                </div>
              </div>

              <div className="social-join-section">
                <strong>Select Loadout</strong>
                <div className="social-join-loadouts">
                  {customLoadouts.map((loadout) => (
                    <button
                      className={`loadout-preset-button ${selectedLoadoutId === loadout.id ? "is-active" : ""}`.trim()}
                      type="button"
                      key={loadout.id}
                      onClick={() => setSelectedLoadoutId(loadout.id)}
                    >
                      <span>{loadout.role}</span>
                      <strong>{loadout.name}</strong>
                      <em>{loadout.primary}</em>
                    </button>
                  ))}
                </div>
              </div>

              <div className="perk-modal-actions">
                <button className="action-button" type="button" onClick={() => setJoinModalFriendId("")}>
                  Cancel
                </button>
                <button className="action-button action-button-confirm" type="button" onClick={() => setJoinModalFriendId("")}>
                  Join
                </button>
              </div>
            </div>
          </div>
        ) : null}

        {inviteToast ? (
          <div className={`social-invite-toast ${isInviteToastClosing ? "is-closing" : ""}`.trim()}>{inviteToast}</div>
        ) : null}
      </ScreenFrame>
    </section>
  );
}
