using Sandbox;

namespace Skateboarding.Engine;

/// <summary>This frame's presses of the rider's own keyboard actions.</summary>
internal readonly record struct RiderKeys( bool Bail, bool Gesture, bool Mount, bool Pickup, bool Jump, bool Sprint, bool Throw,
	bool MountHeld )
{
	/// <summary>
	/// Reads the keyboard and mouse alone, whatever device the engine counts as last; with keyboardOnly false (a rider
	/// without a controller profile) the actions are read as the engine merges them with the controller's buttons.
	/// </summary>
	internal static RiderKeys Read( bool keyboardOnly )
	{
		using var scope = keyboardOnly ? Input.PlayerScope( Input.PlayerIndex.KeyboardAndMouse ) : null;
		// Getting on and off, picking up, dropping and throwing are the board's own (its use and throw controls); the rider
		// reads the bail (its key, or the controller's bail button without a profile) and, on foot with the board in hand,
		// the game's own jump and sprint.
		var bail = SkateBindings.Pressed( "bail" ) || !keyboardOnly && SkateBindings.PadPressed( "bail" );
		return new RiderKeys( bail, false, false, false, Input.Pressed( SandboxActions.Jump ), Input.Down( SandboxActions.Sprint ),
			false, false );
	}
}
