using Sandbox;
using Skateboarding.Engine;

namespace Skateboarding.Systems;

/// <summary>
/// The trick keys while this client's player rides the board. They are the game's own keys for its spawn menu and
/// inventory slots, so each frame of a ride, before anything else reads input, they are read here and then cleared for
/// the rest of the frame: riding never opens the spawn menu or changes the player's tool, and the board reads the keys
/// from here. A ride keeps them by renewing a short lease every frame, so a ride that ends any way at all gives them back.
/// </summary>
public sealed class RideKeys : GameObjectSystem<RideKeys>
{
	// Seconds (real time) the keys stay taken after the ride last renewed its lease.
	private const float Lease = 0.25f;

	private static float _until;
	private static int _held;

	/// <summary>Takes the keys at the very start of each frame.</summary>
	public RideKeys( Scene scene ) : base( scene ) => Listen( Stage.StartUpdate, -10000, Take, "RideKeys" );

	/// <summary>Keeps the keys taken a moment longer: called every frame this client's player rides.</summary>
	internal static void Ride() => _until = RealTime.Now + Lease;

	/// <summary>True while the trick key at this index (<see cref="SandboxActions.TrickKeys"/>) is held on a ride.</summary>
	internal static bool Held( int index ) => (_held & (1 << index)) != 0;

	private void Take()
	{
		if ( Scene.IsEditor )
			return;
		_held = 0;
		if ( RealTime.Now > _until )
			return;
		using var scope = Input.PlayerScope( Input.PlayerIndex.KeyboardAndMouse );
		for ( var i = 0; i < SandboxActions.TrickKeys.Length; i++ )
		{
			if ( Input.Down( SandboxActions.TrickKeys[ i ] ) )
				_held |= 1 << i;
			Input.Clear( SandboxActions.TrickKeys[ i ] );
		}
	}
}
