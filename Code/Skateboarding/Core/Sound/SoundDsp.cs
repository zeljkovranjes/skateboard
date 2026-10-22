namespace Skateboarding.Core.Sound;

/// <summary>White noise from a 32-bit xorshift: the same seed gives the same sound, and nothing is allocated.</summary>
internal struct SoundNoise
{
	private uint _state;

	internal SoundNoise( int seed ) => _state = (uint)seed * 2654435761u | 1u;

	/// <summary>The next sample, from -1 to 1.</summary>
	internal float Next()
	{
		_state ^= _state << 13;
		_state ^= _state >> 17;
		_state ^= _state << 5;
		return (_state >> 8) * (2f / 16777216f) - 1f;
	}

	/// <summary>The next sample, from 0 to 1.</summary>
	internal float Unit() => (Next() + 1f) * 0.5f;

	/// <summary>A value from <paramref name="low"/> to <paramref name="high"/>.</summary>
	internal float Range( float low, float high ) => low + (high - low) * Unit();
}

/// <summary>A one-pole low pass: the gentlest filter, for softening and for smoothing controls.</summary>
internal struct OnePole
{
	private float _y;

	internal float Low( float x, float k ) => _y += k * (x - _y);

	internal void Reset( float to = 0f ) => _y = to;

	/// <summary>The smoothing step for a corner at <paramref name="hz"/>.</summary>
	internal static float Step( float hz, float rate ) => 1f - MathF.Exp( -MathF.Tau * MathF.Min( hz, rate * 0.45f ) / rate );
}

/// <summary>
/// A state-variable filter (the trapezoidal kind): band and low pass of the same input, stable at any setting. Its
/// tuning is set per block (<see cref="Tune"/>), its samples run one at a time.
/// </summary>
internal struct Svf
{
	private float _ic1, _ic2;
	private float _a1, _a2, _a3, _k;

	internal void Tune( float hz, float q, float rate )
	{
		var g = MathF.Tan( MathF.PI * Math.Clamp( hz, 10f, rate * 0.45f ) / rate );
		_k = 1f / MathF.Max( q, 0.05f );
		_a1 = 1f / (1f + g * (g + _k));
		_a2 = g * _a1;
		_a3 = g * _a2;
	}

	/// <summary>Runs one sample, giving its band pass (normalised to unit gain at the centre) and its low pass.</summary>
	internal float Band( float x, out float low )
	{
		var v3 = x - _ic2;
		var v1 = _a1 * _ic1 + _a2 * v3;
		var v2 = _ic2 + _a2 * _ic1 + _a3 * v3;
		_ic1 = 2f * v1 - _ic1;
		_ic2 = 2f * v2 - _ic2;
		low = v2;
		return v1 * _k;
	}

	internal float Band( float x ) => Band( x, out _ );
}

/// <summary>Shaping shared by every sound: the soft clip that keeps peaks round, and easing curves.</summary>
internal static class SoundShape
{
	/// <summary>A soft clip (a rational fit of tanh): leaves quiet sound alone and rounds loud peaks off.</summary>
	internal static float Soft( float x )
	{
		x = Math.Clamp( x, -3f, 3f );
		var x2 = x * x;
		return x * (27f + x2) / (27f + 9f * x2);
	}

	/// <summary>Smooth rise from 0 at <paramref name="from"/> to 1 at <paramref name="to"/>.</summary>
	internal static float Ease( float x, float from, float to )
	{
		var t = Math.Clamp( (x - from) / (to - from), 0f, 1f );
		return t * t * (3f - 2f * t);
	}

	/// <summary>A raised-cosine window: 0 at both ends, 1 in the middle (<paramref name="t"/> from 0 to 1).</summary>
	internal static float Window( float t ) => t <= 0f || t >= 1f ? 0f : 0.5f - 0.5f * MathF.Cos( MathF.Tau * t );
}
