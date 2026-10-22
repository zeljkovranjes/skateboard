using System;
using Sandbox;

namespace Skateboarding.Engine;

/// <summary>
/// The colour cycle of the board's glowing blue parts. It steps through five colours (red, green, yellow, blue, cyan)
/// rather than blending: every tenth of a second a wipe moves one place along the board, tail to nose, and the places
/// it has passed take the new colour while those ahead keep the last one. Once the wipe has crossed all the places, the
/// next colour starts its own wipe. The board is <see cref="Places"/> places long, so one colour's wipe takes
/// <see cref="Places"/> tenths of a second and the whole cycle five times that.
/// </summary>
internal static class BoardGlowCycle
{
	// Milliseconds each step of the wipe lasts.
	private const double StepMs = 100.0;

	/// <summary>How many places long the board is to the wipe.</summary>
	internal const int Places = 15;

	// The colours in order: red, green, yellow, blue, cyan.
	private static readonly Color[] Palette =
	{
		new( 1f, 0.2f, 0.2f ),
		new( 0f, 1f, 0f ),
		new( 1f, 1f, 0f ),
		new( 0f, 0f, 1f ),
		new( 0f, 1f, 1f ),
	};

	/// <summary>The place on the board of a point <paramref name="along"/> it, a share of its length from tail (0) to nose (1).</summary>
	internal static int Place( float along ) => Math.Clamp( (int)MathF.Floor( along * Places ), 0, Places - 1 );

	/// <summary>The colour of an index <see cref="Index"/> gave.</summary>
	internal static Color Colour( int index ) => Palette[ index ];

	/// <summary>
	/// The colour's index at a place: the colour wiping in where the wipe has passed (places before it), the colour
	/// before it where it has yet to reach.
	/// </summary>
	internal static int Index( double ms, int place )
	{
		var t = Math.Max( 0.0, ms );
		var colour = (int)(Math.Floor( t / (Places * StepMs) ) % Palette.Length);
		var wipe = (int)(Math.Floor( t / StepMs ) % Places);
		return place < wipe ? colour : (colour + Palette.Length - 1) % Palette.Length;
	}
}
