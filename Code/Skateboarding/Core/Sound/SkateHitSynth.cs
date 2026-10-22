namespace Skateboarding.Core.Sound;

/// <summary>
/// A board's short sounds, made from real recordings (<see cref="SkateSoundBank"/>) the moment they happen: the pop of the
/// tail, the landing, a push's foot on the ground, the feet catching a flipped board, trucks or deck locking onto a rail
/// and leaving it, and the board knocking about on its own. Each is a different recording than last time, pitched a
/// little differently, darker and quieter the softer it is, and answers the ground it happens on: a wooden ramp booms,
/// metal rings, grass muffles it and rustles, and a ground's own sound is mixed in. Mono at <see cref="Rate"/>, written into
/// a buffer this keeps: read each sound before making the next.
/// </summary>
internal sealed class SkateHitSynth
{
	internal const int Rate = SkateRollSynth.Rate;

	// The longest sound made, in seconds.
	private const float Longest = 1.2f;

	// A low pass high enough to leave a recording as it is.
	private const float Open = 15000f;

	private readonly SkateSoundBank _bank;
	private readonly float[] _mix = new float[(int)(Longest * Rate)];
	private SoundNoise _noise;
	private int _length;

	internal SkateHitSynth( int seed, SkateSoundBank bank )
	{
		_noise = new SoundNoise( seed );
		_bank = bank;
	}

	/// <summary>The ollie's pop, 0 to 1 strong: the tail cracking down on the ground, the ground answering.</summary>
	internal ReadOnlySpan<float> Pop( float strength, SkateSoundSurface ground, SoundGrains grains )
	{
		var s = Math.Clamp( strength, 0f, 1f );
		Begin( 0.5f );
		Play( _bank.Pop( ref _noise ), 0f, 1f, _noise.Range( 0.95f, 1.05f ) * (0.98f + 0.05f * s) );
		Answer( ground, grains, 0.5f );
		return Finish( 0.8f * (0.55f + 0.45f * s), Dull( ground, s ), ground );
	}

	/// <summary>A landing, 0 to 1 hard: wheels, trucks and deck coming down, the ground answering.</summary>
	internal ReadOnlySpan<float> Land( float impact, SkateSoundSurface ground, SoundGrains grains )
	{
		var i = Math.Clamp( impact, 0f, 1f );
		Begin( 0.7f );
		Play( _bank.Land( ref _noise ), 0f, 1f, _noise.Range( 0.95f, 1.05f ) * (1.04f - 0.08f * i) );
		Answer( ground, grains, 0.6f );
		return Finish( 0.9f * (0.35f + 0.65f * i), Dull( ground, 0.4f + 0.6f * i ), ground );
	}

	/// <summary>A push, 0 to 1 strong: the shoe coming down on the ground (in the ground's own footstep) and scuffing it.</summary>
	internal ReadOnlySpan<float> Push( float strength, SkateSoundSurface ground, SoundGrains grains )
	{
		var s = Math.Clamp( strength, 0f, 1f );
		Begin( 0.35f );
		if ( grains is not null )
			Grain( 0f, 1f, 0.25f, grains, true );
		else
			Play( _bank.Knock( ref _noise ), 0f, 0.5f, 0.75f );
		Scuff( 0.015f, 0.4f * (1f - 0.5f * ground.Damp), 0.14f );
		if ( ground.Rustle > 0.01f )
			Rustle( 0f, 0.5f * ground.Rustle, 0.12f );
		return Finish( 0.28f * (0.6f + 0.4f * s), MathF.Min( ground.Bright, 5000f ), ground );
	}

	/// <summary>The feet catching a flipped board, 0 to 1 firm: a soft, dull landing of shoes on grip.</summary>
	internal ReadOnlySpan<float> Catch( float strength )
	{
		var s = Math.Clamp( strength, 0f, 1f );
		Begin( 0.4f );
		Play( _bank.Land( ref _noise ), 0f, 1f, _noise.Range( 1.1f, 1.2f ) );
		return Finish( 0.25f * (0.5f + 0.5f * s), 1600f, default );
	}

	/// <summary>
	/// Locking onto a rail or ledge, 0 to 1 hard: trucks clank on metal (and the rail rings) or crack on concrete; a slide
	/// lands the deck with a wooden thock.
	/// </summary>
	internal ReadOnlySpan<float> GrindOn( SkateGrindSound kind, float strength, SkateSoundSurface rail )
	{
		var s = Math.Clamp( strength, 0f, 1f );
		var pitch = _noise.Range( 0.96f, 1.04f );
		Begin( 0.8f );
		if ( kind == SkateGrindSound.Slide )
		{
			Play( _bank.Land( ref _noise ), 0f, 1f, 0.9f * pitch );
			if ( rail.Ring > 0.3f )
				Play( _bank.Clank( ref _noise ), 0f, 0.3f, pitch );
			return Finish( 0.6f * (0.5f + 0.5f * s), 3500f, default );
		}
		if ( rail.Ring > 0.3f )
		{
			Play( _bank.Clank( ref _noise ), 0f, 1f, pitch );
			return Finish( 0.75f * (0.5f + 0.5f * s), Open, default );
		}
		Play( _bank.Land( ref _noise ), 0f, 1f, 1.05f * pitch );
		Play( _bank.Knock( ref _noise ), 0f, 0.5f, pitch );
		return Finish( 0.7f * (0.5f + 0.5f * s), 9000f, default );
	}

	/// <summary>Leaving a rail, 0 to 1 fast: the trucks' last light ring off the metal, or the board's tap.</summary>
	internal ReadOnlySpan<float> GrindOff( SkateGrindSound kind, float strength, SkateSoundSurface rail )
	{
		var s = Math.Clamp( strength, 0f, 1f );
		Begin( 0.6f );
		if ( kind == SkateGrindSound.Trucks && rail.Ring > 0.3f )
		{
			Play( _bank.Clank( ref _noise ), 0f, 1f, _noise.Range( 1.08f, 1.16f ) );
			return Finish( 0.28f * (0.5f + 0.5f * s), 6000f, default );
		}
		Play( _bank.Knock( ref _noise ), 0f, 1f, _noise.Range( 1.05f, 1.15f ) );
		return Finish( 0.25f * (0.5f + 0.5f * s), 4000f, default );
	}

	/// <summary>The board knocking into something on its own (tumbling from a bail, thrown), 0 to 1 hard.</summary>
	internal ReadOnlySpan<float> Knock( float strength, SkateSoundSurface ground )
	{
		var s = Math.Clamp( strength, 0f, 1f );
		Begin( 0.5f );
		Play( _bank.Knock( ref _noise ), 0f, 1f, _noise.Range( 0.9f, 1.1f ) );
		Answer( ground, null, 0.4f );
		return Finish( 0.65f * (0.25f + 0.75f * s), Dull( ground, s ), ground );
	}

	// How far a hit is low passed: soft grounds and soft hits are darker.
	private static float Dull( in SkateSoundSurface ground, float hard ) =>
		MathF.Min( ground.Bright, Open - (Open - 1200f) * ground.Damp ) * (0.55f + 0.45f * hard);

	// The ground's answer to a hit: metal's ring, grass's rustle and a piece of the ground's own sound (a hollow ground's
	// boom is drawn out of the whole hit in Finish).
	private void Answer( in SkateSoundSurface ground, SoundGrains grains, float level )
	{
		if ( ground.Ring > 0.01f )
			Play( _bank.Clank( ref _noise ), 0f, level * 0.7f * ground.Ring, 1.25f );
		if ( ground.Rustle > 0.01f )
			Rustle( 0f, level * ground.Rustle, 0.1f );
		if ( grains is not null && ground.Grains > 0f )
			Grain( 0f, level * ground.Grains, 0.12f, grains, false );
	}

	private void Begin( float seconds )
	{
		_length = Math.Min( _mix.Length, (int)(seconds * Rate) );
		Array.Clear( _mix, 0, _length );
	}

	// A recording mixed in at a place, at a level against its own peak, read faster (higher) or slower (lower) by pitch.
	private void Play( SoundClip clip, float at, float level, float pitch )
	{
		if ( clip is null || level <= 0.001f )
			return;
		var start = (int)(at * Rate);
		var step = clip.Rate / (double)Rate * pitch;
		var place = 0d;
		for ( var n = start; n < _length && place < clip.Samples.Length; n++, place += step )
			_mix[n] += level * clip.At( place );
	}

	// A short piece of a powerslide's scrub, faded in and out: a shoe scuffing the ground.
	private void Scuff( float at, float level, float length )
	{
		var clip = _bank.Powerslide;
		if ( clip is null || level <= 0.001f )
			return;
		var start = (int)(at * Rate);
		var count = Math.Min( _length - start, (int)(length * Rate) );
		var place = (double)(_noise.Unit() * (clip.Samples.Length - count * 2));
		var step = clip.Rate / (double)Rate * 1.2;
		for ( var n = 0; n < count; n++, place += step )
			_mix[start + n] += level * clip.Around( place ) * SoundShape.Window( n / (float)count );
	}

	// Soft scattered top in quick bursts: grass and leaves brushed.
	private void Rustle( float at, float level, float length )
	{
		var start = (int)(at * Rate);
		var count = Math.Min( _length - start, (int)(length * Rate) );
		var high = new OnePole();
		var top = new OnePole();
		var bursts = new OnePole();
		for ( var n = 0; n < count; n++ )
		{
			var x = _noise.Next();
			var burst = MathF.Max( 0f, bursts.Low( _noise.Unit(), OnePole.Step( 60f, Rate ) ) - 0.35f );
			_mix[start + n] += level * 0.5f * top.Low( x - high.Low( x, OnePole.Step( 1500f, Rate ) ), OnePole.Step( 4000f, Rate ) )
				* burst * SoundShape.Window( n / (float)count );
		}
	}

	// A piece of the ground's own sound: from its start (the whole footstep) or from a loud stretch of it.
	private void Grain( float at, float level, float length, SoundGrains grains, bool fromStart )
	{
		var start = (int)(at * Rate);
		var count = Math.Min( _length - start, (int)(length * Rate) );
		var step = grains.Rate / (float)Rate * _noise.Range( 0.92f, 1.08f );
		var place = fromStart ? 0f : grains.Pick( ref _noise, (int)(count * step) + 2 );
		for ( var n = 0; n < count; n++, place += step )
		{
			var window = fromStart ? MathF.Min( 1f, (count - n) / (0.03f * Rate) ) : SoundShape.Window( n / (float)count );
			_mix[start + n] += level * 2f * grains.At( place ) * window;
		}
	}

	// The whole low passed (twice, at dull Hz), a hollow ground's boom drawn out of it, its peak brought to level and
	// rounded off.
	private ReadOnlySpan<float> Finish( float level, float dull, SkateSoundSurface ground )
	{
		var first = new OnePole();
		var second = new OnePole();
		var step = OnePole.Step( dull, Rate );
		var filter = dull < Open;
		var hollow = new Svf();
		if ( ground.Hollow > 0.01f )
			hollow.Tune( ground.HollowTone, 1.4f, Rate );
		var peak = 0f;
		for ( var n = 0; n < _length; n++ )
		{
			var x = _mix[n];
			if ( filter )
				x = second.Low( first.Low( x, step ), step );
			if ( ground.Hollow > 0.01f )
				x += 2.5f * ground.Hollow * hollow.Band( x );
			_mix[n] = x;
			peak = MathF.Max( peak, MathF.Abs( x ) );
		}
		var gain = peak > 1e-6f ? level / peak : 0f;
		for ( var n = 0; n < _length; n++ )
			_mix[n] = SoundShape.Soft( _mix[n] * gain );
		return _mix.AsSpan( 0, _length );
	}
}
