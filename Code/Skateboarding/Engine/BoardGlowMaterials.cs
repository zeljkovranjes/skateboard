using System.Collections.Generic;
using Sandbox;

namespace Skateboarding.Engine;

/// <summary>
/// The glowing materials every board shares: one copy of each glowing material for each place along a board
/// (<see cref="BoardGlowCycle"/>). The cycle runs on the clock, so every board shows the same colour at the same
/// place, and all of them can draw with the same copies: however many boards there are (one per player), there are at
/// most a few dozen copies, and each frame only those whose step of the cycle has changed are coloured again.
/// </summary>
internal sealed class BoardGlowMaterials
{
	private readonly Dictionary<(Material Original, int Place), int> _byPlace = new();
	private readonly HashSet<Material> _own = new();
	private readonly List<Material> _copies = new();
	private readonly List<int> _places = new();
	private readonly List<int> _shown = new();

	/// <summary>The shared copy of <paramref name="original"/> for a part at <paramref name="place"/>.</summary>
	internal Material For( Material original, int place )
	{
		if ( _byPlace.TryGetValue( (original, place), out var index ) )
			return _copies[ index ];
		var copy = original.CreateCopy();
		_byPlace[ (original, place) ] = _copies.Count;
		_own.Add( copy );
		_copies.Add( copy );
		_places.Add( place );
		_shown.Add( -1 );
		return copy;
	}

	/// <summary>True for a copy made here.</summary>
	internal bool Owns( Material material ) => material is not null && _own.Contains( material );

	/// <summary>Colours each copy as the cycle has it <paramref name="ms"/> milliseconds in, where its step has changed.</summary>
	internal void Show( double ms )
	{
		for ( var copy = 0; copy < _copies.Count; copy++ )
		{
			var index = BoardGlowCycle.Index( ms, _places[ copy ] );
			if ( index == _shown[ copy ] )
				continue;
			_shown[ copy ] = index;
			var colour = BoardGlowCycle.Colour( index );
			_copies[ copy ].Set( "g_vColorTint", colour );
			_copies[ copy ].Set( "g_vSelfIllumTint", colour );
		}
	}
}
