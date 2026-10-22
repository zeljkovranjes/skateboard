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
own tool info, shows the board's keys top left (riding or carrying, keyboard or controller, with your bound keys) as you
get on or pick the board up, fades after a few seconds and comes back whenever you stand still.

### Keyboard and mouse

| Action | Key |
| --- | --- |
| Push / brake | W / S |
| Steer | A / D |
| Ollie (hold to crouch, let go to pop) | Space |
| Kickflip / heelflip (hold with Space) | Alt / X |
| Flick tricks (hold, then move the mouse: pull down, flick up) | Right mouse |
| Catch a flip | Left mouse |
| Powerslide (hold) / revert (tap) | Shift |
| Pump | Ctrl |
| Grab front / back (in the air) | R / F |
| Hand plant | G |
| Hippy jump | Hold W and S together, then let go |
| Get on / off | E |

### Controller

| Action | Button |
| --- | --- |
| Steer | Left stick |
| Flick tricks (pull down, flick up or to a side) | Right stick |
| Push / brake | A / B |
| Powerslide (hold) / revert (tap) | Left trigger |
| Pump | Left stick click |
| Get on / off | X |

Y and the bumpers are left to the game (the bumpers open the spawn and inspect menus).

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
  a boom as it lands in your hand, all made in code (the addon ships no sound files). Not while you ride or carry a board,
  and not while someone else has it.
- **Powerslides.** Hold Shift (left trigger) at speed and the board turns across and scrubs to a stop in about two
  seconds from a fast push; tap it to revert.
- **Brake smoke.** Brake hard at speed (S) and smoke kicks up off the heel of the shoe you drag: white off the shoe,
  greying as it drifts up and fades behind you, more the faster you go. It is drawn in code; the addon ships no
  texture for it.
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

The board model is a placeholder (`lazyduchess.sboxdeck`); swap `Skateboard.BoardModel` on the prefab for your own.
