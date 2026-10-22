# Skateboard

A rideable skateboard for s&box's Sandbox mode. Spawn it from the spawn menu, walk up to it and press **Use** to get
on. Your skater wears your outfit, pushes, carves, ollies, flips, grabs, grinds marked rails and powerslides; bail and
you ragdoll, then get up on foot where you landed. It works in multiplayer and is built for busy servers.

## Using it

1. Open the spawn menu and find **Skateboard** under the addon's entities (category **Vehicles**).
2. Walk up to the board and tap **Use** (E, or X on a controller) to get on, or hold it to pick the board up.
3. Press **Use** again to step off; you stand beside the board.
4. Anywhere on foot, hold **Use** while looking at nothing you can press: your board flies back to your hand from
   wherever it is, like a hammer called home, and you carry it.

While you ride, your own Sandbox player is put away: its movement, tools and body are switched off, and the skater on
the deck takes its place, followed by the skate camera. The HUD's hotbar and health are hidden while you ride the board
and come back when you get off or carry it; the rest of the HUD stays as it is. A controls card, styled like Sandbox's
own tool info, shows the board's main keys top left (riding or carrying, keyboard or controller, as you have them bound,
the docs key first) as you get on or pick the board up, fades after a few seconds and comes back whenever you stand
still.

Press **Q** while riding or carrying the board (**View** on a controller) to open the docs: every move, what it is and
how to do it on keyboard and controller, with a clip of each. Every key is rebindable on the docs' **Keybinds** page; your
keys are saved and shown everywhere (the docs and the controls card) as you have them.

The board is a salvaged street sign: a bent green S&BOX AVE sign on roller-skate wheels and door-hinge axles, with 38
stickers, 270 community portraits underneath and a salvaged mechanical heart in a glass case, whose oil sloshes as you
ride. Carrying it, press **I** (D-pad up) to look it over in the board editor: turn and zoom it, click a portrait
underneath to see who it is, and pick **Edit** at the top to colour it: the sign's paint, the wheels, the axles and the
heart's oil. Everyone sees your colours, and they are saved on your computer, so any board you pick up or ride takes
them, next time too. **Reset** brings back the board as delivered.

### Keyboard and mouse (defaults)

| Action | Key |
| --- | --- |
| Push / brake / steer | W / S / A, D |
| Ollie / nollie (hold to crouch, let go to pop) | Space / Z |
| Kickflip / heelflip (held as you let go of Space or Z) | Alt / X |
| Trick keys: kickflip, shove-it, heelflip, varial kickflip, 360 flip, hardflip, varial heelflip, inward heelflip, laser flip, frontside shove-it, 360 shove-it (hold Z with one for its nollie version) | 1 to 0, - |
| Flick tricks (hold, then move the mouse like a stick) | Right mouse |
| Dark catch (during a flip) | Left mouse |
| Grab front / back hand (in the air) | R / F |
| Manual / nose manual (hold; on a rail, tip to the tail / nose) | C / B |
| Powerslide (hold) / revert (tap) | Shift |
| Pump | Ctrl |
| Hand plant | G |
| Hippy jump | H (or hold W and S, then let go) |
| Coffin | Y |
| Bail | T |
| Get off | E |
| Docs | Q |

While you ride the board has the keyboard to itself (Sandbox's menus, hotbar and undo wait until you get off); voice
chat, chat and the scoreboard stay the game's. V, Tab, Enter, Esc and ` can't be bound.

### Controller

| Action | Button |
| --- | --- |
| Steer (with a grab held: up front flip, down back flip) | Left stick |
| Tricks, manuals (held part way), rail tips, grab picks | Right stick |
| Push / brake / hippy jump | A / B / A and B |
| Powerslide or revert; front-hand grab in the air | Left trigger |
| Back-hand grab | Right trigger |
| Pump (left stick held forward); one foot off in the air | Left stick click |
| Dark catch | Right stick click |
| Hand plant / coffin / ground grab / bail | D-pad up / right / left / down |
| Get off | X |
| Docs | View |

### Carrying the board

| Action | Keyboard and mouse | Controller |
| --- | --- | --- |
| Walk, run | W A S D | Left stick |
| Jump | Space | A |
| Throw it down and ride on | Tap E | Tap X |
| Drop it | Hold E | Hold X |
| Throw it | Left mouse | Right trigger |
| Call it back from anywhere | Hold E (on foot, not carrying) | Hold X |

On foot with the board the skater answers the controls as your Sandbox player does: full speed in about 0.15 s, a
stop in about 0.2 s, and turns at speed. Its legs walk and run with the Citizen's own stride while its arms keep the
board.

## How it behaves

- **Bails.** Land badly, hit the ground too hard or come down head first and the skater ragdolls with the board's
  speed. Once the ragdoll comes to rest (six seconds at most) you are back on foot, as your Sandbox player, where it
  lay; the board stays where it rolled.
- **Runouts.** Riding into a curb or a wall side-on makes the skater step off and run it out: you are left on foot
  where you stepped off. Ollie over curbs.
- **Drops.** A flat, square landing rides away from drops of about 11 m (a 15 m/s landing); land tipped and you bail.
  The board's `Skateboard.HardestLanding` sets the limit.
- **Calling it back.** Your board is the one you last rode or carried (or spawned). Held Use calls it from up to
  2 km away (`RideableSkateboard.RecallRange`), through anything in the way: it shakes where it lies for a moment
  (`RecallShake`, 0.6 s), lifting off the ground, then flies about a second from 30 m and at 70 m/s from farther. Everyone
  hears it: a rumble with crackles of static as it shakes, a thunder crack as it tears loose, a deep whoosh as it flies and
  a boom as it lands in your hand, all made in code. Not while you ride or carry a board,
  and not while someone else has it.
- **Powerslides.** Hold Shift (left trigger) at speed and the board turns across and scrubs to a stop in about two
  seconds from a fast push; tap it to revert.
- **Trick keys.** Hold Q, 1, 2, 3 or 4 to wind up and let go to pop that trick; the longer you hold, the higher it
  pops. While you ride, Q and the number keys are the board's (the spawn menu and the hotbar wait until you get off),
  and the keys show in the bottom left corner.
- **One trick at a time.** A trick plays out, landing and all, before the next can pop: whatever you press during a
  landing waits for it to end.
- **Brake smoke.** Brake hard at speed (S) and smoke kicks up off the heel of the shoe you drag: white off the shoe,
  greying as it drifts up and fades behind you, more the faster you go. It is drawn in code; the addon ships no
  texture for it.
- **Sounds.** Every board sounds as it rides, from real skateboarding recordings shaped as it goes: the wheels roll
  in the recording of the ground under them (park concrete, a city street, wooden planks, tiles, a smooth floor; grass,
  dirt and sand muffled and rustling, and each ground's own footstep sound mixed in), faster, higher and louder with
  speed, and never heard to loop. The tail pops, landings thud as hard as you come down, the feet catch a flip, pushes
  scuff, trucks clank onto a metal rail and grind it or crack onto a ledge, a deck slides, powerslides scrub, a dragging
  shoe brakes and a board on its own tumbles. A different take every time, pitched a little differently. Everyone hears
  every board; nothing is sent over the network for it (each client works it out from what it already knows).
  Turn them up or down on the docs' **Sound** page (Q, then Sound): all of them, or the rolling, tricks and grinds on
  their own, from silent to 150%; your volumes are kept for next time.
- **Flick tricks on the mouse.** While right mouse is held the mouse is the trick stick. Like a thumbstick let go, it
  springs back to centre when the mouse rests; only a windup, pulled down (or up, for a nollie) all the way, stays held
  until you flick.

## Multiplayer

- The host decides who rides each board. The rider's own client then owns and simulates the board, so steering has
  no round trip and the host pays for no one's board. The board stays with its last rider until it comes to rest; if
  that player leaves, the host takes it over.
- Everyone sees every rider, dressed in that player's outfit, and every bail.
- Boards nobody rides fall asleep once they lie still and cost nothing until someone rides, moves or resets them.
- Riders far from your camera are posed less often (every 2nd frame from 12 m, 4th from 24 m, 8th from 48 m).

On a dense level a ridden board's physics costs its rider about 1.3 ms per physics step; a sleeping board costs
nothing, and another player's rider about 0.14 ms a frame up close, less with distance.

## Building and publishing

- `Code/Skateboarding` is the skate motion code this addon runs on; `Components/RideableSkateboard.cs` and
  `Engine/SandboxActions.cs` are the addon's own (getting on and off, networking, Sandbox's input actions).
- The skater's clips live in `Assets/skater` and ship with the package (`"Resources": "skater/*"` in the project).
- Local builds are optimized (`"ReleaseMode": "Release"`), as published packages always are.
- Publish from the s&box editor (the project's **Publish** button); the package is `notpointless.chomnr_skateboard`.
  Make sure `Libraries/` holds nothing before publishing: anything there is packaged with the addon.

The board is the addon's own model in `Assets/models/skateboard_game`: `skateboard.vmdl` (`Skateboard.BoardModel`) and
its lower levels of detail `skateboard_lod1.vmdl` and `skateboard_lod2.vmdl` (`Skateboard.BoardModelLevels`), switching at
the thresholds of `skateboard.vmdl`'s LOD groups. Swap them on the prefab for your own.

## Credits

The board's sounds (`Assets/sounds/skate`) are cut from these recordings, all released under CC0 (public domain) on Freesound:

- "Skateboard_Kick_Flip.wav" by mbezzola, https://freesound.org/s/105075/
- "Skateboard_Ollie_Pop_Shove-It.wav" by mbezzola, https://freesound.org/s/105076/
- "Longboard hitting the ground" by gerardcatala, https://freesound.org/s/153285/
- "Skateboard Jump.wav" by tjandrasounds, https://freesound.org/s/196705/
- "Skateboard rolling down wooden bridge.wav" by Bruno_Garcez, https://freesound.org/s/243762/
- "Skateboard hitting metal rail.wav" by Bruno_Garcez, https://freesound.org/s/243766/
- "Skateboard Drop.1.wav" by rokenjocu, https://freesound.org/s/262081/
- "Skateboarding Indoors" by neotronable, https://freesound.org/s/346380/
- "Skateboard_Grinds.wav" by BlackNeon1234, https://freesound.org/s/348326/
- "BPS-SKATEBOARDING RIDING-Concrete Skatepark-Transition-Cruising.wav" by bspiller5, https://freesound.org/s/478162/
- "BPS-SKATEBOARDING RIDING-City Street-Pushing-Fast-Long Strides-1.wav" by bspiller5, https://freesound.org/s/478165/
- "BPS-SKATEBOARDING GRINDS-Concrete Halfpipe-Truck Grinds.wav" by bspiller5, https://freesound.org/s/478166/
- "skateboard 360 flip" by nolimitkid, https://freesound.org/s/515225/
- "popping up skateboard" by nolimitkid, https://freesound.org/s/515226/
- "dropping skateboard" by nolimitkid, https://freesound.org/s/515227/
- "skateboard ollie" by nolimitkid, https://freesound.org/s/515229/
- "skateboard kickflip" by nolimitkid, https://freesound.org/s/515230/
- "skateboard drop" by nolimitkid, https://freesound.org/s/515231/
- "Powerslide" by jarrondesnudo, https://freesound.org/s/581378/
- "powerslide (2)." by jarrondesnudo, https://freesound.org/s/581379/
- "powerslide 3" by jarrondesnudo, https://freesound.org/s/581380/
- "skateboard grinding on metal rail.wav" by 21100495, https://freesound.org/s/655371/
- "skateboard gring on rail slide mix.wav" by 21100495, https://freesound.org/s/655376/
- "Skate tuile" by Piement_infernal, https://freesound.org/s/713530/
- "SPRTSkate_Drop-skateboard-ground-concrete_01" by nico-audio, https://freesound.org/s/717547/
