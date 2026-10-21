namespace Skateboarding.Engine;

/// <summary>
/// The sound of a board called home like a thrown hammer, made from nothing but code: as it shakes where it lies a deep
/// rumble swells under crackles of static; it tears loose with a thunder crack and a sub-bass boom, flies with a deep
/// rushing whoosh and a low "vwoom" that rise as it nears, and lands in the hand with a heavy boom and a roll of thunder.
/// Low and heavy throughout: no buzzing tone, no rattle. Mono 16-bit samples at <see cref="Rate"/>.
/// </summary>
internal static class RecallSoundSynth
{
	internal const int Rate = 44100;

	// Seconds the catch's boom and thunder roll on after the board lands in the hand, and the longest flight given its
	// own sound.
	private const float CatchRoll = 0.9f;
	private const float LongestFlight = 30f;

	internal static short[] Build( float shake, float flight, int seed )
	{
		shake = Math.Clamp( shake, 0f, 5f );
		flight = Math.Clamp( flight, 0.05f, LongestFlight );
		var mix = new float[(int)((shake + flight + CatchRoll) * Rate)];
		var random = new Random( seed );
		if ( shake > 0f )
		{
			Rumble( mix, 0f, shake + 0.3f, random, ( t, length ) => Ease( t / shake ) * 0.45f * Fade( t, length, 0.25f ) );
			Crackle( mix, 0f, shake, random, t => 3f + 17f * Ease( t / shake ), t => 0.12f + 0.3f * Ease( t / shake ) );
			Crack( mix, shake, 0.55f, random );
			Boom( mix, shake, 0.6f, 62f, 30f, 0.45f );
		}
		Whoosh( mix, shake, flight, random );
		Crackle( mix, shake, flight, random, t => 3f, t => 0.08f );
		Boom( mix, shake + flight, 1f, 78f, 32f, 0.55f );
		Thud( mix, shake + flight, 0.7f, random );
		Rumble( mix, shake + flight, CatchRoll, random, ( t, length ) => 0.55f * MathF.Exp( -t / 0.3f ) * MathF.Min( 1f, t / 0.01f ) );
		var samples = new short[mix.Length];
		for ( var i = 0; i < mix.Length; i++ )
			samples[i] = (short)(MathF.Tanh( mix[i] ) * 0.85f * short.MaxValue);
		return samples;
	}

	// Smooth rise from 0 to 1.
	private static float Ease( float x )
	{
		x = Math.Clamp( x, 0f, 1f );
		return x * x * (3f - 2f * x);
	}

	// 1 inside a span, easing out over its last seconds.
	private static float Fade( float t, float length, float seconds ) => MathF.Min( 1f, (length - t) / seconds );

	// Thunder: brown noise (the deep end of noise), kept below about 150 Hz, at a level given by time into it.
	private static void Rumble( float[] mix, float at, float length, Random random, Func<float, float, float> level )
	{
		var start = (int)(at * Rate);
		var count = (int)(length * Rate);
		var brown = 0f;
		var low = 0f;
		var smoothing = 1f - MathF.Exp( -MathF.Tau * 150f / Rate );
		for ( var n = 0; n < count && start + n < mix.Length; n++ )
		{
			brown = brown * 0.995f + ((float)random.NextDouble() * 2f - 1f) * 0.1f;
			low += smoothing * (brown - low);
			mix[start + n] += low * 2.5f * level( n / (float)Rate, length );
		}
	}

	// Static crackling: bursts of a few sharp clicks close together, at a rate (bursts a second) and level given by time.
	private static void Crackle( float[] mix, float at, float length, Random random, Func<float, float> rate, Func<float, float> level )
	{
		var t = (float)-Math.Log( 1d - random.NextDouble() ) / rate( 0f );
		while ( t < length )
		{
			var clicks = random.Next( 2, 6 );
			var when = t;
			for ( var c = 0; c < clicks; c++ )
			{
				Spark( mix, at + when, level( t ) * (0.5f + 0.5f * (float)random.NextDouble()), random );
				when += 0.002f + 0.006f * (float)random.NextDouble();
			}
			t += (float)-Math.Log( 1d - random.NextDouble() ) / MathF.Max( rate( t ), 0.1f );
		}
	}

	// One click of static: a few milliseconds of noise kept between about 600 Hz and 3 kHz, so it crunches rather than
	// thumps or hisses.
	private static void Spark( float[] mix, float at, float level, Random random )
	{
		var start = (int)(at * Rate);
		var count = random.Next( 60, 200 );
		float floor = 0f, top = 0f, top2 = 0f;
		var under = 1f - MathF.Exp( -MathF.Tau * 600f / Rate );
		var over = 1f - MathF.Exp( -MathF.Tau * 3000f / Rate );
		for ( var n = 0; n < count && start + n < mix.Length; n++ )
		{
			var noise = (float)random.NextDouble() * 2f - 1f;
			floor += under * (noise - floor);
			top += over * (noise - floor - top);
			top2 += over * (top - top2);
			mix[start + n] += top2 * 2.5f * level * MathF.Exp( -n / (count * 0.35f) );
		}
	}

	// A thunder crack: a sharp burst of mid-range noise dying in a few tens of milliseconds.
	private static void Crack( float[] mix, float at, float level, Random random )
	{
		var start = (int)(at * Rate);
		var count = (int)(0.12f * Rate);
		float low = 0f, band = 0f;
		var tune = 2f * MathF.Sin( MathF.PI * 900f / Rate );
		for ( var n = 0; n < count && start + n < mix.Length; n++ )
		{
			var noise = (float)random.NextDouble() * 2f - 1f;
			low += tune * band;
			var high = noise - low - 1.2f * band;
			band += tune * high;
			mix[start + n] += band * level * 1.5f * MathF.Exp( -n / (0.025f * Rate) );
		}
	}

	// A sub-bass boom: a pure tone falling from high to low Hz and dying away over length seconds.
	private static void Boom( float[] mix, float at, float level, float high, float low, float length )
	{
		var start = (int)(at * Rate);
		var count = (int)(length * Rate);
		var phase = 0d;
		for ( var n = 0; n < count && start + n < mix.Length; n++ )
		{
			var k = n / (float)count;
			phase += (low + (high - low) * MathF.Exp( -5f * k )) / Rate;
			var envelope = MathF.Exp( -4f * k ) * MathF.Min( 1f, n / (0.004f * Rate) );
			mix[start + n] += MathF.Sin( (float)(phase % 1d) * MathF.Tau ) * level * envelope;
		}
	}

	// The board striking the hand: a short, dull knock of low noise.
	private static void Thud( float[] mix, float at, float level, Random random )
	{
		var start = (int)(at * Rate);
		var count = (int)(0.08f * Rate);
		var low = 0f;
		var smoothing = 1f - MathF.Exp( -MathF.Tau * 400f / Rate );
		for ( var n = 0; n < count && start + n < mix.Length; n++ )
		{
			low += smoothing * ((float)random.NextDouble() * 2f - 1f - low);
			mix[start + n] += low * 3f * level * MathF.Exp( -n / (0.015f * Rate) );
		}
	}

	// The flight: deep rushing air, noise through a band that rises from 120 Hz to 700 Hz as it nears, and a soft low tone
	// gliding from 45 Hz to 95 Hz under it, both swelling toward the hand.
	private static void Whoosh( float[] mix, float shake, float flight, Random random )
	{
		var start = (int)(shake * Rate);
		var last = Math.Min( (int)((shake + flight) * Rate), mix.Length );
		var length = Math.Max( last - start, 1 );
		float low = 0f, band = 0f, soft = 0f, softer = 0f;
		var phase = 0d;
		for ( var i = start; i < last; i++ )
		{
			var f = (i - start) / (float)length;
			var centre = 120f * MathF.Pow( 700f / 120f, f );
			var tune = 2f * MathF.Sin( MathF.PI * centre / Rate );
			var noise = (float)random.NextDouble() * 2f - 1f;
			low += tune * band;
			var high = noise - low - 0.9f * band;
			band += tune * high;
			// The band's upper skirt taken down further, so the air rushes rather than hisses.
			var roll = 1f - MathF.Exp( -MathF.Tau * centre * 2.5f / Rate );
			soft += roll * (band - soft);
			softer += roll * (soft - softer);
			phase += 45f * MathF.Pow( 95f / 45f, f ) / Rate;
			var swell = 0.3f + 0.7f * f * f;
			var edges = MathF.Min( 1f, (i - start) / (0.05f * Rate) ) * MathF.Min( 1f, (last - i) / (0.01f * Rate) );
			mix[i] += (softer * 2.2f + MathF.Sin( (float)(phase % 1d) * MathF.Tau ) * 0.35f) * swell * edges;
		}
	}
}
