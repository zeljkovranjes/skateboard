using System;
using System.Collections.Generic;
using Sandbox;
using Sandbox.UI;

namespace Skateboarding.Engine.BoardEditing;

/// <summary>
/// The board editor: the board the player carries, alone in the middle of the screen over the game blurred, to turn
/// over and look at. Dragging with the left mouse button turns it, with the right (or middle) one moves it, the wheel
/// zooms and a double click puts it back as it opened; on a controller the right stick turns it and the triggers zoom.
/// Everything eases to where it is asked to go, so nothing jumps. The editor key, Esc, or B on a controller closes it.
/// The community portraits on the underside grow under the mouse; a click on one pops it out, outlines it and brings in
/// a card telling who it is (Esc or a click off it lets it go).
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
	// The portrait card, and where and when the left button went down (a press let go where it went down, quickly, is a
	// click; one that moved is a drag), and whether it went down on the card.
	private BoardEditorCard _card;
	private Vector2 _pressedAt;
	private RealTimeSince _sincePressed;
	private bool _pressedOnCard;

	internal BoardEditor()
	{
		StyleSheet.Parse( BoardEditorStyles.Sheet );
		AcceptsFocus = true;
		var view = AddChild<ScenePanel>( "view" );
		_stage = new BoardEditorStage( view.RenderScene );
		_card = AddChild<BoardEditorCard>( "card" );
	}

	// Made on first use as well: an editor made before a reload added it has none.
	private BoardEditorCard Card => _card ??= AddChild<BoardEditorCard>( "card" );

	/// <summary>
	/// Opens the editor on <paramref name="board"/>, hiding <paramref name="hide"/> (the player, the rider and the board
	/// in the game) from the game's cameras until it closes.
	/// </summary>
	internal void Open( GameObject board, Scene scene, IEnumerable<GameObject> hide )
	{
		_stage.Build( board );
		Reset();
		_shownYaw = _yaw;
		_shownPitch = _pitch;
		_shownZoom = _zoom;
		_shownShift = _shift;
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
		Card.Hide();
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
		if ( IsOpen )
		{
			SkateBindings.Blocked = false;
			_hidden.Show();
		}
		IsOpen = false;
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
		_pressedOnCard = Within( e.Target, _card );
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

	// Esc: lets a picked portrait go if there is one, or else closes the editor.
	private void Back()
	{
		if ( Card.Showing )
		{
			Card.Hide();
			_stage.Portraits.Choose( -1 );
			return;
		}
		Close();
	}

	// A click on the board: picks the portrait under the mouse and shows who it is, or lets the picked one go.
	private void Click()
	{
		var id = _stage.Portraits.Choose( _stage.Portraits.Pick( MouseRay() ) );
		if ( PortraitDirectory.Find( id ) is { } person )
			Card.Show( person );
		else
			Card.Hide();
	}

	// The portrait under the mouse grows, and the cursor becomes a hand over it; none while the board is dragged.
	private void Hover()
	{
		var tile = _turning || _moving ? -1 : _stage.Portraits.Pick( MouseRay() );
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
			var view = 2f * MathF.Tan( BoardEditorStage.Fov * 0.5f * MathF.PI / 180f );
			var perPixel = _stage.FitDistance * _shownZoom * view / Screen.Height;
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
		ShowBoard();
		_stage.Portraits.Animate( RealTime.Delta );
	}

	private void ShowBoard() => _stage.Show( _shownYaw, _shownPitch, _stage.FitDistance * _shownZoom, _shownShift );

	private static float Dead( float value ) => MathF.Abs( value ) < StickDead ? 0f : value;
}
