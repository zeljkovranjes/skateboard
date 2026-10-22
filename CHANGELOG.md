# Changelog

## 2026-10-21
Title: First Ride
### Added
# Riding
Rideable skateboard - a Sandbox entity (Vehicles) anyone can spawn and get on with Use: their own player is put away (movement, physics body, body, tools; its inventory stays on for the spawn menu), a skater in their outfit (clothes, body groups and skin copied from their body) rides the deck, followed by the skate camera, and Use again steps off beside the board. Getting off waits for the press that got on to be let go
Controls - every skate control on Sandbox's own input actions, for keyboard and mouse and a controller (A push, B brake, left trigger powerslide, left stick click pump, X on and off)
Bails - the skater ragdolls with the board's speed; once the ragdoll comes to rest (six seconds at most) the player is back on foot where it lay, and the board stays where it rolled. A runout (riding side-on into a curb or wall) leaves the player on foot where they stepped off
Drops - a flat landing rides away from about 11 m (Skateboard.HardestLanding 15 m/s); a tipped one bails
Carrying the board - hold Use on the board to pick it up, then walk, run and jump with it: tap Use to throw it down and ride on, hold Use to drop it, left mouse (right trigger) to throw it. On foot with it the skater answers the controls as the Sandbox player does (full speed in about 0.15 s, a stop in about 0.2 s, turns at speed), holds the deck between thumb and fingers and keeps it there, and the use prompt is gone while carrying or riding
Calling the board back - hold Use on foot, pressing nothing, and your board (the one you last rode or carried, or spawned) flies back to your hand from wherever it is: up to 2 km (RecallRange), through anything in the way, about a second from 30 m and 70 m/s from farther; the skater reaches out for it and catches it. It shakes where it lies first (0.6 s, lifting off the ground) and everyone hears it: a sound made in code as it plays (no sound files), a rumble and crackles of static as it shakes, a thunder crack and boom as it tears loose, a deep rising whoosh as it flies and a heavy boom as it lands. Not while riding or carrying, and only your own board (the host checks)
Clear screen - the HUD's hotbar and health are hidden while you ride the board and come back as you get off or carry it (kept hidden every frame of the ride, so the game's own menus cannot bring them back mid-ride); the rest of the HUD stays
Controls card - the board's keys shown top left the way Sandbox shows a tool's (title, description and key glyphs on its gradient, accent and grid): a riding card and a carrying card, for keyboard or controller with the player's own bindings; it shows as you get on or pick the board up, fades after 7 seconds and comes back whenever you stand still
Powerslides that stop - a held powerslide scrubs the board to a stop in about 2 s from a fast push, where it glided on sideways for over 4 s
# Networking
Riding in multiplayer - the host decides who rides; the rider's client owns and simulates the board (no input round trip, no cost to the host) and keeps it until it comes to rest; if that player leaves the host takes it back. Every client dresses every rider in its player's outfit, hides that player's body and sees every bail
# Performance
Sleeping boards, distance posing, map gathering of only nearby shapes, faster edge welding and optimized local builds: a sleeping board costs nothing, a ridden one about 1.3 ms per physics step on a dense level, another player's rider about 0.14 ms a frame up close and less with distance
# Animation
The skater's clips load as the board appears, not as someone gets on, and the skater is never drawn in its bind pose
Standing idle - standing with the board, the skater no longer repeats the same 13 s: a 39 s idle generated from it with GenMM
Flick tricks - the controls card's "Trick stick" (hold right mouse, then flick) is now "Flick tricks", which says what it does
Feet on the deck - the skater's shoes stand on the deck instead of floating up to 2 in over it, and lie flat on it over the tail
Smooth motion - every bone of the skater carries on from what was shown, its speed changing by no more than a budget in a frame: clip changes and pose jumps ease over instead of snapping
Stock walk - on foot the skater's hips and legs walk, run, start, stop and jump with the Citizen's own walk while its arms keep the board; on the board nothing changes
Grabs that hold - a hand that reaches the board in a grab (R, F or both) takes hold of the deck where it is and stays on it, turning with the board, until just before the landing; fingers no longer sink into the deck
Planted feet - feet stay where they stand on the deck instead of sliding back into each clip's stance at an ollie's wind-up and after landings
Catching the called board - as the board flies back, the carrying hand reaches out for it, open, and closes on it, and the board stays in the hand (it could float beside it after a call)
Flips and landings - the deck drops away from the feet as it flips and the feet lift clear of it, and a board adjust held through a landing settles under the feet in about 0.3 s instead of swinging round in 0.15 s
