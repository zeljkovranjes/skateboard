namespace Skateboarding.Engine.Finish;

/// <summary>The parts of a board its rider can colour.</summary>
internal enum BoardFinishPart
{
	Paint,
	Wheels,
	Axles,
	Oil
}

/// <summary>
/// How a board is finished: the colour picked for each part (<see cref="BoardFinishPart"/>), by its place in that part's
/// palette (<see cref="BoardFinishPalette"/>); 0 is the board as delivered. Written as a short text ("p2w0a1o3") for
/// the network and the player's saved board; anything unreadable, or out of a palette, reads as delivered.
/// </summary>
internal readonly record struct BoardLook( int Paint, int Wheels, int Axles, int Oil )
{
	/// <summary>The board as delivered: every part its own colour.</summary>
	internal static BoardLook Delivered => default;

	internal int this[ BoardFinishPart part ] => part switch
	{
		BoardFinishPart.Paint => Paint,
		BoardFinishPart.Wheels => Wheels,
		BoardFinishPart.Axles => Axles,
		_ => Oil
	};

	/// <summary>This look with <paramref name="part"/> in its colour <paramref name="choice"/>.</summary>
	internal BoardLook With( BoardFinishPart part, int choice ) => part switch
	{
		BoardFinishPart.Paint => this with { Paint = choice },
		BoardFinishPart.Wheels => this with { Wheels = choice },
		BoardFinishPart.Axles => this with { Axles = choice },
		_ => this with { Oil = choice }
	};

	internal string Encode() => $"p{Paint}w{Wheels}a{Axles}o{Oil}";

	/// <summary>The look written in <paramref name="text"/>; the delivered board for empty or unreadable text.</summary>
	internal static BoardLook Decode( string text )
	{
		var look = Delivered;
		if ( string.IsNullOrEmpty( text ) )
			return look;
		var at = 0;
		while ( at < text.Length )
		{
			var key = text[ at++ ];
			var start = at;
			while ( at < text.Length && char.IsDigit( text[ at ] ) )
				at++;
			if ( at == start || !int.TryParse( text.Substring( start, at - start ), out var choice ) )
				return Delivered;
			look = key switch
			{
				'p' => look with { Paint = Fit( BoardFinishPart.Paint, choice ) },
				'w' => look with { Wheels = Fit( BoardFinishPart.Wheels, choice ) },
				'a' => look with { Axles = Fit( BoardFinishPart.Axles, choice ) },
				'o' => look with { Oil = Fit( BoardFinishPart.Oil, choice ) },
				_ => look
			};
		}
		return look;
	}

	// A choice within its palette, or the delivered colour.
	private static int Fit( BoardFinishPart part, int choice ) =>
		choice >= 0 && choice < BoardFinishPalette.Of( part ).Length ? choice : 0;
}
