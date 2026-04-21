export default function UtilityNav({ activeScreen, items, onSelect }) {
  return (
    <nav className="utility-nav" aria-label="In-game navigation">
      {items.map((item) => {
        const targetScreen = item.target ?? item.id;
        const isActive = item.id === activeScreen || targetScreen === activeScreen;

        return (
          <button
            key={item.id}
            className={`utility-nav-button ${isActive ? "is-active" : ""}`.trim()}
            type="button"
            aria-current={isActive ? "page" : undefined}
            onClick={() => onSelect(targetScreen)}
          >
            {item.label}
          </button>
        );
      })}
    </nav>
  );
}
