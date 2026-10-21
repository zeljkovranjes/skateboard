namespace Skateboarding.Engine;

/// <summary>
/// The skate controls on Sandbox's own input actions: an addon cannot add actions to the game it runs in, so each skate
/// control reads one the game already has. A player riding has their own object switched off, so these never also
/// drive their tools or movement. Keyboard and mouse, then the controller (in its own input scope, the same actions
/// read its buttons).
/// </summary>
internal static class SandboxActions
{
	// Keyboard and mouse.
	internal const string Push = "Forward";
	internal const string Brake = "Backward";
	internal const string Ollie = "Jump";
	internal const string Flick = "Attack2";
	internal const string Catch = "Attack1";
	internal const string FlipLeft = "Walk";
	internal const string FlipRight = "View";
	internal const string Slide = "Run";
	internal const string Pump = "Duck";
	internal const string GrabFront = "Reload";
	internal const string GrabBack = "Flashlight";
	internal const string HandPlant = "Drop";

	/// <summary>Gets on and off the board (keyboard E, controller X: Sandbox's Use).</summary>
	internal const string GetOff = "Use";

	// The controller's buttons by position, as Sandbox binds them. Y has no action there, and the bumpers open its spawn
	// and inspect menus, so the board leaves them alone.
	internal const string PadSouth = "Jump";
	internal const string PadEast = "Reload";
	internal const string PadWest = "Use";
	internal const string PadLeftStick = "Run";
	internal const string PadRightStick = "View";
	internal const string PadLeftTrigger = "Attack2";
	internal const string PadUp = "Flashlight";
	internal const string PadLeft = "SlotPrev";
	internal const string PadRight = "SlotNext";
	internal const string PadDown = "Undo";

	// The controller's skate controls: A pushes, B brakes, the left trigger slides and clicking the left stick pumps.
	internal const string PadPush = PadSouth;
	internal const string PadBrake = PadEast;
	internal const string PadSlide = PadLeftTrigger;
	internal const string PadPump = PadLeftStick;
}
