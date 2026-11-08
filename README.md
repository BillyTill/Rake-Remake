# Rake (2015) - Unity Remake

Complete remake of the Rake (2015) game systems based on decompiled source from `D:\Rake Multiplayer\Decompiled_Source`.

## Project Structure

```
Assets/Scripts/
├── Core/                    # Base classes, interfaces, utilities
│   ├── Unit_obj.cs          # Base damageable entity
│   ├── DynamicObject.cs     # Interactive world objects
│   ├── PlayerInput.cs       # Input constants
│   ├── DamageType.cs        # Damage type strings
│   ├── DP.cs                # Detection point utility
│   ├── FootstepChanger.cs   # Surface footstep sounds
│   ├── HideZoneTrigger.cs   # Hiding spots (lockers, bushes)
│   ├── IORotator.cs         # Auto-rotation
│   ├── IPausable.cs         # Pause interface
│   ├── IQuittable.cs        # Quit interface
│   ├── RootMotionScript.cs  # Animator root motion
│   ├── VisualizeJoint.cs    # Debug joint visualization
│   ├── AutoDestroy.cs       # Auto-destroy timer
│   ├── TimePause.cs         # Time scale control
│   └── AppQuit.cs           # Application quit handler
│
├── Player/                  # Player controller systems
│   ├── FPSController.cs     # Main player controller (singleton)
│   ├── PlayerMovement.cs    # Movement, stamina, footsteps
│   ├── CameraMouseLook.cs   # Mouse look, focus targeting
│   ├── FPSCamera.cs         # Interaction raycasting
│
├── Weapons/                 # Weapon system
│   ├── Weapon.cs            # Base weapon class
│   ├── WeaponManager.cs     # Inventory, switching, ammo
│   ├── Rifle_weapon.cs      # Shotgun (ID:1) - 25 dmg, stun
│   ├── SniperRifle_weapon.cs# Sniper (ID:6) - 200 dmg, scope
│   ├── Camera_weapon.cs     # Camera placer (ID:2)
│   ├── Trap_weapon.cs       # Bear trap placer (ID:4)
│   ├── Food_weapon.cs       # Bait placer (ID:5)
│   ├── MedKit_weapon.cs     # Medkit (ID:8) - heal 100
│   ├── Binocular_weapon.cs  # Binoculars (ID:7)
│   ├── Map_weapon.cs        # Map (ID:3)
│
├── Camera/                  # Camera surveillance system
│   ├── CameraManager.cs     # Manages up to 5 active cameras
│   ├── CameraComputer.cs    # Van computer for viewing cameras
│   ├── RecordCamera_obj.cs  # Stationary camera on mount
│   ├── AnimalRecordCamera_obj.cs # Pig Cam (on boar)
│   ├── RECCameras_obj.cs    # Base camera mount class
│   ├── CameraAngle.cs       # View angle constraints
│   ├── Vintage.cs           # Film effect
│   ├── CameraFilterBase.cs  # Base post-process effect
│   ├── CameraFilterPack_NightVision.cs # Night vision
│   ├── CameraFilterPack_Vignette.cs    # Vignette
│   ├── CameraFilterPack_Blur.cs        # Blur effects
│   └── SetupCamera.cs       # Camera setup effects
│
├── AI/                      # Enemy & Animal AI
│   ├── Enemy_AI.cs          # Rake AI (complex state machine)
│   ├── Animal_AI.cs         # Boar/Deer AI
│   ├── AnimalHead.cs        # Animal vision detection
│   ├── AnimalsManager.cs    # Population management
│
├── Items/                   # World items & pickups
│   ├── Item.cs              # Base pickup class
│   ├── Rifle_item.cs        # Rifle pickup
│   ├── SniperRifle_item.cs  # Sniper pickup
│   ├── RifleAmmo_item.cs    # Rifle ammo
│   ├── SniperAmmo_item.cs   # Sniper ammo
│   ├── Camera_item.cs       # Camera pickup
│   ├── Trap_item.cs         # Bear trap pickup
│   ├── Food_item.cs         # Bait pickup
│   ├── Medkit_item.cs       # Medkit pickup
│   ├── BoxOfCameras_item.cs # Camera box
│   ├── FoodPoint.cs         # Animal feeding point
│   ├── Trap_proj.cs         # Placed bear trap
│   ├── Window_obj.cs        # Breakable window
│   ├── Door_obj.cs          # Interactive door
│   ├── AnimalCage_obj.cs    # Boar trap for Pig Cam
│   ├── SoundTrigger.cs      # Noise maker
│   ├── ScreemerSystem.cs    # Jump scare
│   ├── Notebook_obj.cs      # Lore notebook
│   ├── TabletComputer_item.cs # Tablet UI
│   ├── SetupCameras_objective.cs # Tutorial objective
│   ├── TrapDoorSwitcher_obj.cs   # Trap door switch
│   ├── TrapRotationCorrect.cs    # Trap ground alignment
│   ├── Trigger_objective.cs      # Trigger objective
│   ├── LevelSkip.cs              # Level transition
│   ├── Log_trigger.cs            # Noisy logs
│   ├── Forest_trigger.cs         # Zone ambience
│   ├── Rain_trigger.cs           # Rain zone
│   ├── Flashlight_trigger.cs     # Flashlight zone
│   ├── RakeDead_obj.cs           # Dead Rake
│   ├── TakeItems_objective.cs    # Collect items objective
│
├── UI/                      # User interface
│   ├── InformationUI.cs     # Hints, objectives, pickups
│   ├── Inventory.cs         # Inventory UI
│   ├── Inventory_item.cs    # Inventory slot
│   ├── PauseManager.cs      # Pause menu
│   ├── MainMenu.cs          # Main menu
│   ├── Menu_button.cs       # Button helper
│   ├── SuvivalGuideMenu.cs  # F1 guide
│
├── Managers/                # Game management systems
│   ├── NotificationsManager.cs # Event system
│   ├── LevelItemsManager.cs # Level object registry
│   ├── SaveGameSystem.cs    # Save/load (PlayerPrefs)
│   ├── GameTimeManager.cs   # In-game clock
│   ├── GameTimer.cs         # 3-hour mission timer
│   ├── ObjectiveManager.cs  # Objective tracking
│   ├── RainManager.cs       # Weather (minimal)
│   ├── GameManager.cs       # Central coordinator
│
└── Multiplayer/             # LAN multiplayer support
    ├── NetworkedPlayer.cs   # Player network sync
    ├── RM_RakeMovement.cs   # Player-controlled Rake
```

## Game Mechanics Implemented

### Core Gameplay Loop
- **3-Hour Mission Timer**: Real-time 3 hour limit, divided into 3 nights
- **Day Phase**: Preparation - place cameras, gather supplies, set traps
- **Night Phase**: Hunt - Rake patrols, player monitors cameras, engages
- **Night Progression**: Night 1 (Passive) → Night 2 (Active) → Night 3 (Aggressive)

### Rake AI (Enemy_AI)
**Patrol Phases:**
- **Follow**: Silent stalking, sniffing sounds
- **Stealth**: Within 70 units, slow walk, massive damage (75)
- **Chase**: Player looked at Rake → glowing eyes, active pursuit

**Combat Phases (on damage):**
- **Low Health (<1400 HP)**: Scream, speed/damage increase, glow removed
- **Blocking**: Covers face, immune from front/sides, vulnerable from back
- **Fake Retreat (<1950 HP or too many traps)**: Runs to cave, returns later

**Navigation**: NavMesh pathfinding, opens doors, climbs ladders

### Camera System
- **Max 5 simultaneous cameras**
- **Stationary cameras** on fixed mounts
- **Pig Cam**: Trap boar → attach camera → release for mobile view
- **Van computer**: Switch cameras, rotate view, motion detection
- **Night vision** post-process effect

### Weapons
| Weapon | ID | Damage | Clip | Role |
|--------|-----|--------|------|------|
| Rifle (Shotgun) | 1 | 25 | 2 | Close range, stuns 4s |
| Sniper Rifle | 6 | 200 | 2 | Long range, high damage |
| Camera | 2 | - | 5 | Place surveillance |
| Bear Trap | 4 | 50 | 3 | Immobilize 7s |
| Bait | 5 | - | 5 | Lure boars/Rake |
| Medkit | 8 | - | 3 | Heal 100 HP |
| Binoculars | 7 | - | ∞ | Long range view |
| Map | 3 | - | ∞ | Show objectives |

### Multiplayer (Alpha)
- LAN/Hamachi support (up to 4 players)
- One player as Rake, others as Hunters
- Rake player has custom movement (RM_RakeMovement)

## Save/Load System
Persists via PlayerPrefs:
- Player health, weapons, ammo
- Rake health
- Camera positions & states
- Trap positions
- World items, sound triggers, screemers
- Window break states
- Current level

## Key Differences from Original
1. **Modern Unity API**: Updated from Unity 5.0 to 2022+ compatible code
2. **Clean Architecture**: Separated concerns, dependency injection ready
3. **Event System**: NotificationsManager for decoupled communication
4. **Multiplayer Ready**: NetworkedPlayer base for LAN play
5. **Extensible**: Easy to add new weapons, AI states, items

## Setup Instructions
1. Create Unity 2022+ project
2. Copy `Assets/Scripts` into project
3. Set up Input Manager with axes: Horizontal, Vertical, Run, Jump, Fire1, Fire2, Reload, Use, Inventory, Flashlight, Cancel
4. Create layers: Player, Rake, Animal, Interaction, RemotePlayer
5. Set up tags: Player, Rake, Animal
6. Configure NavMesh for AI navigation
7. Assign prefabs in Inspector for managers

## Decompiled Source Reference
Original source: `D:\Rake Multiplayer\Decompiled_Source\Assembly-CSharp.decompiled.cs`
Architecture notes: `D:\Rake Multiplayer\Decompiled_Source\ARCHITECTURE_NOTES.md`

## Weather System
**NOT MODIFIED** per requirements. Minimal RainManager included for compatibility only.