export default function UtilityNav({ activeScreen, items, onSelect }) {
  return (
    <nav className="utility-nav" aria-label="In-game navigation">
      {items.map((item) => {
        const isActive = item.id === activeScreen;

        return (
          <button
            key={item.id}
            className={`utility-nav-button ${isActive ? "is-active" : ""}`.trim()}
            type="button"
            aria-current={isActive ? "page" : undefined}
            onClick={() => onSelect(item.id)}
          >
            {item.label}
          </button>
        );
      })}
    </nav>
  );
}
