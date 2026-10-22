namespace Skateboarding.Core.Sound;

/// <summary>What a board grinds with, as its sound hears it.</summary>
internal enum SkateGrindSound
{
	/// <summary>Not grinding.</summary>
	None,
	/// <summary>Metal trucks on the rail: 50-50s, 5-0s, nosegrinds.</summary>
	Trucks,
	/// <summary>The wooden deck on the rail: boardslides, noseslides, tailslides.</summary>
	Slide
}

/// <summary>What a board is doing this frame, as its continuous sound (<see cref="SkateRollSynth"/>) reads it.</summary>
internal readonly record struct SkateSoundControls
{
	/// <summary>Speed along the ground, in metres a second.</summary>
	public float Speed { get; init; }

	/// <summary>How much of the board rolls on the ground: 0 in the air, 1 on all four wheels.</summary>
	public float Rolling { get; init; }

	/// <summary>The ground under the wheels.</summary>
	public SkateSoundSurface Ground { get; init; }

	/// <summary>The ground's own sound, cut into grains; null when it has none.</summary>
	public SoundGrains Grains { get; init; }

	/// <summary>What the board grinds with.</summary>
	public SkateGrindSound Grind { get; init; }

	/// <summary>The rail or ledge ground on.</summary>
	public SkateSoundSurface Rail { get; init; }

	/// <summary>Sideways speed of the wheels over the ground (a powerslide), in metres a second.</summary>
	public float Slide { get; init; }

	/// <summary>How hard a shoe drags on the ground to slow the board, 0 to 1.</summary>
	public float Brake { get; init; }

	/// <summary>How fast the board turns in the air (a flip, a shuvit), in radians a second.</summary>
	public float Spin { get; init; }
}
