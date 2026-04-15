import { useEffect, useRef, useState } from "react";
import ScreenTabs from "./components/ScreenTabs";
import CampaignScreen from "./screens/CampaignScreen";
import MainMenuScreen from "./screens/MainMenuScreen";
import OnlineLobbyScreen from "./screens/OnlineLobbyScreen";
import LoadoutScreen from "./screens/LoadoutScreen";
import {
  itemCards,
  loadoutCategories,
  loadoutSlots,
  missionDifficulties,
  newsItems,
  playerSlots,
  profileStats,
  screenTabs,
  summaryStats
} from "./data";

const screenRegistry = {
  "campaign-screen": CampaignScreen,
  "main-menu": MainMenuScreen,
  "online-lobby": OnlineLobbyScreen,
  "loadout-screen": LoadoutScreen
};

export default function App() {
  const [activeScreen, setActiveScreen] = useState("main-menu");
  const [isReady, setIsReady] = useState(false);
  const [isStageFullscreen, setIsStageFullscreen] = useState(false);
  const stageRef = useRef(null);
  const ActiveScreen = screenRegistry[activeScreen];

  useEffect(() => {
    const handleFullscreenChange = () => {
      setIsStageFullscreen(document.fullscreenElement === stageRef.current);
    };

    document.addEventListener("fullscreenchange", handleFullscreenChange);
    return () => document.removeEventListener("fullscreenchange", handleFullscreenChange);
  }, []);

  const toggleStageFullscreen = async () => {
    if (!stageRef.current) {
      return;
    }

    if (document.fullscreenElement === stageRef.current) {
      await document.exitFullscreen();
      return;
    }

    await stageRef.current.requestFullscreen();
  };

  return (
    <div className="page-shell">
      <main>
        <section className="ui-reference standalone" id="ui-reference" aria-labelledby="ui-reference-title">
          <div className="reference-intro panel">
            <div>
              <p className="eyebrow">Unity Prefab Reference</p>
              <h1 id="ui-reference-title">Project Z UI System</h1>
              <p className="lede">
                Desktop-first menu mockups for a co-op zombie shooter, organized into reusable React components that can
                later map cleanly to Unity panels, cards, tabs, and prefab variants.
              </p>
            </div>

            <div className="reference-actions">
              <ScreenTabs tabs={screenTabs} activeScreen={activeScreen} onSelect={setActiveScreen} />
              <button className="stage-toggle-button" type="button" onClick={toggleStageFullscreen}>
                {isStageFullscreen ? "Exit Fullscreen" : "Fullscreen Mock"}
              </button>
            </div>
          </div>

          <div className="reference-meta">
            <div className="meta-card">
              <span>Focus</span>
              <strong>16:9 Desktop Menus</strong>
            </div>
            <div className="meta-card">
              <span>Tone</span>
              <strong>Gritty - Military - Cinematic</strong>
            </div>
            <div className="meta-card">
              <span>Build Goal</span>
              <strong>Reusable Unity-Friendly Panels</strong>
            </div>
          </div>

          <div className="screen-stage" ref={stageRef}>
            <ActiveScreen
              onBack={() => setActiveScreen("main-menu")}
              onOpenCampaign={() => setActiveScreen("campaign-screen")}
              profileStats={profileStats}
              newsItems={newsItems}
              playerSlots={playerSlots}
              missionDifficulties={missionDifficulties}
              isReady={isReady}
              onToggleReady={() => setIsReady((current) => !current)}
              loadoutCategories={loadoutCategories}
              loadoutSlots={loadoutSlots}
              itemCards={itemCards}
              summaryStats={summaryStats}
            />
          </div>
        </section>
      </main>
    </div>
  );
}
