# Ten Days to Sundown: Asset Source Folder

## Overview
Source files for everything the studio designs: wireframes, character drafts and traces, card art, brand art, palette and the reference-pack template. This folder lives under `Docs/` on purpose, outside Unity's `Assets/` folder (see "Why here" below). Finished, game-ready sprites are copied into `Assets/_Project/Art/` when they are used in a scene.

## Structure
```
Docs/assets/
├── wireframes/      UI/UX wireframes, one folder per version (v0.1 = initial idea draft)
├── palette/         merged palette: .json (code), .gpl (Krita / GIMP / Aseprite), .png (swatches)
├── characters/
│   ├── drafts/      the design team's original drawings
│   └── trace_tests/ vector trace tests (SVG) and the comparison image
├── cards/           card illustrations: originals and which file is which
├── brand/           Silong Shelters logo, sun mascot, posters (coming)
└── references/      reference-pack template and STYLE_NOTES.md (what we take from each folder)
```

## Rules
- **Masters vs game files:** keep SVG / PSD / full-size PNG masters here. Export PNG at 2x into `Assets/_Project/Art/Sprites/...` for Unity.
- **Names:** lowercase with underscores, what it is first: `card_food_front.svg`, `thin_upperarm_L_line.png`.
- **Versions:** never overwrite a reviewed file; add `_v2`, `_v3`.
- **Originality:** only the team's own art lives here. Reference images from other games stay in the reference zip, never in the repo or the game.

## Why here (and not a root folder called `assets`)
Unity's project folder is `Assets/`. Windows and macOS treat `assets` and `Assets` as the same name, so a root `assets/` folder would merge with or break the Unity project on teammates' machines. `Docs/assets/` avoids that. Unity also does not try to import source files placed here.

## Status (2026-11-26)
| Folder | Status |
|---|---|
| wireframes | v0.1 done; screens 01 to 09 built in Unity on update-layout (Nov 24); review pending (W19) |
| palette | done |
| characters | drafts in; full part sets pending (checklist C15, C16) |
| cards | 44 illustrations in (fronts, backs, Day/Night); kept as images, in the game since Nov 26 |
| brand | brief ready: Silong Shelters, rubber hose sun mascot (A20) |
| references | filled Nov 26; takeaways in references/STYLE_NOTES.md (images kept out of the repo) |
