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
