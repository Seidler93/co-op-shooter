import { useEffect, useState } from "react";
import ScreenFrame from "../components/ScreenFrame";
import { Panel, PanelHeading, StatusPill } from "../components/Panel";

export default function MainMenuScreen({ onOpenCampaign, profileStats, newsItems, playerSlots }) {
  const noteGroups = [
    {
      id: "patch",
      items: newsItems.filter((item) => item.category === "patch")
    },
    {
      id: "community",
      items: newsItems.filter((item) => item.category === "community")
    }
  ];

  const [activeNotes, setActiveNotes] = useState(() =>
    Object.fromEntries(noteGroups.map((group) => [group.id, 0]))
  );

  useEffect(() => {
    const patchItems = newsItems.filter((item) => item.category === "patch");
    const communityItems = newsItems.filter((item) => item.category === "community");

    const patchIntervalId = window.setInterval(() => {
      setActiveNotes((current) =>
        Object.fromEntries([
          ["patch", ((current.patch ?? 0) + 1) % patchItems.length],
          ["community", current.community ?? 0]
        ])
      );
    }, 7600);

    const communityIntervalId = window.setInterval(() => {
      setActiveNotes((current) =>
        Object.fromEntries([
          ["patch", current.patch ?? 0],
          ["community", ((current.community ?? 0) + 1) % communityItems.length]
        ])
      );
    }, 12200);

    return () => {
      window.clearInterval(patchIntervalId);
      window.clearInterval(communityIntervalId);
    };
  }, [newsItems]);

  return (
    <section className="game-screen is-active" id="main-menu">
      <ScreenFrame backdropClassName="backdrop-main" gridClassName="screen-grid-main">
        <div className="main-menu-column">
          <div className="title-block">
            <h2 className="game-title">
              <span className="game-title-prefix">PROJECT:</span>
              <span className="game-title-main">BLACKWELL</span>
            </h2>
          </div>

          <nav className="menu-stack" aria-label="Main menu navigation">
            {["Campaign", "Nightmare Mode", "Barracks", "Social", "Settings", "Quit"].map((label) => (
              <button
                key={label}
                className="menu-button"
                type="button"
                onClick={label === "Campaign" ? onOpenCampaign : undefined}
              >
                {label}
              </button>
            ))}
          </nav>
        </div>

        <div className="main-menu-center-spacer" aria-hidden="true" />

        <div className="main-menu-side">
          <Panel className="profile-card profile-card-compact">
            <div className="profile-head">
              <div className="profile-avatar">AZ</div>
              <div>
                <h3>Ajax Seventy</h3>
                <p>Level {profileStats.find((stat) => stat.label === "Level")?.value ?? "--"}</p>
              </div>
            </div>
            <div className="level-progress" aria-hidden="true">
              <div className="level-progress-fill" />
            </div>
          </Panel>

          <Panel className="party-card party-card-compact">
            <PanelHeading title="Party" status={<StatusPill tone="ready">3/4 Online</StatusPill>} />
            <div className="party-list-compact">
              {playerSlots.slice(0, 4).map((slot) => (
                <div className={`party-row ${slot.tone}`.trim()} key={slot.name}>
                  <strong>{slot.name}</strong>
                  <span>{slot.badge}</span>
                </div>
              ))}
            </div>
          </Panel>

          <div className="news-card-stack">
            {noteGroups.map((group) => {
              const activeIndex = activeNotes[group.id] ?? 0;
              const item = group.items[activeIndex];

              return (
                <article className="news-item news-item-compact" key={group.id}>
                  <span className="news-tag">{item.tag}</span>
                  <h4>{item.title}</h4>
                  <p>{item.body}</p>

                  <div className="news-item-footer">
                    <div className="note-indicators" aria-label={`${group.id} notes`}>
                      {group.items.map((entry, index) => (
                        <button
                          key={`${group.id}-${entry.title}`}
                          aria-label={`Show ${group.id} note ${index + 1}`}
                          aria-pressed={activeIndex === index}
                          className={`note-indicator ${activeIndex === index ? "is-active" : ""}`.trim()}
                          onClick={() =>
                            setActiveNotes((current) => ({
                              ...current,
                              [group.id]: index
                            }))
                          }
                          type="button"
                        />
                      ))}
                    </div>
                  </div>
                </article>
              );
            })}
          </div>
        </div>
      </ScreenFrame>
    </section>
  );
}
