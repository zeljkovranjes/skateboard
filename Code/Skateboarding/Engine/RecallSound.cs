using Sandbox;

namespace Skateboarding.Engine;

/// <summary>Plays the sound of a board called home (<see cref="RecallSoundSynth"/>), made here, following the board.</summary>
internal static class RecallSound
{
	// How loud it plays.
	private const float Volume = 0.8f;

	internal static void Play( GameObject board, float shake, float flight, int seed )
	{
		if ( Application.IsHeadless || !board.IsValid() )
			return;
		// The stream is let go once played: like the engine's own speech it is freed once its sound has finished.
		var stream = new SoundStream( RecallSoundSynth.Rate );
		stream.WriteData( RecallSoundSynth.Build( shake, flight, seed ) );
		stream.Close();
		var handle = stream.Play( Volume );
		if ( handle is null )
			return;
		handle.Parent = board;
		handle.LocalTransform = new Transform( Vector3.Zero );
		handle.FollowParent = true;
	}
}
