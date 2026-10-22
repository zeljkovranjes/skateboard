using Sandbox;
using Skateboarding.Core.Sound;

namespace Skateboarding.Engine;

/// <summary>
/// A board's continuous sound playing in the world: a stream that follows the board, kept a few hundredths of a second
/// ahead with what its <see cref="SkateRollSynth"/> makes of the board's controls each frame. Its short sounds play once
/// each, through <see cref="Once"/>.
/// </summary>
internal sealed class SkateSoundVoice : IDisposable
{
	private const int Rate = SkateRollSynth.Rate;

	// Seconds of sound kept queued ahead: enough to ride out an uneven frame, few enough that it keeps up with the board.
	private const float Ahead = 0.06f;

	// Most seconds written in one frame (after a long hitch the stream catches up from now, not from where it stalled).
	private const float MostWrite = 0.12f;

	// Seconds the stream fades out over when let go.
	private const float LetGo = 0.05f;

	private readonly SoundStream _stream;
	private readonly SoundHandle _handle;
	private readonly SkateRollSynth _synth;
	private readonly float[] _mix = new float[(int)(MostWrite * Rate)];
	private readonly short[] _pcm = new short[(int)(MostWrite * Rate)];

	// Samples of a short sound, ready to hand to its stream.
	private static readonly short[] OncePcm = new short[(int)(1.5f * Rate)];

	internal SkateSoundVoice( GameObject board, int seed, SkateSoundBank bank )
	{
		_synth = new SkateRollSynth( seed, bank );
		_stream = new SoundStream( Rate );
		_handle = _stream.Play();
		Follow( _handle, board );
	}

	/// <summary>True once the board has been silent a while: the voice can be let go.</summary>
	internal bool Quiet => _synth.Quiet;

	/// <summary>Tops the stream up to <see cref="Ahead"/> with the sound of these controls.</summary>
	internal void Feed( in SkateSoundControls controls )
	{
		var want = Math.Min( (int)(Ahead * Rate) - _stream.QueuedSampleCount, _mix.Length );
		var room = _stream.MaxWriteSampleCount;
		if ( room > 0 )
			want = Math.Min( want, room );
		if ( want <= 0 )
			return;
		var mix = _mix.AsSpan( 0, want );
		_synth.Render( mix, controls );
		_stream.WriteData( Pcm( mix, _pcm ) );
	}

	public void Dispose()
	{
		_handle?.Stop( LetGo );
		_stream.Dispose();
	}

	/// <summary>Plays a short sound once at the board, following it; the stream is let go once it has played.</summary>
	internal static void Once( GameObject board, ReadOnlySpan<float> sound )
	{
		if ( sound.Length == 0 || !board.IsValid() )
			return;
		var stream = new SoundStream( Rate );
		stream.WriteData( Pcm( sound[..Math.Min( sound.Length, OncePcm.Length )], OncePcm ) );
		stream.Close();
		Follow( stream.Play(), board );
	}

	private static void Follow( SoundHandle handle, GameObject board )
	{
		if ( handle is null )
			return;
		handle.Parent = board;
		handle.LocalTransform = new Transform( Vector3.Zero );
		handle.FollowParent = true;
	}

	private static Span<short> Pcm( ReadOnlySpan<float> sound, short[] into )
	{
		for ( var i = 0; i < sound.Length; i++ )
			into[i] = (short)(Math.Clamp( sound[i], -1f, 1f ) * short.MaxValue);
		return into.AsSpan( 0, sound.Length );
	}
}
