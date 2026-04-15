export default function ScreenTabs({ tabs, activeScreen, onSelect }) {
  return (
    <div className="screen-tabs" role="tablist" aria-label="Menu screens">
      {tabs.map((tab) => {
        const isActive = tab.id === activeScreen;

        return (
          <button
            key={tab.id}
            className={`screen-tab ${isActive ? "is-active" : ""}`.trim()}
            type="button"
            role="tab"
            aria-selected={isActive}
            onClick={() => onSelect(tab.id)}
          >
            {tab.label}
          </button>
        );
      })}
    </div>
  );
}
