using Sandbox;
using Skateboarding.Core.Sound;

namespace Skateboarding.Engine;

/// <summary>
/// The player's board sound volumes (see <see cref="SkateSoundMix"/>), set on the docs' Sound page and kept between
/// sessions in the game's data folder on their machine. Every board this client hears plays at them.
/// </summary>
internal static class SkateSoundSettings
{
	private const string File = "skateboard/sound.json";

	private sealed class Saved
	{
		public float All { get; set; } = 1f;
		public float Rolling { get; set; } = 1f;
		public float Tricks { get; set; } = 1f;
		public float Grinds { get; set; } = 1f;
	}

	private static SkateSoundMix? _mix;

	/// <summary>The volumes as they are now (read from the data folder the first time).</summary>
	internal static SkateSoundMix Mix => _mix ??= Load();

	/// <summary>Sets the volumes at once; <see cref="Save"/> keeps them.</summary>
	internal static void Set( SkateSoundMix mix ) => _mix = mix.Clamped();

	/// <summary>Every volume back as the sounds are made, and kept.</summary>
	internal static void Reset()
	{
		_mix = SkateSoundMix.Full;
		Save();
	}

	internal static void Save()
	{
		var mix = Mix;
		try
		{
			FileSystem.Data.CreateDirectory( "skateboard" );
			FileSystem.Data.WriteJson( File, new Saved { All = mix.All, Rolling = mix.Rolling, Tricks = mix.Tricks, Grinds = mix.Grinds } );
		}
		catch ( Exception error )
		{
			Log.Warning( $"Skateboard: your sound volumes could not be saved ({error.Message})." );
		}
	}

	private static SkateSoundMix Load()
	{
		try
		{
			if ( !FileSystem.Data.FileExists( File ) )
				return SkateSoundMix.Full;
			var saved = FileSystem.Data.ReadJson<Saved>( File );
			return saved is null ? SkateSoundMix.Full : new SkateSoundMix( saved.All, saved.Rolling, saved.Tricks, saved.Grinds ).Clamped();
		}
		catch ( Exception error )
		{
			Log.Warning( $"Skateboard: your sound volumes could not be read ({error.Message}); the sounds play as made." );
			return SkateSoundMix.Full;
		}
	}
}
