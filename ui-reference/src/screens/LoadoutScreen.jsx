import { useMemo, useState } from "react";
import ScreenFrame from "../components/ScreenFrame";
import { Panel, PanelHeading, StatusPill } from "../components/Panel";

const weaponStatOrder = ["damage", "control", "mobility", "handling"];

function SlotVisual({ imageClass, compact = false }) {
  return (
    <div className={`armory-slot-visual ${compact ? "is-compact" : ""}`.trim()} aria-hidden="true">
      <div className={`campaign-item-silhouette ${imageClass}`.trim()} />
    </div>
  );
}

function WeaponPlaceholder({ weapon, compact = false }) {
  return (
    <div className={`weapon-card-preview ${weapon?.imageVariantClass ?? ""} ${compact ? "is-compact" : ""}`.trim()} aria-hidden="true">
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

function formatBenefit(perk) {
  if (perk?.variants?.[0]?.effect) {
    return perk.variants[0].effect;
  }

  return perk?.description ?? "Passive benefit";
}

function getPerkIconClass(perk) {
  if (perk?.type === "Survival") {
    return "perk-triage";
  }

  if (perk?.title?.toLowerCase().includes("ammo")) {
    return "perk-ammo";
  }

  return "perk-cold";
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
  const [activeWeaponClass, setActiveWeaponClass] = useState(weaponClassTabs[0] ?? "");
  const [selectedWeaponId, setSelectedWeaponId] = useState(
    armoryWeapons.find((weapon) => weapon.className === weaponClassTabs[0])?.id ?? armoryWeapons[0]?.id ?? ""
  );
  const [selectedEquipmentId, setSelectedEquipmentId] = useState(equipmentOptions[0]?.id ?? "");
  const [selectedFieldUpgradeId, setSelectedFieldUpgradeId] = useState(fieldUpgradeOptions[0]?.id ?? "");
  const [activePerkType, setActivePerkType] = useState(perkTypeTabs[0] ?? "All");
  const [selectedPerkId, setSelectedPerkId] = useState(perkCatalog[0]?.id ?? "");
  const [activeAttachmentSlot, setActiveAttachmentSlot] = useState(armoryWeapons[0]?.attachments?.[0]?.slot ?? null);
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
  const [loadoutEquipmentSelections, setLoadoutEquipmentSelections] = useState(() =>
    Object.fromEntries(
      customLoadouts.map((loadout, index) => [
        loadout.id,
        equipmentOptions.find((item) => item.title === loadout.equipment)?.id ?? equipmentOptions[index % equipmentOptions.length]?.id ?? ""
      ])
    )
  );
  const [loadoutFieldUpgradeSelections, setLoadoutFieldUpgradeSelections] = useState(() =>
    Object.fromEntries(
      customLoadouts.map((loadout, index) => [
        loadout.id,
        fieldUpgradeOptions.find((item) => item.title === loadout.fieldUpgrade)?.id ??
          fieldUpgradeOptions[index % fieldUpgradeOptions.length]?.id ??
          ""
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
  const [loadoutPerkSelections, setLoadoutPerkSelections] = useState(() =>
    Object.fromEntries(
      customLoadouts.map((loadout) => [
        loadout.id,
        loadout.perks
          .map((perkName) => perkCatalog.find((perk) => perk.title === perkName)?.id)
          .filter(Boolean)
      ])
    )
  );
  const [swapModalPerkId, setSwapModalPerkId] = useState(null);
  const [swapModalPerkIds, setSwapModalPerkIds] = useState([]);

  const activeLoadout = customLoadouts.find((loadout) => loadout.id === activeLoadoutId) ?? customLoadouts[0];
  const activeLoadoutWeapons = loadoutWeaponSelections[activeLoadout?.id] ?? {};
  const primaryWeapon = armoryWeapons.find((weapon) => weapon.id === activeLoadoutWeapons.primary) ?? armoryWeapons[0];
  const secondaryWeapon = armoryWeapons.find((weapon) => weapon.id === activeLoadoutWeapons.secondary) ?? armoryWeapons[1] ?? armoryWeapons[0];
  const activeEquipment =
    equipmentOptions.find((item) => item.id === loadoutEquipmentSelections[activeLoadout?.id]) ?? equipmentOptions[0];
  const activeFieldUpgrade =
    fieldUpgradeOptions.find((item) => item.id === loadoutFieldUpgradeSelections[activeLoadout?.id]) ?? fieldUpgradeOptions[0];

  const filteredWeapons = useMemo(
    () => armoryWeapons.filter((weapon) => weapon.className === activeWeaponClass),
    [activeWeaponClass, armoryWeapons]
  );
  const selectedWeapon =
    armoryWeapons.find((weapon) => weapon.id === selectedWeaponId) ?? filteredWeapons[0] ?? armoryWeapons[0];
  const selectedEquipment = equipmentOptions.find((item) => item.id === selectedEquipmentId) ?? activeEquipment ?? equipmentOptions[0];
  const selectedFieldUpgrade =
    fieldUpgradeOptions.find((item) => item.id === selectedFieldUpgradeId) ?? activeFieldUpgrade ?? fieldUpgradeOptions[0];
  const filteredPerks = useMemo(
    () =>
      perkCatalog
        .filter((perk) => activePerkType === "All" || perk.type === activePerkType)
        .sort((left, right) => left.cost - right.cost || left.level - right.level || left.title.localeCompare(right.title)),
    [activePerkType, perkCatalog]
  );
  const selectedPerk = perkCatalog.find((perk) => perk.id === selectedPerkId) ?? filteredPerks[0] ?? perkCatalog[0];
  const equippedPerkIds = loadoutPerkSelections[activeLoadout?.id] ?? [];
  const equippedPerks = equippedPerkIds
    .map((perkId) => perkCatalog.find((perk) => perk.id === perkId))
    .filter(Boolean);
  const capacityUsed = equippedPerks.reduce((total, perk) => total + perk.cost, 0);
  const remainingCapacity = Math.max(0, 6 - capacityUsed);
  const activeOverviewAttachment =
    primaryWeapon?.attachments.find((attachment) => attachment.slot === activeAttachmentSlot) ?? primaryWeapon?.attachments?.[0];
  const swapModalPerk = perkCatalog.find((perk) => perk.id === swapModalPerkId) ?? null;
  const swapModalPerks = swapModalPerkIds
    .map((perkId) => perkCatalog.find((perk) => perk.id === perkId))
    .filter(Boolean);
  const swapModalCapacityUsed = swapModalPerks.reduce((total, perk) => total + perk.cost, 0);
  const swapModalCanConfirm = swapModalPerk ? swapModalCapacityUsed + swapModalPerk.cost <= 6 : false;

  const isWeaponSelection = activeSlotId === "primary" || activeSlotId === "secondary";
  const isEquipmentSelection = activeSlotId === "equipment";
  const isFieldUpgradeSelection = activeSlotId === "field-upgrade";
  const isAttachmentSelection = activeSlotId === "attachments";
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
  const primaryWeaponStats = getWeaponStats(primaryWeapon);

  const handleOpenWeaponSelection = (slotId) => {
    setActiveSlotId(slotId);
    const currentWeapon = armoryWeapons.find((weapon) => weapon.id === activeLoadoutWeapons[slotId]) ?? armoryWeapons[0];
    setSelectedWeaponId(currentWeapon?.id ?? armoryWeapons[0]?.id ?? "");
    setActiveWeaponClass(currentWeapon?.className ?? weaponClassTabs[0] ?? "");
  };

  const loadoutCards = [
    {
      id: "attachments",
      title: "Attachments",
      item: "",
      tone: "attachments",
      imageClass: primaryWeapon?.imageClass ?? "weapon-rifle",
      meta: ""
    },
    {
      id: "primary",
      title: "Primary",
      item: primaryWeapon?.title ?? activeLoadout?.primary ?? "Primary Weapon",
      tone: "large",
      imageClass: primaryWeapon?.imageClass ?? "weapon-rifle",
      meta: `${primaryWeapon?.className ?? "Weapon"} · ${primaryWeapon?.attachments?.length ?? 0} attachments`
    },
    {
      id: "secondary",
      title: "Secondary",
      item: secondaryWeapon?.title ?? activeLoadout?.secondary ?? "Secondary Weapon",
      tone: "large",
      imageClass: secondaryWeapon?.imageClass ?? "weapon-sidearm",
      meta: `${secondaryWeapon?.className ?? "Weapon"} · ${secondaryWeapon?.attachments?.length ?? 0} attachments`
    },
    {
      id: "equipment",
      title: "Equipment",
      item: activeEquipment?.title ?? activeLoadout?.equipment ?? "Equipment",
      tone: "medium",
      imageClass: activeEquipment?.imageClass ?? "gear-mine",
      meta: "Tactical slot"
    },
    {
      id: "field-upgrade",
      title: "Field Upgrade",
      item: activeFieldUpgrade?.title ?? activeLoadout?.fieldUpgrade ?? "Field Upgrade",
      tone: "medium",
      imageClass: activeFieldUpgrade?.imageClass ?? "gear-turret",
      meta: "Recharge ability"
    },
    {
      id: "perks",
      title: "Perks",
      item: `${equippedPerks.length} Equipped`,
      tone: "perks",
      imageClass: "perk-cold",
      meta: `${capacityUsed}/6 Capacity`
    }
  ];

  const addSelectedPerk = ({ closeOnSuccess = false } = {}) => {
    if (!selectedPerk) {
      return;
    }

    if (equippedPerkIds.includes(selectedPerk.id)) {
      if (closeOnSuccess) {
        setActiveSlotId(null);
      }
      return;
    }

    if (selectedPerk.cost <= remainingCapacity) {
      setLoadoutPerkSelections((current) => ({
        ...current,
        [activeLoadout.id]: [...(current[activeLoadout.id] ?? []), selectedPerk.id]
      }));
      if (closeOnSuccess) {
        setActiveSlotId(null);
      }
      return;
    }

    setSwapModalPerkId(selectedPerk.id);
    setSwapModalPerkIds(equippedPerkIds);
  };

  const removeEquippedPerk = (perkIdToRemove) => {
    setLoadoutPerkSelections((current) => ({
      ...current,
      [activeLoadout.id]: (current[activeLoadout.id] ?? []).filter((perkId) => perkId !== perkIdToRemove)
    }));
  };

  const toggleSwapModalPerkRemoval = (perkIdToToggle) => {
    setSwapModalPerkIds((current) => current.filter((perkId) => perkId !== perkIdToToggle));
  };

  const closeSwapModal = () => {
    setSwapModalPerkId(null);
    setSwapModalPerkIds([]);
  };

  const confirmSwapModalSelection = () => {
    if (!swapModalPerk || !swapModalCanConfirm) {
      closeSwapModal();
      return;
    }

    setLoadoutPerkSelections((current) => ({
      ...current,
      [activeLoadout.id]: [...swapModalPerkIds, swapModalPerk.id]
    }));
    closeSwapModal();
  };

  return (
    <section className="game-screen is-active" id="loadout-screen">
      <ScreenFrame backdropClassName="backdrop-loadout" gridClassName="screen-grid-loadout-rework" topBar={topBar}>
        <div className="loadout-shell">
          <Panel className="loadout-presets-card">
            <PanelHeading kicker="Loadout List" title="Custom Built Loadouts" />
            <div className="loadout-preset-list">
              {customLoadouts.map((loadout) => (
                <button
                  className={`loadout-preset-button ${loadout.id === activeLoadout.id ? "is-active" : ""}`.trim()}
                  type="button"
                  key={loadout.id}
                  onClick={() => {
                    setActiveLoadoutId(loadout.id);
                    setActiveSlotId(null);
                    setActiveAttachmentSlot(
                      (armoryWeapons.find((weapon) => weapon.id === loadoutWeaponSelections[loadout.id]?.primary) ?? armoryWeapons[0])?.attachments?.[0]?.slot ??
                        null
                    );
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
                      <WeaponPlaceholder weapon={weapon} compact />
                    </button>
                  ))}
                </div>
              </Panel>

              <Panel className="armory-detail-card armory-detail-card-compact">
                <PanelHeading kicker="Weapon Preview" title={selectedWeapon?.title ?? "Weapon"} status={<StatusPill tone="ready">Candidate</StatusPill>} />
                <SlotVisual imageClass={selectedWeapon?.imageClass ?? "weapon-rifle"} />
                <div className="summary-bars weapon-preview-bars">
                  {weaponStatOrder.map((label) => (
                    <div className="summary-row" key={label}>
                      <span>{label}</span>
                      <div className="bar">
                        <i style={{ width: `${selectedWeaponStats[label] ?? 0}%` }}></i>
                      </div>
                      <strong>{selectedWeaponStats[label] ?? "--"}</strong>
                    </div>
                  ))}
                </div>
                <div className="armory-detail-copy">
                  <p>Confirm this weapon to equip it to the selected loadout slot.</p>
                </div>
                <button
                  className="action-button action-button-primary action-button-confirm"
                  type="button"
                  onClick={() => {
                    setLoadoutWeaponSelections((current) => ({
                      ...current,
                      [activeLoadout.id]: {
                        ...current[activeLoadout.id],
                        [activeSlotId]: selectedWeapon.id
                      }
                    }));
                    if (activeSlotId === "primary") {
                      setActiveAttachmentSlot(selectedWeapon.attachments?.[0]?.slot ?? null);
                    }
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
                <button
                  className="action-button action-button-primary action-button-confirm"
                  type="button"
                  onClick={() => {
                    setLoadoutEquipmentSelections((current) => ({
                      ...current,
                      [activeLoadout.id]: selectedEquipment.id
                    }));
                    setActiveSlotId(null);
                  }}
                >
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
                <button
                  className="action-button action-button-primary action-button-confirm"
                  type="button"
                  onClick={() => {
                    setLoadoutFieldUpgradeSelections((current) => ({
                      ...current,
                      [activeLoadout.id]: selectedFieldUpgrade.id
                    }));
                    setActiveSlotId(null);
                  }}
                >
                  Confirm Selection
                </button>
              </Panel>
            </>
          ) : isAttachmentSelection ? (
            <>
              <Panel className="armory-browser-card">
                <PanelHeading kicker="Attachment Select" title={primaryWeapon?.title ?? "Primary Weapon"} status={<StatusPill tone="warning">Gunsmith</StatusPill>} />
                <div className="armory-detail-copy">
                  <p>Select a slot, then choose the attachment option you want on this build.</p>
                </div>
                <div className="attachment-select-layout">
                  <div className="armory-simple-list">
                    {primaryWeapon?.attachments.map((attachment) => (
                      <button
                        className={`armory-weapon-card ${attachment.slot === activeOverviewAttachment?.slot ? "is-selected" : ""}`.trim()}
                        type="button"
                        key={attachment.slot}
                        onClick={() => setActiveAttachmentSlot(attachment.slot)}
                      >
                        <span className="item-class">{attachment.slot}</span>
                        <strong>{getEquippedAttachmentOption(primaryWeapon, attachment).title}</strong>
                        <span>{getEquippedAttachmentOption(primaryWeapon, attachment).tune}</span>
                      </button>
                    ))}
                  </div>

                  <div className="armory-attachment-option-list armory-attachment-option-list-wide">
                    {activeOverviewAttachment?.options?.map((option) => (
                      <button
                        className={`armory-attachment-option ${
                          option.title ===
                          (attachmentSelections[primaryWeapon.id]?.[activeOverviewAttachment.slot] ?? activeOverviewAttachment.selected)
                            ? "is-selected"
                            : ""
                        }`.trim()}
                        type="button"
                        key={option.title}
                        onClick={() =>
                          setAttachmentSelections((current) => ({
                            ...current,
                            [primaryWeapon.id]: {
                              ...current[primaryWeapon.id],
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
              </Panel>

              <Panel className="armory-detail-card attachment-live-build-card">
                <PanelHeading kicker="Live Build" title={primaryWeapon?.title ?? "Weapon"} status={<StatusPill tone="ready">Updated Stats</StatusPill>} />
                <WeaponPlaceholder weapon={primaryWeapon} />
                <div className="summary-bars weapon-summary-bars">
                  {weaponStatOrder.map((label) => (
                    <div className="summary-row" key={label}>
                      <span>{label}</span>
                      <div className="bar">
                        <i style={{ width: `${getWeaponStats(primaryWeapon)[label] ?? 0}%` }}></i>
                      </div>
                      <strong>{getWeaponStats(primaryWeapon)[label] ?? "--"}</strong>
                    </div>
                  ))}
                </div>
                <div className="weapon-summary-note">
                  <strong>Selected Attachment</strong>
                  <p>
                    {activeOverviewAttachment
                      ? `${getEquippedAttachmentOption(primaryWeapon, activeOverviewAttachment).title} - ${
                          getEquippedAttachmentOption(primaryWeapon, activeOverviewAttachment).tune
                        }`
                      : "Select a slot to tune this platform."}
                  </p>
                </div>
                <div className="selected-perk-list attachment-change-list">
                  {primaryWeapon?.attachments.map((attachment) => (
                    <div className="selected-perk-row" key={attachment.slot}>
                      <div className={`campaign-item-silhouette ${getEquippedAttachmentOption(primaryWeapon, attachment).imageClass}`.trim()}></div>
                      <div>
                        <strong>{attachment.slot}</strong>
                        <span>{getEquippedAttachmentOption(primaryWeapon, attachment).title}</span>
                      </div>
                      <em>{getEquippedAttachmentOption(primaryWeapon, attachment).tune}</em>
                    </div>
                  ))}
                </div>
                <button className="action-button action-button-primary action-button-confirm" type="button" onClick={() => setActiveSlotId(null)}>
                  Confirm Attachments
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
                    const isEquipped = equippedPerkIds.includes(perk.id);
                    const canAfford = isEquipped || perk.cost <= remainingCapacity;

                    return (
                      <button
                        className={`armory-weapon-card perk-select-card ${perk.id === selectedPerk?.id ? "is-selected" : ""} ${
                          canAfford ? "" : "is-dimmed"
                        }`.trim()}
                        type="button"
                        key={perk.id}
                        onClick={() => setSelectedPerkId(perk.id)}
                      >
                        <div className={`campaign-perk-icon ${getPerkIconClass(perk)}`.trim()}></div>
                        <div className="perk-select-copy">
                          <strong>{perk.title}</strong>
                          <span>{formatBenefit(perk)}</span>
                        </div>
                        <em className="perk-select-cost">Cost {perk.cost}</em>
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

                <button
                  className="action-button action-button-primary action-button-confirm"
                  type="button"
                  onClick={() => addSelectedPerk({ closeOnSuccess: false })}
                >
                  Add Perk
                </button>

                <div className="perk-equipped-summary">
                  <div className="perk-capacity-row">
                    <strong>Currently Selected</strong>
                    <span>{remainingCapacity} Points Left</span>
                  </div>
                  <div className="selected-perk-list">
                    {equippedPerks.map((perk) => (
                      <div className="selected-perk-row" key={perk.id}>
                        <div className={`campaign-perk-icon selected-perk-icon ${getPerkIconClass(perk)}`.trim()}></div>
                        <div>
                          <strong>{perk.title}</strong>
                          <span>{formatBenefit(perk)}</span>
                        </div>
                        <button className="selected-perk-remove" type="button" aria-label={`Remove ${perk.title}`} onClick={() => removeEquippedPerk(perk.id)}>
                          ×
                        </button>
                        <em>Cost {perk.cost}</em>
                      </div>
                    ))}
                  </div>
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

                <div className="perk-action-row">
                  <button className="action-button action-button-primary action-button-confirm" type="button" onClick={() => setActiveSlotId(null)}>
                    Confirm Perks
                  </button>
                </div>
              </Panel>
            </>
          ) : (
            <>
              <Panel className="loadout-overview-card cod-loadout-overview-card">
                <PanelHeading kicker="Loadout View" title={activeLoadout?.label ?? "Custom Loadout"} />

                <div className="loadout-showcase">
                  <div className="loadout-hero-stage">
                    <div className="loadout-hero-surface" aria-hidden="true">
                      <div className="loadout-hero-grid"></div>
                    </div>

                    <div className="loadout-weapon-hero">
                      <WeaponPlaceholder weapon={primaryWeapon} />
                      <div className="loadout-support-overlay">
                        <div className="loadout-support-card">
                          <WeaponPlaceholder weapon={secondaryWeapon} compact />
                          <strong>{secondaryWeapon?.title}</strong>
                        </div>
                        <div className="loadout-support-card">
                          <SlotVisual imageClass={activeEquipment?.imageClass ?? "gear-mine"} compact />
                          <strong>{activeEquipment?.title}</strong>
                        </div>
                        <div className="loadout-support-card">
                          <SlotVisual imageClass={activeFieldUpgrade?.imageClass ?? "gear-turret"} compact />
                          <strong>{activeFieldUpgrade?.title}</strong>
                        </div>
                      </div>
                    </div>
                  </div>

                  <div className="loadout-options-grid cod-loadout-cards">
                    {loadoutCards.map((card) => (
                      <button
                        className={`loadout-option-card tone-${card.tone}`.trim()}
                        type="button"
                        key={card.id}
                        onClick={() => {
                          if (card.id === "primary" || card.id === "secondary") {
                            handleOpenWeaponSelection(card.id);
                            return;
                          }

                          setActiveSlotId(card.id);
                        }}
                      >
                        <div>
                          <p className="panel-kicker">{card.title}</p>
                          <strong>{card.item}</strong>
                          <span className="loadout-option-meta">{card.meta}</span>
                        </div>

                        {card.id === "perks" ? (
                          <div className="loadout-perk-icons">
                            {equippedPerks.slice(0, 6).map((perk) => (
                              <div className={`campaign-perk-icon ${perk.type === "Survival" ? "perk-triage" : "perk-cold"}`.trim()} key={perk.id}></div>
                            ))}
                          </div>
                        ) : card.id === "attachments" ? (
                          <div className="loadout-attachment-summary">
                            {primaryWeapon?.attachments.map((attachment) => (
                              <div className="loadout-attachment-summary-row" key={attachment.slot}>
                                <span>{attachment.slot}</span>
                                <strong>{getEquippedAttachmentOption(primaryWeapon, attachment).title}</strong>
                              </div>
                            ))}
                          </div>
                        ) : (
                          <div className="loadout-option-visual">
                            <div className={`campaign-item-silhouette ${card.imageClass}`.trim()} />
                          </div>
                        )}
                      </button>
                    ))}
                  </div>
                </div>
              </Panel>

              <Panel className="loadout-stats-card cod-loadout-stats-card">
                <PanelHeading kicker="Loadout Dossier" title={primaryWeapon?.title ?? "Weapon"} />

                <div className="summary-bars weapon-summary-bars">
                  {weaponStatOrder.map((label) => (
                    <div className="summary-row" key={label}>
                      <span>{label}</span>
                      <div className="bar">
                        <i style={{ width: `${primaryWeaponStats[label] ?? 0}%` }}></i>
                      </div>
                      <strong>{primaryWeaponStats[label] ?? "--"}</strong>
                    </div>
                  ))}
                </div>

                <div className="loadout-dossier-grid">
                  <div className="weapon-summary-note">
                    <strong>Current Build</strong>
                    <p>{primaryWeapon?.className ?? "Weapon class"}</p>
                  </div>
                  <div className="weapon-summary-note">
                    <strong>Tactical</strong>
                    <p>{activeEquipment?.title ?? "Equipment"}</p>
                  </div>
                  <div className="weapon-summary-note">
                    <strong>Field Upgrade</strong>
                    <p>{activeFieldUpgrade?.title ?? "Field Upgrade"}</p>
                  </div>
                  <div className="weapon-summary-note">
                    <strong>Focused Attachment</strong>
                    <p>
                      {activeOverviewAttachment
                        ? `${getEquippedAttachmentOption(primaryWeapon, activeOverviewAttachment).title} - ${
                            getEquippedAttachmentOption(primaryWeapon, activeOverviewAttachment).tune
                          }`
                        : "Select an attachment card to inspect the slot."}
                    </p>
                  </div>
                </div>

                <div className="perk-benefit-list">
                  <div className="perk-capacity-row">
                    <strong>Current Buffs</strong>
                    <span>{equippedPerks.length} Active</span>
                  </div>
                  <div className="equipped-perk-chip-list">
                    {equippedPerks.map((perk) => (
                      <div className="campaign-perk-chip" key={perk.id}>
                        {perk.title}
                      </div>
                    ))}
                  </div>
                  <div className="perk-capacity-row">
                    <strong>Perk Benefits</strong>
                    <span>{capacityUsed}/6 Capacity</span>
                  </div>
                  <div className="bar">
                    <i style={{ width: `${Math.min((capacityUsed / 6) * 100, 100)}%` }}></i>
                  </div>
                  {equippedPerks.map((perk) => (
                    <div className="perk-benefit-row" key={perk.id}>
                      <div>
                        <strong>{perk.title}</strong>
                        <span>{formatBenefit(perk)}</span>
                      </div>
                      <em>{perk.type}</em>
                    </div>
                  ))}
                </div>
              </Panel>
            </>
          )}
        </div>
        {swapModalPerk ? (
          <div className="perk-modal-backdrop" role="presentation">
            <div className="perk-swap-modal" role="dialog" aria-modal="true" aria-labelledby="perk-swap-title">
              <div className="panel-heading">
                <div>
                  <p className="panel-kicker">Swap Required</p>
                  <h3 id="perk-swap-title">{swapModalPerk.title}</h3>
                </div>
                <span className="slot-meta">Cost {swapModalPerk.cost}</span>
              </div>
              <div className="armory-detail-copy">
                <p>
                  You do not have enough perk points remaining. Remove equipped perks until {swapModalPerk.title} fits in the loadout.
                </p>
              </div>
              <div className="selected-perk-list">
                {swapModalPerks.map((perk) => (
                  <div className="selected-perk-row" key={perk.id}>
                    <div className={`campaign-perk-icon selected-perk-icon ${getPerkIconClass(perk)}`.trim()}></div>
                    <div>
                      <strong>{perk.title}</strong>
                      <span>{formatBenefit(perk)}</span>
                    </div>
                    <button
                      className="selected-perk-remove"
                      type="button"
                      aria-label={`Remove ${perk.title}`}
                      onClick={() => toggleSwapModalPerkRemoval(perk.id)}
                    >
                      ×
                    </button>
                    <em>Cost {perk.cost}</em>
                  </div>
                ))}
              </div>
              <div className="perk-capacity-row">
                <strong>Pending Capacity</strong>
                <span>
                  {swapModalCapacityUsed + (swapModalPerk?.cost ?? 0)}/6
                </span>
              </div>
              <div className="bar">
                <i style={{ width: `${Math.min((((swapModalCapacityUsed + (swapModalPerk?.cost ?? 0)) / 6) * 100), 100)}%` }}></i>
              </div>
              <button className="action-button action-button-primary action-button-confirm" type="button" onClick={confirmSwapModalSelection}>
                {swapModalCanConfirm ? "Confirm Swap" : "Cancel"}
              </button>
            </div>
          </div>
        ) : null}
      </ScreenFrame>
    </section>
  );
}
