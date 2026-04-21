import { useState } from "react";
import ScreenFrame from "../components/ScreenFrame";
import { Panel, PanelHeading, StatusPill } from "../components/Panel";

const barracksCategories = ["Stats", "Campaign", "Weapons", "Perks", "Equipment", "Field Upgrades", "Cosmetics"];

const barracksCareerSnapshot = [
  { label: "Deaths", value: "1,128" },
  { label: "Revives", value: "317" },
  { label: "Objectives Completed", value: "94" },
  { label: "Highest Difficulty Clear", value: "Nightmare" },
  { label: "Longest Survival", value: "42:18" },
  { label: "Squad MVPs", value: "28" }
];

const trackedChallenges = [
  {
    title: "Night Shift",
    objective: "Complete 3 Veteran or higher missions after wave 10.",
    progress: "2 / 3",
    reward: "Operator Title: Night Cleaner"
  },
  {
    title: "No One Left",
    objective: "Perform 25 squad revives in Campaign missions.",
    progress: "18 / 25",
    reward: "Calling Card: Guardian Pulse"
  },
  {
    title: "Pinned Down",
    objective: "Get 150 turret-assisted eliminations with field upgrades.",
    progress: "111 / 150",
    reward: "Field Upgrade Skin: Iron Watch"
  },
  {
    title: "Close Call",
    objective: "Survive 20 critical-health encounters and finish the mission.",
    progress: "13 / 20",
    reward: "Calling Card: Last Breath"
  },
  {
    title: "Full Sweep",
    objective: "Complete 12 secondary objectives across Campaign operations.",
    progress: "7 / 12",
    reward: "Armor Decal: Sweep Team"
  }
];

export default function BarracksScreen({ topBar }) {
  const [selectedCategory, setSelectedCategory] = useState("Stats");

  return (
    <section className="game-screen is-active" id="barracks-screen">
      <ScreenFrame backdropClassName="backdrop-loadout" gridClassName="screen-grid-barracks" topBar={topBar}>
        <Panel className="barracks-operator-card">
          <PanelHeading kicker="Barracks" title="Categories" status={<StatusPill tone="ready">{selectedCategory}</StatusPill>} />
          <div className="barracks-avatar-card">
            <div className="barracks-category-list">
              {barracksCategories.map((category) => (
                <button
                  className={`barracks-category-button ${selectedCategory === category ? "is-active" : ""}`.trim()}
                  type="button"
                  key={category}
                  onClick={() => setSelectedCategory(category)}
                >
                  {category}
                </button>
              ))}
            </div>
          </div>
        </Panel>

        <div className="barracks-main-column">
          <Panel className="barracks-record-card">
            <PanelHeading kicker="Service Record" title="Career Snapshot" status={<StatusPill tone="cold">Updated</StatusPill>} />
            <div className="barracks-stat-grid">
              {barracksCareerSnapshot.map((stat) => (
                <div className="barracks-stat-tile" key={stat.label}>
                  <span>{stat.label}</span>
                  <strong>{stat.value}</strong>
                </div>
              ))}
            </div>

            <div className="barracks-completion-card">
              <div className="barracks-completion-head">
                <strong>Total Challenges Completed</strong>
                <span>68%</span>
              </div>
              <div className="bar barracks-completion-bar" aria-hidden="true">
                <i style={{ width: "68%" }}></i>
              </div>
            </div>
          </Panel>

          <Panel className="barracks-record-card">
            {selectedCategory === "Stats" ? (
              <>
                <PanelHeading
                  kicker="Operator Showcase"
                  title="Ajax Seventy"
                  status={
                    <div className="barracks-showcase-actions">
                      {/* <StatusPill tone="warning">Urban Ash Armor</StatusPill> */}
                      <button className="action-button action-button-confirm barracks-edit-button" type="button">
                        Edit
                      </button>
                    </div>
                  }
                />
                <div className="barracks-operator-showcase">
                  <div className="barracks-character-stage" aria-hidden="true">
                    <div className="barracks-character-glow" />
                    <div className="barracks-character-silhouette" />
                  </div>

                  
                </div>
              </>
            ) : (
              <>
                <PanelHeading kicker="Progression" title={`${selectedCategory} Challenges`} status={<StatusPill tone="cold">Coming Soon</StatusPill>} />
                <div className="barracks-empty-state">
                  <strong>{selectedCategory}</strong>
                  <p>
                    This category will hold your detailed unlocks, milestone ribbons, challenge chains, and item mastery
                    progress.
                  </p>
                </div>
              </>
            )}
          </Panel>
        </div>

        <Panel className="barracks-side-card">
          <PanelHeading kicker="Tracked" title="Challenges" status={<StatusPill tone="ready">5 Active</StatusPill>} />
          <div className="barracks-unlock-list">
            {trackedChallenges.map((challenge) => (
              <div className="barracks-challenge-card" key={challenge.title}>
                <strong>{challenge.title}</strong>
                <p>{challenge.objective}</p>
                <div className="campaign-check-row">
                  <span>Progress</span>
                  <strong>{challenge.progress}</strong>
                </div>
                <div className="campaign-check-row">
                  <span>Reward</span>
                  <strong>{challenge.reward}</strong>
                </div>
              </div>
            ))}
          </div>
        </Panel>
      </ScreenFrame>
    </section>
  );
}
