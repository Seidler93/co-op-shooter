
# Co-op Shooter – Feature Roadmap Checklist

Last audited against the Unity project: September 27, 2026.

Legend:

- `[x]` implemented in the current project
- `[ ]` not implemented or not yet production-ready

This checklist records shipped code, not just the original design plan. Some implemented
features still need tuning and multiplayer playtesting before they should be considered final.

This document tracks systems that help the project feel closer to **AAA-quality gameplay smoothness** while staying realistic for a solo developer.

---

# Phase 1 – Core AAA Feel (Highest Priority)

## Weapon Feel
- [x] Camera/weapon recoil
- [x] Weapon kickback
- [x] Weapon sway while idle
- [x] Muzzle flash
- [x] Bullet tracers
- [ ] Shell ejection
- [ ] Weapon smoke particles
- [x] Networked weapon audio (additional layering/polish remains)
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
- [x] Acceleration / deceleration system
- [ ] Sprint FOV increase (ADS zoom is implemented)
- [ ] Camera tilt when strafing
- [ ] Landing impact camera effect
- [ ] Footstep audio system
- [x] Weapon sway/bob responds to movement

## Crosshair System
- [ ] Dynamic spread from movement
- [x] Spread increase from shooting
- [ ] Spread penalty while airborne
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
- [x] Fire-rate tuning and shop upgrades
- [x] Recoil tuning
- [ ] Per-weapon recoil patterns
- [ ] Reload-speed variation
- [x] Spread/bloom tuning

## Equipment System
- [x] Networked grenades
- [ ] Mines
- [ ] Turrets
- [ ] Stim pack
- [ ] Temporary shield

---

# Phase 3 – Zombies Roguelike Mode

## Wave System
- [x] Server-authoritative zombie wave spawning
- [x] Increasing enemy-count and enemy-type scaling
- [x] Wave counter UI
- [x] Points shop during runs
- [ ] Dedicated timed shop/intermission between waves

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

- [x] Base zombie variants
- [x] Small/fast zombie
- [x] Boss/tank enemy
- [x] Exploder
- [x] Spitter (ranged)

AI Improvements:
- [ ] Swarm behavior
- [x] Target closest living player
- [x] Attack cooldowns
- [x] NavMesh pathing and spawn-lane support

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
- [x] Player downed, bleedout, and revive system
- [ ] Shared economy (score is currently per-player)
- [ ] Explicit player-count difficulty scaling
- [x] Team-wipe game over and restart flow

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
- [x] Network-bound ammo counter
- [ ] Hitmarker UI
- [ ] Kill feed
- [x] Wave counter
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
