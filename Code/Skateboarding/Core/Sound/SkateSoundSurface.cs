namespace Skateboarding.Core.Sound;

/// <summary>Which recorded roll a ground plays (see <see cref="SkateSoundBank"/>).</summary>
internal enum SkateRollRecording
{
	/// <summary>Smooth park concrete.</summary>
	Park,
	/// <summary>A rough city street: asphalt, paving, and the loose grounds under their filtering.</summary>
	Street,
	/// <summary>Wooden planks: ramps, decks, bridges.</summary>
	Wood,
	/// <summary>Tiles, their joints ticking by.</summary>
	Tile,
	/// <summary>A smooth indoor floor: polished stone, sheet metal, glass.</summary>
	Smooth
}

/// <summary>
/// How a kind of ground sounds under a skateboard: which recorded roll it plays and how that recording, and every hit on
/// the ground, is shaped for it. Each value is a plain number (but the recording), so two grounds blend (<see cref="Blend"/>)
/// as the board crosses from one to the other.
/// </summary>
/// <param name="Roll">The recorded roll it plays.</param>
/// <param name="Rumble">How loud its roll is, 0 to 1.</param>
/// <param name="Bright">Low pass (Hz) over its roll and its hits: how much top the ground lets through.</param>
/// <param name="Hollow">How much a hollow ground (a ramp, a deck) booms under the roll and the hits, 0 to 1.</param>
/// <param name="HollowTone">Hz of that boom.</param>
/// <param name="Ring">How much a metal ground rings under the hits, 0 to 1.</param>
/// <param name="Rustle">Level of soft, scattered top (grass, leaves, sand) under the wheels, 0 to 1.</param>
/// <param name="Damp">How soft the ground is, 0 hard to 1 soft: dulls and quiets the board's hits on it.</param>
/// <param name="Grains">Share of the ground's own sound (see <see cref="SoundGrains"/>) mixed into the roll and pushes.</param>
internal readonly record struct SkateSoundSurface(
	SkateRollRecording Roll, float Rumble, float Bright, float Hollow, float HollowTone, float Ring, float Rustle,
	float Damp, float Grains )
{
	// A low pass high enough to leave a recording as it is.
	private const float Open = 16000f;

	/// <summary>Park concrete: the recording as it is.</summary>
	internal static SkateSoundSurface Concrete => new( SkateRollRecording.Park, 1f, Open, 0f, 140f, 0f, 0f, 0.1f, 0.2f );

	/// <summary>Polished stone, marble: a smooth, quieter roll.</summary>
	internal static SkateSoundSurface Smooth => new( SkateRollRecording.Smooth, 0.85f, Open, 0f, 140f, 0f, 0f, 0.05f, 0.15f );

	/// <summary>Tiled floor: smooth, the tiles' joints ticking by.</summary>
	internal static SkateSoundSurface Tile => new( SkateRollRecording.Tile, 0.9f, Open, 0f, 140f, 0f, 0f, 0.05f, 0.15f );

	/// <summary>Asphalt and rough stone: the street's gravelly roar.</summary>
	internal static SkateSoundSurface Asphalt => new( SkateRollRecording.Street, 1f, Open, 0f, 140f, 0f, 0.03f, 0.15f, 0.25f );

	/// <summary>Brick and paving: the street's roar, a little darker.</summary>
	internal static SkateSoundSurface Brick => Asphalt with { Bright = 9000f };

	/// <summary>A wooden ramp or deck: planks rolling by over a hollow boom.</summary>
	internal static SkateSoundSurface Wood => new( SkateRollRecording.Wood, 0.8f, Open, 0.6f, 140f, 0f, 0f, 0.2f, 0.25f );

	/// <summary>Sheet metal and steel: a smooth roll, the hits ringing.</summary>
	internal static SkateSoundSurface Metal => new( SkateRollRecording.Smooth, 0.85f, 11000f, 0.2f, 180f, 0.6f, 0f, 0f, 0.2f );

	/// <summary>Grass: a muffled roll under a rustle.</summary>
	internal static SkateSoundSurface Grass => new( SkateRollRecording.Street, 0.45f, 1100f, 0f, 140f, 0f, 0.8f, 0.85f, 0.6f );

	/// <summary>Dirt and soil: a dull crunching roll.</summary>
	internal static SkateSoundSurface Dirt => new( SkateRollRecording.Street, 0.7f, 2000f, 0f, 140f, 0f, 0.35f, 0.6f, 0.5f );

	/// <summary>Gravel: the street's roar with the stones' crunch.</summary>
	internal static SkateSoundSurface Gravel => new( SkateRollRecording.Street, 0.9f, 5000f, 0f, 140f, 0f, 0.5f, 0.4f, 0.7f );

	/// <summary>Sand and snow: a hushed roll that sinks.</summary>
	internal static SkateSoundSurface Sand => new( SkateRollRecording.Street, 0.35f, 800f, 0f, 140f, 0f, 0.6f, 0.95f, 0.6f );

	/// <summary>Carpet, cloth and foam: nearly silent.</summary>
	internal static SkateSoundSurface Soft => new( SkateRollRecording.Smooth, 0.3f, 700f, 0f, 140f, 0f, 0.2f, 0.9f, 0.3f );

	/// <summary>Glass and ceramic: smooth and bright, the hits ringing a little.</summary>
	internal static SkateSoundSurface Glass => Smooth with { Ring = 0.2f };

	/// <summary>Rubber and plastic: a soft, dull roll.</summary>
	internal static SkateSoundSurface Rubber => new( SkateRollRecording.Smooth, 0.6f, 2500f, 0.1f, 160f, 0f, 0f, 0.6f, 0.25f );

	/// <summary>The ground part way from <paramref name="a"/> to <paramref name="b"/> (its recording switches half way).</summary>
	internal static SkateSoundSurface Blend( in SkateSoundSurface a, in SkateSoundSurface b, float t )
	{
		if ( t <= 0f ) return a;
		if ( t >= 1f ) return b;
		return new( t < 0.5f ? a.Roll : b.Roll, L( a.Rumble, b.Rumble ), L( a.Bright, b.Bright ), L( a.Hollow, b.Hollow ),
			L( a.HollowTone, b.HollowTone ), L( a.Ring, b.Ring ), L( a.Rustle, b.Rustle ), L( a.Damp, b.Damp ),
			L( a.Grains, b.Grains ) );
		float L( float x, float y ) => x + (y - x) * t;
	}

	/// <summary>
	/// The ground a name gives (a surface's, a material's, its tags), or null when nothing in it is known: grass, dirt,
	/// gravel, sand, snow, wood, metal, glass, tile, stone, brick, asphalt, concrete, carpet, rubber and the like.
	/// </summary>
	internal static SkateSoundSurface? Named( string name )
	{
		if ( string.IsNullOrEmpty( name ) )
			return null;
		var words = name.ToLowerInvariant().Split( Separators, StringSplitOptions.RemoveEmptyEntries );
		foreach ( var (starts, surface) in Names )
			foreach ( var start in starts )
				foreach ( var word in words )
					if ( word.StartsWith( start, StringComparison.Ordinal ) )
						return surface;
		return null;
	}

	private static readonly char[] Separators = { ' ', '_', '-', '.', '/', '\\', ',', '(', ')' };

	// The starts of words, in the order they are tried: the soft and loose grounds first, so "grass_concrete_edge" rolls
	// as grass. Matched at the start of each word of a name, so "office" is not ice nor "event" a vent.
	private static readonly (string[] Starts, SkateSoundSurface Surface)[] Names =
	{
		(new[] { "grass", "lawn", "turf", "foliage", "leaves", "hay" }, Grass),
		(new[] { "mud", "dirt", "soil", "earth" }, Dirt),
		(new[] { "gravel", "pebble", "rubble" }, Gravel),
		(new[] { "sandstone" }, Asphalt),
		(new[] { "sand", "snow", "ice" }, Sand),
		(new[] { "carpet", "fabric", "cloth", "foam", "cushion", "mattress", "flesh" }, Soft),
		(new[] { "rubber", "plastic", "tire", "tyre" }, Rubber),
		(new[] { "glass", "ceramic", "porcelain" }, Glass),
		(new[] { "metal", "steel", "iron", "alumin", "chain", "grate", "vent", "pipe", "rail", "duct", "sheetmetal" }, Metal),
		(new[] { "wood", "plank", "plywood", "ramp", "crate", "board" }, Wood),
		(new[] { "tile" }, Tile),
		(new[] { "marble", "polished", "linoleum", "plaster" }, Smooth),
		(new[] { "brick", "cobble", "paving", "paver" }, Brick),
		(new[] { "asphalt", "tarmac", "road", "rock", "stone", "boulder" }, Asphalt),
		(new[] { "concrete", "cement", "sidewalk", "pavement" }, Concrete),
	};

	/// <summary>
	/// The ground a surface's physics suggests when its name says nothing: very dense is metal, light and springy is wood,
	/// grippy and soft is grass or dirt, and anything else rolls as concrete.
	/// </summary>
	internal static SkateSoundSurface Physical( float density, float friction, float elasticity )
	{
		if ( density >= 5000f )
			return Metal;
		if ( density > 0f && density < 1000f )
			return elasticity > 0.5f ? Rubber : Wood;
		if ( friction >= 1.1f && elasticity < 0.2f )
			return Dirt;
		return Concrete;
	}

	/// <summary>
	/// This ground coloured by a sound of its own (see <see cref="SoundPrint"/>): a dull, low sound darkens the roll a
	/// little and a bright one opens it, and a bursty, crackling one makes a soft ground rustle more, so two grounds the
	/// names call the same still sound like themselves.
	/// </summary>
	internal SkateSoundSurface Coloured( in SoundPrint print )
	{
		if ( !print.Heard )
			return this;
		return this with
		{
			Bright = MathF.Min( Open, Bright * Math.Clamp( print.Centre / 1500f, 0.75f, 1.15f ) ),
			Rustle = MathF.Min( 1f, Rustle + 0.25f * print.Rough * Damp )
		};
	}
}
