namespace Skateboarding.Core.Sound;

/// <summary>A recorded sound held as samples, read at any place between them (so it plays at any pitch).</summary>
internal sealed class SoundClip
{
	/// <summary>The samples, from -1 to 1.</summary>
	internal float[] Samples { get; }

	/// <summary>Samples a second.</summary>
	internal int Rate { get; }

	internal SoundClip( float[] samples, int rate )
	{
		Samples = samples;
		Rate = rate;
	}

	/// <summary>A clip of 16-bit mono samples; null when there are none.</summary>
	internal static SoundClip From( ReadOnlySpan<short> samples, int rate )
	{
		if ( samples.Length == 0 || rate < 1000 )
			return null;
		var floats = new float[samples.Length];
		for ( var i = 0; i < samples.Length; i++ )
			floats[i] = samples[i] / (float)short.MaxValue;
		return new SoundClip( floats, rate );
	}

	/// <summary>Seconds long.</summary>
	internal float Seconds => Samples.Length / (float)Rate;

	/// <summary>The sound at a place between samples; silence outside the clip.</summary>
	internal float At( double place )
	{
		if ( place < 0d )
			return 0f;
		var i = (int)place;
		if ( i + 1 >= Samples.Length )
			return i < Samples.Length ? Samples[i] : 0f;
		var t = (float)(place - i);
		return Samples[i] + (Samples[i + 1] - Samples[i]) * t;
	}

	/// <summary>The sound at a place between samples, the clip read round and round.</summary>
	internal float Around( double place )
	{
		var length = Samples.Length;
		place %= length;
		if ( place < 0d )
			place += length;
		var i = (int)place;
		var t = (float)(place - i);
		var next = i + 1 < length ? i + 1 : 0;
		return Samples[i] + (Samples[next] - Samples[i]) * t;
	}
}

/// <summary>
/// Plays a recorded loop (a roll, a grind) at any pitch for as long as it is wanted without ever being heard to repeat:
/// it reads the recording round and round, and every second or so crossfades (equal power, so the level holds) to a new
/// place in it, picked at random.
/// </summary>
internal struct LoopPlayer
{
	// Seconds a crossfade to a new place takes, and the shortest and longest stretch played from one place.
	private const float Crossfade = 0.08f;
	private const float Shortest = 0.6f;
	private const float Longest = 1.6f;

	private double _here, _next;
	private float _fade;
	private bool _fading;
	private int _untilJump;
	private SoundClip _clip;

	/// <summary>
	/// The next sample of <paramref name="clip"/> at <paramref name="rate"/> (its speed against its own: above 1 is higher
	/// and faster), for an output at <paramref name="outputRate"/> samples a second.
	/// </summary>
	internal float Next( SoundClip clip, float rate, int outputRate, ref SoundNoise noise )
	{
		if ( clip is null )
			return 0f;
		if ( clip != _clip )
		{
			_clip = clip;
			_here = noise.Unit() * clip.Samples.Length;
			_fading = false;
			_untilJump = (int)(noise.Range( Shortest, Longest ) * outputRate);
		}
		var step = clip.Rate / (double)outputRate * rate;
		if ( !_fading && --_untilJump <= 0 )
		{
			_next = noise.Unit() * clip.Samples.Length;
			_fade = 0f;
			_fading = true;
		}
		var sample = clip.Around( _here );
		_here += step;
		if ( !_fading )
			return sample;
		var other = clip.Around( _next );
		_next += step;
		_fade += 1f / (Crossfade * outputRate);
		if ( _fade >= 1f )
		{
			_here = _next;
			_fading = false;
			_untilJump = (int)(noise.Range( Shortest, Longest ) * outputRate);
			return other;
		}
		var angle = _fade * MathF.PI * 0.5f;
		return sample * MathF.Cos( angle ) + other * MathF.Sin( angle );
	}
}
