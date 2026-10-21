# Skateboard

A rideable skateboard for s&box's Sandbox mode. Spawn it from the spawn menu, walk up to it and press **Use** to get
on. Your skater wears your outfit, pushes, carves, ollies, flips, grabs, grinds marked rails and powerslides; bail and
you ragdoll, then get up on foot where you landed. It works in multiplayer and is built for busy servers.

## Using it

1. Open the spawn menu and find **Skateboard** under the addon's entities (category **Vehicles**).
2. Walk up to the board and press **Use** (E, or X on a controller).
3. Press **Use** again to step off; you stand beside the board.

While you ride, your own Sandbox player is put away: its movement, tools and body are switched off, and the skater on
the deck takes its place, followed by the skate camera. The spawn menu and the rest of the game's UI keep working.

### Keyboard and mouse

| Action | Key |
| --- | --- |
| Push / brake | W / S |
| Steer | A / D |
| Ollie (hold to crouch, let go to pop) | Space |
| Kickflip / heelflip (hold with Space) | Alt / X |
| Trick stick (hold, then move the mouse: pull down, flick up) | Right mouse |
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
| Trick stick (pull down, flick up or to a side) | Right stick |
| Push / brake | A / B |
| Powerslide (hold) / revert (tap) | Left trigger |
| Pump | Left stick click |
| Get on / off | X |

Y and the bumpers are left to the game (the bumpers open the spawn and inspect menus).

## How it behaves

- **Bails.** Land badly, hit the ground too hard or come down head first and the skater ragdolls with the board's
  speed. Once the ragdoll comes to rest (six seconds at most) you are back on foot, as your Sandbox player, where it
  lay; the board stays where it rolled.
- **Runouts.** Riding into a curb or a wall side-on makes the skater step off and run it out: you are left on foot
  where you stepped off. Ollie over curbs.
- **Drops.** A flat, square landing rides away from drops of about 11 m (a 15 m/s landing); land tipped and you bail.
  The board's `Skateboard.HardestLanding` sets the limit.

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
