import { weaponCatalog } from "./weapons";

export const screenTabs = [
  { id: "main-menu", label: "Main Menu" },
  { id: "online-lobby", label: "Online Lobby" },
  { id: "loadout-screen", label: "Loadout" }
];

export const profileStats = [
  { label: "Level", value: "28" },
  { label: "Kills", value: "4,312" },
  { label: "Survival", value: "82%" }
];

export const newsItems = [
  {
    category: "patch",
    tag: "Patch 0.1.17",
    title: "Downtown hospital raid added to mission rotation",
    body: "Night variant now available with volatile infected packs and tighter extraction timing."
  },
  {
    category: "patch",
    tag: "Patch 0.1.16",
    title: "Armory requisition costs reduced for early progression",
    body: "Starter weapon unlocks now come online faster so new squads can build viable kits sooner."
  },
  {
    category: "patch",
    tag: "Hotfix",
    title: "Dedicated server desync reduced during extraction waves",
    body: "Host migration and last-minute revive timing now resolve more consistently in four-player sessions."
  },
  {
    category: "community",
    tag: "Community",
    title: "Weekend challenge goes live in 06:12:44",
    body: "Finish three veteran runs for an exclusive squad banner and weapon charm."
  },
  {
    category: "community",
    tag: "Community",
    title: "Squad finder spotlight is rotating featured groups tonight",
    body: "Browse curated teams looking for tanks, medics, and high-difficulty clears from the social board."
  },
  {
    category: "community",
    tag: "Broadcast",
    title: "Developer briefing starts at 8 PM CST",
    body: "The next stream covers infected variants, map pacing, and upcoming lobby improvements."
  }
];

export const playerSlots = [
  { badge: "Host", name: "Ajax Seventy", role: "Assault Specialist", tone: "is-host" },
  { badge: "Ready", name: "Vera North", role: "Medic Support", tone: "is-ready" },
  { badge: "Ready", name: "Reyes", role: "Heavy Breacher", tone: "is-ready" },
  { badge: "Open", name: "Invite Slot", role: "Waiting for player", tone: "is-empty" }
];

export const missionDifficulties = ["Standard", "Veteran", "Nightmare"];

export const loadoutCategories = ["Assault", "Marksman", "Support", "Demolitions"];

export const customLoadouts = [
  {
    id: "custom-1",
    label: "Custom Loadout 1",
    role: "Assault",
    primary: "MX-12 Carbine",
    secondary: "Rook-9 Sidearm",
    equipment: "Shock Mine",
    fieldUpgrade: "Sentry Turret",
    perks: ["Reload Efficiency", "Awareness", "Quick ADS", "Fast Hands", "Interaction Speed"],
    stats: [
      { label: "Damage", value: 78 },
      { label: "Control", value: 71 },
      { label: "Mobility", value: 62 },
      { label: "Support", value: 84 }
    ]
  },
  {
    id: "custom-2",
    label: "Custom Loadout 2",
    role: "Breacher",
    primary: "Bastion 58",
    secondary: "Hammer-45",
    equipment: "Breach Charge",
    fieldUpgrade: "Ammo Crate",
    perks: ["Magazine Capacity", "Minor Armor", "Recoil Control", "First Shot Control"],
    stats: [
      { label: "Damage", value: 90 },
      { label: "Control", value: 58 },
      { label: "Mobility", value: 44 },
      { label: "Support", value: 52 }
    ]
  },
  {
    id: "custom-3",
    label: "Custom Loadout 3",
    role: "Medic",
    primary: "Viper CQC",
    secondary: "Medic-7",
    equipment: "Stim Pack",
    fieldUpgrade: "Healing Field",
    perks: ["Revive Specialist", "Quick Recovery", "Threat Ping", "Fast Hands", "Awareness"],
    stats: [
      { label: "Damage", value: 61 },
      { label: "Control", value: 66 },
      { label: "Mobility", value: 74 },
      { label: "Support", value: 93 }
    ]
  },
  {
    id: "custom-4",
    label: "Custom Loadout 4",
    role: "Recon",
    primary: "Longwatch 7",
    secondary: "Rook-9 Sidearm",
    equipment: "Motion Sensor",
    fieldUpgrade: "Recon Drone",
    perks: ["Awareness", "Threat Ping", "Sprint Recovery", "Deep Pockets", "Reserve Boost"],
    stats: [
      { label: "Damage", value: 86 },
      { label: "Control", value: 64 },
      { label: "Mobility", value: 57 },
      { label: "Support", value: 68 }
    ]
  }
];

export const loadoutOptionCards = [
  { id: "primary", title: "Primary", item: "MX-12 Carbine", tone: "large", imageClass: "weapon-rifle" },
  { id: "secondary", title: "Secondary", item: "Rook-9 Sidearm", tone: "large", imageClass: "weapon-sidearm" },
  { id: "equipment", title: "Equipment", item: "Shock Mine", tone: "medium", imageClass: "gear-mine" },
  { id: "field-upgrade", title: "Field Upgrade", item: "Sentry Turret", tone: "medium", imageClass: "gear-turret" },
  { id: "perks", title: "Perks", item: "3 Equipped", tone: "perks", imageClass: "perk-cold" }
];

export const weaponClassTabs = ["Assault Rifles", "SMGs", "LMGs", "Marksman"];

export const armoryWeapons = [
  ...weaponCatalog
];

export const equipmentOptions = [
  {
    id: "shock-mine",
    title: "Shock Mine",
    body: "Area denial device that chains electricity through infected packs near choke points.",
    imageClass: "gear-mine"
  },
  {
    id: "breach-charge",
    title: "Breach Charge",
    body: "Timed explosive for clearing barricades and heavily clustered threats in tight hallways.",
    imageClass: "gear-mine"
  },
  {
    id: "stim-pack",
    title: "Stim Pack",
    body: "Quick-use injector that restores mobility and stabilizes squadmates during critical pushes.",
    imageClass: "gear-mine"
  },
  {
    id: "motion-sensor",
    title: "Motion Sensor",
    body: "Deployable scanner that reveals incoming infected routes and flanking specials.",
    imageClass: "gear-mine"
  }
];

export const fieldUpgradeOptions = [
  {
    id: "sentry-turret",
    title: "Sentry Turret",
    body: "Automated weapon platform that locks lanes and buys time during horde escalations.",
    imageClass: "gear-turret"
  },
  {
    id: "ammo-crate",
    title: "Ammo Crate",
    body: "Resupply station that refills squad ammunition and supports longer defense holds.",
    imageClass: "gear-turret"
  },
  {
    id: "healing-field",
    title: "Healing Field",
    body: "Portable support emitter that restores health over time inside a safe radius.",
    imageClass: "gear-turret"
  },
  {
    id: "recon-drone",
    title: "Recon Drone",
    body: "Remote scouting drone that tags incoming threats and highlights objective lanes.",
    imageClass: "gear-turret"
  }
];

export const perkTypeTabs = ["All", "Weapon", "Utility", "Survival"];

export const perkCatalog = [
  {
    id: "reload-efficiency",
    title: "Reload Efficiency",
    level: 1,
    cost: 2,
    type: "Weapon",
    notes: "Base perk",
    description: "Improves baseline reload flow across your equipped weapons.",
    variants: [
      { title: "Combat Reload", cost: 2, unlock: "Blackwell Reputation", effect: "Faster reload if mag not empty" },
      { title: "Last Stand Reload", cost: 2, unlock: "Blackwell Reputation", effect: "Faster reload at low HP" },
      { title: "Chain Reload", cost: 3, unlock: "Blackwell Reputation", effect: "Massive speed after kill" }
    ]
  },
  {
    id: "awareness",
    title: "Awareness",
    level: 1,
    cost: 1,
    type: "Utility",
    notes: "Base perk",
    description: "Highlights nearby threats and improves your read on dangerous elites.",
    variants: [
      { title: "Threat Focus", cost: 1, unlock: "Blackwell Reputation", effect: "Longer elite highlight" },
      { title: "Team Awareness", cost: 2, unlock: "Blackwell Reputation", effect: "Allies see highlights" },
      { title: "Pulse Scan", cost: 2, unlock: "Blackwell Reputation", effect: "Periodic scan" }
    ]
  },
  { id: "quick-ads", title: "Quick ADS", level: 2, cost: 1, type: "Weapon", notes: "Operator Rank", description: "Brings sights up faster for snap engagements." },
  { id: "fast-hands", title: "Fast Hands", level: 2, cost: 1, type: "Utility", notes: "Operator Rank", description: "Speeds up weapon handling and utility transitions." },
  { id: "interaction-speed", title: "Interaction Speed", level: 3, cost: 1, type: "Utility", notes: "Operator Rank", description: "Shortens objective, revive, and pickup interactions." },
  {
    id: "magazine-capacity",
    title: "Magazine Capacity",
    level: 4,
    cost: 2,
    type: "Weapon",
    notes: "Base perk",
    description: "Increases magazine depth for longer firing strings.",
    variants: [
      { title: "Extended Capacity", cost: 2, unlock: "Blackwell Reputation", effect: "Bigger mag, slower reload" },
      { title: "Lightweight Mags", cost: 2, unlock: "Blackwell Reputation", effect: "Faster ADS + reload" },
      { title: "Overflow", cost: 2, unlock: "Blackwell Reputation", effect: "Chance to not consume ammo" }
    ]
  },
  { id: "minor-armor", title: "Minor Armor", level: 4, cost: 1, type: "Survival", notes: "Operator Rank", description: "Adds a small survivability buffer against routine hits." },
  {
    id: "revive-specialist",
    title: "Revive Specialist",
    level: 6,
    cost: 2,
    type: "Utility",
    notes: "Base perk",
    description: "Improves revive speed and makes recovery-focused roles stronger.",
    variants: [
      { title: "Reinforced Revive", cost: 2, unlock: "Blackwell Reputation", effect: "Temporary armor after revive" },
      { title: "Shared Recovery", cost: 2, unlock: "Blackwell Reputation", effect: "Heal both players" },
      { title: "Mobile Revive", cost: 2, unlock: "Blackwell Reputation", effect: "Move while reviving" }
    ]
  },
  { id: "quick-recovery", title: "Quick Recovery", level: 6, cost: 1, type: "Survival", notes: "Operator Rank", description: "Helps you stabilize faster after taking damage." },
  { id: "flinch-resistance", title: "Flinch Resistance", level: 7, cost: 1, type: "Survival", notes: "Operator Rank", description: "Reduces aim disruption when hit under pressure." },
  {
    id: "recoil-control",
    title: "Recoil Control",
    level: 8,
    cost: 2,
    type: "Weapon",
    notes: "Base perk",
    description: "Tightens weapon kick and improves sustained accuracy.",
    variants: [
      { title: "Precision Control", cost: 2, unlock: "Blackwell Reputation", effect: "Strong ADS recoil reduction" },
      { title: "Sustained Fire", cost: 2, unlock: "Blackwell Reputation", effect: "Recoil improves while firing" },
      { title: "Reactive Control", cost: 2, unlock: "Blackwell Reputation", effect: "Recoil reduced after hit" }
    ]
  },
  { id: "threat-ping", title: "Threat Ping", level: 8, cost: 1, type: "Utility", notes: "Operator Rank", description: "Marks dangerous targets for faster squad reactions." },
  { id: "steady-hands", title: "Steady Hands", level: 9, cost: 1, type: "Weapon", notes: "Operator Rank", description: "Improves shot steadiness during movement and target tracking." },
  {
    id: "sprint-recovery",
    title: "Sprint Recovery",
    level: 11,
    cost: 2,
    type: "Survival",
    notes: "Base perk",
    description: "Recovers stamina and movement control after hard repositioning.",
    variants: [
      { title: "Momentum", cost: 2, unlock: "Blackwell Reputation", effect: "Build speed while moving" },
      { title: "Tactical Reset", cost: 2, unlock: "Blackwell Reputation", effect: "Kill restores stamina" },
      { title: "Evasive Movement", cost: 2, unlock: "Blackwell Reputation", effect: "Reduce damage while sprinting" }
    ]
  },
  { id: "first-shot-control", title: "First Shot Control", level: 11, cost: 1, type: "Weapon", notes: "Operator Rank", description: "Sharpens the opening shot for more reliable burst accuracy." },
  { id: "deep-pockets", title: "Deep Pockets", level: 12, cost: 1, type: "Utility", notes: "Operator Rank", description: "Expands carrying utility and mission pickup capacity." },
  {
    id: "elite-damage",
    title: "Elite Damage",
    level: 13,
    cost: 3,
    type: "Weapon",
    notes: "Base perk",
    description: "Boosts your effectiveness against elite infected and armored threats.",
    variants: [
      { title: "Weakpoint Specialist", cost: 3, unlock: "Blackwell Reputation", effect: "High crit, weaker body damage" },
      { title: "Finisher", cost: 3, unlock: "Blackwell Reputation", effect: "Bonus vs low HP enemies" },
      { title: "Armor Breaker", cost: 3, unlock: "Blackwell Reputation", effect: "Armor penetration" }
    ]
  },
  { id: "reserve-boost", title: "Reserve Boost", level: 13, cost: 1, type: "Utility", notes: "Operator Rank", description: "Adds reserve resources to keep sustained fights online longer." },
  {
    id: "equipment-efficiency",
    title: "Equipment Efficiency",
    level: 15,
    cost: 2,
    type: "Utility",
    notes: "Base perk",
    description: "Improves the output and reliability of tactical equipment.",
    variants: [
      { title: "Scavenger", cost: 2, unlock: "Blackwell Reputation", effect: "Chance to refund equipment" },
      { title: "Overcharge", cost: 3, unlock: "Blackwell Reputation", effect: "Stronger effect, longer cooldown" },
      { title: "Quick Deploy", cost: 2, unlock: "Blackwell Reputation", effect: "Faster use, weaker output" }
    ]
  },
  { id: "quick-recharge", title: "Quick Recharge", level: 16, cost: 1, type: "Utility", notes: "Operator Rank", description: "Shortens recharge timing on active gear and support tools." },
  {
    id: "ammo-efficiency",
    title: "Ammo Efficiency",
    level: 17,
    cost: 2,
    type: "Weapon",
    notes: "Base perk",
    description: "Improves ammo economy for longer runs and defense waves.",
    variants: [
      { title: "Conservation", cost: 2, unlock: "Blackwell Reputation", effect: "Chance not to consume ammo" },
      { title: "Ammo Burst", cost: 2, unlock: "Blackwell Reputation", effect: "Bonus after reload" },
      { title: "Reserve Boost+", cost: 2, unlock: "Blackwell Reputation", effect: "Larger reserves" }
    ]
  },
  { id: "field-battery", title: "Field Battery", level: 18, cost: 1, type: "Utility", notes: "Operator Rank", description: "Supports faster recharge cycles for deployables and upgrades." },
  {
    id: "damage-resistance",
    title: "Damage Resistance",
    level: 19,
    cost: 3,
    type: "Survival",
    notes: "Base perk",
    description: "Defines sturdier builds and improves high-risk frontline survivability.",
    variants: [
      { title: "Fortified", cost: 3, unlock: "Blackwell Reputation", effect: "Stronger while stationary" },
      { title: "Adrenaline", cost: 3, unlock: "Blackwell Reputation", effect: "Stronger at low HP" },
      { title: "Recovery Shield", cost: 3, unlock: "Blackwell Reputation", effect: "Temp resistance after hit" }
    ]
  },
  { id: "extra-charge", title: "Extra Charge", level: 20, cost: 1, type: "Utility", notes: "Operator Rank", description: "Increases use count on charge-based gear." },
  { id: "supply-specialist", title: "Supply Specialist", level: 21, cost: 1, type: "Utility", notes: "Operator Rank", description: "Improves resource drops and team resupply reliability." },
  { id: "efficient-operator", title: "Efficient Operator", level: 23, cost: 1, type: "Utility", notes: "Operator Rank", description: "General-purpose efficiency perk that smooths out high-pressure runs." },
  { id: "overstock", title: "Overstock", level: 25, cost: 1, type: "Utility", notes: "Operator Rank", description: "Further expands carried support resources and reserve utility." }
];

export const loadoutSlots = [
  {
    kicker: "Primary Weapon",
    title: "MX-12 Carbine",
    body: "Controlled recoil, fast handling, optimized for tight urban corridors.",
    meta: "Suppressor · Red Dot · 45-Round Mag",
    selected: true
  },
  {
    kicker: "Secondary Weapon",
    title: "Rook-9 Sidearm",
    body: "Reliable backup with strong close-quarters stagger on infected rushers.",
    meta: "Extended Mag · Laser"
  },
  {
    kicker: "Equipment",
    title: "Shock Mine",
    body: "Area denial device that chains between clustered targets near choke points.",
    meta: "Charges: 2"
  },
  {
    kicker: "Perk Package",
    title: "Rapid Triage",
    body: "Revive allies faster and gain temporary resistance after a successful rescue.",
    meta: "Support Tier III"
  }
];

export const itemCards = [
  { itemClass: "AR", title: "MX-12 Carbine", body: "Balanced assault platform", selected: true },
  { itemClass: "SMG", title: "Viper CQC", body: "High mobility room clearer" },
  { itemClass: "LMG", title: "Bastion 58", body: "Suppression-focused fire support" },
  { itemClass: "DMR", title: "Longwatch 7", body: "Precision anti-specialist rifle" }
];

export const summaryStats = [
  { label: "Damage", value: 78 },
  { label: "Control", value: 71 },
  { label: "Mobility", value: 62 },
  { label: "Support", value: 84 }
];
