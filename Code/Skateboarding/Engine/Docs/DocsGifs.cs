namespace Skateboarding.Engine.Docs;

/// <summary>
/// The docs' clips (animated WebP): one short clip of each move, docs/clips/&lt;id&gt;.webp in the addon's files, with
/// what each one shows. A move whose clip is not there yet shows a placeholder in its place.
/// </summary>
internal static class DocsGifs
{
	/// <summary>Where the clips are, among the addon's files.</summary>
	internal const string Folder = "docs/clips";

	/// <summary>Every clip the docs show, and what it is to show.</summary>
	internal static readonly (string Id, string Shows)[] All =
	{
		("get-on-off", "Walk up to the board, step on, ride a few metres, step off"),
		("push", "Push off from standing and keep pushing up to speed, side view"),
		("brake", "Drag the back foot at speed until stopped, smoke off the heel, side view"),
		("steer", "Carve left then right at speed, from behind"),
		("kick-turn", "Nearly stopped, pivot the board round on its tail"),
		("fakie", "Roll backwards (fakie) after a slope or a revert, from the side"),
		("powerslide", "Powerslide to a stop from a fast push, from behind"),
		("revert", "Land and tap the slide key: a quick 180 on the ground"),
		("pump", "Pump through a curved ramp and gain speed, from the side"),
		("hippy-jump", "Jump straight up off the rolling board and land back on it"),
		("ollie", "Ollie on flat ground at speed, side view"),
		("nollie", "Nollie (popped off the nose) on flat ground, side view"),
		("plant", "Boneless: front hand (R) held on the ground, then push (W): foot planted, spring up"),
		("fastplant", "Fastplant: back hand (F) held on the ground, then push (W): foot planted, spring up"),
		("kickflip", "Kickflip on flat ground, side view, catch and land"),
		("heelflip", "Heelflip on flat ground, side view"),
		("shuvit", "Pop shove-it (backside) on flat ground"),
		("fs-shuvit", "Frontside pop shove-it on flat ground"),
		("360-shuvit", "360 pop shove-it on flat ground"),
		("varial-kickflip", "Varial kickflip on flat ground"),
		("varial-heelflip", "Varial heelflip on flat ground"),
		("hardflip", "Hardflip on flat ground"),
		("inward-heelflip", "Inward heelflip on flat ground"),
		("360-flip", "360 flip (tre flip) on flat ground"),
		("laser-flip", "Laser flip on flat ground"),
		("board-adjust", "Board shifted under the feet in the air without a grab: Space held in the air"),
		("late-flip", "Ollie, then a late shove-it on the way down: Space tapped in the air (off a kicker for height)"),
		("dark-catch", "Flip caught upside down (dark catch), grip side down"),
		("front-flip", "Off a ramp: grab and front flip, land"),
		("back-flip", "Off a ramp: grab and back flip, land"),
		("grab-indy", "Indy grab (back hand, toe side) off a ramp"),
		("grab-melon", "Melon grab (front hand, heel side) off a ramp"),
		("grab-mute", "Mute grab (front hand, toe side) off a ramp"),
		("grab-stale", "Stalefish grab (back hand, heel side, behind) off a ramp"),
		("grab-nose", "Nose grab off a ramp"),
		("grab-tail", "Tail grab off a ramp"),
		("grab-crail", "Crail grab (both hands toward the nose) off a ramp"),
		("grab-nosecrail", "Nose crail grab off a ramp"),
		("grab-seatbelt", "Seatbelt grab off a ramp"),
		("grab-rocket", "Rocket air (both hands on the nose) off a ramp"),
		("christ-air", "Christ air: a grab let go, arms out, off a ramp"),
		("superman", "Superman: both hands on the board, then the catch, legs out, off a kicker"),
		("no-foot", "No-foot air: the catch during a nose grab, off a kicker"),
		("one-foot", "One-foot grab: back foot off the board in the air"),
		("manual", "Manual on flat ground for a few metres"),
		("nose-manual", "Nose manual on flat ground for a few metres"),
		("50-50", "50-50 grind along a rail, from the side"),
		("5-0", "5-0 grind (back truck) along a rail"),
		("nosegrind", "Nosegrind (front truck) along a rail"),
		("boardslide", "Boardslide: board across the rail, between the trucks"),
		("noseslide", "Noseslide: board across the rail on its nose"),
		("tailslide", "Tailslide: board across the rail on its tail"),
		("grind-pop-out", "Grind a rail and ollie off it: Space tapped on the rail"),
		("slope", "Roll down a slope and pick up speed, from the side"),
		("drop-in", "Board in hand on a ramp's deck facing over the lip, tap E, drop in and ride away"),
		("hand-plant", "Hand plant on a quarter pipe lip, drop back in"),
		("coffin", "Coffin: lying flat on the rolling board"),
		("carry", "Pick the board up and walk with it"),
		("throw", "Throw the board ahead while carrying it"),
		("call-board", "Hold Use with no board in hand: it shakes and flies back to the hand"),
		("bail", "Land a flip badly and bail, ragdoll with the board's speed"),
		("runout", "Ride side-on into a curb and run it out on foot"),
	};

	/// <summary>The clip's path among the addon's files.</summary>
	internal static string Path( string id ) => $"{Folder}/{id}.webp";
}
