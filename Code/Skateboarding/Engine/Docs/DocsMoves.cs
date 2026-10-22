using Sandbox;
using Sandbox.UI;

namespace Skateboarding.Engine.Docs;

/// <summary>
/// A page's moves, one after another: the move's name and what it is, how to do it on the keyboard and on a controller
/// (a short definition list), and its GIF under them once there is one.
/// </summary>
internal static class DocsMoves
{
	internal static void Add( Panel page, DocsMove[] moves )
	{
		foreach ( var move in moves )
		{
			var section = page.AddChild<Panel>( "move" );
			section.AddChild<Label>( "h2" ).Text = move.Title;
			section.AddChild<Label>( "p" ).Text = move.What;
			var how = section.AddChild<Panel>( "dl" );
			// One line for both devices when it names no keys; a keyboard line alone when only the keyboard's was checked.
			if ( move.Controller is null )
				Row( how, move.Keyboard.Contains( '{' ) ? "Keyboard" : "How", move.Keyboard );
			else
			{
				Row( how, "Keyboard", move.Keyboard );
				Row( how, "Controller", move.Controller );
			}
			if ( move.Note is not null )
				section.AddChild<Label>( "note" ).Text = move.Note;
			if ( move.Gif is not null )
				Gif( section, move.Gif );
		}
	}

	private static void Row( Panel list, string device, string line )
	{
		var row = list.AddChild<Panel>( "dl-row" );
		row.AddChild<Label>( "dt" ).Text = device;
		DocsInputLine.Add( row, line, "dd" );
	}

	// The move's GIF, playing; nothing until the clip is there.
	private static void Gif( Panel section, string id )
	{
		var path = DocsGifs.Path( id );
		var texture = FileSystem.Mounted.FileExists( path ) ? Texture.Load( FileSystem.Mounted, path, false ) : null;
		if ( texture is not null && texture.Width > 1 )
			section.AddChild<Image>( "figure" ).Texture = texture;
	}
}
