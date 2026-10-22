using System.Collections.Generic;

namespace Skateboarding.Core.Sound;

/// <summary>
/// What a ground's own sound is like, measured from its samples: where its weight sits (<see cref="Centre"/>), how rough
/// and bursty it is, and how much of it is low. Colours the board's procedural roll (see
/// <see cref="SkateSoundSurface.Coloured"/>).
/// </summary>
/// <param name="Centre">Rough centre of its spectrum, in Hz.</param>
/// <param name="Rough">How bursty it is, 0 smooth (a hiss) to 1 crackling (gravel underfoot).</param>
/// <param name="Low">Share of its energy below about 250 Hz, 0 to 1.</param>
internal readonly record struct SoundPrint( float Centre, float Rough, float Low )
{
	/// <summary>True when measured from a sound (false for a ground with no sound of its own).</summary>
	internal bool Heard => Centre > 0f;
}

/// <summary>
/// A ground's own sound (a footstep's, a scrape's) cut ready to be scattered under the wheels in tiny pieces, so the
/// board's roll and push carry the ground's real texture: grass rustles, gravel crunches. The samples are kept quiet and
/// even (their loudness evened out) and only their loud stretches are drawn from.
/// </summary>
internal sealed class SoundGrains
{
	// The longest stretch of a sound kept, in seconds, the level its loudness is evened to and the highest its peaks may
	// then reach (a sharp click in a quiet sound is brought down rather than the sound brought up to it).
	private const float Longest = 3f;
	private const float Level = 0.25f;
	private const float Highest = 1f;

	// Length (seconds) of the windows the loudness is measured over, and the share of the loudest a window needs to be
	// drawn from.
	private const float Window = 0.01f;
	private const float Loud = 0.25f;

	/// <summary>The samples, at <see cref="Rate"/>.</summary>
	internal float[] Samples { get; }

	/// <summary>Samples a second.</summary>
	internal int Rate { get; }

	/// <summary>Where the loud stretches start: the places a grain may be cut from.</summary>
	internal int[] Starts { get; }

	/// <summary>What the sound is like.</summary>
	internal SoundPrint Print { get; }

	private SoundGrains( float[] samples, int rate, int[] starts, SoundPrint print )
	{
		Samples = samples;
		Rate = rate;
		Starts = starts;
		Print = print;
	}

	/// <summary>Grains cut from 16-bit mono samples at <paramref name="rate"/>; null when they hold nothing to hear.</summary>
	internal static SoundGrains From( ReadOnlySpan<short> source, int rate )
	{
		if ( rate < 1000 || source.Length < rate / 20 )
			return null;
		var length = Math.Min( source.Length, (int)(Longest * rate) );
		var samples = new float[length];
		var mean = 0d;
		for ( var i = 0; i < length; i++ )
			mean += source[i];
		mean /= length;
		for ( var i = 0; i < length; i++ )
			samples[i] = (float)((source[i] - mean) / short.MaxValue);

		// Loudness window by window: the loud stretches are the ones drawn from, and the whole is evened to Level.
		var window = Math.Max( 16, (int)(Window * rate) );
		var windows = length / window;
		var loudness = new float[windows];
		var loudest = 0f;
		double energy = 0d, slope = 0d, low = 0d;
		var smooth = 0f;
		var lowStep = OnePole.Step( 250f, rate );
		for ( var w = 0; w < windows; w++ )
		{
			var sum = 0f;
			for ( var i = w * window; i < (w + 1) * window; i++ )
			{
				var x = samples[i];
				sum += x * x;
				var step = i > 0 ? x - samples[i - 1] : 0f;
				slope += step * step;
				smooth += lowStep * (x - smooth);
				low += smooth * smooth;
			}
			energy += sum;
			loudness[w] = MathF.Sqrt( sum / window );
			loudest = MathF.Max( loudest, loudness[w] );
		}
		if ( loudest < 1e-4f || energy <= 0d )
			return null;

		var starts = new List<int>();
		var loud = 0;
		var rms = 0d;
		for ( var w = 0; w < windows; w++ )
		{
			if ( loudness[w] < loudest * Loud )
				continue;
			starts.Add( w * window );
			rms += loudness[w] * loudness[w];
			loud++;
		}
		var peak = 0f;
		for ( var i = 0; i < length; i++ )
			peak = MathF.Max( peak, MathF.Abs( samples[i] ) );
		var gain = MathF.Min( Level / (float)Math.Sqrt( rms / Math.Max( loud, 1 ) ), Highest / peak );
		for ( var i = 0; i < length; i++ )
			samples[i] *= gain;

		// The spectrum's rough centre from how steep the sound is against how loud (a sine's slope over its level is its
		// frequency), how much louder its loud stretches are than its average, and its low share.
		var centre = (float)(rate / MathF.Tau * Math.Sqrt( slope / energy ));
		var average = MathF.Sqrt( (float)(energy / (windows * window)) );
		var rough = Math.Clamp( (loudest / MathF.Max( average, 1e-5f ) - 2f) / 6f, 0f, 1f );
		var share = Math.Clamp( (float)(low / energy), 0f, 1f );
		return new( samples, rate, starts.ToArray(), new( centre, rough, share ) );
	}

	/// <summary>A place to start a grain of <paramref name="length"/> samples, drawn from the loud stretches.</summary>
	internal int Pick( ref SoundNoise noise, int length )
	{
		var start = Starts.Length > 0 ? Starts[(int)(noise.Unit() * (Starts.Length - 1))] : 0;
		return Math.Clamp( start, 0, Math.Max( 0, Samples.Length - length - 1 ) );
	}

	/// <summary>The sound at a place between samples.</summary>
	internal float At( float place )
	{
		var i = (int)place;
		if ( i < 0 || i + 1 >= Samples.Length )
			return 0f;
		var t = place - i;
		return Samples[i] + (Samples[i + 1] - Samples[i]) * t;
	}
}
