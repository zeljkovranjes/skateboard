using System.Collections.Generic;
using System.Threading.Tasks;
using Sandbox;
using Skateboarding.Core.Sound;

namespace Skateboarding.Engine;

/// <summary>
/// How each surface sounds under a skateboard (see <see cref="SkateSoundSurface"/>), worked out once per surface: from its
/// name and tags (grass, wood, metal, gravel...), its acoustic material, its base surfaces in turn, or else its physics.
/// A sound of the surface's own (its scrape, else its footstep or soft impact) is then loaded in the background, measured
/// to colour the board's roll and cut into grains the roll and push are sprinkled with, so the board carries that
/// ground's real texture.
/// </summary>
internal sealed class SkateSoundGrounds
{
	// Base surfaces followed, at most (a surface's base, that one's base...).
	private const int DeepestBase = 6;

	private sealed class Ground
	{
		public SkateSoundSurface Sound;
		public SoundGrains Grains;
		public SoundFile File;
		public Task<short[]> Loading;
	}

	private readonly Dictionary<Surface, Ground> _known = new();

	/// <summary>How a surface sounds, and its own sound's grains once they have loaded (null until then, or without one).</summary>
	internal (SkateSoundSurface Sound, SoundGrains Grains) Of( Surface surface )
	{
		if ( surface is null )
			return (SkateSoundSurface.Concrete, null);
		if ( !_known.TryGetValue( surface, out var ground ) )
		{
			ground = new Ground { Sound = Read( surface ), File = OwnSound( surface ) };
			try
			{
				ground.Loading = ground.File?.GetSamplesAsync();
			}
			catch ( Exception )
			{
				ground.Loading = null;
			}
			_known[ surface ] = ground;
		}
		if ( ground.Loading is { IsCompleted: true } loading )
		{
			ground.Loading = null;
			if ( loading.IsCompletedSuccessfully && loading.Result is { Length: > 0 } samples )
			{
				ground.Grains = SoundGrains.From( samples, ground.File.Rate );
				if ( ground.Grains is not null )
					ground.Sound = ground.Sound.Coloured( ground.Grains.Print );
			}
		}
		return (ground.Sound, ground.Grains);
	}

	// The surface's own name and tags first, then its acoustic material, then its base surfaces', then its physics.
	private static SkateSoundSurface Read( Surface surface )
	{
		var at = surface;
		for ( var depth = 0; at is not null && depth < DeepestBase; depth++, at = at.GetBaseSurface() )
		{
			if ( SkateSoundSurface.Named( at.ResourceName ) is { } named )
				return named;
			if ( SkateSoundSurface.Named( at.Tags ) is { } tagged )
				return tagged;
			if ( Acoustic( at.AudioSurface ) is { } acoustic )
				return acoustic;
		}
		return SkateSoundSurface.Physical( surface.Density, surface.Friction, surface.Elasticity );
	}

	private static SkateSoundSurface? Acoustic( AudioSurface audio ) => audio switch
	{
		AudioSurface.Brick => SkateSoundSurface.Brick,
		AudioSurface.Concrete => SkateSoundSurface.Concrete,
		AudioSurface.Asphalt or AudioSurface.Rock => SkateSoundSurface.Asphalt,
		AudioSurface.Ceramic or AudioSurface.Glass => SkateSoundSurface.Glass,
		AudioSurface.Marble or AudioSurface.Linoleum or AudioSurface.Plaster or AudioSurface.AcousticTile => SkateSoundSurface.Smooth,
		AudioSurface.Gravel => SkateSoundSurface.Gravel,
		AudioSurface.Wood => SkateSoundSurface.Wood,
		AudioSurface.Metal or AudioSurface.Steel => SkateSoundSurface.Metal,
		AudioSurface.Sand or AudioSurface.Snow => SkateSoundSurface.Sand,
		AudioSurface.Soil => SkateSoundSurface.Dirt,
		AudioSurface.Carpet or AudioSurface.Fabric or AudioSurface.Foam or AudioSurface.Curtain or AudioSurface.Leather
			or AudioSurface.Paper or AudioSurface.Water => SkateSoundSurface.Soft,
		_ => null
	};

	// The sound the surface makes rubbed or stepped on: a scrape is best (it is long and even), a footstep will do.
	private static SoundFile OwnSound( Surface surface )
	{
		var at = surface;
		for ( var depth = 0; at is not null && depth < DeepestBase; depth++, at = at.GetBaseSurface() )
		{
			var sounds = at.SoundCollection;
			foreach ( var sound in new[] { sounds.ScrapeRough, sounds.ScrapeSmooth, sounds.FootLeft, sounds.FootRight,
				sounds.FootLand, sounds.ImpactSoft } )
			{
				if ( sound?.Sounds is not { Count: > 0 } files )
					continue;
				foreach ( var file in files )
					if ( file is not null )
						return file;
			}
		}
		return null;
	}
}
