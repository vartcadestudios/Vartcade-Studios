# Naming Conventions — Vartcade Studios

## Folders
PascalCase. e.g. `Scripts`, `Prefabs`, `Environment`

## Scripts
PascalCase, matching the class name exactly.
e.g. `PlayerController.cs` contains `class PlayerController`

## Prefabs
PascalCase, optionally prefixed by category for clarity in flat lists.
e.g. `UI_MainMenuButton`, `Enemy_Goblin`, `Env_Tree01`

## Scenes
PascalCase, grouped by purpose.
e.g. `Boot`, `MainMenu`, `Level_01`, `Level_02`

## Scene Hierarchy Parents
Wrapped in square brackets to visually separate structural objects from actual GameObjects.
e.g. `[Managers]`, `[Environment]`, `[UI]`

## Git Branches
lower-case, hyphenated, prefixed by type.
e.g. `feature/scene-architecture`, `bugfix/enemy-collision`, `chore/gitignore-update`

## Commits
Short imperative summary, present tense.
e.g. `Add scene hierarchy scaffold`, `Fix player controller null ref`
