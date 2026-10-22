using Sandbox;
using Skateboarding.Core;
using Skateboarding.Core.Input;

namespace Skateboarding.Engine;

/// <summary>
/// Reads the local keyboard and mouse into board input through the skater's own key bindings (SkateBindings), merged
/// with a gamepad's sticks and buttons (DeviceInputMerge: neither device locks the other out). The steering keys ramp the
/// trucks in and out instead of jumping to full lock. Holding the ollie (or nollie) key winds up and letting it go pops,
/// with a flip key held for a kickflip or heelflip; each trick key winds up while held and pops its own trick when let go
/// (TrickButtons), and the manual keys hold a manual. Holding the trick stick key turns the mouse into the trick stick:
/// pull down to crouch, flick up (or up and to a side) to pop; like a thumbstick let go, it springs back to centre when the
/// mouse rests, unless held out at the windup. Holding push and brake together (or the hippy jump key) crouches for a
/// hippy jump, and letting go jumps. In the air, push or brake while grabbing leans into a front or back flip.
/// On a controller (Pad) the left stick steers (and, with a grab, leans into a flip) and the right stick flicks tricks and
/// holds manuals; A pushes, B brakes and both together crouch for a hippy jump; the left trigger powerslides on the ground
/// and grabs with the front hand in the air, the right trigger grabs with the back hand; L3 pumps (a foot off in the air),
/// R3 catches, and the D-pad plants a hand (up), lies back in a coffin (right), grabs with the front hand on the ground
/// (left) and bails (down).
/// </summary>
internal sealed class LocalSkateInput
{
	private int _charging;
	private float _release;
	private float _releaseX;
	private float _releaseY;
	private bool _popSpent;
	private readonly KeyRamp _steer = new( SteerIn, SteerOut );
	private Vector2 _flick;
	private readonly TrickButtons _trickKeys = new();
	private float _rested;
	private float _wound;

	// Seconds for a steering key to reach full lock, and for released steering to centre.
	private const float SteerIn = 0.35f;
	private const float SteerOut = 0.15f;

	// Mouse travel, in pixels, that moves the trick stick from centre to its edge.
	private const float FlickTravel = 90f;

	// Seconds the mouse rests before the stick springs back, and how fast (1/s) it does; how near centre counts as let
	// go, and how far out a windup is (where a pop is armed).
	private const float RestGrace = 0.06f;
	private const float SpringRate = 25f;
	private const float Centred = 0.15f;
	private const float WoundUp = 0.65f;

	// How far a manual key holds the stick: inside a manual's 0.2 to 0.9, short of a pop's wind-up.
	private const float ManualStick = 0.45f;

	// How far a trigger is pulled to grab.
	private const float TriggerHeld = 0.5f;

	/// <summary>True while the mouse drives the trick stick (and so not the camera).</summary>
	internal static bool Flicking => SkateBindings.Down( "flick" );

	/// <summary>
	/// The keyboard's movement on the board, from the bound push, brake and steering keys (forward and left), in place of
	/// the game's movement keys (<paramref name="move"/>), which ride with the board only as the player walks.
	/// </summary>
	internal static Vector3 RidingMove( Vector3 move ) => new(
		(SkateBindings.Down( "push" ) ? 1f : 0f) - (SkateBindings.Down( "brake" ) ? 1f : 0f),
		(SkateBindings.Down( "steer-left" ) ? 1f : 0f) - (SkateBindings.Down( "steer-right" ) ? 1f : 0f), 0f );

	/// <summary>The keyboard and mouse together with the first controller (the board-only controls).</summary>
	internal SkateInput Read( bool goofy, float delta )
	{
		var raw = LocalControllerInput.Read();
		var pad = DeviceInputMerge.Settle( raw );
		var move = RidingMove( DeviceMerge.KeyboardMove( raw ) );
		return DeviceMerge.Board( ReadKeys( goofy, delta, move ), DeviceMerge.SteeringKey( move ), Pad( pad, goofy ),
			DeviceInputMerge.Activity( pad ) );
	}

	// The controller's hippy jump crouch (A and B held together), kept until both are let go, and its grab's pick.
	private static bool _padHippy;
	private static readonly GrabSelection _padGrab = new();

	/// <summary>
	/// A controller read without a controller profile: the sticks steer, flick tricks and hold manuals, and the buttons do
	/// the rest (see the class summary), the triggers as the board's hands in the air (<paramref name="airborne"/>).
	/// pad is a settled reading. The D-pad's coffin and front-hand grab ride with the keys (ReadKeys): the merge takes a
	/// controller's grab only while a trigger is pulled.
	/// </summary>
	internal static SkateInput Pad( ControllerReading pad, bool goofy, bool airborne = false )
	{
		if ( SkateBindings.Blocked )
			return new SkateInput( Goofy: goofy );
		using var scope = Input.PlayerScope( Input.PlayerIndex.Controller1 );
		var push = PadButton( "push" );
		var brake = PadButton( "brake" );
		_padHippy = push && brake || _padHippy && (push || brake);
		var front = airborne && pad.LeftTrigger > TriggerHeld;
		var back = pad.RightTrigger > TriggerHeld;
		var grab = AirGrabs.From( _padGrab.Step( front, back, pad.RightX, pad.RightY, goofy, Time.Delta ) );
		var stance = goofy ? SkatePushFeet.Left : SkatePushFeet.Right;
		return new SkateInput( -pad.LeftX, push && !_padHippy ? 1f : 0f, brake && !_padHippy ? 1f : 0f, pad.RightX, pad.RightY,
			grab == SkateAirGrab.None ? 0f : pad.LeftY, Slide: !airborne && PadButton( "powerslide" ), Goofy: goofy,
			Pump: !airborne && PadButton( "pump" ), Grab: grab )
		{
			HippyJump = _padHippy,
			Catch = PadButton( "catch" ),
			FeetOff = airborne && PadButton( "pump" ) ? stance : SkatePushFeet.None,
			HandPlant = PadButton( "hand-plant" )
		};
	}

	// A control's controller button (or a script's, SkateBindings.SimulatedPad).
	private static bool PadButton( string id ) => SkateBindings.PadAction( id ) is { } action && SkateBindings.PadActionDown( action );

	/// <summary>
	/// The keyboard and mouse alone, whatever device the engine counts as last. move is the bound keys' movement on the
	/// board (RidingMove). While not <paramref name="ready"/> (a trick still playing out on the ground) the mouse stick lets
	/// go and no trick key starts a trick.
	/// </summary>
	internal SkateInput ReadKeys( bool goofy, float delta, Vector3 move, bool ready = true )
	{
		using var scope = Input.PlayerScope( Input.PlayerIndex.KeyboardAndMouse );
		var steer = _steer.Step( move.y, delta );
		_steerKey = move.y;
		var (flickX, flickY) = Keys( delta );
		if ( Flicking && ready )
		{
			var moved = new Vector2( Input.MouseDelta.x, -Input.MouseDelta.y );
			_flick += moved / FlickTravel;
			if ( _flick.Length > 1f )
				_flick = _flick.Normal;
			_flick = Sprung( _flick, moved, SkateBindings.Down( "grab-front" ) || SkateBindings.Down( "grab-back" ), delta );
			flickX = _flick.x;
			flickY = _flick.y;
		}
		else
		{
			_flick = Vector2.Zero;
			_wound = 0f;
		}
		if ( _trickKeys.Step( HeldTrick(), SkateBindings.Down( "nollie" ), goofy, ready, delta ) is { } trick )
		{
			flickX = trick.X;
			flickY = trick.Y;
			// The ollie or nollie key held with a trick key (the nollie key picks a nollie) pops nothing more of its own.
			_popSpent = true;
			_charging = 0;
			_release = 0f;
		}
		return Board( steer, flickX, flickY, goofy, delta );
	}

	// The trick key held (the first, if several are), as its trick.
	private static TrickKind HeldTrick()
	{
		foreach ( var key in TrickButtons.Keys )
		{
			if ( SkateBindings.Down( key.Id ) )
				return key.Trick;
		}
		return TrickKind.None;
	}

	// A thumbstick let go springs back to centre; a mouse cannot be let go, so its stick does that itself once the mouse
	// rests. Only a windup holds: out on the side the stick was first pulled to from centre, past where a pop is armed,
	// it goes to the edge and stays there until the mouse moves it on. The flick after it springs back like any stick
	// left part-way, so where the mouse happened to stop never goes on holding a manual (which steers with only part of
	// the trucks), a windup or a flip after the trick. While a grab is held the stick tweaks it and stays where it is.
	private Vector2 Sprung( Vector2 stick, Vector2 moved, bool grabbing, float delta )
	{
		_rested = moved == Vector2.Zero ? _rested + delta : 0f;
		var resting = _rested >= RestGrace;
		if ( resting && stick.Length < Centred )
			_wound = 0f;
		else if ( _wound == 0f && MathF.Abs( stick.y ) >= WoundUp )
			_wound = MathF.Sign( stick.y );
		if ( !resting || grabbing )
			return stick;
		if ( _wound != 0f && stick.y * _wound >= WoundUp )
			return stick.Normal;
		return stick * MathF.Exp( -SpringRate * delta );
	}

	// The ollie key held winds up (the stick pulled back), the nollie key the other way; let go, the stick flicks the
	// other way for a moment, toward a flip key held then. A manual key holds the stick part way back (the nose manual's
	// part way forward). After a trick key the ollie and nollie keys do nothing until both are up.
	private (float X, float Y) Keys( float delta )
	{
		var ollie = SkateBindings.Down( "ollie" );
		var nollie = SkateBindings.Down( "nollie" ) && !ollie;
		_popSpent &= ollie || nollie;
		var charging = _popSpent ? 0 : ollie ? -1 : nollie ? 1 : 0;
		if ( _charging != 0 && charging == 0 && !_popSpent )
		{
			_release = 0.06f;
			_releaseX = FlipKeys();
			_releaseY = -_charging;
		}
		_charging = charging;
		// A flip key still held after the pop keeps the flip turning, as a stick held at the end of its flick does.
		var held = FlipKeys();
		var manual = (SkateBindings.Down( "nose-manual" ) ? ManualStick : 0f) - (SkateBindings.Down( "manual" ) ? ManualStick : 0f);
		var flickX = _release > 0f ? _releaseX : charging != 0 ? 0f : held;
		var flickY = charging != 0 ? charging : _release > 0f ? _releaseY : manual;
		_release = Math.Max( 0f, _release - delta );
		return (flickX, flickY);
	}

	private static float FlipKeys() => (SkateBindings.Down( "flip-right" ) ? 1f : 0f) - (SkateBindings.Down( "flip-left" ) ? 1f : 0f);

	private SkateInput Board( float steer, float flickX, float flickY, bool goofy, float delta )
	{
		var push = SkateBindings.Down( "push" );
		var brake = SkateBindings.Down( "brake" );
		// Both held (or the hippy jump key) is the hippy jump's crouch, which neither pushes nor brakes. It stays held until
		// both are let go, so letting one go a moment before the other still jumps.
		_hippy = push && brake || SkateBindings.Down( "hippy-jump" ) || _hippy && (push || brake);
		// The coffin is both hands on the board and both feet off it. On a controller the coffin and the front hand on
		// the ground are D-pad buttons, read here with the keys (the merge takes a controller's grab only on a trigger).
		var coffin = SkateBindings.Held( "coffin" );
		var front = SkateBindings.Held( "grab-front" ) || coffin;
		var back = SkateBindings.Down( "grab-back" ) || coffin;
		// The grab is picked from the hands and the trick stick: the mouse stick while flicking, otherwise the steering
		// keys for the toe or heel side and the manual keys for the tail (pulled back) or nose (pushed forward).
		var grab = AirGrabs.From( _grab.Step( front, back, Flicking ? flickX : -_steerKey, flickY, goofy, delta ) );
		var lean = grab == SkateAirGrab.None ? 0f : (push ? 1f : 0f) - (brake ? 1f : 0f);
		var slide = SkateBindings.Down( "powerslide" );
		var both = SkatePushFeet.Left | SkatePushFeet.Right;
		var still = _hippy || coffin;
		return new( steer, push && !still ? 1f : 0f, brake && !still ? 1f : 0f, flickX, flickY, coffin ? 0f : lean,
			Slide: slide, Goofy: goofy, Pump: SkateBindings.Down( "pump" ), Grab: grab )
		{
			// Both hands on the board with both feet off is a coffin, not a hippy jump.
			HippyJump = _hippy && !(front && back),
			Catch = SkateBindings.Down( "catch" ),
			// The powerslide key in the air takes the back foot off the board; the coffin both feet, in the air and on the
			// ground.
			FeetOff = coffin ? both : slide ? (goofy ? SkatePushFeet.Left : SkatePushFeet.Right) : SkatePushFeet.None,
			PushFeet = coffin ? both : SkatePushFeet.None,
			HandPlant = SkateBindings.Down( "hand-plant" )
		};
	}

	private bool _hippy;
	private readonly GrabSelection _grab = new();

	// The steering keys as pressed, unramped: picking a grab's side reads them at once.
	private float _steerKey;
}
