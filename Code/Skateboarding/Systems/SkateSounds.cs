using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sandbox;
using Skateboarding.Components;
using Skateboarding.Core.Sound;
using Skateboarding.Engine;

namespace Skateboarding.Systems;

/// <summary>
/// Every skateboard's sounds, played from real skateboarding recordings (sounds/skate) and shaped as it goes: wheels
/// rolling (faster, higher and louder with speed, in the recording of the ground under them, darkened, boomed or
/// rustled for it and sprinkled with that ground's own sound), the pop of the tail, flips whooshing and the feet catching
/// them, landings, pushes, grinds and slides on rails and ledges, powerslides, a shoe dragged to brake and a board
/// knocking about on its own. Every client hears every board from what it already knows of it, so nothing is sent over
/// the network, and boards beyond earshot cost nothing.
/// </summary>
public sealed class SkateSounds : GameObjectSystem<SkateSounds>
{
	// Inches from the listener beyond which a board is not heard.
	private const float Earshot = 4000f;

	// The recordings, loaded once for the whole game (decoded to samples in the background as the first board is heard).
	private static readonly SkateSoundBank Bank = new();
	private static readonly Dictionary<string, (SoundFile File, Task<short[]> Samples)> Loading = new();
	private static bool _loadStarted;

	private readonly Dictionary<Skateboard, SkateBoardSound> _boards = new();
	private readonly Dictionary<Skateboard, SkateRider> _riders = new();
	private readonly HashSet<Skateboard> _heard = new();
	private readonly SkateSoundGrounds _grounds = new();
	private readonly SkateHitSynth _hits = new( 1, Bank );

	/// <summary>Listens to the scene's boards.</summary>
	public SkateSounds( Scene scene ) : base( scene ) => Listen( Stage.FinishUpdate, 30, Update, "SkateSounds" );

	private void Update()
	{
		if ( Application.IsHeadless || Scene.IsEditor )
			return;
		Load();
		_riders.Clear();
		foreach ( var rider in Scene.GetAllComponents<SkateRider>() )
			if ( rider.Board.IsValid() )
				_riders[ rider.Board ] = rider;
		var listener = Scene.Camera?.WorldPosition;
		_heard.Clear();
		foreach ( var board in Scene.GetAllComponents<Skateboard>() )
		{
			if ( listener is { } ear && board.WorldPosition.DistanceSquared( ear ) > Earshot * Earshot )
				continue;
			if ( !_boards.TryGetValue( board, out var sound ) )
				_boards[ board ] = sound = new SkateBoardSound( board, _grounds, _hits, Bank );
			sound.Update( _riders.GetValueOrDefault( board ), Time.Delta );
			_heard.Add( board );
		}
		// Boards gone, out of earshot or switched off (carried in a hand) fall silent and are heard afresh when back.
		if ( _heard.Count == _boards.Count )
			return;
		foreach ( var gone in _boards.Keys.Where( board => !_heard.Contains( board ) ).ToList() )
			Forget( gone );
	}

	// Starts decoding every recording, then takes each into the bank as it is ready.
	private static void Load()
	{
		if ( !_loadStarted )
		{
			_loadStarted = true;
			foreach ( var name in SkateSoundBank.Names )
			{
				var file = SoundFile.Load( $"sounds/skate/{name}.vsnd" );
				if ( file is null )
				{
					Log.Warning( $"Skate sounds: sounds/skate/{name} is missing." );
					continue;
				}
				Loading[ name ] = (file, file.GetSamplesAsync());
			}
		}
		if ( Loading.Count == 0 )
			return;
		foreach ( var (name, (file, samples)) in Loading.ToList() )
		{
			if ( !samples.IsCompleted )
				continue;
			Loading.Remove( name );
			if ( samples.IsCompletedSuccessfully && samples.Result is { Length: > 0 } decoded )
				Bank.Add( name, SoundClip.From( decoded, file.Rate ) );
			else
				Log.Warning( $"Skate sounds: sounds/skate/{name} could not be decoded." );
		}
	}

	private void Forget( Skateboard board )
	{
		if ( !_boards.Remove( board, out var sound ) )
			return;
		sound.Dispose();
	}

	public override void Dispose()
	{
		foreach ( var sound in _boards.Values )
			sound.Dispose();
		_boards.Clear();
		base.Dispose();
	}
}
