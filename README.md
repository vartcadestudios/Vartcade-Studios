# Vartcade Studios

Unity 2D project repository.

## Branch Strategy

- `main` — protected, stable/release branch. No direct commits; merge via reviewed PR only.
- `develop` — active integration branch. Feature branches merge here first.
- `feature/<name>` — one branch per task or feature (e.g. `feature/scene-architecture`).

**Protection rules**
- Pull request before merging
- Require at least 1 approval
- Disallow force-pushes
- Disallow branch deletion

## Folder Structure

```
Assets/
├── _Project/
│   ├── Art/
│   │   ├── Sprites/
│   │   ├── Animations/
│   │   ├── Materials/
│   │   └── Fonts/
│   ├── Audio/
│   │   ├── Music/
│   │   └── SFX/
│   ├── Prefabs/
│   │   ├── Characters/
│   │   ├── Environment/
│   │   └── UI/
│   ├── Scenes/
│   │   ├── Boot/
│   │   ├── Levels/
│   │   └── UI/
│   ├── Scripts/
│   │   ├── Data/         
│   │   ├── Controllers/   
│   │   ├── Managers/      
│   │   ├── UI/
│   │   └── Utils/
│   ├── Settings/        
│   └── Resources/
└── ThirdParty/            
```

## Scene Hierarchy Convention

Every scene should be organized under empty parent GameObjects (no components attached to the parents themselves):

```
[Managers]
├── GameManager
├── AudioManager
├── SceneLoader
└── SaveManager

[Environment]
├── Background
├── Tilemap
├── Obstacles
└── SpawnPoints

[Entities]
├── Player
├── Enemies
└── Pickups

[UI]
├── Canvas_HUD
├── Canvas_Menus
└── EventSystem

[Cameras]
└── MainCamera

[Lighting]
└── GlobalLight (2D)
```

See `NAMING_CONVENTIONS.md` for naming rules.
