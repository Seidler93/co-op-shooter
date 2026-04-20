export const weaponCatalog = [
  {
    id: "mx-12",
    className: "Assault Rifles",
    title: "MX-12 Carbine",
    imageClass: "weapon-rifle",
    imageVariantClass: "weapon-preview-balanced",
    attachments: [
      {
        slot: "Muzzle",
        selected: "Flash Cage",
        options: [
          { title: "Flash Cage", imageClass: "attachment-muzzle", tune: "Control +6" },
          { title: "Ported Brake", imageClass: "attachment-muzzle", tune: "Kick -5" },
          { title: "Spec Suppressor", imageClass: "attachment-muzzle", tune: "Noise -7" }
        ]
      },
      {
        slot: "Barrel",
        selected: "Taskforce 14.5",
        options: [
          { title: "Taskforce 14.5", imageClass: "attachment-barrel", tune: "Range +8" },
          { title: "CQB Shorty", imageClass: "attachment-barrel", tune: "Mobility +6" },
          { title: "Velocity Match", imageClass: "attachment-barrel", tune: "Bullet Speed +7" }
        ]
      },
      {
        slot: "Optic",
        selected: "Holo MK2",
        options: [
          { title: "Holo MK2", imageClass: "attachment-optic", tune: "Target ID +4" },
          { title: "Reflex Ghost", imageClass: "attachment-optic", tune: "ADS +5" },
          { title: "Scout 2x", imageClass: "attachment-optic", tune: "Zoom +4" }
        ]
      },
      {
        slot: "Magazine",
        selected: "45-Round Mag",
        options: [
          { title: "45-Round Mag", imageClass: "attachment-magazine", tune: "Ammo +10" },
          { title: "Fast Mag", imageClass: "attachment-magazine", tune: "Reload +7" },
          { title: "Drum Stack", imageClass: "attachment-magazine", tune: "Ammo +16" }
        ]
      },
      {
        slot: "Underbarrel",
        selected: "Angled Grip",
        options: [
          { title: "Angled Grip", imageClass: "attachment-underbarrel", tune: "Handling +5" },
          { title: "Operator Grip", imageClass: "attachment-underbarrel", tune: "Control +6" },
          { title: "Breacher Stop", imageClass: "attachment-underbarrel", tune: "Hipfire +4" }
        ]
      }
    ],
    attributes: {
      damage: 78,
      control: 71,
      mobility: 62,
      handling: 84
    }
  },
  {
    id: "vandal-19",
    className: "Assault Rifles",
    title: "Vandal-19",
    imageClass: "weapon-rifle",
    imageVariantClass: "weapon-preview-heavy",
    attachments: [
      {
        slot: "Muzzle",
        selected: "Compensator X",
        options: [
          { title: "Compensator X", imageClass: "attachment-muzzle", tune: "Control +8" },
          { title: "Strike Brake", imageClass: "attachment-muzzle", tune: "Burst +5" },
          { title: "Heavy Suppressor", imageClass: "attachment-muzzle", tune: "Noise -8" }
        ]
      },
      {
        slot: "Laser",
        selected: "Point Beam",
        options: [
          { title: "Point Beam", imageClass: "attachment-laser", tune: "Hipfire +6" },
          { title: "Snap Laser", imageClass: "attachment-laser", tune: "ADS +4" },
          { title: "TAC Pointer", imageClass: "attachment-laser", tune: "Target Track +5" }
        ]
      },
      {
        slot: "Optic",
        selected: "Reflex S1",
        options: [
          { title: "Reflex S1", imageClass: "attachment-optic", tune: "ADS +4" },
          { title: "Combat Holo", imageClass: "attachment-optic", tune: "Clarity +5" },
          { title: "Scout 3x", imageClass: "attachment-optic", tune: "Range +4" }
        ]
      },
      {
        slot: "Stock",
        selected: "Reinforced Stock",
        options: [
          { title: "Reinforced Stock", imageClass: "attachment-stock", tune: "Stability +6" },
          { title: "Ranger Stock", imageClass: "attachment-stock", tune: "Sway -5" },
          { title: "Collapsed Stock", imageClass: "attachment-stock", tune: "Mobility +6" }
        ]
      },
      {
        slot: "Rear Grip",
        selected: "Wrap Grip",
        options: [
          { title: "Wrap Grip", imageClass: "attachment-rear-grip", tune: "Sprint Out +5" },
          { title: "Grip Tape", imageClass: "attachment-rear-grip", tune: "Handling +4" },
          { title: "Rubber Grip", imageClass: "attachment-rear-grip", tune: "Recoil +5" }
        ]
      }
    ],
    attributes: {
      damage: 86,
      control: 58,
      mobility: 54,
      handling: 61
    }
  },
  {
    id: "viper-cqc",
    className: "SMGs",
    title: "Viper CQC",
    imageClass: "weapon-sidearm",
    imageVariantClass: "weapon-preview-close",
    attachments: [
      {
        slot: "Muzzle",
        selected: "Micro Brake",
        options: [
          { title: "Micro Brake", imageClass: "attachment-muzzle", tune: "Kick -4" },
          { title: "Flash Hider", imageClass: "attachment-muzzle", tune: "Muzzle Flash -6" },
          { title: "Mini Suppressor", imageClass: "attachment-muzzle", tune: "Noise -5" }
        ]
      },
      {
        slot: "Laser",
        selected: "Quickline Laser",
        options: [
          { title: "Quickline Laser", imageClass: "attachment-laser", tune: "Sprint ADS +7" },
          { title: "Hipfire Dot", imageClass: "attachment-laser", tune: "Spread -5" },
          { title: "Blue Trace", imageClass: "attachment-laser", tune: "Tracking +4" }
        ]
      },
      {
        slot: "Magazine",
        selected: "Fast Mag",
        options: [
          { title: "Fast Mag", imageClass: "attachment-magazine", tune: "Reload +8" },
          { title: "Extended Mag", imageClass: "attachment-magazine", tune: "Ammo +8" },
          { title: "Light Mag", imageClass: "attachment-magazine", tune: "Mobility +4" }
        ]
      },
      {
        slot: "Stock",
        selected: "Wire Stock",
        options: [
          { title: "Wire Stock", imageClass: "attachment-stock", tune: "Mobility +6" },
          { title: "Tac Stock", imageClass: "attachment-stock", tune: "ADS Strafe +5" },
          { title: "Stability Stock", imageClass: "attachment-stock", tune: "Control +5" }
        ]
      },
      {
        slot: "Rear Grip",
        selected: "Textured Wrap",
        options: [
          { title: "Textured Wrap", imageClass: "attachment-rear-grip", tune: "Handling +5" },
          { title: "Grip Mesh", imageClass: "attachment-rear-grip", tune: "Reload +4" },
          { title: "Recoil Pad", imageClass: "attachment-rear-grip", tune: "Control +4" }
        ]
      }
    ],
    attributes: {
      damage: 61,
      control: 66,
      mobility: 88,
      handling: 90
    }
  },
  {
    id: "bastion-58",
    className: "LMGs",
    title: "Bastion 58",
    imageClass: "weapon-rifle",
    imageVariantClass: "weapon-preview-braced",
    attachments: [
      {
        slot: "Muzzle",
        selected: "Heavy Suppressor",
        options: [
          { title: "Heavy Suppressor", imageClass: "attachment-muzzle", tune: "Noise -8" },
          { title: "Comp Brake", imageClass: "attachment-muzzle", tune: "Control +6" },
          { title: "Vent Brake", imageClass: "attachment-muzzle", tune: "Heat -4" }
        ]
      },
      {
        slot: "Barrel",
        selected: "Support Long Barrel",
        options: [
          { title: "Support Long Barrel", imageClass: "attachment-barrel", tune: "Velocity +9" },
          { title: "Siege Barrel", imageClass: "attachment-barrel", tune: "Range +8" },
          { title: "Balanced Tube", imageClass: "attachment-barrel", tune: "Handling +4" }
        ]
      },
      {
        slot: "Optic",
        selected: "Scout 3x",
        options: [
          { title: "Scout 3x", imageClass: "attachment-optic", tune: "Lane Hold +5" },
          { title: "Holo Guard", imageClass: "attachment-optic", tune: "Target ID +4" },
          { title: "Overwatch 4x", imageClass: "attachment-optic", tune: "Zoom +6" }
        ]
      },
      {
        slot: "Bipod",
        selected: "Deploy Bipod",
        options: [
          { title: "Deploy Bipod", imageClass: "attachment-bipod", tune: "Mounted Control +10" },
          { title: "Light Bipod", imageClass: "attachment-bipod", tune: "Setup +5" },
          { title: "Anchor Bipod", imageClass: "attachment-bipod", tune: "Recoil +8" }
        ]
      },
      {
        slot: "Ammo Feed",
        selected: "Linked Belt",
        options: [
          { title: "Linked Belt", imageClass: "attachment-ammo-feed", tune: "Capacity +12" },
          { title: "Rapid Feed", imageClass: "attachment-ammo-feed", tune: "Reload +5" },
          { title: "Tracer Belt", imageClass: "attachment-ammo-feed", tune: "Visibility +4" }
        ]
      }
    ],
    attributes: {
      damage: 90,
      control: 52,
      mobility: 38,
      handling: 46
    }
  },
  {
    id: "longwatch-7",
    className: "Marksman",
    title: "Longwatch 7",
    imageClass: "weapon-rifle",
    imageVariantClass: "weapon-preview-precision",
    attachments: [
      {
        slot: "Muzzle",
        selected: "Crown Brake",
        options: [
          { title: "Crown Brake", imageClass: "attachment-muzzle", tune: "Recoil +5" },
          { title: "Precision Suppressor", imageClass: "attachment-muzzle", tune: "Noise -7" },
          { title: "Stabilizer", imageClass: "attachment-muzzle", tune: "Sway -4" }
        ]
      },
      {
        slot: "Barrel",
        selected: "Precision 22",
        options: [
          { title: "Precision 22", imageClass: "attachment-barrel", tune: "Range +10" },
          { title: "Fluted Barrel", imageClass: "attachment-barrel", tune: "Handling +5" },
          { title: "Heavy Match", imageClass: "attachment-barrel", tune: "Velocity +8" }
        ]
      },
      {
        slot: "Optic",
        selected: "Overwatch 6x",
        options: [
          { title: "Overwatch 6x", imageClass: "attachment-optic", tune: "Zoom +8" },
          { title: "Hunter 4x", imageClass: "attachment-optic", tune: "ADS +4" },
          { title: "Variable Scope", imageClass: "attachment-optic", tune: "Flex +5" }
        ]
      },
      {
        slot: "Stock",
        selected: "Marksman Stock",
        options: [
          { title: "Marksman Stock", imageClass: "attachment-stock", tune: "Idle Sway -6" },
          { title: "Ridge Stock", imageClass: "attachment-stock", tune: "Stability +5" },
          { title: "Feather Stock", imageClass: "attachment-stock", tune: "Mobility +4" }
        ]
      },
      {
        slot: "Ammunition",
        selected: "Match Rounds",
        options: [
          { title: "Match Rounds", imageClass: "attachment-ammo", tune: "Crit +7" },
          { title: "AP Rounds", imageClass: "attachment-ammo", tune: "Pen +6" },
          { title: "Pressure Load", imageClass: "attachment-ammo", tune: "Damage +5" }
        ]
      }
    ],
    attributes: {
      damage: 94,
      control: 63,
      mobility: 41,
      handling: 57
    }
  }
];
