using Sandbox;
using Sandbox.UI;

namespace Skateboarding.Engine.Docs;

/// <summary>
/// A line of docs text with its keys in it: "Hold {ollie} to crouch" comes out as words and key chips, each key as it is
/// bound right now. {id} is a binding's keyboard key, {pad:id} its controller button, {ls} and {rs} the controller's
/// left and right sticks. The words are laid out one by one, so the line wraps around its chips as text does.
/// </summary>
internal static class DocsInputLine
{
	/// <summary>Adds the line to <paramref name="parent"/> as a wrapping row with the given class.</summary>
	internal static Panel Add( Panel parent, string line, string className )
	{
		var row = parent.AddChild<Panel>( className );
		row.AddClass( "flow" );
		var at = 0;
		while ( at < line.Length )
		{
			var open = line.IndexOf( '{', at );
			var close = open < 0 ? -1 : line.IndexOf( '}', open );
			if ( close < 0 )
			{
				Words( row, line[ at.. ] );
				break;
			}
			Words( row, line[ at..open ] );
			Token( row, line[ (open + 1)..close ] );
			at = close + 1;
		}
		return row;
	}

	// The text between chips, a word at a time. Punctuation straight after a chip sits against it, and an opening bracket
	// straight before one sits against the chip.
	private static void Words( Panel row, string text )
	{
		if ( text.Length == 0 )
			return;
		var tight = !char.IsWhiteSpace( text[ 0 ] );
		var words = text.Split( ' ', System.StringSplitOptions.RemoveEmptyEntries );
		for ( var i = 0; i < words.Length; i++ )
		{
			var label = row.AddChild<Label>( "word" );
			label.Text = words[ i ];
			if ( tight )
				label.AddClass( "tight" );
			if ( i == words.Length - 1 && words[ i ].EndsWith( '(' ) && !char.IsWhiteSpace( text[ ^1 ] ) )
				label.AddClass( "opens" );
			tight = false;
		}
	}

	private static void Token( Panel row, string token )
	{
		if ( token is "ls" or "rs" )
		{
			// The pad glyphs the bindings use (SkateBindings.PadGlyph), so a stick reads like the buttons beside it.
			var stick = Texture.Load( $"/ui/glyphs/xbox/{(token == "ls" ? "leftstickx" : "rightstickx")}.svg?w=128&h=128", false );
			Glyph( row, stick, token == "ls" ? "Left stick" : "Right stick" );
			return;
		}
		if ( token.StartsWith( "pad:" ) )
		{
			var id = token[ 4.. ];
			Glyph( row, SkateBindings.PadGlyph( id ), PadName( id ) );
			return;
		}
		var key = SkateBindings.KeyOf( token );
		var chip = row.AddChild<Label>( "kbd" );
		chip.Text = SkateBindings.KeyLabel( token );
		if ( string.IsNullOrWhiteSpace( key ) )
			chip.AddClass( "unbound" );
	}

	// A controller button's name for when it has no glyph.
	private static string PadName( string id )
	{
		var pad = SkateBindings.Get( id )?.Pad;
		return string.IsNullOrWhiteSpace( pad ) ? "Unbound" : pad;
	}

	// A glyph image as tall as a key chip, or its name on a chip when there is no glyph.
	private static void Glyph( Panel row, Texture texture, string name )
	{
		if ( texture is null || texture.Height <= 0 )
		{
			var chip = row.AddChild<Label>( "kbd" );
			chip.Text = name;
			return;
		}
		var glyph = row.AddChild<Image>( "glyph" );
		glyph.Texture = texture;
		glyph.Style.Width = Length.Pixels( DocsStyles.GlyphHeight * texture.Width / texture.Height );
	}
}
