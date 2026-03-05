
# Coop Shooter – Feature Roadmap Checklist

This document tracks systems that help the project feel closer to **AAA-quality gameplay smoothness** while staying realistic for a solo developer.

---

# Phase 1 – Core AAA Feel (Highest Priority)

## Weapon Feel
- [ ] Camera recoil
- [ ] Weapon kickback animation
- [ ] Weapon sway while idle
- [ ] Muzzle flash
- [ ] Bullet tracers
- [ ] Shell ejection
- [ ] Weapon smoke particles
- [ ] Layered weapon audio
- [ ] Camera shake when firing
- [ ] Hitmarker sound

## Hit Feedback
- [ ] Hitmarker UI
- [ ] Headshot marker
- [ ] Enemy hit reaction animation
- [ ] Kill confirmation sound
- [ ] Damage direction indicator
- [ ] Optional damage numbers

## Movement Polish
- [ ] Acceleration / deceleration system
- [ ] Sprint FOV increase
- [ ] Camera tilt when strafing
- [ ] Landing impact camera effect
- [ ] Footstep audio system
- [ ] Weapon bob while moving

## Crosshair System
- [ ] Dynamic spread from movement
- [x] Spread increase from shooting
- [ ] Spread penalty while jumping
- [x] Tightening when aiming
- [x] Crosshair animation

---

# Phase 2 – Combat Depth

## Enemy Hit Reactions
- [ ] Enemy stagger when damaged
- [ ] Headshot stun reaction
- [ ] Knockback reaction
- [ ] Limb damage system

## Dismemberment (Zombies)
- [ ] Head destruction
- [ ] Arm damage affecting attacks
- [ ] Leg damage causing crawl enemies

## Weapon Variety
- [ ] Pistol archetype
- [ ] SMG archetype
- [ ] Assault rifle archetype
- [ ] Shotgun archetype
- [ ] Sniper archetype

Weapon attributes:
- [ ] Fire rate tuning
- [ ] Recoil patterns
- [ ] Reload speed variation
- [ ] Spread tuning

## Equipment System
- [ ] Grenades
- [ ] Mines
- [ ] Turrets
- [ ] Stim pack
- [ ] Temporary shield

---

# Phase 3 – Zombies Roguelike Mode

## Wave System
- [ ] Basic zombie wave spawning
- [ ] Increasing difficulty scaling
- [ ] Wave counter UI
- [ ] Break/shop between waves

## Checkpoint System
- [ ] Checkpoint every X waves
- [ ] Restart from last checkpoint
- [ ] Save player progression at checkpoint

Example structure:

Run
Wave 1–5  
Shop  
Wave 6–10  
Shop  
Boss  
Checkpoint

---

## Run-Based Upgrade System

After each wave choose an upgrade.

- [ ] Random upgrade generator
- [ ] Upgrade selection UI
- [ ] Upgrade stacking system

Example upgrades:
- [ ] +10% fire rate
- [ ] +20% headshot damage
- [ ] +15% reload speed
- [ ] Armor regeneration
- [ ] Electric bullets
- [ ] Explosive rounds

---

## Meta Progression (Between Runs)

- [ ] Weapon unlock tree
- [ ] Armor upgrades
- [ ] Equipment unlocks
- [ ] Character starting bonuses
- [ ] Starting loadout selection

---

# Phase 4 – Enemy Variety

Zombie types:

- [ ] Walker (slow)
- [ ] Runner (fast)
- [ ] Tank (high HP)
- [ ] Exploder
- [ ] Spitter (ranged)

AI Improvements:
- [ ] Swarm behavior
- [ ] Target closest player
- [ ] Attack cooldowns
- [ ] Pathing improvements

---

# Phase 5 – Multiplayer Modes

## Team Deathmatch
- [ ] Kill limit system
- [ ] Match timer
- [ ] Respawn system
- [ ] Spawn protection
- [ ] Kill feed
- [ ] Scoreboard UI

## Zombies Co‑op Mode
- [ ] Player revive system
- [ ] Shared economy
- [ ] Co‑op scaling difficulty
- [ ] Team wipe restart

---

# Phase 6 – Game Polish

## Audio
- [ ] Layered gun audio
- [ ] Zombie sounds
- [ ] Ambient environment audio
- [ ] Low health heartbeat sound

## Camera Effects
- [ ] Damage vignette
- [ ] Explosion camera shake
- [ ] Sprint FOV effect
- [ ] Recoil kick

## UI Systems
- [ ] Ammo counter
- [ ] Hitmarker UI
- [ ] Kill feed
- [ ] Wave counter
- [ ] Minimap
- [ ] Damage direction indicator

---

# Phase 7 – Advanced Systems

## Spawn Director
Adaptive difficulty system.

- [ ] Track player performance
- [ ] Increase enemies if players dominate
- [ ] Reduce enemies if players struggle

Inspired by Left 4 Dead.

---

## Modular Weapon System

Weapon parts:

- [ ] Barrel
- [ ] Stock
- [ ] Grip
- [ ] Magazine
- [ ] Scope

Each modifies stats.

Example:
Long barrel → accuracy  
Short barrel → mobility  
Drum mag → reload speed penalty

---

# Development Strategy

Recommended build order:

### Phase 1
Core weapon feel and combat polish.

### Phase 2
Enemy reactions and weapon variety.

### Phase 3
Zombies roguelike systems.

### Phase 4
Multiplayer modes.

### Phase 5
Game polish and audio.

---

# Goal

Create a **fast, smooth, replayable co‑op shooter** that feels satisfying to play even with a small development team.
