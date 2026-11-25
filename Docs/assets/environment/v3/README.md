# Environment v3.2 (Nov 26): the picked look

Andre's picks from the Environment Drafts canvas, combined and redrawn by Claude. Not in the game yet:
these are the approved-direction drafts, built in layers so the same pictures can go into Unity (checklist E05-E08).

## What was picked
- **Room:** R-B (Filipino home bunker) with R-A's steel shelf, poster and pipes and R-C's portrait, children's
  drawings and peeling wallpaper. Spider web, crows on the wire, tally marks, card-art labels (Millenium Tuna,
  Peak, First Aid), the shelf empties over the 10 days.
- **Outside / menu:** M-A Filipino street (sari-sari store, jeepney, rooster, Silong billboard, houses with laundry,
  water tank on a roof platform, bunker hatch).
- **Props:** P01-P20, redrawn (proper jeepney and tandang).  **Motion:** A01-A10.  **Light:** L1-L5 built in.
- **Pixel details:** X01-X12 at 3 px cells (1080p).  **Overlays:** OV3, OV7, OV8 + OV10 (new warm balance).
- **Rondo:** each of the 8 results changes both scenes.

## Style rules (v3.2)
- Thin, sketchy ink (2-3 px at 1080p) with a faint second pass; grime, drips, rust and paper grain.
- Paper cut-out 2.5D (Paper Mario feel): every object has a pale paper edge and a soft shadow on the surface
  behind it; seated characters are turned slightly toward the middle; print-dot (halftone) shading in dark areas.
  Things painted or scratched on a wall (web, crack, tally, stains) get no paper edge.
- Cast: dark grey long-sleeved top by default; brass paper-fastener at the shoulder joint.
- Table: smaller (far edge y = 650 at 1080p, x 420-1500), banig mat in the middle, scratches, rings, spill, candle.

## Files
- `renders/` room and street for Day 1 / 5 / 9, Night and each Rondo result (1280x720 JPG previews).
- `props/` P01-P20 as transparent PNGs (400x300).
- `src/` the Python that draws everything (SVG drawn in code, rendered with cairosvg, textures in numpy).
  Paths inside point at Claude's workspace; change `OUT`, `CAST` and `ROOT` to rebuild elsewhere.
  `python3 room.py d1 night tornado` renders states; `python3 room.py hero` exports the moving layers.

## Canvas
"10 Days to Sundown: Environment Drafts" (claude.ai artifact), right-hand v3 column: THE GAME (20 s loop),
Main menu, Dial + wheel, Room/Street (moving), states, props, pixel, overlays, cast.

## Menu UI kit (v3.3, Nov 26)
`ui/` holds the main-menu pieces, drawn in the same paper style, after the zip's button references:
- `btn_play` (brick), `btn_bots` (cream), `btn_tutorial` (gold), `btn_shop` (teal): paper buttons with a bevel,
  a darker lip, brass rivets and a pale cut edge. `btn_small` and `btn_quit`: wood plank buttons.
  For Unity, 9-slice them with about 40 px borders so the rivets stay in the corners.
- `mirror_back` + `mirror_glass` + `mirror_frame`: the vanity mirror the avatar looks out of (the glass is an
  ellipse 400 x 510 centred at 260, 410 in the 520 x 720 frame). `plaque`: the name plate.
- `shop`: TINDAHAN stall with 8 locked slots (no clothing yet).
- `screen_frame`: 1920 x 1080 wooden frame with brass corners, inner shadow and a slight vignette; see-through middle.

## In the game (Nov 26, v3.3)
Approved by Andre and added to the branch. Everything falls back to the older code-drawn look if a sprite is missing.

| Where | What | Code |
|---|---|---|
| `Resources/RoomV3` | 12 painted room backdrops (Day 1-3, 4-7, 8+, Night, 8 Rondo results) + table, lamp, fan blades, spider (day and night) | `TableRoom.BuildArtRoom` |
| `Resources/StreetV3` | menu street + clouds, crows, laundry | `StreetBackdrop` |
| `Resources/UI3` | paper buttons (9-sliced), mirror, plaque, shop, screen frame | `MainMenuController` (art mode) |
| `Resources/Dial` | day/night porthole: disc, horizon, frame, tick | `DayNightDial` (top bar) |
| `Resources/Wheel` | Rondo wheel face, two bulb rims, hub, pointer | `RondoDial.BuildArt` |
| `Resources/Overlays` | OV7 paper, OV3 dusk, OV8 night (+ OV10 warm in code) | `ScreenOverlays`, Settings > Screen filters |
| `Resources/Puppets` | cast parts with a paper edge; each part casts a hard paper shadow | `Puppet` |

Layout changes: the table's far edge is now y = 650 (was 556), seats at x 520 / 960 / 1400, puppets 400 px tall
(were 300), side seats turned slightly in. The positions of the separate pieces are in `RoomArtLayout.cs`,
written by `src/export_game.py`. To re-export: `python3 export_game.py room|street|ui|dial|wheel|overlays`.
