using System;
using System.Linq;
using Sandbox;
using Skateboarding.Engine;

namespace Skateboarding.Components;

/// <summary>
/// Holding Use on foot, pressing nothing, calls the player's own board back to the hand from wherever it is: it flies to
/// them and they carry it. Riding or carrying a board the player is put away, so nothing is called then.
/// </summary>
public sealed class BoardRecall : GameObjectSystem
{
	// Seconds Use is held before the board is called: as long as the hold that picks up a board in reach.
	private const float HoldToCall = 0.35f;

	private float _held;
	private bool _spent;

	public BoardRecall( Scene scene ) : base( scene ) => Listen( Stage.StartUpdate, 10, Update, "BoardRecall" );

	private void Update()
	{
		if ( !Input.Down( SandboxActions.GetOff ) )
		{
			_held = 0f;
			_spent = false;
			return;
		}
		if ( _spent )
			return;
		// Only the player on foot here calls: put away while riding or carrying, its controller is off.
		var player = Scene.GetAllComponents<PlayerController>().FirstOrDefault( p => !p.IsProxy );
		if ( player is null )
		{
			_spent = true;
			return;
		}
		// A hold that presses something (a door, a button, a board in reach to pick up) is that thing's.
		if ( player.Pressed.IsValid() )
		{
			_spent = true;
			return;
		}
		_held += Time.Delta;
		if ( _held < HoldToCall )
			return;
		_spent = true;
		Own( player.WorldPosition )?.Recall( player.GameObject.Root );
	}

	// The board this player calls: the latest of their own that no one has, or else one no one has ever had, the nearest.
	private RideableSkateboard Own( Vector3 from )
	{
		var me = Connection.Local?.Id ?? Guid.Empty;
		return Scene.GetAllComponents<RideableSkateboard>()
			.Where( board => board.RiderConnection == Guid.Empty && (board.OwnerId == me || board.OwnerId == Guid.Empty) )
			.OrderByDescending( board => board.OwnerId == me )
			.ThenByDescending( board => board.OwnedAt )
			.ThenBy( board => board.WorldPosition.Distance( from ) )
			.FirstOrDefault();
	}
}
