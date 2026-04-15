import ScreenFrame from "../components/ScreenFrame";
import { Panel, PanelHeading, StatusPill } from "../components/Panel";

function SummaryRow({ label, value }) {
  return (
    <div className="summary-row">
      <span>{label}</span>
      <div className="bar">
        <i style={{ width: `${value}%` }}></i>
      </div>
    </div>
  );
}

export default function LoadoutScreen({ loadoutCategories, loadoutSlots, itemCards, summaryStats }) {
  return (
    <section className="game-screen is-active" id="loadout-screen">
      <ScreenFrame backdropClassName="backdrop-loadout" gridClassName="screen-grid-loadout">
        <Panel className="category-card">
          <p className="panel-kicker">Inventory Index</p>
          <div className="tab-stack" role="tablist" aria-label="Loadout categories">
            {loadoutCategories.map((category, index) => (
              <button
                className={`category-tab ${index === 0 ? "is-active" : ""}`.trim()}
                type="button"
                key={category}
              >
                {category}
              </button>
            ))}
          </div>
        </Panel>

        <div className="loadout-column">
          <div className="slot-grid">
            {loadoutSlots.map((slot) => (
              <Panel className={`slot-card ${slot.selected ? "selected" : ""}`.trim()} key={slot.title}>
                <p className="panel-kicker">{slot.kicker}</p>
                <h3>{slot.title}</h3>
                <p>{slot.body}</p>
                <span className="slot-meta">{slot.meta}</span>
              </Panel>
            ))}
          </div>

          <Panel className="item-browser">
            <PanelHeading
              kicker="Available Gear"
              title="Item Cards"
              status={<StatusPill tone="cold">12 Items</StatusPill>}
            />
            <div className="item-card-grid">
              {itemCards.map((item) => (
                <button className={`item-card ${item.selected ? "is-selected" : ""}`.trim()} type="button" key={item.title}>
                  <span className="item-class">{item.itemClass}</span>
                  <strong>{item.title}</strong>
                  <span>{item.body}</span>
                </button>
              ))}
            </div>
          </Panel>
        </div>

        <Panel className="summary-card">
          <PanelHeading
            kicker="Operator Summary"
            title="Character Stats"
            status={<StatusPill tone="ready">Combat Ready</StatusPill>}
          />
          <div className="summary-bars">
            {summaryStats.map((stat) => (
              <SummaryRow key={stat.label} label={stat.label} value={stat.value} />
            ))}
          </div>
          <div className="summary-note">
            <strong>Build Notes</strong>
            <p>Balanced assault setup with revive utility and trap coverage for tighter co-op lanes.</p>
          </div>
        </Panel>
      </ScreenFrame>
    </section>
  );
}
