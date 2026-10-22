using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Sandbox.UI;
using Skateboarding.Engine.Finish;

namespace Skateboarding.Engine.BoardEditing;

/// <summary>
/// The board editor: the board the player carries, alone in the middle of the screen over the game blurred, to turn
/// over and look at. Dragging with the left mouse button turns it, with the right (or middle) one moves it, the wheel
/// zooms and a double click puts it back as it opened; on a controller the right stick turns it and the triggers zoom.
/// Everything eases to where it is asked to go, so nothing jumps. The editor key, Esc, or B on a controller closes it.
/// The community portraits on the underside grow under the mouse; a click on one pops it out, outlines it and pushes the
/// board aside to the left, and then who it is comes in from the right (Esc or a click off it lets it go: the words go
/// out to the right, then the board comes back to the middle). Its modes, Inspect and Edit, stand along the top
/// (<see cref="BoardEditorModes"/>); it opens in Inspect. Edit pushes the board aside as a pick does, and the board's
/// colours come in from the right (<see cref="BoardEditorEdit"/>): each pick shows on the board here at once, goes onto
/// the board itself for everyone to see, and is kept as the player's own board. Esc or Done goes back to Inspect.
/// </summary>
internal sealed class BoardEditor : Panel
{
	// Degrees the board turns per pixel dragged, and per second with the stick held all the way.
	private const float TurnPerPixel = 0.35f;
	private const float StickTurn = 150f;
	// A stick moved less than this reads as at rest.
	private const float StickDead = 0.15f;
	// How far the board may tip forward or back (degrees).
	private const float MaxPitch = 85f;
	// The zoom (a share of the distance that fits the whole board): per wheel notch, per second with a trigger held all
	// the way, and its nearest and furthest.
	private const float ZoomPerNotch = 0.12f;
	private const float TriggerZoom = 1.4f;
	private const float NearestZoom = 0.35f;
	private const float FurthestZoom = 2.5f;
	// How quickly (per second) the board shown follows where it is asked to be.
	private const float Follow = 14f;
	// How it is turned as the editor opens: from a little to the side and above, the grip and a truck in view.
	private const float StartYaw = 35f;
	private const float StartPitch = 22f;
	// A left press let go within this long (seconds) and this near (pixels) where it went down is a click, not a drag.
	private const float ClickTime = 0.4f;
	private const float ClickSlop = 5f;
	// A picked portrait pushes the board aside, to the left, for who it is: the share of the view's width the board's
	// middle moves by, how much further back it stands meanwhile, how long the push takes (seconds), how long after it
	// starts the card comes in, and how long after the card goes the board comes back.
	private const float AsideShare = 0.22f;
	private const float AsideBack = 0.12f;
	private const float AsideTime = 0.55f;
	private const float CardAfter = 0.3f;
	private const float BackAfter = 0.15f;

	/// <summary>True while the editor is open on this client.</summary>
	internal static bool IsOpen { get; private set; }

	/// <summary>Real time since the editor last closed: the press that closed it must not open it again.</summary>
	internal static RealTimeSince SinceClosed { get; private set; } = 10f;

	private readonly BoardEditorStage _stage;
	private readonly BoardEditorHide _hidden = new();
	private bool _justOpened;
	private bool _turning;
	private bool _moving;
	private Vector2 _mouse;
	// Where the board is asked to be and where it is shown: turned (degrees), zoomed and moved across the view (inches).
	private float _yaw;
	private float _pitch;
	private float _zoom = 1f;
	private Vector2 _shift;
	private float _shownYaw;
	private float _shownPitch;
	private float _shownZoom = 1f;
	private Vector2 _shownShift;
	// The portrait card, the board's settings and the modes along the top, and where and when the left button went down
	// (a press let go where it went down, quickly, is a click; one that moved is a drag), and whether it went down on any
	// of them.
	private BoardEditorCard _card;
	private BoardEditorEdit _edit;
	// The board being looked over, whose colours Edit changes.
	private Components.RideableSkateboard _board;
	private BoardEditorModes _modes;
	private Vector2 _pressedAt;
	private RealTimeSince _sincePressed;
	private bool _pressedOnCard;
	// How far the board is pushed aside (0 in the middle, 1 aside), eased from where it was to where it is going; and the
	// person whose card, or the settings, come in once the board is on its way.
	private float _asideFrom;
	private float _asideTo;
	private RealTimeSince _sinceAside;
	private PortraitInfo _coming;
	private bool _editComing;
	private RealTimeSince _sinceComing;

	/// <summary>The look the panel was made with (<see cref="BoardEditorStyles.Version"/>; 0 for one made before it was kept).</summary>
	internal int Version { get; private set; }

	private readonly Label _exit;

	internal BoardEditor()
	{
		Version = BoardEditorStyles.Version;
		StyleSheet.Parse( BoardEditorStyles.Sheet );
		AcceptsFocus = true;
		var view = AddChild<ScenePanel>( "view" );
		_stage = new BoardEditorStage( view.RenderScene );
		_card = AddChild<Panel>( "card-slot" ).AddChild<BoardEditorCard>( "card" );
		_edit = AddChild<Panel>( "edit-slot" ).AddChild<BoardEditorEdit>( "edit" );
		_edit.Done = () => _modes?.Select( BoardEditorModes.Inspect );
		_edit.Changed = Finished;
		_modes = AddChild<BoardEditorModes>( "modes" );
		_modes.Changed = ModeChanged;
		// Exit, at the bottom right as in the docs: closes the editor with the mouse.
		_exit = AddChild<Label>( "exit" );
		_exit.Text = "Exit";
		_exit.AddEventListener( "onclick", Close );
	}

	/// <summary>The modes along the top.</summary>
	internal BoardEditorModes Modes => _modes;

	// True while the board's settings are in use (Edit along the top).
	private bool Editing => _modes?.Selected == BoardEditorModes.Edit;

	// The card, in its slot on the right; made on first use as well, and moved into the slot, for an editor made before a
	// reload changed them.
	private BoardEditorCard Card
	{
		get
		{
			var slot = Children.FirstOrDefault( child => child.HasClass( "card-slot" ) ) ?? AddChild<Panel>( "card-slot" );
			_card ??= slot.AddChild<BoardEditorCard>( "card" );
			if ( _card.Parent != slot )
				_card.Parent = slot;
			return _card;
		}
	}

	/// <summary>
	/// Opens the editor on <paramref name="board"/>, hiding <paramref name="hide"/> (the player, the rider and the board
	/// in the game) from the game's cameras until it closes.
	/// </summary>
	internal void Open( GameObject board, Scene scene, IEnumerable<GameObject> hide )
	{
		_stage.Build( board );
		_board = board.Components.Get<Components.RideableSkateboard>( FindMode.EverythingInSelfAndDescendants );
		_stage.Finish( BoardLook.Decode( _board?.Look ) );
		Reset();
		_shownYaw = _yaw;
		_shownPitch = _pitch;
		_shownZoom = _zoom;
		_shownShift = _shift;
		_coming = null;
		_editComing = false;
		_asideFrom = _asideTo = 0f;
		_modes?.Select( BoardEditorModes.Inspect, false );
		_edit?.Hide();
		ShowBoard();
		_hidden.Hide( scene, hide );
		SetClass( "open", true );
		IsOpen = true;
		_justOpened = true;
		SkateBindings.Blocked = true;
		Focus();
	}

	internal void Close()
	{
		if ( !IsOpen )
			return;
		SetClass( "open", false );
		IsOpen = false;
		SinceClosed = 0f;
		_turning = false;
		_moving = false;
		_coming = null;
		_editComing = false;
		_asideFrom = _asideTo = 0f;
		Card.Hide();
		_edit?.Hide();
		_modes?.Select( BoardEditorModes.Inspect, false );
		_stage.Portraits.Choose( -1 );
		SetClass( "pointing", false );
		SkateBindings.Blocked = false;
		_hidden.Show();
		Blur();
	}

	/// <summary>
	/// The open editor's frame (BoardEditorSystem, as each frame starts): its closing keys and buttons, the sticks and
	/// triggers, a drag under way, and the board eased toward where it is asked to be. It closes itself if the board is
	/// no longer carried.
	/// </summary>
	internal void Frame( bool carried )
	{
		if ( !IsOpen )
			return;
		if ( !carried )
		{
			Close();
			return;
		}
		if ( !HasFocus )
			Focus();
		if ( Input.EscapePressed )
		{
			Input.EscapePressed = false;
			Back();
			return;
		}
		var padClose = false;
		using ( Input.PlayerScope( Input.PlayerIndex.Controller1 ) )
		{
			padClose = Input.Pressed( "Reload" );
			Sticks();
		}
		// The press that opened the editor must not close it again.
		if ( _justOpened )
			_justOpened = false;
		else if ( padClose || SkateBindings.Toggled( "board-editor" ) )
		{
			Close();
			return;
		}
		Drag();
		Hover();
		Ease();
		_hidden.Frame();
	}

	public override void OnDeleted()
	{
		// Only the open panel lets go: a closed one, made again with a newer look, must not close the one in its place.
		if ( IsOpen && HasClass( "open" ) )
		{
			SkateBindings.Blocked = false;
			_hidden.Show();
			IsOpen = false;
		}
		base.OnDeleted();
	}

	public override void OnButtonTyped( ButtonEvent e )
	{
		if ( !e.Pressed || _justOpened )
			return;
		var button = Docs.DocsKeyNames.FromButton( e.Button );
		if ( button == "escape" )
			Back();
		else if ( button == SkateBindings.KeyOf( "board-editor" ) )
			Close();
	}

	protected override void OnMouseDown( MousePanelEvent e )
	{
		base.OnMouseDown( e );
		_pressedOnCard = Within( e.Target, _card ) || Within( e.Target, _edit ) || Within( e.Target, _modes ) || Within( e.Target, _exit );
		if ( _pressedOnCard )
			return;
		if ( e.MouseButton == MouseButtons.Left )
		{
			_turning = true;
			_pressedAt = Mouse.Position;
			_sincePressed = 0f;
		}
		else if ( e.MouseButton is MouseButtons.Right or MouseButtons.Middle )
			_moving = true;
		_mouse = Mouse.Position;
	}

	protected override void OnMouseUp( MousePanelEvent e )
	{
		base.OnMouseUp( e );
		if ( e.MouseButton == MouseButtons.Left )
		{
			_turning = false;
			if ( !_pressedOnCard && _sincePressed < ClickTime && (Mouse.Position - _pressedAt).Length < ClickSlop )
				Click();
		}
		else if ( e.MouseButton is MouseButtons.Right or MouseButtons.Middle )
			_moving = false;
	}

	protected override void OnDoubleClick( MousePanelEvent e )
	{
		base.OnDoubleClick( e );
		Reset();
	}

	public override void OnMouseWheel( Vector2 value )
	{
		base.OnMouseWheel( value );
		_zoom = Math.Clamp( _zoom * MathF.Exp( value.y * ZoomPerNotch ), NearestZoom, FurthestZoom );
	}

	// Esc: back to Inspect from Edit, or lets a picked portrait go if there is one, or else closes the editor.
	private void Back()
	{
		if ( Editing )
		{
			_modes.Select( BoardEditorModes.Inspect );
			return;
		}
		if ( Card.Showing || _coming is not null )
		{
			_stage.Portraits.Choose( -1 );
			LetGo();
			return;
		}
		Close();
	}

	// A click on the board: picks the portrait under the mouse and shows who it is, or lets the picked one go (in Inspect;
	// in Edit the portraits are left alone).
	private void Click()
	{
		if ( !Editing )
			Pick( _stage.Portraits.Pick( MouseRay() ) );
	}

	// A mode put in use along the top. Edit lets a picked portrait go and pushes the board aside, as a pick does, and then
	// the board's colours come in, showing its look; Inspect sends them out to the right and brings the board back to the
	// middle.
	private void ModeChanged( int mode )
	{
		if ( mode == BoardEditorModes.Edit )
		{
			if ( _edit is not null )
				_edit.Look = BoardLook.Decode( _board?.Look );
			_stage.Portraits.Choose( -1 );
			_coming = null;
			Card.Hide();
			_editComing = true;
			_sinceComing = 0f;
			Aside( 1f, 0f );
			return;
		}
		_editComing = false;
		_edit?.Hide();
		Aside( 0f, BackAfter );
	}

	/// <summary>
	/// Picks a portrait (by its place among them; -1 for none): the board is pushed aside and then who it is comes in, or,
	/// with the board aside already, the card changes over to them. None lets the picked one go.
	/// </summary>
	internal void Pick( int tile )
	{
		var id = _stage.Portraits.Choose( tile );
		if ( PortraitDirectory.Find( id ) is not { } person )
		{
			LetGo();
			return;
		}
		if ( _coming is not null )
		{
			_coming = person;
			return;
		}
		if ( Card.Showing )
		{
			Card.Show( person );
			return;
		}
		_coming = person;
		_sinceComing = 0f;
		Aside( 1f, 0f );
	}

	// A colour picked in Edit: the editor's board shows it at once, the board itself takes it (the host sets it, and
	// every client draws it), and it is kept as the player's own board for next time.
	private void Finished( BoardLook look )
	{
		_stage.Finish( look );
		_board?.Finish( look );
		BoardLookStore.Save( look );
	}

	// The card goes out to the right, then the board comes back to the middle.
	private void LetGo()
	{
		_coming = null;
		Card.Hide();
		Aside( 0f, BackAfter );
	}

	// Sets the board going aside (1) or back to the middle (0) from wherever it is now, after delay seconds.
	private void Aside( float to, float delay )
	{
		if ( to == _asideTo )
			return;
		_asideFrom = AsideNow;
		_asideTo = to;
		_sinceAside = -delay;
	}

	// How far aside the board is now: slow out of the middle, quick across, slow into place.
	private float AsideNow
	{
		get
		{
			var t = Math.Clamp( _sinceAside / AsideTime, 0f, 1f );
			t = t < 0.5f ? 4f * t * t * t : 1f - MathF.Pow( 2f - 2f * t, 3f ) * 0.5f;
			return _asideFrom + (_asideTo - _asideFrom) * t;
		}
	}

	// The portrait under the mouse grows, and the cursor becomes a hand over it; none while the board is dragged.
	private void Hover()
	{
		var tile = _turning || _moving || Editing ? -1 : _stage.Portraits.Pick( MouseRay() );
		_stage.Portraits.Hover( tile );
		SetClass( "pointing", tile >= 0 );
	}

	private Ray MouseRay() => _stage.RayAt( Mouse.Position, Screen.Size );

	// Whether a panel is the holder or inside it.
	private static bool Within( Panel panel, Panel holder )
	{
		for ( var at = panel; at is not null; at = at.Parent )
		{
			if ( at == holder )
				return true;
		}
		return false;
	}

	/// <summary>The editor's scene (the board's copy, its portraits and the camera).</summary>
	internal BoardEditorStage Stage => _stage;

	/// <summary>Turns the board to <paramref name="yaw"/> and <paramref name="pitch"/> (degrees), as a drag would.</summary>
	internal void Turn( float yaw, float pitch )
	{
		_yaw = yaw;
		_pitch = Math.Clamp( pitch, -MaxPitch, MaxPitch );
	}

	// Back to how it opened: turned to show the grip and a truck, the whole board in view, in the middle.
	private void Reset()
	{
		_yaw = StartYaw;
		_pitch = StartPitch;
		_zoom = 1f;
		_shift = Vector2.Zero;
	}

	// The right stick turns the board, the triggers zoom (left out, right in).
	private void Sticks()
	{
		var delta = RealTime.Delta;
		var x = Dead( Input.GetAnalog( InputAnalog.RightStickX ) );
		var y = Dead( Input.GetAnalog( InputAnalog.RightStickY ) );
		_yaw += x * StickTurn * delta;
		_pitch = Math.Clamp( _pitch + y * StickTurn * delta, -MaxPitch, MaxPitch );
		var zoom = Input.GetAnalog( InputAnalog.LeftTrigger ) - Input.GetAnalog( InputAnalog.RightTrigger );
		_zoom = Math.Clamp( _zoom * MathF.Exp( zoom * TriggerZoom * delta ), NearestZoom, FurthestZoom );
	}

	// A drag under way: turning with the left button, moving with the right or middle one, from the press on the editor
	// to its release (OnMouseUp). The buttons are read from the editor's own mouse events, not the game's input: the
	// editor takes the mouse while it is open, so the game sees no button held at all.
	private void Drag()
	{
		var now = Mouse.Position;
		var moved = now - _mouse;
		_mouse = now;
		if ( _turning )
		{
			_yaw += moved.x * TurnPerPixel;
			_pitch = Math.Clamp( _pitch + moved.y * TurnPerPixel, -MaxPitch, MaxPitch );
		}
		if ( _moving && Screen.Height > 0f )
		{
			// Inches the board moves per pixel dragged, so it stays under the cursor at the distance it is shown.
			var perPixel = ShownDistance * View / Screen.Height;
			_shift += new Vector2( moved.x, -moved.y ) * perPixel;
		}
	}

	// The board shown eased toward where it is asked to be, by the same share each second at any frame rate.
	private void Ease()
	{
		var step = 1f - MathF.Exp( -Follow * RealTime.Delta );
		_shownYaw += (_yaw - _shownYaw) * step;
		_shownPitch += (_pitch - _shownPitch) * step;
		_shownZoom += (_zoom - _shownZoom) * step;
		_shownShift += (_shift - _shownShift) * step;
		// Who was picked, or the board's settings, come in once the board is on its way aside.
		if ( _coming is not null && _sinceComing >= CardAfter )
		{
			Card.Show( _coming );
			_coming = null;
		}
		if ( _editComing && _sinceComing >= CardAfter )
		{
			_edit?.Show();
			_editComing = false;
		}
		ShowBoard();
		_stage.Portraits.Animate( RealTime.Delta );
	}

	// The board as shown, pushed aside by a share of the view's width at the distance it stands (the same share at any
	// zoom).
	private void ShowBoard()
	{
		var distance = ShownDistance;
		var wide = Screen.Height > 0f ? Screen.Width / Screen.Height : 16f / 9f;
		var push = AsideShare * wide * View * distance * AsideNow;
		_stage.Show( _shownYaw, _shownPitch, distance, _shownShift - new Vector2( push, 0f ) );
	}

	// Inches from the camera to the board as shown: zoomed, and a little further back while aside.
	private float ShownDistance => _stage.FitDistance * _shownZoom * (1f + AsideBack * AsideNow);

	// The view's height over the distance to what it shows.
	private static float View => 2f * MathF.Tan( BoardEditorStage.Fov * 0.5f * MathF.PI / 180f );

	private static float Dead( float value ) => MathF.Abs( value ) < StickDead ? 0f : value;
}
