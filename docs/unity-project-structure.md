# Unity Project Structure

Project-owned game content belongs under `Assets/_Project`. Imported packages and
Asset Store content belong under `Assets/ThirdParty`; large source packs that are
listed in the root `.gitignore` remain local and should not be committed.

```text
Assets/
|-- _Project/
|   |-- Scripts/
|   |-- Art/
|   |-- Animations/
|   |-- Prefabs/
|   |-- Scenes/
|   |-- Audio/
|   |-- Materials/
|   |-- VFX/
|   `-- UI/
|-- ThirdParty/
`-- Plugins/
```

## Script layout

```text
Scripts/
|-- Core/                  engine-level systems and utilities
|-- Player/
|   |-- Controllers/
|   |-- Combat/
|   |-- Animation/
|   |-- Camera/
|   `-- Input/
|-- Enemy/
|   |-- AI/
|   |-- Combat/
|   `-- Animation/
|-- Weapons/
|   |-- Systems/
|   `-- Data/
|-- Combat/
|   |-- Damage/
|   |-- Health/
|   `-- HitDetection/
|-- Networking/
|-- UI/
|   |-- HUD/
|   |-- Menus/
|   `-- Shop/
|-- Systems/
|   |-- RoundSystem/
|   |-- Progression/
|   `-- Spawning/
|-- Interactables/
|-- Data/ScriptableObjects/
`-- Managers/              thin coordination components only
```

The live movement implementation remains part of the existing player stack. Input,
movement, camera, animation, combat state, and network ownership must stay separate
components rather than being replaced by a second all-in-one controller.
