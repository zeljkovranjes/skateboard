using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Skateboarding.Engine;
using Skateboarding.Engine.Finish;

namespace Skateboarding.Systems;

/// <summary>
/// Draws every board in its look (<see cref="Components.RideableSkateboard.Look"/>), on every client, so everyone sees
/// each board as its rider finished it: the colourable parts' material slots draw with the shared tinted copies
/// (<see cref="BoardFinishMaterials"/>), and a board with the salvaged heart gets its sloshing oil bath. A board is
/// finished again only when its look or its drawn models change. The player's own board comes back: the look they
/// saved (<see cref="BoardLookStore"/>) goes onto any board they pick up or get on. Nothing draws on a dedicated server.
/// </summary>
public sealed class BoardFinishSystem : GameObjectSystem<BoardFinishSystem>
{
	// Seconds between looks for new boards (a look changed on a board already known shows at once).
	private const float Rescan = 0.5f;

	private sealed class Finished
	{
		internal string Look;
		internal readonly List<ModelRenderer> Renderers = new();
	}

	private readonly Dictionary<Components.RideableSkateboard, Finished> _boards = new();
	private readonly List<Components.RideableSkateboard> _gone = new();
	private readonly List<ModelRenderer> _renderers = new();
	private Components.RideableSkateboard _held;
	private RealTimeSince _sinceScan = Rescan;

	/// <summary>Finishes the boards as each frame finishes, before it is drawn.</summary>
	public BoardFinishSystem( Scene scene ) : base( scene ) => Listen( Stage.FinishUpdate, 0, Finish, "BoardFinish" );

	private void Finish()
	{
		if ( Scene.IsEditor || Application.IsDedicatedServer )
			return;
		BringOwnBoard();
		if ( _sinceScan >= Rescan )
		{
			_sinceScan = 0f;
			Scan();
		}
		foreach ( var (board, finished) in _boards )
		{
			if ( board.IsValid() && finished.Look != board.Look )
				Apply( board, finished );
		}
	}

	// Every board in the scene, finished where it is new or its drawn models changed; boards gone are dropped.
	private void Scan()
	{
		foreach ( var board in Scene.GetAllComponents<Components.RideableSkateboard>() )
		{
			if ( !_boards.TryGetValue( board, out var finished ) )
			{
				finished = new Finished();
				_boards[ board ] = finished;
			}
			_renderers.Clear();
			_renderers.AddRange( board.GameObject.Components.GetAll<ModelRenderer>( FindMode.EnabledInSelfAndDescendants ) );
			if ( !finished.Renderers.SequenceEqual( _renderers ) )
				Apply( board, finished );
		}
		_gone.Clear();
		foreach ( var board in _boards.Keys )
		{
			if ( !board.IsValid() )
				_gone.Add( board );
		}
		foreach ( var board in _gone )
			_boards.Remove( board );
	}

	private static void Apply( Components.RideableSkateboard board, Finished finished )
	{
		var look = BoardLook.Decode( board.Look );
		finished.Look = board.Look;
		finished.Renderers.Clear();
		finished.Renderers.AddRange( board.GameObject.Components.GetAll<ModelRenderer>( FindMode.EnabledInSelfAndDescendants ) );
		BoardFinishRenderers.Apply( finished.Renderers, look, BoardFinishMaterials.Shared );
		var skateboard = board.Board;
		if ( skateboard?.BoardModel is not { } model )
			return;
		var parts = BoardModelParts.Of( model, skateboard.BoardModelLevels, out _ );
		if ( parts is null || Deck( skateboard.GameObject ) is not { } deck )
			return;
		BoardFinishRenderers.Oil( deck, model, parts.Placement, look, BoardFinishMaterials.Shared );
	}

	// The drawn deck the game made of the board's model.
	private static GameObject Deck( GameObject board ) =>
		board.Children.FirstOrDefault( child => child.Name == "Generated Skateboard" )?.Children
			.FirstOrDefault( child => child.Name == "Deck" );

	// The board this client's player holds (rides or carries) takes the look they saved, once each time they take one.
	private void BringOwnBoard()
	{
		var local = Connection.Local?.Id ?? Guid.Empty;
		Components.RideableSkateboard held = null;
		if ( local != Guid.Empty )
		{
			foreach ( var board in _boards.Keys )
			{
				if ( board.IsValid() && board.RiderConnection == local )
				{
					held = board;
					break;
				}
			}
		}
		if ( held == _held )
			return;
		_held = held;
		if ( held is null || BoardLookStore.Load() is not { } saved )
			return;
		if ( BoardLook.Decode( held.Look ) != saved )
			held.Finish( saved );
	}
}
