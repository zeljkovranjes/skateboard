# Changelog

## 2026-10-21
Title: First Ride
### Added
# Riding
Rideable skateboard - a Sandbox entity (Vehicles) anyone can spawn and get on with Use: their own player is put away (movement, physics body, body, tools; its inventory stays on for the spawn menu), a skater in their outfit (clothes, body groups and skin copied from their body) rides the deck, followed by the skate camera, and Use again steps off beside the board. Getting off waits for the press that got on to be let go
Controls - every skate control on Sandbox's own input actions, for keyboard and mouse and a controller (A push, B brake, left trigger powerslide, left stick click pump, X on and off)
Bails - the skater ragdolls with the board's speed; once the ragdoll comes to rest (six seconds at most) the player is back on foot where it lay, and the board stays where it rolled. A runout (riding side-on into a curb or wall) leaves the player on foot where they stepped off
Drops - a flat landing rides away from about 11 m (Skateboard.HardestLanding 15 m/s); a tipped one bails
# Networking
Riding in multiplayer - the host decides who rides; the rider's client owns and simulates the board (no input round trip, no cost to the host) and keeps it until it comes to rest; if that player leaves the host takes it back. Every client dresses every rider in its player's outfit, hides that player's body and sees every bail
# Performance
Sleeping boards, distance posing, map gathering of only nearby shapes, faster edge welding and optimized local builds: a sleeping board costs nothing, a ridden one about 1.3 ms per physics step on a dense level, another player's rider about 0.14 ms a frame up close and less with distance
# Animation
The skater's clips load as the board appears, not as someone gets on, and the skater is never drawn in its bind pose
