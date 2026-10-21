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
		// On the board only: getting on and off is the board's own press, and there is no walking, picking up or
		// throwing, so only the jump is read.
		return new RiderKeys( false, false, false, false, Input.Pressed( SandboxActions.Ollie ), false, false, false );
	}
}
