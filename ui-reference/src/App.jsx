import { useEffect, useRef, useState } from "react";
import UtilityNav from "./components/UtilityNav";
import BarracksScreen from "./screens/BarracksScreen";
import CampaignScreen from "./screens/CampaignScreen";
import MainMenuScreen from "./screens/MainMenuScreen";
import NightmareModeScreen from "./screens/NightmareModeScreen";
import OnlineLobbyScreen from "./screens/OnlineLobbyScreen";
import LoadoutScreen from "./screens/LoadoutScreen";
import SettingsScreen from "./screens/SettingsScreen";
import SocialScreen from "./screens/SocialScreen";
import {
  armoryWeapons,
  customLoadouts,
  equipmentOptions,
  fieldUpgradeOptions,
  itemCards,
  loadoutCategories,
  loadoutOptionCards,
  loadoutSlots,
  missionDifficulties,
  newsItems,
  perkCatalog,
  perkTypeTabs,
  playerSlots,
  profileStats,
  summaryStats,
  weaponClassTabs
} from "./data";

const createUiClickPlayer = () => {
  let audioContext = null;

  return () => {
    const AudioContextClass = window.AudioContext || window.webkitAudioContext;
    if (!AudioContextClass) {
      return;
    }

    if (!audioContext) {
      audioContext = new AudioContextClass();
    }

    if (audioContext.state === "suspended") {
      audioContext.resume().catch(() => {});
    }

    const now = audioContext.currentTime;
    const oscillator = audioContext.createOscillator();
    const gainNode = audioContext.createGain();
    const filter = audioContext.createBiquadFilter();

    oscillator.type = "triangle";
    oscillator.frequency.setValueAtTime(420, now);
    oscillator.frequency.exponentialRampToValueAtTime(280, now + 0.04);

    filter.type = "lowpass";
    filter.frequency.setValueAtTime(1200, now);
    filter.Q.setValueAtTime(0.8, now);

    gainNode.gain.setValueAtTime(0.0001, now);
    gainNode.gain.exponentialRampToValueAtTime(0.035, now + 0.008);
    gainNode.gain.exponentialRampToValueAtTime(0.0001, now + 0.06);

    oscillator.connect(filter);
    filter.connect(gainNode);
    gainNode.connect(audioContext.destination);

    oscillator.start(now);
    oscillator.stop(now + 0.065);
  };
};

const screenRegistry = {
  "barracks-screen": BarracksScreen,
  "campaign-screen": CampaignScreen,
  "main-menu": MainMenuScreen,
  "nightmare-mode": NightmareModeScreen,
  "online-lobby": OnlineLobbyScreen,
  "loadout-screen": LoadoutScreen,
  "settings-screen": SettingsScreen,
  "social-screen": SocialScreen
};

const utilityNavItems = [
  { id: "main-menu", label: "Home" },
  { id: "campaign-screen", label: "Campaign" },
  { id: "nightmare-mode", label: "Nightmare Mode" },
  { id: "online-lobby", label: "Lobby" },
  { id: "loadout-screen", label: "Loadout" },
  { id: "barracks-screen", label: "Barracks" },
  { id: "social-screen", label: "Social" },
  { id: "settings-screen", label: "Settings" }
];

export default function App() {
  const [activeScreen, setActiveScreen] = useState("main-menu");
  const [isReady, setIsReady] = useState(false);
  const [isStageFullscreen, setIsStageFullscreen] = useState(false);
  const stageRef = useRef(null);
  const uiClickPlayerRef = useRef(null);

  if (!uiClickPlayerRef.current && typeof window !== "undefined") {
    uiClickPlayerRef.current = createUiClickPlayer();
  }

  useEffect(() => {
    const handleFullscreenChange = () => {
      setIsStageFullscreen(document.fullscreenElement === stageRef.current);
    };

    document.addEventListener("fullscreenchange", handleFullscreenChange);
    return () => document.removeEventListener("fullscreenchange", handleFullscreenChange);
  }, []);

  useEffect(() => {
    const handlePointerDown = (event) => {
      const target = event.target;
      if (!(target instanceof Element)) {
        return;
      }

      const interactiveTarget = target.closest("button, [role='button']");
      if (!interactiveTarget || interactiveTarget.hasAttribute("disabled")) {
        return;
      }

      uiClickPlayerRef.current?.();
    };

    document.addEventListener("pointerdown", handlePointerDown, true);
    return () => document.removeEventListener("pointerdown", handlePointerDown, true);
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
              <h1 id="ui-reference-title">Project: Blackwell</h1>
            
            </div>

            <div className="reference-actions">
              <button className="stage-toggle-button" type="button" onClick={toggleStageFullscreen}>
                {isStageFullscreen ? "Exit Fullscreen" : "Fullscreen Mock"}
              </button>
            </div>
          </div>

          <div className="screen-stage" ref={stageRef}>
            {Object.entries(screenRegistry).map(([screenId, ScreenComponent]) => {
              const utilityNav =
                screenId === "main-menu" ? null : (
                  <UtilityNav activeScreen={activeScreen} items={utilityNavItems} onSelect={setActiveScreen} />
                );

              return (
                <div
                  className={`screen-stage-layer ${activeScreen === screenId ? "is-active" : ""}`.trim()}
                  key={screenId}
                  aria-hidden={activeScreen === screenId ? undefined : true}
                >
                  <ScreenComponent
                    onBack={() => setActiveScreen("main-menu")}
                    onNavigate={setActiveScreen}
                    onOpenCampaign={() => setActiveScreen("campaign-screen")}
                    onOpenNightmare={() => setActiveScreen("nightmare-mode")}
                    profileStats={profileStats}
                    newsItems={newsItems}
                    playerSlots={playerSlots}
                    missionDifficulties={missionDifficulties}
                    isReady={isReady}
                    onToggleReady={() => setIsReady((current) => !current)}
                    armoryWeapons={armoryWeapons}
                    customLoadouts={customLoadouts}
                    equipmentOptions={equipmentOptions}
                    fieldUpgradeOptions={fieldUpgradeOptions}
                    loadoutCategories={loadoutCategories}
                    loadoutOptionCards={loadoutOptionCards}
                    loadoutSlots={loadoutSlots}
                    itemCards={itemCards}
                    perkCatalog={perkCatalog}
                    perkTypeTabs={perkTypeTabs}
                    summaryStats={summaryStats}
                    topBar={utilityNav}
                    weaponClassTabs={weaponClassTabs}
                  />
                </div>
              );
            })}
          </div>
        </section>
      </main>
    </div>
  );
}
