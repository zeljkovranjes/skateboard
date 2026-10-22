using Sandbox;

namespace Skateboarding.Engine.Finish;

/// <summary>A colour a part can take: its name, the colour shown on its swatch, and the tint its material takes.</summary>
internal readonly record struct BoardFinishColour( string Name, Color Swatch, Color Tint );

/// <summary>
/// The colours each part of the salvaged board can take, the first of each as delivered, and the material each part is
/// drawn with (its tint is what changes). The sign's paint and the tyres are drawn grey in the atlas (lifted to
/// <see cref="AtlasGrey"/>), so their tint is the colour wanted over that grey; the hinge hardware is drawn as delivered
/// and its tint shades the bare steel; the oil's tint is its colour.
/// </summary>
internal static class BoardFinishPalette
{
	// The grey the paint and tyres are drawn in (see tools rewrite_salvage_atlas.py).
	private const float AtlasGrey = 0.8f;

	private static readonly BoardFinishColour[] Paint =
	{
		Painted( "Street green", 0.175f, 0.3658f, 0.2604f ),
		Painted( "Highway blue", 0.1f, 0.24f, 0.55f ),
		Painted( "Stop red", 0.62f, 0.08f, 0.07f ),
		Painted( "Warning yellow", 0.8f, 0.62f, 0.06f ),
		Painted( "Detour orange", 0.8f, 0.36f, 0.06f ),
		Painted( "Midnight", 0.06f, 0.06f, 0.07f ),
		Painted( "Plain white", 0.78f, 0.78f, 0.76f ),
		Painted( "Violet", 0.32f, 0.14f, 0.48f ),
	};

	private static readonly BoardFinishColour[] Wheels =
	{
		Painted( "Charcoal", 0.1718f, 0.1793f, 0.1806f ),
		Painted( "White", 0.8f, 0.79f, 0.76f ),
		Painted( "Red", 0.55f, 0.09f, 0.08f ),
		Painted( "Blue", 0.12f, 0.26f, 0.62f ),
		Painted( "Yellow", 0.8f, 0.62f, 0.1f ),
		Painted( "Green", 0.13f, 0.48f, 0.24f ),
		Painted( "Pink", 0.8f, 0.4f, 0.56f ),
	};

	private static readonly BoardFinishColour[] Axles =
	{
		new( "Bare steel", new Color( 0.56f, 0.55f, 0.53f ), Color.White ),
		Shaded( "Black", 0.18f, 0.18f, 0.2f ),
		Shaded( "Brass", 1f, 0.78f, 0.38f ),
		Shaded( "Copper", 0.95f, 0.55f, 0.38f ),
		Shaded( "Red", 0.9f, 0.25f, 0.22f ),
		Shaded( "Blue", 0.35f, 0.5f, 0.95f ),
	};

	private static readonly BoardFinishColour[] Oil =
	{
		Poured( "Black", 0.0392f, 0.0339f, 0.0245f ),
		Poured( "Gold", 0.55f, 0.38f, 0.06f ),
		Poured( "Toxic green", 0.18f, 0.55f, 0.06f ),
		Poured( "Blood red", 0.42f, 0.02f, 0.02f ),
		Poured( "Deep blue", 0.05f, 0.18f, 0.55f ),
		Poured( "Purple", 0.3f, 0.06f, 0.42f ),
	};

	/// <summary>The parts, in the order the board editor lists them.</summary>
	internal static readonly BoardFinishPart[] Parts =
		{ BoardFinishPart.Paint, BoardFinishPart.Wheels, BoardFinishPart.Axles, BoardFinishPart.Oil };

	internal static BoardFinishColour[] Of( BoardFinishPart part ) => part switch
	{
		BoardFinishPart.Paint => Paint,
		BoardFinishPart.Wheels => Wheels,
		BoardFinishPart.Axles => Axles,
		_ => Oil
	};

	internal static string Title( BoardFinishPart part ) => part switch
	{
		BoardFinishPart.Paint => "Paint",
		BoardFinishPart.Wheels => "Wheels",
		BoardFinishPart.Axles => "Axles",
		_ => "Oil"
	};

	/// <summary>The file name (without folder or extension) of the material the part is drawn with.</summary>
	internal static string MaterialFile( BoardFinishPart part ) => part switch
	{
		BoardFinishPart.Paint => "game_salvage_paint",
		BoardFinishPart.Wheels => "game_salvage_rubber",
		BoardFinishPart.Axles => "game_salvage_hinge",
		_ => "heart_blackoil"
	};

	/// <summary>The oil's own material, which the heart's hoses and its sloshing bath are drawn with.</summary>
	internal const string OilMaterialPath = "models/skateboard_game/heart_blackoil.vmat";

	// A colour drawn over the atlas' grey.
	private static BoardFinishColour Painted( string name, float r, float g, float b ) =>
		new( name, new Color( r, g, b ), new Color( Fit( r / AtlasGrey ), Fit( g / AtlasGrey ), Fit( b / AtlasGrey ) ) );

	// A colour shading bare metal.
	private static BoardFinishColour Shaded( string name, float r, float g, float b ) =>
		new( name, new Color( r * 0.6f, g * 0.6f, b * 0.6f ), new Color( r, g, b ) );

	// A colour of its own (the oil's material is plain white under its tint).
	private static BoardFinishColour Poured( string name, float r, float g, float b ) =>
		new( name, new Color( r, g, b ), new Color( r, g, b ) );

	private static float Fit( float value ) => value > 1f ? 1f : value;
}
