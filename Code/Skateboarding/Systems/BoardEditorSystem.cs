using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Skateboarding.Engine;
using Skateboarding.Engine.BoardEditing;
using Skateboarding.Engine.Docs;

namespace Skateboarding.Systems;

/// <summary>
/// Opens the board editor (<see cref="BoardEditor"/>) with its key, I unless rebound, or the controller's D-pad up,
/// while this client's player carries the board. While it is open the game is held still: every game action but voice
/// chat, chat and the scoreboard is cleared, on the keyboard and the controller, and the move and look read as at rest,
/// so the player neither walks nor looks about while the board is turned with the mouse. The editor lives on a screen
/// panel of its own, made the first time it opens, above the game's HUD and below the docs'.
/// </summary>
public sealed class BoardEditorSystem : GameObjectSystem<BoardEditorSystem>
{
	// The game's actions the editor leaves to the game: voice chat, chat and the scoreboard.
	private static readonly HashSet<string> Kept = new( StringComparer.OrdinalIgnoreCase ) { "Voice", "Chat", "Score" };
	// The game action the controller's editor button (D-pad up) is read through; the game's flashlight is on it too.
	private const string PadAction = "Flashlight";

	private GameObject _screen;
	private BoardEditor _editor;
	private Components.Skateboard _opening;

	/// <summary>Watches for the editor key every frame, after the ride's keys are taken and before the docs look.</summary>
	public BoardEditorSystem( Scene scene ) : base( scene ) => Listen( Stage.StartUpdate, -9500, Watch, "BoardEditor" );

	private void Watch()
	{
		if ( Scene.IsEditor || Application.IsDedicatedServer )
			return;
		var carried = Carried();
		if ( BoardEditor.IsOpen )
		{
			_editor?.Frame( carried.IsValid() );
			Hold();
			return;
		}
		if ( SkateDocs.IsOpen || !carried.IsValid() || BoardEditor.SinceClosed < 0.3f )
		{
			_opening = null;
			return;
		}
		if ( _opening is null && SkateBindings.Toggled( "board-editor" ) )
		{
			_opening = carried;
			// The game's flashlight shares the button: the press is the editor's.
			using ( Input.PlayerScope( Input.PlayerIndex.Controller1 ) )
				Input.ReleaseAction( PadAction );
		}
		if ( _opening is null )
			return;
		// The screen panel makes its root a frame after it is created: the editor opens as soon as there is one.
		if ( Editor() is not { } editor )
			return;
		var board = _opening;
		_opening = null;
		if ( board != carried )
			return;
		var rideable = board.Components.Get<Components.RideableSkateboard>();
		editor.Open( board.GameObject, Scene, new[] { board.GameObject, rideable?.Rider, rideable?.Player } );
		Hold();
	}

	// The board this client's player carries, or null.
	private Components.Skateboard Carried() => Scene.GetAllComponents<Components.SkateRider>()
		.FirstOrDefault( rider => !rider.IsProxy && rider.Mode == SkateRiderMode.Carrying )?.Board;

	// The game held still for a frame: its actions cleared in the keyboard's scope and the controller's (cleared in the
	// shared one, the controller's held buttons would be written into the keyboard's), and its move and look at rest.
	private static void Hold()
	{
		foreach ( var player in new[] { Input.PlayerIndex.KeyboardAndMouse, Input.PlayerIndex.Controller1 } )
		{
			using var scope = Input.PlayerScope( player );
			foreach ( var action in Input.ActionNames )
			{
				if ( !Kept.Contains( action ) )
					Input.Clear( action );
			}
		}
		Input.AnalogMove = Vector3.Zero;
		Input.AnalogLook = Angles.Zero;
	}

	private BoardEditor Editor()
	{
		if ( !_screen.IsValid() )
		{
			_screen = Scene.CreateObject( true );
			_screen.Name = "Board editor";
			_screen.NetworkMode = NetworkMode.Never;
			_screen.Flags |= GameObjectFlags.NotSaved;
			var screen = _screen.Components.Create<ScreenPanel>();
			screen.ZIndex = 250;
			screen.ScaleStrategy = ScreenPanel.AutoScale.FollowDesktopScaling;
		}
		if ( _editor is null || !_editor.IsValid )
		{
			var root = _screen.Components.Get<ScreenPanel>()?.GetPanel();
			if ( root is null )
				return null;
			_editor = new BoardEditor { Parent = root };
		}
		return _editor;
	}
}
