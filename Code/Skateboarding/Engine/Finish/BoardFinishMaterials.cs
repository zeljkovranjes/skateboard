using System;
using System.Collections.Generic;
using Sandbox;

namespace Skateboarding.Engine.Finish;

/// <summary>
/// The finished materials every board shares: one tinted copy of a part's material for each colour in use. However
/// many boards there are (one per player), there are at most a few dozen copies, each made once. The delivered colour
/// is the material itself, so a board as delivered draws with no copies at all.
/// </summary>
internal sealed class BoardFinishMaterials
{
	/// <summary>The copies every board and the board editor draw with.</summary>
	internal static BoardFinishMaterials Shared { get; } = new();

	private readonly Dictionary<(Material Original, int Choice), Material> _copies = new();

	/// <summary>
	/// What a slot drawn with <paramref name="original"/> (one of <paramref name="part"/>'s materials) draws with in the
	/// colour <paramref name="choice"/>.
	/// </summary>
	internal Material For( Material original, BoardFinishPart part, int choice )
	{
		if ( choice <= 0 || original is null )
			return original;
		if ( _copies.TryGetValue( (original, choice), out var copy ) && copy is not null )
			return copy;
		copy = original.CreateCopy();
		copy.Set( "g_vColorTint", BoardFinishPalette.Of( part )[ choice ].Tint );
		_copies[ (original, choice) ] = copy;
		return copy;
	}

	/// <summary>The part whose colour a material gives, or null for a material no colour changes.</summary>
	internal static BoardFinishPart? PartOf( Material material )
	{
		var name = material?.Name;
		if ( string.IsNullOrEmpty( name ) )
			return null;
		foreach ( var part in BoardFinishPalette.Parts )
		{
			if ( name.Contains( BoardFinishPalette.MaterialFile( part ), StringComparison.OrdinalIgnoreCase ) )
				return part;
		}
		return null;
	}

	/// <summary>The oil's material in the look's colour.</summary>
	internal Material Oil( BoardLook look ) =>
		For( Material.Load( BoardFinishPalette.OilMaterialPath ), BoardFinishPart.Oil, look.Oil );
}
