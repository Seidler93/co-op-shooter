import { useMemo, useState } from "react";
import ScreenFrame from "../components/ScreenFrame";
import { Panel, PanelHeading, StatusPill } from "../components/Panel";

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

export default function LoadoutScreen({
  armoryWeapons,
  customLoadouts,
  equipmentOptions,
  fieldUpgradeOptions,
  loadoutOptionCards,
  perkCatalog,
  perkTypeTabs,
  summaryStats,
  topBar,
  weaponClassTabs
}) {
  const [activeLoadoutId, setActiveLoadoutId] = useState(customLoadouts[0]?.id ?? "");
  const [activeSlotId, setActiveSlotId] = useState(null);
  const [activeAttachmentSlot, setActiveAttachmentSlot] = useState(armoryWeapons[0]?.attachments?.[0]?.slot ?? null);
  const [activeWeaponClass, setActiveWeaponClass] = useState(weaponClassTabs[0] ?? "");
  const [selectedWeaponId, setSelectedWeaponId] = useState(
    armoryWeapons.find((weapon) => weapon.className === weaponClassTabs[0])?.id ?? armoryWeapons[0]?.id ?? ""
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
  const filteredWeapons = useMemo(
    () => armoryWeapons.filter((weapon) => weapon.className === activeWeaponClass),
    [activeWeaponClass, armoryWeapons]
  );
  const selectedWeapon =
    armoryWeapons.find((weapon) => weapon.id === selectedWeaponId) ?? filteredWeapons[0] ?? armoryWeapons[0];
  const selectedAttachment =
    selectedWeapon?.attachments.find((attachment) => attachment.slot === activeAttachmentSlot) ?? selectedWeapon?.attachments?.[0];
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

  return (
    <section className="game-screen is-active" id="loadout-screen">
      <ScreenFrame backdropClassName="backdrop-loadout" gridClassName="screen-grid-loadout-rework" topBar={topBar}>
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
                        setActiveAttachmentSlot(firstWeapon.attachments?.[0]?.slot ?? null);
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
                    onClick={() => {
                      setSelectedWeaponId(weapon.id);
                      setActiveAttachmentSlot(weapon.attachments?.[0]?.slot ?? null);
                    }}
                  >
                    <span className="item-class">{weapon.className.replace(" Rifles", "")}</span>
                    <strong>{weapon.title}</strong>
                    <WeaponPlaceholder weapon={weapon} />
                  </button>
                ))}
              </div>
            </Panel>

            <Panel className="armory-detail-card">
              <PanelHeading kicker="Gunsmith" title={selectedWeapon?.title ?? "Weapon"} status={<StatusPill tone="ready">Selected</StatusPill>} />
              <SlotVisual imageClass={selectedWeapon?.imageClass ?? "weapon-rifle"} />
              <div className="weapon-attribute-grid">
                {Object.entries(selectedWeapon?.attributes ?? {}).map(([label, value]) => (
                  <div className="weapon-attribute-card" key={label}>
                    <span>{label}</span>
                    <strong>{value}</strong>
                  </div>
                ))}
              </div>
              <div className="armory-gunsmith-layout">
                <div className="armory-attachment-stack">
                  {selectedWeapon?.attachments.map((attachment) => (
                    <button
                      className={`armory-attachment-slot-button ${attachment.slot === selectedAttachment?.slot ? "is-selected" : ""}`.trim()}
                      type="button"
                      key={attachment.slot}
                      onClick={() => setActiveAttachmentSlot(attachment.slot)}
                    >
                      <AttachmentPlaceholder
                        attachment={{
                          slot: attachment.slot,
                          ...getEquippedAttachmentOption(selectedWeapon, attachment)
                        }}
                      />
                    </button>
                  )) ?? null}
                </div>
                <div className="armory-attachment-option-panel">
                  <div className="panel-heading">
                    <div>
                      <p className="panel-kicker">Attachment Options</p>
                      <h3>{selectedAttachment?.slot ?? "Select Slot"}</h3>
                    </div>
                    <span className="slot-meta">{selectedAttachment?.options?.length ?? 0} Options</span>
                  </div>
                  <div className="armory-attachment-option-list">
                    {selectedAttachment?.options?.map((option) => (
                      <button
                        className={`armory-attachment-option ${
                          option.title === (attachmentSelections[selectedWeapon.id]?.[selectedAttachment.slot] ?? selectedAttachment.selected)
                            ? "is-selected"
                            : ""
                        }`.trim()}
                        type="button"
                        key={option.title}
                        onClick={() =>
                          setAttachmentSelections((current) => ({
                            ...current,
                            [selectedWeapon.id]: {
                              ...current[selectedWeapon.id],
                              [selectedAttachment.slot]: option.title
                            }
                          }))
                        }
                      >
                        <AttachmentPlaceholder attachment={{ slot: selectedAttachment.slot, ...option }} />
                      </button>
                    )) ?? null}
                  </div>
                </div>
              </div>
              <button className="action-button action-button-primary" type="button" onClick={() => setActiveSlotId(null)}>
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
            <Panel className="loadout-overview-card">
              <PanelHeading kicker="Current Build" title={activeLoadout?.label ?? "Custom Loadout"} status={<StatusPill tone="ready">Editable</StatusPill>} />
              <div className="loadout-options-grid">
                {loadoutOptionCards.map((option) => (
                  <button
                    className={`loadout-option-card tone-${option.tone}`.trim()}
                    type="button"
                    key={option.id}
                    onClick={() => {
                      if (
                        option.id === "primary" ||
                        option.id === "secondary" ||
                        option.id === "equipment" ||
                        option.id === "field-upgrade" ||
                        option.id === "perks"
                      ) {
                        setActiveSlotId(option.id);
                      }
                    }}
                  >
                    <div>
                      <span className="panel-kicker">{option.title}</span>
                      <strong>{option.item}</strong>
                    </div>
                    {option.id === "perks" ? (
                      <div className="loadout-perk-icons" aria-hidden="true">
                        <div className="campaign-perk-icon perk-triage" />
                        <div className="campaign-perk-icon perk-ammo" />
                        <div className="campaign-perk-icon perk-cold" />
                      </div>
                    ) : (
                      <SlotVisual imageClass={option.imageClass} />
                    )}
                  </button>
                ))}
              </div>
            </Panel>

            <Panel className="loadout-stats-card">
              <PanelHeading kicker="Operator Profile" title="Player Stats" status={<StatusPill tone="cold">Live Build</StatusPill>} />
              <div className="summary-bars">
                {(activeLoadout?.stats ?? []).map((stat) => (
                  <div className="summary-row" key={stat.label}>
                    <span>{stat.label}</span>
                    <div className="bar">
                      <i style={{ width: `${stat.value}%` }}></i>
                    </div>
                  </div>
                ))}
              </div>
              <div className="summary-note">
                <strong>Role Focus</strong>
                <p>Balanced assault package tuned for mobility, revive utility, and steady pressure in co-op lanes.</p>
              </div>
            </Panel>
          </>
        )}
      </ScreenFrame>
    </section>
  );
}
