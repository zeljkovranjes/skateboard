using Sandbox;
using Skateboarding.Components;
using Skateboarding.Core.Sound;

namespace Skateboarding.Engine;

/// <summary>
/// Listens to one board for its sounds. Each frame it reads what the board does from what every client knows of it (its
/// state, where it is, how it is shown and what its rider does), so every player hears every board the same way with
/// nothing sent for it: it keeps the board's rolling, grinding, sliding, braking and flipping sound going, and plays its
/// pops, landings, pushes, catches, grinds' starts and ends and its knocks as they happen. The board that is simulated
/// here hears its own pops and landings from the simulation itself, at the exact step.
/// </summary>
internal sealed class SkateBoardSound : IDisposable
{
	// Seconds between looks at the ground under the board.
	private const float LookEvery = 0.1f;

	// Inches above the board a look at the ground starts, and how far below the board it reaches.
	private const float LookFrom = 4f;
	private const float LookDown = 24f;

	// A pop seen from outside: the board's climb jumping by this much (m/s) within PopWindow seconds, off ground no
	// steeper than PopSlope (the up of the deck), within PopAfter seconds of leaving it.
	private const float PopJump = 1.4f;
	private const float PopWindow = 0.08f;
	private const float PopSlope = 0.8f;
	private const float PopAfter = 0.15f;

	// Seconds in the air before coming down counts as a landing (shorter is a bump in the ground).
	private const float LeastAir = 0.06f;

	// A flipped board is caught when its spin (rad/s) falls under Caught after reaching Flipped.
	private const float Flipped = 9f;
	private const float Caught = 3.5f;

	// A board on its own knocks into something when its speed changes by this much (m/s) in a frame, no more often than
	// every KnockGap seconds.
	private const float KnockJump = 2.2f;
	private const float KnockGap = 0.08f;

	// Seconds the board must be still and silent before its voice is let go.
	private const float Linger = 1f;

	private readonly Skateboard _board;
	private readonly SkateSoundGrounds _grounds;
	private readonly SkateHitSynth _hits;
	private readonly SkateSoundBank _bank;
	private SkateSoundVoice _voice;

	private bool _seen;
	private Vector3 _position;
	private Rotation _shown;
	private Vector3 _velocity;
	private float _spin;
	private readonly float[] _climbs = new float[16];
	private readonly float[] _climbTimes = new float[16];
	private int _climbAt;

	private bool _air, _bailed, _pushing, _popped, _caught;
	private SkateGrind _grind;
	private float _airFor, _fall, _spinPeak, _groundUp = 1f, _sinceKnock, _quietFor, _lookWait;
	private (SkateSoundSurface Sound, SoundGrains Grains) _ground = (SkateSoundSurface.Concrete, null);
	private (SkateSoundSurface Sound, SoundGrains Grains) _rail = (SkateSoundSurface.Metal, null);
	private bool _listening;

	// A pop and the fastest landing the simulation reported since the last frame, played from Update (never from inside
	// the board's own step, so nothing here can break it).
	private bool _heardPop;
	private float _heardLanding = -1f;

	internal SkateBoardSound( Skateboard board, SkateSoundGrounds grounds, SkateHitSynth hits, SkateSoundBank bank )
	{
		_board = board;
		_grounds = grounds;
		_hits = hits;
		_bank = bank;
	}

	/// <summary>Hears this frame of the board, ridden by <paramref name="rider"/> (null when no one rides it).</summary>
	internal void Update( SkateRider rider, float delta )
	{
		var board = _board;
		Listen( !board.IsProxy );
		var riding = rider is { Mode: SkateRiderMode.Riding };
		var state = board.State;
		var on = board.Enabled && board.Active && delta > 0f;
		// Where the board is drawn this frame (its physics moves only on fixed steps, so is read as drawn between them).
		var position = board.RenderTransform.Position;
		var shown = board.DisplayedTransform.Rotation;
		if ( !_seen || !on )
		{
			_seen = on;
			_position = position;
			_shown = shown;
			_velocity = Vector3.Zero;
			_air = state.IsAirborne;
			_grind = state.Grind;
			_bailed = state.IsBailed;
			Feed( default );
			return;
		}

		if ( _heardPop )
			Pop( 3f );
		if ( _heardLanding >= 0f )
			Land( _heardLanding );
		_heardPop = false;
		_heardLanding = -1f;

		// How the board moves (m/s) and how fast the shown board turns (rad/s), lightly smoothed.
		var velocity = (position - _position) / delta / SkateUnits.InchesPerMetre;
		var turn = Rotation.Difference( _shown, shown ).Angle().DegreeToRadian() / delta;
		_position = position;
		_shown = shown;
		var jump = (velocity - _velocity).Length;
		_velocity = Vector3.Lerp( _velocity, velocity, MathF.Min( 1f, delta / 0.03f ) );
		_spin = MathX.Lerp( _spin, MathF.Min( turn, 60f ), MathF.Min( 1f, delta / 0.03f ) );
		Climb( _velocity.z );

		var grinding = state.Grind != SkateGrind.None;
		var air = state.IsAirborne && !grinding;
		var rolling = state.IsAirborne || grinding ? 0f : state.GroundedWheels >= 4 ? 1f : state.GroundedWheels >= 2 ? 0.8f : 0.5f;
		var speed = MathF.Abs( state.Speed );
		Look( delta, grinding, rider );

		// Leaving the ground and coming back down.
		if ( state.IsAirborne && !_air )
		{
			_airFor = 0f;
			_fall = 0f;
			_popped = false;
			_caught = false;
			_spinPeak = 0f;
		}
		if ( state.IsAirborne )
		{
			_airFor += delta;
			_fall = MathF.Max( _fall, -_velocity.z );
			if ( !_listening && riding && !_popped && _airFor < PopAfter && _groundUp > PopSlope && Climbed() > PopJump )
				Pop( MathF.Max( _velocity.z, 0f ) );
		}
		else
		{
			_groundUp = board.WorldRotation.Up.z;
			if ( _air && !_listening && _airFor >= LeastAir )
				Land( _fall );
		}

		// Locking onto a rail, changing grind and leaving it.
		if ( state.Grind != _grind )
		{
			if ( _grind == SkateGrind.None )
				Once( _hits.GrindOn( Kind( state.Grind ), Math.Clamp( speed / 6f, 0.3f, 1f ), _rail.Sound ) );
			else if ( state.Grind == SkateGrind.None )
				Once( _hits.GrindOff( Kind( _grind ), Math.Clamp( speed / 6f, 0.3f, 1f ), _rail.Sound ) );
			else if ( Kind( state.Grind ) != Kind( _grind ) )
				Once( _hits.GrindOn( Kind( state.Grind ), 0.4f, _rail.Sound ) );
		}

		// A flipped board caught by the feet.
		if ( air && riding )
		{
			_spinPeak = MathF.Max( _spinPeak, _spin );
			if ( !_caught && _spinPeak > Flipped && _spin < Caught )
			{
				_caught = true;
				Once( _hits.Catch( Math.Clamp( _spinPeak / 25f, 0.3f, 1f ) ) );
			}
		}

		// A push: the pushing foot coming down on the ground.
		var pushing = riding && rolling > 0f && board.PushContact == true;
		if ( pushing && !_pushing )
			Once( _hits.Push( Math.Clamp( 0.4f + speed / 8f, 0.4f, 1f ), _ground.Sound, _ground.Grains ) );
		_pushing = pushing;

		// A bail lets the board go with a knock; a board on its own knocks into whatever stops it short.
		_sinceKnock += delta;
		if ( state.IsBailed && !_bailed )
			Knock( Math.Clamp( speed / 7f, 0.3f, 1f ) );
		else if ( !riding && jump > KnockJump && _sinceKnock > KnockGap )
			Knock( Math.Clamp( (jump - 2f) / 6f, 0.1f, 1f ) );

		_air = state.IsAirborne;
		_grind = state.Grind;
		_bailed = state.IsBailed;

		// What the continuous sound reads: the roll, the grind, a powerslide's sideways slip, a dragging shoe, a spin.
		var slip = rolling > 0f ? MathF.Abs( Vector3.Dot( _velocity, board.WorldRotation.Right ) ) : 0f;
		var braking = riding && rider.AnimationState?.Contains( ".Brake." ) == true;
		Feed( new SkateSoundControls
		{
			Speed = speed,
			Rolling = rolling,
			Ground = _ground.Sound,
			Grains = _ground.Grains,
			Grind = grinding ? Kind( state.Grind ) : SkateGrindSound.None,
			Rail = _rail.Sound,
			Slide = slip,
			Brake = braking ? Math.Clamp( speed / 5f, 0f, 1f ) : 0f,
			Spin = air || !riding && rolling == 0f ? _spin : 0f,
		} );
	}

	public void Dispose()
	{
		Listen( false );
		_voice?.Dispose();
		_voice = null;
	}

	// The board simulated here is heard from its own pops and landings; a board simulated elsewhere is watched for them.
	private void Listen( bool listen )
	{
		if ( listen == _listening )
			return;
		_listening = listen;
		if ( listen )
		{
			_board.TrickStarted += OnTrick;
			_board.Landed += OnLanded;
		}
		else
		{
			_board.TrickStarted -= OnTrick;
			_board.Landed -= OnLanded;
		}
	}

	private void OnTrick( SkateTrick trick ) => _heardPop = true;

	private void OnLanded( float speed ) => _heardLanding = MathF.Max( _heardLanding, speed );

	private void Pop( float climb )
	{
		_popped = true;
		Once( _hits.Pop( Math.Clamp( (climb - 1f) / 2.5f, 0.3f, 1f ), _ground.Sound, _ground.Grains ) );
	}

	private void Land( float speed )
	{
		if ( speed < 0.3f )
			return;
		Once( _hits.Land( Math.Clamp( (speed - 0.3f) / 4f, 0.1f, 1f ), _ground.Sound, _ground.Grains ) );
	}

	private void Knock( float strength )
	{
		_sinceKnock = 0f;
		Once( _hits.Knock( strength, _ground.Sound ) );
	}

	private void Once( ReadOnlySpan<float> sound ) => SkateSoundVoice.Once( _board.GameObject, sound );

	// The ground under the board (or the rail it grinds), looked at every LookEvery seconds.
	private void Look( float delta, bool grinding, SkateRider rider )
	{
		if ( (_lookWait -= delta) > 0f )
			return;
		_lookWait = LookEvery;
		var from = _board.WorldPosition + Vector3.Up * LookFrom;
		var trace = _board.Scene.Trace.Ray( from, from + Vector3.Down * (LookFrom + LookDown) )
			.IgnoreGameObjectHierarchy( _board.GameObject );
		if ( rider.IsValid() )
			trace = trace.IgnoreGameObjectHierarchy( rider.GameObject );
		var hit = trace.Run();
		if ( !hit.Hit )
			return;
		if ( grinding )
			_rail = _grounds.Of( hit.Surface );
		else
			_ground = _grounds.Of( hit.Surface );
	}

	// The board's climb (m/s) frame by frame, kept for PopWindow seconds.
	private void Climb( float climb )
	{
		_climbAt = (_climbAt + 1) % _climbs.Length;
		_climbs[ _climbAt ] = climb;
		_climbTimes[ _climbAt ] = Time.Now;
	}

	// How much faster the board climbs now than at its slowest within PopWindow seconds.
	private float Climbed()
	{
		var now = _climbs[ _climbAt ];
		var lowest = now;
		for ( var i = 0; i < _climbs.Length; i++ )
			if ( Time.Now - _climbTimes[ i ] <= PopWindow )
				lowest = MathF.Min( lowest, _climbs[ i ] );
		return now - lowest;
	}

	// Plays the continuous sound, starting the voice when the board makes some and letting it go once it has long been
	// silent.
	private void Feed( in SkateSoundControls controls )
	{
		var sounding = controls.Speed > 0.15f && (controls.Rolling > 0f || controls.Grind != SkateGrindSound.None)
			|| controls.Spin > 4f;
		_quietFor = sounding ? 0f : _quietFor + Time.Delta;
		if ( _voice is null )
		{
			if ( !sounding )
				return;
			_voice = new SkateSoundVoice( _board.GameObject, _board.GameObject.Id.GetHashCode(), _bank );
		}
		_voice.Feed( controls );
		if ( _quietFor > Linger && _voice.Quiet )
		{
			_voice.Dispose();
			_voice = null;
		}
	}

	private static SkateGrindSound Kind( SkateGrind grind ) => grind switch
	{
		SkateGrind.None => SkateGrindSound.None,
		SkateGrind.FiftyFifty or SkateGrind.FiveO or SkateGrind.Nosegrind => SkateGrindSound.Trucks,
		_ => SkateGrindSound.Slide
	};
}
