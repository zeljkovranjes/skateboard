using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Skateboarding.Engine;

namespace Skateboarding.Systems;

/// <summary>
/// The keyboard while this client's player rides the board. The board reads its keys as keys (SkateBindings), so for
/// each frame of a ride, before anything else reads input, the game's own input actions are cleared: whatever keys the
/// player rides with, the spawn menu, the inspect menu, undo and the hotbar stay shut. Only voice chat, chat and the
/// scoreboard are left to the game. A controller's buttons are read through the game's actions, which the board needs,
/// so on a controller only the buttons that open the game's menus or undo are taken, the moment they go down (the
/// board's bail is on undo's). A ride keeps all this by renewing a short lease every frame, so a ride that ends any way
/// at all gives the keyboard back. Carrying the board only the docs' key is taken, so it opens the docs and nothing of
/// the game's; on foot without the board every key is the game's.
/// </summary>
public sealed class RideKeys : GameObjectSystem<RideKeys>
{
	// Seconds (real time) the keyboard stays taken after the ride last renewed its lease.
	private const float Lease = 0.25f;

	// The game's actions a ride leaves to the game: voice chat, chat and the scoreboard.
	private static readonly HashSet<string> Kept = new( StringComparer.OrdinalIgnoreCase ) { "Voice", "Chat", "Score" };

	// The controller buttons a ride takes from the game as they go down: the spawn and inspect menus (the bumpers) and
	// undo (the board's bail).
	private static readonly string[] PadTaken = { "SpawnMenu", "InspectMenu", "Undo" };

	private static float _until;

	/// <summary>Takes the keys at the very start of each frame; at its very end, moves the simulated keys on.</summary>
	public RideKeys( Scene scene ) : base( scene )
	{
		Listen( Stage.StartUpdate, -10000, Take, "RideKeys" );
		Listen( Stage.FinishUpdate, 10000, EndFrame, "RideKeysEnd" );
	}

	/// <summary>Keeps the keyboard taken a moment longer: called every frame this client's player rides.</summary>
	internal static void Ride() => _until = RealTime.Now + Lease;

	/// <summary>True while a ride has the keyboard.</summary>
	internal static bool Riding => RealTime.Now <= _until;

	/// <summary>The controller's bail button went down this frame (it is the game's undo, which a ride takes).</summary>
	internal static bool PadBail { get; private set; }

	/// <summary>True while this client's player has the board: riding or carrying it (the board's own controls answer).</summary>
	internal static bool WithBoard { get; private set; }

	private void Take()
	{
		if ( Scene.IsEditor )
			return;
		PadBail = false;
		var carrying = !Riding && Scene.GetAllComponents<Components.SkateRider>()
			.Any( rider => !rider.IsProxy && rider.Mode == SkateRiderMode.Carrying );
		WithBoard = Riding || carrying;
		if ( !Riding )
		{
			if ( carrying )
				TakeDocsKey();
			return;
		}
		PadBail = SkateBindings.PadActionPressed( SkateBindings.PadAction( "bail" ) );
		using ( Input.PlayerScope( Input.PlayerIndex.Controller1 ) )
		{
			foreach ( var action in PadTaken )
			{
				if ( Input.Pressed( action ) )
					Input.ReleaseAction( action );
			}
		}
		// Cleared in the keyboard's own scope: cleared in the shared one, the controller's held buttons would be written
		// into the keyboard's actions.
		using var scope = Input.PlayerScope( Input.PlayerIndex.KeyboardAndMouse );
		foreach ( var action in Input.ActionNames )
		{
			if ( !Kept.Contains( action ) )
				Input.Clear( action );
		}
	}

	private void EndFrame()
	{
		if ( !Scene.IsEditor )
			SkateBindings.EndFrame();
	}

	// Carrying the board the docs' key is the docs': while it is held, any game action on the same key is cleared.
	private static void TakeDocsKey()
	{
		var key = SkateBindings.KeyOf( "docs" );
		if ( string.IsNullOrEmpty( key ) || !Input.Keyboard.Down( key ) )
			return;
		using var scope = Input.PlayerScope( Input.PlayerIndex.KeyboardAndMouse );
		foreach ( var action in Input.ActionNames )
		{
			if ( !Kept.Contains( action ) && SameKey( Input.GetButtonOrigin( action, true ), key ) )
				Input.Clear( action );
		}
	}

	// The game names a bound key as it reads on the key cap ("F1", "SPACE"); compared on its letters and digits.
	private static bool SameKey( string origin, string key ) => origin is not null && Plain( origin ) == Plain( key );

	private static string Plain( string name ) => new string( name.Where( char.IsLetterOrDigit ).ToArray() ).ToLowerInvariant();
}
