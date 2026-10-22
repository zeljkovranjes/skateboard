using System;
using Sandbox;

namespace Skateboarding.Engine.Finish;

/// <summary>
/// The player's own board, kept between sessions in the game's data folder on their machine: the look they last
/// finished a board with, which every board they ride or carry takes from then on.
/// </summary>
internal static class BoardLookStore
{
	private const string File = "skateboard/my-board.json";

	private sealed class Saved
	{
		public string Look { get; set; }
	}

	/// <summary>The look the player last saved, or null if they never finished a board.</summary>
	internal static BoardLook? Load()
	{
		try
		{
			if ( !FileSystem.Data.FileExists( File ) )
				return null;
			var saved = FileSystem.Data.ReadJson<Saved>( File );
			return string.IsNullOrEmpty( saved?.Look ) ? null : BoardLook.Decode( saved.Look );
		}
		catch ( Exception error )
		{
			Log.Warning( $"Skateboard: your saved board could not be read ({error.Message}); boards keep their own look." );
			return null;
		}
	}

	internal static void Save( BoardLook look )
	{
		try
		{
			FileSystem.Data.CreateDirectory( "skateboard" );
			FileSystem.Data.WriteJson( File, new Saved { Look = look.Encode() } );
		}
		catch ( Exception error )
		{
			Log.Warning( $"Skateboard: your board could not be saved ({error.Message})." );
		}
	}
}
