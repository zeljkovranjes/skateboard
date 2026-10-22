namespace Skateboarding.Core.Sound;

/// <summary>
/// How loud the player wants the boards' sounds, each 0 (silent) to <see cref="Loudest"/>, 1 as they are made: all of
/// them, and the rolling (wheels, pushes, powerslides, braking), the tricks (pops, flips, catches, landings, a board
/// knocking about) and the grinds (grinds, slides, rail hits) on their own.
/// </summary>
internal readonly record struct SkateSoundMix( float All, float Rolling, float Tricks, float Grinds )
{
	/// <summary>The most any of them can be turned up to.</summary>
	internal const float Loudest = 1.5f;

	/// <summary>Every sound as it is made.</summary>
	internal static SkateSoundMix Full => new( 1f, 1f, 1f, 1f );

	/// <summary>The same mix with every volume kept between silent and <see cref="Loudest"/>.</summary>
	internal SkateSoundMix Clamped() => new( Keep( All ), Keep( Rolling ), Keep( Tricks ), Keep( Grinds ) );

	private static float Keep( float volume ) => float.IsFinite( volume ) ? Math.Clamp( volume, 0f, Loudest ) : 1f;
}
