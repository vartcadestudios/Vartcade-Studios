# cards

The design team's card illustrations (received Nov 26 in the reference pack).

## Decision: keep them as images, not vectors
The cards are finished 1240 x 1748 painted illustrations. Vector tracing would flatten their
texture and lighting, so they stay as images. "Pixelated" cards came from scaling, not the art:
the game copies are exported at about 2x their largest on-screen size (640 x 902), sharpened a
little, with mipmaps so the small table cards stay smooth.

## Folders
```
cards/
└── originals/   full size (1240 x 1748, JPG q90), renamed by what each card is
    ├── Fronts/  front_<kind or action>
    ├── Backs/   back_<kind>_<supply|trash|key>, back_flashlight, back_night_skip, back_day_skip
    └── Days/    day_01..day_10, night_01..night_10
```
Game copies: `Assets/_Project/Resources/Cards/` (same names, 640 x 902), loaded by `CardArt.cs`.

## Which file is which (original name -> game name)
| Original | Game name | Card |
|---|---|---|
| Front cards/Supply - 1 | front_medicine | Medical Supply |
| Front cards/Supply - 2 | front_food | Food Supply |
| Front cards/Supply - 3 | front_water | Water Supply |
| Front cards/Supply - 4 | front_entertainment | Entertainment Supply |
| Front cards/Swap (art says SWITCH) | front_swap_deck | Swap Deck: swap hands with one player |
| Front cards/Switch (art says EVERYONE SWAP) | front_everyone_switches | Everyone Switches |
| Front cards/Reverse | front_reverse_turn | Reverse Turn |
| Front cards/Skip | front_skip_next_player | Skip Next Player |
| Front cards/Plus | front_everyone_plus_one | Everyone +1 Card |
| Back cards/<Kind>_<Supply/Trash/Key> | back_<kind>_<supply/trash/key> | Medical -> medicine |
| Back cards/Flashlight, Skip_Night, Skip_Day | back_flashlight, back_night_skip, back_day_skip | Night actions |
| 10 Days/Supply - 25, 27 ... 43 | day_01 ... day_10 | Day cards |
| 10 Days/Supply - 26, 28 ... 44 | night_01 ... night_10 | Night cards |

## Open questions
- Swap / Switch: matched by the printed title (to confirm).
- Morning cards with a Trash back have no kind, so for now they borrow a kind's Trash art by
  Morning type. A plain Trash back would be cleaner.
- Face-down back (draw pile): `back_facedown`, made by Claude from the Night card design with a
  TEN DAYS TO SUNDOWN plate. Replace it any time with a team-drawn back of the same name.

## Must stay true (game rules)
- The Bunker Key's FRONT is identical to its kind's Supply front (it uses the same file).
- Each kind's title is printed on the art, so kinds are never told apart by colour alone.
