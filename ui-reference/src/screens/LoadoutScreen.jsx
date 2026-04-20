import { useMemo, useState } from "react";
import ScreenFrame from "../components/ScreenFrame";
import { Panel, PanelHeading, StatusPill } from "../components/Panel";

const weaponStatOrder = ["damage", "control", "mobility", "handling"];

function SlotVisual({ imageClass }) {
  return (
    <div className="armory-slot-visual" aria-hidden="true">
      <div className={`campaign-item-silhouette ${imageClass}`.trim()} />
    </div>
  );
}

function WeaponPlaceholder({ weapon }) {
  return (
    <div className={`weapon-card-preview ${weapon?.imageVariantClass ?? ""}`.trim()} aria-hidden="true">
      <div className="weapon-card-preview-frame">
        <div className={`campaign-item-silhouette ${weapon?.imageClass ?? "weapon-rifle"}`.trim()} />
      </div>
    </div>
  );
}

function AttachmentPlaceholder({ attachment }) {
  return (
    <div className="armory-attachment-card">
      <div className="campaign-item-visual armory-attachment-visual" aria-hidden="true">
        <div className={`campaign-item-silhouette ${attachment?.imageClass ?? "attachment-optic"}`.trim()} />
      </div>
      <div className="armory-attachment-copy">
        <span>{attachment?.slot ?? "Attachment"}</span>
        <strong>{attachment?.title ?? "Placeholder Mod"}</strong>
        <em>{attachment?.tune ?? "Tuning Pending"}</em>
      </div>
    </div>
  );
}

function getAttachmentDelta(tune) {
  if (!tune) {
    return {};
  }

  const valueMatch = tune.match(/([+-]?\d+)/);
  const value = valueMatch ? Number(valueMatch[1]) : 0;
  const lowerTune = tune.toLowerCase();

  if (["range", "velocity", "bullet speed", "damage", "crit", "pen", "zoom"].some((term) => lowerTune.includes(term))) {
    return { damage: value };
  }

  if (["control", "kick", "recoil", "stability", "sway", "burst"].some((term) => lowerTune.includes(term))) {
    return { control: value };
  }

  if (["mobility", "strafe"].some((term) => lowerTune.includes(term))) {
    return { mobility: value };
  }

  if (["handling", "ads", "reload", "sprint", "target", "hipfire", "track", "clarity", "flex", "setup", "noise", "heat"].some((term) =>
    lowerTune.includes(term)
  )) {
    return { handling: value };
  }

  return {};
}

export default function LoadoutScreen({
  armoryWeapons,
  customLoadouts,
  equipmentOptions,
  fieldUpgradeOptions,
  perkCatalog,
  perkTypeTabs,
  topBar,
  weaponClassTabs
}) {
  const [activeLoadoutId, setActiveLoadoutId] = useState(customLoadouts[0]?.id ?? "");
  const [activeSlotId, setActiveSlotId] = useState(null);
  const [activeWeaponEditSlot, setActiveWeaponEditSlot] = useState("primary");
  const [activeAttachmentSlot, setActiveAttachmentSlot] = useState(armoryWeapons[0]?.attachments?.[0]?.slot ?? null);
  const [activeWeaponClass, setActiveWeaponClass] = useState(weaponClassTabs[0] ?? "");
  const [selectedWeaponId, setSelectedWeaponId] = useState(
    armoryWeapons.find((weapon) => weapon.className === weaponClassTabs[0])?.id ?? armoryWeapons[0]?.id ?? ""
  );
  const [loadoutWeaponSelections, setLoadoutWeaponSelections] = useState(() =>
    Object.fromEntries(
      customLoadouts.map((loadout, index) => [
        loadout.id,
        {
          primary: armoryWeapons.find((weapon) => weapon.title === loadout.primary)?.id ?? armoryWeapons[index % armoryWeapons.length]?.id ?? "",
          secondary:
            armoryWeapons.find((weapon) => weapon.title === loadout.secondary)?.id ??
            armoryWeapons[(index + 1) % armoryWeapons.length]?.id ??
            armoryWeapons[0]?.id ??
            ""
        }
      ])
    )
  );
  const [attachmentSelections, setAttachmentSelections] = useState(() =>
    Object.fromEntries(
      armoryWeapons.map((weapon) => [
        weapon.id,
        Object.fromEntries(weapon.attachments.map((attachment) => [attachment.slot, attachment.selected]))
      ])
    )
  );
  const [selectedEquipmentId, setSelectedEquipmentId] = useState(equipmentOptions[0]?.id ?? "");
  const [selectedFieldUpgradeId, setSelectedFieldUpgradeId] = useState(fieldUpgradeOptions[0]?.id ?? "");
  const [activePerkType, setActivePerkType] = useState(perkTypeTabs[0] ?? "All");
  const [selectedPerkId, setSelectedPerkId] = useState(perkCatalog[0]?.id ?? "");

  const activeLoadout = customLoadouts.find((loadout) => loadout.id === activeLoadoutId) ?? customLoadouts[0];
  const activeLoadoutWeapons = loadoutWeaponSelections[activeLoadout?.id] ?? {};
  const filteredWeapons = useMemo(
    () => armoryWeapons.filter((weapon) => weapon.className === activeWeaponClass),
    [activeWeaponClass, armoryWeapons]
  );
  const selectedWeapon =
    armoryWeapons.find((weapon) => weapon.id === selectedWeaponId) ?? filteredWeapons[0] ?? armoryWeapons[0];
  const equippedWeapon =
    armoryWeapons.find((weapon) => weapon.id === activeLoadoutWeapons[activeWeaponEditSlot]) ?? armoryWeapons[0];
  const selectedEquipment = equipmentOptions.find((item) => item.id === selectedEquipmentId) ?? equipmentOptions[0];
  const selectedFieldUpgrade =
    fieldUpgradeOptions.find((item) => item.id === selectedFieldUpgradeId) ?? fieldUpgradeOptions[0];
  const filteredPerks = useMemo(
    () => perkCatalog.filter((perk) => activePerkType === "All" || perk.type === activePerkType),
    [activePerkType, perkCatalog]
  );
  const selectedPerk = perkCatalog.find((perk) => perk.id === selectedPerkId) ?? filteredPerks[0] ?? perkCatalog[0];
  const equippedPerks = activeLoadout?.perks
    .map((perkName) => perkCatalog.find((perk) => perk.title === perkName))
    .filter(Boolean);
  const capacityUsed = equippedPerks.reduce((total, perk) => total + perk.cost, 0);

  const isWeaponSelection = activeSlotId === "primary" || activeSlotId === "secondary";
  const isEquipmentSelection = activeSlotId === "equipment";
  const isFieldUpgradeSelection = activeSlotId === "field-upgrade";
  const isPerkSelection = activeSlotId === "perks";

  const getEquippedAttachmentOption = (weapon, attachment) => {
    const selectedTitle = attachmentSelections[weapon.id]?.[attachment.slot] ?? attachment.selected;
    return attachment.options.find((option) => option.title === selectedTitle) ?? attachment.options[0];
  };

  const getWeaponStats = (weapon) => {
    if (!weapon) {
      return {};
    }

    return weapon.attachments.reduce(
      (stats, attachment) => {
        const delta = getAttachmentDelta(getEquippedAttachmentOption(weapon, attachment)?.tune);

        return {
          damage: Math.max(0, Math.min(100, stats.damage + (delta.damage ?? 0))),
          control: Math.max(0, Math.min(100, stats.control + (delta.control ?? 0))),
          mobility: Math.max(0, Math.min(100, stats.mobility + (delta.mobility ?? 0))),
          handling: Math.max(0, Math.min(100, stats.handling + (delta.handling ?? 0)))
        };
      },
      { ...weapon.attributes }
    );
  };

  const selectedWeaponStats = getWeaponStats(selectedWeapon);
  const equippedWeaponStats = getWeaponStats(equippedWeapon);
  const activeOverviewAttachment =
    equippedWeapon?.attachments.find((attachment) => attachment.slot === activeAttachmentSlot) ?? equippedWeapon?.attachments?.[0];

  const handleOpenWeaponSelection = (slotId) => {
    setActiveSlotId(slotId);
    setActiveWeaponEditSlot(slotId);
    const currentWeapon = armoryWeapons.find((weapon) => weapon.id === activeLoadoutWeapons[slotId]) ?? armoryWeapons[0];
    setSelectedWeaponId(currentWeapon?.id ?? armoryWeapons[0]?.id ?? "");
    setActiveWeaponClass(currentWeapon?.className ?? weaponClassTabs[0] ?? "");
  };

  return (
    <section className="game-screen is-active" id="loadout-screen">
      <ScreenFrame backdropClassName="backdrop-loadout" gridClassName="screen-grid-loadout-rework" topBar={topBar}>
        <div className="loadout-shell">
          <Panel className="loadout-presets-card">
            <PanelHeading kicker="Loadout List" title="Custom Built Loadouts" status={<StatusPill tone="cold">4 Saved</StatusPill>} />
            <div className="loadout-preset-list">
              {customLoadouts.map((loadout) => (
                <button
                  className={`loadout-preset-button ${loadout.id === activeLoadout.id ? "is-active" : ""}`.trim()}
                  type="button"
                  key={loadout.id}
                  onClick={() => {
                    setActiveLoadoutId(loadout.id);
                    setActiveSlotId(null);
                  }}
                >
                  <span>{loadout.role}</span>
                  <strong>{loadout.label}</strong>
                  <em>{loadout.primary}</em>
                </button>
              ))}
            </div>
          </Panel>

          {isWeaponSelection ? (
            <>
              <Panel className="armory-browser-card">
                <PanelHeading
                  kicker="Weapon Select"
                  title={activeSlotId === "primary" ? "Primary Weapon" : "Secondary Weapon"}
                  status={<StatusPill tone="warning">{activeWeaponClass}</StatusPill>}
                />

                <div className="armory-class-tabs" role="tablist" aria-label="Weapon classes">
                  {weaponClassTabs.map((weaponClass) => (
                    <button
                      className={`category-tab ${weaponClass === activeWeaponClass ? "is-active" : ""}`.trim()}
                      type="button"
                      key={weaponClass}
                      onClick={() => {
                        setActiveWeaponClass(weaponClass);
                        const firstWeapon = armoryWeapons.find((weapon) => weapon.className === weaponClass);
                        if (firstWeapon) {
                          setSelectedWeaponId(firstWeapon.id);
                        }
                      }}
                    >
                      {weaponClass}
                    </button>
                  ))}
                </div>

                <div className="armory-weapon-list">
                  {filteredWeapons.map((weapon) => (
                    <button
                      className={`armory-weapon-card ${weapon.id === selectedWeapon?.id ? "is-selected" : ""}`.trim()}
                      type="button"
                      key={weapon.id}
                      onClick={() => setSelectedWeaponId(weapon.id)}
                    >
                      <span className="item-class">{weapon.className.replace(" Rifles", "")}</span>
                      <strong>{weapon.title}</strong>
                      <WeaponPlaceholder weapon={weapon} />
                    </button>
                  ))}
                </div>
              </Panel>

              <Panel className="armory-detail-card armory-detail-card-compact">
                <PanelHeading kicker="Weapon Preview" title={selectedWeapon?.title ?? "Weapon"} status={<StatusPill tone="ready">Candidate</StatusPill>} />
                <SlotVisual imageClass={selectedWeapon?.imageClass ?? "weapon-rifle"} />
                <div className="weapon-attribute-grid">
                  {weaponStatOrder.map((label) => (
                    <div className="weapon-attribute-card" key={label}>
                      <span>{label}</span>
                      <strong>{selectedWeaponStats[label] ?? "--"}</strong>
                    </div>
                  ))}
                </div>
                <div className="armory-detail-copy">
                  <p>Confirm this weapon to equip it to the loadout. Attachment tuning happens in the main gunsmith view.</p>
                </div>
                <button
                  className="action-button action-button-primary"
                  type="button"
                  onClick={() => {
                    setLoadoutWeaponSelections((current) => ({
                      ...current,
                      [activeLoadout.id]: {
                        ...current[activeLoadout.id],
                        [activeSlotId]: selectedWeapon.id
                      }
                    }));
                    setActiveWeaponEditSlot(activeSlotId);
                    setActiveAttachmentSlot(selectedWeapon.attachments?.[0]?.slot ?? null);
                    setActiveSlotId(null);
                  }}
                >
                  Confirm Selection
                </button>
              </Panel>
            </>
          ) : isEquipmentSelection ? (
            <>
              <Panel className="armory-browser-card">
                <PanelHeading kicker="Equipment Select" title="Equipment" status={<StatusPill tone="warning">Tactical</StatusPill>} />
                <div className="armory-simple-list">
                  {equipmentOptions.map((item) => (
                    <button
                      className={`armory-weapon-card ${item.id === selectedEquipment?.id ? "is-selected" : ""}`.trim()}
                      type="button"
                      key={item.id}
                      onClick={() => setSelectedEquipmentId(item.id)}
                    >
                      <strong>{item.title}</strong>
                    </button>
                  ))}
                </div>
              </Panel>

              <Panel className="armory-detail-card">
                <PanelHeading kicker="Details" title={selectedEquipment?.title ?? "Equipment"} status={<StatusPill tone="ready">Selected</StatusPill>} />
                <SlotVisual imageClass={selectedEquipment?.imageClass ?? "gear-mine"} />
                <div className="armory-detail-copy">
                  <p>{selectedEquipment?.body}</p>
                </div>
                <button className="action-button action-button-primary" type="button" onClick={() => setActiveSlotId(null)}>
                  Confirm Selection
                </button>
              </Panel>
            </>
          ) : isFieldUpgradeSelection ? (
            <>
              <Panel className="armory-browser-card">
                <PanelHeading kicker="Field Upgrade" title="Select Upgrade" status={<StatusPill tone="warning">Support</StatusPill>} />
                <div className="armory-simple-list">
                  {fieldUpgradeOptions.map((item) => (
                    <button
                      className={`armory-weapon-card ${item.id === selectedFieldUpgrade?.id ? "is-selected" : ""}`.trim()}
                      type="button"
                      key={item.id}
                      onClick={() => setSelectedFieldUpgradeId(item.id)}
                    >
                      <strong>{item.title}</strong>
                    </button>
                  ))}
                </div>
              </Panel>

              <Panel className="armory-detail-card">
                <PanelHeading kicker="Details" title={selectedFieldUpgrade?.title ?? "Field Upgrade"} status={<StatusPill tone="ready">Selected</StatusPill>} />
                <SlotVisual imageClass={selectedFieldUpgrade?.imageClass ?? "gear-turret"} />
                <div className="armory-detail-copy">
                  <p>{selectedFieldUpgrade?.body}</p>
                </div>
                <button className="action-button action-button-primary" type="button" onClick={() => setActiveSlotId(null)}>
                  Confirm Selection
                </button>
              </Panel>
            </>
          ) : isPerkSelection ? (
            <>
              <Panel className="armory-browser-card">
                <PanelHeading kicker="Perk Select" title="Perk Selection" status={<StatusPill tone="warning">{activePerkType}</StatusPill>} />
                <div className="armory-class-tabs" role="tablist" aria-label="Perk types">
                  {perkTypeTabs.map((type) => (
                    <button
                      className={`category-tab ${type === activePerkType ? "is-active" : ""}`.trim()}
                      type="button"
                      key={type}
                      onClick={() => {
                        setActivePerkType(type);
                        const firstPerk = perkCatalog.find((perk) => type === "All" || perk.type === type);
                        if (firstPerk) {
                          setSelectedPerkId(firstPerk.id);
                        }
                      }}
                    >
                      {type}
                    </button>
                  ))}
                </div>

                <div className="armory-weapon-list perk-selection-list">
                  {filteredPerks.map((perk) => {
                    const isEquipped = activeLoadout?.perks.includes(perk.title);

                    return (
                      <button
                        className={`armory-weapon-card ${perk.id === selectedPerk?.id ? "is-selected" : ""}`.trim()}
                        type="button"
                        key={perk.id}
                        onClick={() => setSelectedPerkId(perk.id)}
                      >
                        <span className="item-class">{perk.type}</span>
                        <strong>{perk.title}</strong>
                        <span>
                          Lv {perk.level} · Cost {perk.cost}
                          {isEquipped ? " · Equipped" : ""}
                        </span>
                      </button>
                    );
                  })}
                </div>
              </Panel>

              <Panel className="armory-detail-card perk-detail-card">
                <PanelHeading
                  kicker={selectedPerk?.type ?? "Perk"}
                  title={selectedPerk?.title ?? "Perk"}
                  status={<StatusPill tone="ready">Cost {selectedPerk?.cost ?? "--"}</StatusPill>}
                />
                <div className="perk-detail-copy">
                  <p>{selectedPerk?.description}</p>
                  <div className="campaign-check-row">
                    <strong>Unlock</strong>
                    <span>Operator Rank {selectedPerk?.level ?? "--"}</span>
                  </div>
                  <div className="campaign-check-row">
                    <strong>Notes</strong>
                    <span>{selectedPerk?.notes ?? "Operator Rank"}</span>
                  </div>
                </div>

                <div className="perk-variant-list">
                  <strong>Variants</strong>
                  {selectedPerk?.variants?.length ? (
                    selectedPerk.variants.map((variant) => (
                      <div className="armory-attachment-slot" key={variant.title}>
                        <span>{variant.title}</span>
                        <strong>Cost {variant.cost}</strong>
                      </div>
                    ))
                  ) : (
                    <div className="armory-detail-copy">
                      <p>No reputation variants on this perk.</p>
                    </div>
                  )}
                </div>

                <div className="perk-equipped-summary">
                  <div className="perk-capacity-row">
                    <strong>Capacity</strong>
                    <span>{capacityUsed}/6</span>
                  </div>
                  <div className="bar">
                    <i style={{ width: `${Math.min((capacityUsed / 6) * 100, 100)}%` }}></i>
                  </div>
                  <div className="equipped-perk-chip-list">
                    {equippedPerks.map((perk) => (
                      <div className="campaign-perk-chip" key={perk.id}>
                        {perk.title}
                      </div>
                    ))}
                  </div>
                </div>

                <button className="action-button action-button-primary" type="button" onClick={() => setActiveSlotId(null)}>
                  Confirm Selection
                </button>
              </Panel>
            </>
          ) : (
            <>
              <Panel className="loadout-overview-card gunsmith-overview-card">
                <PanelHeading kicker="Gunsmith" title={activeLoadout?.label ?? "Custom Loadout"} status={<StatusPill tone="ready">Attachment Edit</StatusPill>} />
                <div className="armory-class-tabs" role="tablist" aria-label="Weapon slots">
                  {["primary", "secondary"].map((slotId) => (
                    <button
                      className={`category-tab ${slotId === activeWeaponEditSlot ? "is-active" : ""}`.trim()}
                      type="button"
                      key={slotId}
                      onClick={() => {
                        setActiveWeaponEditSlot(slotId);
                        const slotWeapon = armoryWeapons.find((weapon) => weapon.id === activeLoadoutWeapons[slotId]) ?? armoryWeapons[0];
                        setActiveAttachmentSlot(slotWeapon?.attachments?.[0]?.slot ?? null);
                      }}
                    >
                      {slotId === "primary" ? "Primary Weapon" : "Secondary Weapon"}
                    </button>
                  ))}
                </div>
                <div className="loadout-overview-actions">
                  <button className="action-button" type="button" onClick={() => handleOpenWeaponSelection(activeWeaponEditSlot)}>
                    Change Weapon
                  </button>
                </div>
                <div className="armory-gunsmith-layout">
                  <div className="armory-attachment-stack">
                    {equippedWeapon?.attachments.map((attachment) => (
                      <button
                        className={`armory-attachment-slot-button ${attachment.slot === activeOverviewAttachment?.slot ? "is-selected" : ""}`.trim()}
                        type="button"
                        key={attachment.slot}
                        onClick={() => setActiveAttachmentSlot(attachment.slot)}
                      >
                        <AttachmentPlaceholder
                          attachment={{
                            slot: attachment.slot,
                            ...getEquippedAttachmentOption(equippedWeapon, attachment)
                          }}
                        />
                      </button>
                    )) ?? null}
                  </div>
                  <div className="armory-attachment-option-panel">
                    <div className="panel-heading">
                      <div>
                        <p className="panel-kicker">Attachment Options</p>
                        <h3>{activeOverviewAttachment?.slot ?? "Select Slot"}</h3>
                      </div>
                      <span className="slot-meta">{activeOverviewAttachment?.options?.length ?? 0} Options</span>
                    </div>
                    <div className="armory-attachment-option-list">
                      {activeOverviewAttachment?.options?.map((option) => (
                        <button
                          className={`armory-attachment-option ${
                            option.title ===
                            (attachmentSelections[equippedWeapon.id]?.[activeOverviewAttachment.slot] ?? activeOverviewAttachment.selected)
                              ? "is-selected"
                              : ""
                          }`.trim()}
                          type="button"
                          key={option.title}
                          onClick={() =>
                            setAttachmentSelections((current) => ({
                              ...current,
                              [equippedWeapon.id]: {
                                ...current[equippedWeapon.id],
                                [activeOverviewAttachment.slot]: option.title
                              }
                            }))
                          }
                        >
                          <AttachmentPlaceholder attachment={{ slot: activeOverviewAttachment.slot, ...option }} />
                        </button>
                      )) ?? null}
                    </div>
                  </div>
                </div>
              </Panel>

              <Panel className="loadout-stats-card weapon-summary-card">
                <PanelHeading
                  kicker={activeWeaponEditSlot === "primary" ? "Primary Weapon" : "Secondary Weapon"}
                  title={equippedWeapon?.title ?? "Weapon"}
                  status={<StatusPill tone="cold">Live Tuning</StatusPill>}
                />
                <WeaponPlaceholder weapon={equippedWeapon} />
                <div className="summary-bars weapon-summary-bars">
                  {weaponStatOrder.map((label) => (
                    <div className="summary-row" key={label}>
                      <span>{label}</span>
                      <div className="bar">
                        <i style={{ width: `${equippedWeaponStats[label] ?? 0}%` }}></i>
                      </div>
                      <strong>{equippedWeaponStats[label] ?? "--"}</strong>
                    </div>
                  ))}
                </div>
                <div className="weapon-summary-note">
                  <strong>Selected Attachment</strong>
                  <p>
                    {activeOverviewAttachment
                      ? `${getEquippedAttachmentOption(equippedWeapon, activeOverviewAttachment).title} · ${
                          getEquippedAttachmentOption(equippedWeapon, activeOverviewAttachment).tune
                        }`
                      : "Select a slot to tune this platform."}
                  </p>
                </div>
              </Panel>
            </>
          )}
        </div>
      </ScreenFrame>
    </section>
  );
}
