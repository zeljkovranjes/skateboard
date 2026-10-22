namespace Skateboarding.Core.Sound;

/// <summary>
/// A board's continuous sound, played from real recordings (<see cref="SkateSoundBank"/>) shaped sample by sample by what
/// the board does: the roll of its wheels (the recording of the ground under it, faster and higher the faster it rolls,
/// louder with speed, darkened on soft ground and booming on hollow ground, sprinkled with the ground's own sound), the
/// grind of trucks on metal or concrete and the slide of the deck, the scrub of a powerslide, a shoe dragging to brake,
/// and the whoosh of a board flipping. Loops never repeat (<see cref="LoopPlayer"/>), every control eases to its new
/// value and grounds crossfade into each other, so nothing steps or clicks. Mono at <see cref="Rate"/>.
/// </summary>
internal sealed class SkateRollSynth
{
	internal const int Rate = 44100;

	// Samples between control updates.
	private const int Block = 32;

	// Seconds a level takes to rise and to fall most of the way, a ground's settings to give way to the next and one
	// recorded roll to crossfade into another.
	private const float Rise = 0.03f;
	private const float Fall = 0.09f;
	private const float GroundChange = 0.06f;
	private const float RollChange = 0.2f;

	// Speed (m/s) the recordings were made at: there they play at their own pitch and the roll is at its full level.
	private const float RecordedRoll = 6f;
	private const float RecordedGrind = 5.5f;

	// How loud each layer is at its full level: the roll sits well under the hits (SkateHitSynth), a grind and a
	// powerslide over it, a dragging shoe and a flip's whoosh under it.
	private const float RollLoudness = 0.6f;
	private const float MetalGrindLoudness = 3.8f;
	private const float LedgeGrindLoudness = 2f;
	private const float SlideLoudness = 2.3f;
	private const float ScrubLoudness = 0.75f;
	private const float DragLoudness = 0.6f;
	private const float WhooshLoudness = 0.45f;

	// Grains of the ground's own sound playing at once, at most.
	private const int MostGrains = 8;

	private readonly SkateSoundBank _bank;
	private SoundNoise _noise;
	private SkateSoundSurface _ground = SkateSoundSurface.Concrete;
	private SkateSoundSurface _rail = SkateSoundSurface.Metal;
	private float _roll, _grind, _slide, _brake, _spin, _trucks = 1f;
	private float _speed, _slip, _spinRate;
	private SoundGrains _grainSource;

	private LoopPlayer _rollNow, _rollWas, _grindLoop, _slideLoop, _scrubLoop, _dragLoop;
	private SoundClip _rollClip, _rollWasClip;
	private float _rollMix = 1f;

	private OnePole _rollLow1, _rollLow2, _scrubLow1, _scrubLow2, _dragLow1, _dragLow2, _grainLow;
	private OnePole _rustleHigh, _rustleLow, _rustleBurst, _dc;
	private Svf _hollow, _whoosh;
	private float _rollStep, _scrubStep, _dragStep;
	private float _angle;
	private int _quiet;

	private struct Grain
	{
		public float Place, Step, Age, Length, Gain;
	}

	private readonly Grain[] _grains = new Grain[MostGrains];
	private float _grainWait;

	internal SkateRollSynth( int seed, SkateSoundBank bank )
	{
		_noise = new SoundNoise( seed );
		_bank = bank;
	}

	/// <summary>True once the board has made no sound for half a second: its voice can be let go.</summary>
	internal bool Quiet => _quiet > Rate / 2;

	/// <summary>Writes the next samples of the board's sound, easing from the last controls to these.</summary>
	internal void Render( Span<float> output, in SkateSoundControls controls )
	{
		for ( var start = 0; start < output.Length; start += Block )
		{
			var count = Math.Min( Block, output.Length - start );
			Steer( controls, count );
			for ( var i = 0; i < count; i++ )
				output[start + i] = Sample();
		}
	}

	// Per block: the levels ease towards the controls' and the filters are tuned to them.
	private void Steer( in SkateSoundControls c, int count )
	{
		var step = count / (float)Rate;
		var speed = MathF.Max( c.Speed, 0f );
		var grinding = c.Grind != SkateGrindSound.None;
		var rolling = grinding ? 0f : Math.Clamp( c.Rolling, 0f, 1f );
		var roll = rolling * SoundShape.Ease( speed, 0.1f, 0.9f ) * MathF.Pow( MathF.Min( speed, 14f ) / RecordedRoll, 0.8f );
		var grind = grinding ? SoundShape.Ease( speed, 0.2f, 1f ) * MathF.Pow( MathF.Min( speed, 12f ) / RecordedGrind, 0.6f ) : 0f;
		var slide = rolling * SoundShape.Ease( c.Slide, 0.8f, 3f ) * MathF.Min( 1.2f, 0.6f + c.Slide / 8f );
		var brake = rolling * Math.Clamp( c.Brake, 0f, 1f );
		var spin = SoundShape.Ease( c.Spin, 4f, 12f ) * MathF.Min( 1f, c.Spin / 25f );
		_roll = Ease( _roll, roll, step );
		_grind = Ease( _grind, grind, step );
		_slide = Ease( _slide, slide, step );
		_brake = Ease( _brake, brake, step );
		_spin = Ease( _spin, spin, step );
		var settle = 1f - MathF.Exp( -step / Rise );
		_speed += (speed - _speed) * settle;
		_slip += (c.Slide - _slip) * settle;
		_spinRate = c.Spin;
		if ( grinding )
			_trucks += ((c.Grind == SkateGrindSound.Trucks ? 1f : 0f) - _trucks) * settle;

		var change = 1f - MathF.Exp( -step / GroundChange );
		if ( _ground != c.Ground )
		{
			_ground = SkateSoundSurface.Blend( _ground, c.Ground, change );
			if ( MathF.Abs( _ground.Bright - c.Ground.Bright ) < 5f && MathF.Abs( _ground.Rumble - c.Ground.Rumble ) < 0.01f )
				_ground = c.Ground;
		}
		if ( grinding )
			_rail = c.Rail;
		_grainSource = c.Grains;

		// A new ground's recorded roll fades in over the last one's.
		var clip = _bank?.Roll( _ground.Roll );
		if ( clip != _rollClip )
		{
			_rollWas = _rollNow;
			_rollWasClip = _rollClip;
			_rollNow = default;
			_rollClip = clip;
			_rollMix = _rollWasClip is null ? 1f : 0f;
		}
		_rollMix = MathF.Min( 1f, _rollMix + step / RollChange );

		// Softer grounds and slower wheels are darker.
		var v = _speed;
		_rollStep = OnePole.Step( _ground.Bright * Math.Clamp( 0.35f + 0.11f * v, 0.35f, 1f ), Rate );
		_scrubStep = OnePole.Step( _ground.Bright * 0.8f, Rate );
		_dragStep = OnePole.Step( MathF.Min( 3200f, _ground.Bright ), Rate );
		_hollow.Tune( _ground.HollowTone, 1.4f, Rate );
		_whoosh.Tune( 250f + 40f * MathF.Min( c.Spin, 40f ), 1.4f, Rate );
	}

	private static float Ease( float level, float target, float step ) =>
		level + (target - level) * (1f - MathF.Exp( -step / (target > level ? Rise : Fall) ));

	private float Sample()
	{
		var mix = 0f;
		if ( _roll > 1e-4f )
			mix += Roll();
		if ( _grind > 1e-4f )
			mix += Grind();
		if ( _slide > 1e-4f )
			mix += Scrub();
		if ( _brake > 1e-4f )
			mix += Drag();
		if ( _spin > 1e-4f )
			mix += Whoosh();
		mix += Grains();
		mix -= _dc.Low( mix, OnePole.Step( 25f, Rate ) );
		var sample = SoundShape.Soft( mix );
		_quiet = MathF.Abs( sample ) < 1e-4f && _roll + _grind + _slide + _brake + _spin < 1e-4f ? _quiet + 1 : 0;
		return sample;
	}

	// The wheels on the ground: the ground's recorded roll at the wheels' pace, its boom and its rustle.
	private float Roll()
	{
		var g = _ground;
		var rate = Math.Clamp( 0.62f + 0.065f * _speed, 0.6f, 1.45f );
		var sound = _rollNow.Next( _rollClip, rate, Rate, ref _noise );
		if ( _rollMix < 1f )
		{
			var angle = _rollMix * MathF.PI * 0.5f;
			sound = sound * MathF.Sin( angle ) + _rollWas.Next( _rollWasClip, rate, Rate, ref _noise ) * MathF.Cos( angle );
		}
		sound = _rollLow2.Low( _rollLow1.Low( sound, _rollStep ), _rollStep );
		if ( g.Hollow > 0.01f )
			sound += 2f * g.Hollow * _hollow.Band( sound );

		// Rustle: soft scattered top in bursts (grass brushing the wheels).
		if ( g.Rustle > 0.01f )
		{
			var r = _noise.Next();
			var high = r - _rustleHigh.Low( r, OnePole.Step( 1500f, Rate ) );
			var burst = _rustleBurst.Low( _noise.Unit(), OnePole.Step( 18f, Rate ) );
			burst = MathF.Max( 0f, burst - 0.38f ) * 4f;
			sound += 0.2f * g.Rustle * burst * _rustleLow.Low( high, OnePole.Step( 3500f, Rate ) );
		}
		return RollLoudness * _roll * g.Rumble * sound;
	}

	// Trucks grinding metal or concrete, or the deck sliding, at the board's pace.
	private float Grind()
	{
		var rate = Math.Clamp( 0.75f + 0.045f * _speed, 0.75f, 1.3f );
		var trucks = _trucks;
		var sound = 0f;
		if ( trucks > 0.01f )
		{
			var metal = _rail.Ring > 0.3f;
			var clip = metal ? _bank?.GrindMetal : _bank?.GrindConcrete;
			sound += (metal ? MetalGrindLoudness : LedgeGrindLoudness) * trucks * _grindLoop.Next( clip, rate, Rate, ref _noise );
		}
		if ( trucks < 0.99f )
			sound += SlideLoudness * (1f - trucks) * _slideLoop.Next( _bank?.Slide, rate, Rate, ref _noise );
		return _grind * sound;
	}

	// A powerslide: the wheels scrubbing sideways, faster and higher the faster they slip.
	private float Scrub()
	{
		var rate = Math.Clamp( 0.85f + 0.04f * _slip, 0.85f, 1.25f );
		var sound = _scrubLoop.Next( _bank?.Powerslide, rate, Rate, ref _noise );
		sound = _scrubLow2.Low( _scrubLow1.Low( sound, _scrubStep ), _scrubStep );
		return ScrubLoudness * _slide * (0.4f + 0.6f * _ground.Rumble) * sound;
	}

	// A shoe dragged over the ground to slow the board: a powerslide's scrub, higher, softer and darker.
	private float Drag()
	{
		var sound = _dragLoop.Next( _bank?.Powerslide, 1.3f, Rate, ref _noise );
		sound = _dragLow2.Low( _dragLow1.Low( sound, _dragStep ), _dragStep );
		return DragLoudness * _brake * (1f - 0.5f * _ground.Damp) * sound;
	}

	// The board flipping or spinning in the air: air rushing past it, loudest as its broad faces cut through.
	private float Whoosh()
	{
		_angle += _spinRate / Rate;
		if ( _angle > MathF.Tau ) _angle -= MathF.Tau;
		var faces = 0.3f + 0.7f * MathF.Abs( MathF.Sin( _angle ) );
		return WhooshLoudness * _spin * faces * _whoosh.Band( _noise.Next() );
	}

	// Tiny pieces of the ground's own sound scattered under the wheels and under a dragging shoe.
	private float Grains()
	{
		var source = _grainSource;
		var sound = 0f;
		if ( source is not null )
		{
			var rate = (5f + 9f * _speed) * _roll * _ground.Grains + 30f * _brake;
			if ( rate > 0.5f && (_grainWait -= 1f) <= 0f )
			{
				_grainWait = -MathF.Log( 1f - 0.999f * _noise.Unit() ) / rate * Rate;
				Start( source );
			}
		}
		for ( var i = 0; i < MostGrains; i++ )
		{
			ref var grain = ref _grains[i];
			if ( grain.Length <= 0f )
				continue;
			if ( source is null || grain.Age >= grain.Length )
			{
				grain.Length = 0f;
				continue;
			}
			sound += grain.Gain * SoundShape.Window( grain.Age / grain.Length ) * source.At( grain.Place );
			grain.Place += grain.Step;
			grain.Age += 1f;
		}
		return _grainLow.Low( sound, OnePole.Step( MathF.Max( _ground.Bright * 0.6f, 2000f * _brake ), Rate ) );
	}

	private void Start( SoundGrains source )
	{
		for ( var i = 0; i < MostGrains; i++ )
		{
			ref var grain = ref _grains[i];
			if ( grain.Length > 0f )
				continue;
			var length = _noise.Range( 0.03f, 0.07f ) * Rate;
			grain.Step = source.Rate / (float)Rate * _noise.Range( 0.85f, 1.12f );
			grain.Place = source.Pick( ref _noise, (int)(length * grain.Step) + 2 );
			grain.Age = 0f;
			grain.Length = length;
			// Under the roll they belong to (a quiet ground's grains are quiet), and under a dragging shoe as loud as it.
			grain.Gain = (0.5f * RollLoudness * _roll * _ground.Rumble * _ground.Grains + DragLoudness * 0.26f * _brake)
				* _noise.Range( 0.5f, 1f );
			return;
		}
	}
}
