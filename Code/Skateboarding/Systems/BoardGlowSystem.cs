using System.Collections.Generic;
using Sandbox;
using Skateboarding.Engine;

namespace Skateboarding.Systems;

/// <summary>
/// Cycles the colour of every board's glowing blue parts (<see cref="BoardGlowCycle"/>). The cycle runs on the clock, so
/// every board, and every client, shows the same colours at the same moment, and all the boards draw with the same few
/// material copies (<see cref="BoardGlowMaterials"/>): the work each frame is the same however many players ride,
/// and only the copies whose step of the cycle changed are coloured again. The boards and their parts are looked for
/// once a second (<see cref="BoardGlowParts"/>). Nothing runs on a dedicated server, which draws nothing.
/// </summary>
public sealed class BoardGlowSystem : GameObjectSystem<BoardGlowSystem>
{
	// The file names of the glowing blue materials whose colour cycles.
	private static readonly string[] Glowing =
		{ "mat_powered_sboxblue_game", "mat_deck_chargedchannels_game", "mat_gravity_chargeindicator_game" };

	// Seconds between looks for boards and their parts.
	private const float Rescan = 1f;

	private readonly Dictionary<Components.Skateboard, BoardGlowParts> _boards = new();
	private readonly List<Components.Skateboard> _gone = new();
	private readonly BoardGlowMaterials _shared = new();
	private RealTimeSince _sinceScan = Rescan;

	/// <summary>Colours the glowing parts as each frame finishes, before it is drawn.</summary>
	public BoardGlowSystem( Scene scene ) : base( scene ) => Listen( Stage.FinishUpdate, 0, Glow, "BoardGlow" );

	private void Glow()
	{
		if ( Scene.IsEditor || Application.IsDedicatedServer )
			return;
		if ( _sinceScan >= Rescan )
		{
			_sinceScan = 0f;
			Scan();
		}
		_shared.Show( RealTime.Now * 1000.0 );
	}

	// Every board in the scene with its glowing parts, found again where its models changed; boards gone are dropped.
	private void Scan()
	{
		foreach ( var board in Scene.GetAllComponents<Components.Skateboard>() )
		{
			if ( !_boards.TryGetValue( board, out var parts ) )
			{
				parts = new BoardGlowParts();
				_boards[ board ] = parts;
			}
			parts.Find( board.GameObject, Glowing, _shared );
		}
		_gone.Clear();
		foreach ( var board in _boards.Keys )
		{
			if ( !board.IsValid() )
				_gone.Add( board );
		}
		foreach ( var board in _gone )
			_boards.Remove( board );
	}
}
