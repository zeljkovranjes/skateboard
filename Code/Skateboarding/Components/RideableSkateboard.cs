using System.Collections.Generic;
using Sandbox;
using Skateboarding.Engine;
using Skateboarding.Systems;

namespace Skateboarding.Components;

/// <summary>
/// Lets a player get on this board with Use: their own body, movement and tools are put away while they ride, and a skate rider in
/// their clothes takes its place on the deck, driven by their input and followed by the skate camera. A bail ragdolls
/// the rider; once it has come to rest the player gets up there on foot, and the board stays where it rolled. Use again
/// gets off, with the player standing beside the board. The host decides who rides; the rider's own client owns and
/// simulates the board it rides (keeping it until the board comes to rest and sleeps), so each player pays for their own
/// board only and steers it with no round trip.
/// </summary>
[Title( "Rideable Skateboard" ), Category( "Skate Motion" ), Icon( "skateboarding" )]
public sealed class RideableSkateboard : Component, Component.IPressable
{
	/// <summary>The rider's clip set among the addon's files.</summary>
	[Property, Group( "Animation" )] public string AnimationClips { get; set; } = "skater/skater-corrected.sman";

	/// <summary>The graph that plays the clips.</summary>
	[Property, Group( "Animation" )] public string AnimationGraph { get; set; } = "skater/skater.graph.json";

	/// <summary>Body used when the player's own cannot be found.</summary>
	[Property] public Model FallbackBody { get; set; } = Model.Load( "models/citizen/citizen.vmdl" );

	/// <summary>The connection riding this board, or empty.</summary>
	[Sync( SyncFlags.FromHost )] public Guid RiderConnection { get; set; }

	/// <summary>The skate rider on the deck while someone rides.</summary>
	[Sync( SyncFlags.FromHost )] public GameObject Rider { get; set; }

	/// <summary>The player object that got on, switched off while it rides.</summary>
	[Sync( SyncFlags.FromHost )] public GameObject Player { get; set; }

	[RequireComponent] public Skateboard Board { get; set; }

	// Inches beside the board a player is put when they get off, and above the ground wherever they are put.
	private const float GetOffSide = 32f;
	private const float GetOffLift = 4f;

	// Seconds a bailed rider lies before the player gets up even if the ragdoll never comes to rest.
	private const float GetUpAtLatest = 6f;

	// The parts of the player switched off while it rides, to switch back on as it gets off: its movement (which also
	// reads its inventory's keys) and physics body, and every child that was on (its body and clothes, colliders, held
	// tool, flashlight) but its voice. The inventory itself stays on: the spawn menu reads the tool gun through it.
	private readonly List<Component> _stowedComponents = new();
	private readonly List<GameObject> _stowedChildren = new();
	private GameObject _stowedPlayer;
	private static readonly string[] StowedComponentTypes = { "PlayerController", "Rigidbody" };
	private const string KeptChild = "Voice";

	private GameObject _camera;
	private GameObject _dressed;
	// Whether this client has put the riding player away, and whether that player is this client's own.
	private bool _riding;
	private bool _mine;
	private TimeSince _bailed;
	private bool _wasBailed;
	// Where the player gets up after a bail, once it has asked to get off.
	private Vector3? _fellAt;

	// Use gets on and off: the press that got the rider on is still down as it starts riding, so getting off waits for
	// Use to be let go first.
	private bool _useLetGo;

	// Whether this rider's client has got the board it now owns ready to ride.
	private bool _boardTaken;

	protected override void OnStart()
	{
		// Spawned, the board lies free with no one on it (so it can fall asleep), until someone gets on.
		if ( !IsProxy && RiderConnection == Guid.Empty )
			Board.SetRiderAttached( false );
		// The rider's clips are read once per scene, a moment's work: done as the board appears, not as someone gets on.
		Scene.GetSystem<RiderAnimationLibrary>()?.Get( AnimationClips, AnimationGraph );
	}

	bool IPressable.CanPress( IPressable.Event e ) => RiderConnection == Guid.Empty && PlayerOf( e ).IsValid();

	bool IPressable.Press( IPressable.Event e )
	{
		var player = PlayerOf( e );
		if ( RiderConnection != Guid.Empty || !player.IsValid() )
			return false;
		RequestRide( player );
		return true;
	}

	IPressable.Tooltip? IPressable.GetTooltip( IPressable.Event e ) =>
		RiderConnection == Guid.Empty ? new IPressable.Tooltip( "Ride", "skateboarding", "Get on the board" ) : null;

	// The player object behind whoever pressed: the top of the presser's hierarchy.
	private static GameObject PlayerOf( IPressable.Event e ) => e.Source.IsValid() ? e.Source.GameObject.Root : null;

	/// <summary>Host: puts the asking player on the board when no one rides it.</summary>
	[Rpc.Host]
	private void RequestRide( GameObject player )
	{
		var caller = Rpc.Caller ?? Connection.Local;
		if ( RiderConnection != Guid.Empty || !player.IsValid() || caller is null )
			return;
		var rider = new GameObject( true, "Skate rider" );
		rider.WorldTransform = new Transform( WorldPosition, Rotation.FromYaw( WorldRotation.Yaw() ) );
		var skate = rider.Components.Create<SkateRider>();
		skate.Board = Board;
		skate.CharacterModel = BodyOf( player );
		skate.AnimationClips = AnimationClips;
		skate.AnimationGraph = AnimationGraph;
		rider.NetworkSpawn( caller );
		// The rider's client simulates the board from here (getting it ready once it owns it); if that player leaves, the
		// host takes the board back rather than it going with them.
		Board.GameObject.Network.SetOrphanedMode( NetworkOrphaned.Host );
		Board.GameObject.Network.AssignOwnership( caller );
		Board.SetDriver( caller );
		Rider = rider;
		Player = player;
		RiderConnection = caller.Id;
	}

	/// <summary>Host: takes the rider off, leaving the board free.</summary>
	[Rpc.Host]
	private void RequestGetOff()
	{
		if ( Rpc.Caller is { } caller && caller.Id != RiderConnection )
			return;
		Release();
	}

	private void Release()
	{
		if ( Rider.IsValid() )
			Rider.Destroy();
		Rider = null;
		Player = null;
		RiderConnection = Guid.Empty;
		Board.SetDriver( null );
		Board.SetInput( default );
		Board.SetRiderAttached( false );
	}

	protected override void OnUpdate()
	{
		// The board's owner is whoever rode it last, so the host's own duties go by being the host.
		if ( !Networking.IsActive || Networking.IsHost )
			HostUpdate();
		Dress();
		var riding = RiderConnection != Guid.Empty && Rider.IsValid() && Player.IsValid();
		if ( riding && !_riding )
			StartRiding( Connection.Local is { } local && local.Id == RiderConnection );
		else if ( !riding && _riding )
			StopRiding();
		if ( _riding && _mine )
			RideUpdate();
	}

	// Host: a rider whose object or player is gone (both go with a player who leaves) frees the board; a bail lets the
	// board roll free.
	private void HostUpdate()
	{
		if ( RiderConnection == Guid.Empty )
			return;
		if ( !Rider.IsValid() || !Player.IsValid() )
		{
			Release();
			return;
		}
		if ( Board.State.IsBailed && !_wasBailed && !Board.IsProxy )
			Board.SetRiderAttached( false );
		_wasBailed = Board.State.IsBailed;
	}

	// The riding player's own client.
	private void RideUpdate()
	{
		// Once this client owns the board it gets it ready to ride: upright, loaded with the rider, from rest.
		if ( !_boardTaken && !Board.IsProxy )
		{
			Board.TurnUpright();
			Board.SetRiderAttached( true );
			Board.ResetSimulation();
			_boardTaken = true;
		}
		var rider = Rider.Components.Get<SkateRider>();
		if ( !rider.IsValid() )
			return;
		// The player object stays with the rider, so it is where the rider is when it comes back.
		if ( Player.IsValid() )
			Player.WorldPosition = Rider.WorldPosition;
		GetUpAfterBail( rider );
		if ( !Input.Down( SandboxActions.GetOff ) )
			_useLetGo = true;
		else if ( _useLetGo && rider.Mode == SkateRiderMode.Riding && Input.Pressed( SandboxActions.GetOff ) )
			RequestGetOff();
	}

	// Off the board any way but Use (a bail, or a landing the rider steps off from), the player gets up on foot where the
	// rider fell once its ragdoll has come to rest and it stands, or has lain long enough.
	private void GetUpAfterBail( SkateRider rider )
	{
		if ( rider.Mode == SkateRiderMode.Riding )
		{
			_bailed = 0f;
			return;
		}
		if ( _fellAt is not null || rider.Mode == SkateRiderMode.Bailed && _bailed < GetUpAtLatest )
			return;
		_fellAt = Rider.WorldPosition;
		RequestGetOff();
	}

	// Every client hides the riding player's own body; its own client also takes its controls and follows the rider.
	private void StartRiding( bool mine )
	{
		_riding = true;
		_mine = mine;
		Stow( mine );
		if ( !mine )
			return;
		_fellAt = null;
		_useLetGo = false;
		_boardTaken = false;
		_camera = new GameObject( true, "Skate camera" );
		_camera.NetworkMode = NetworkMode.Never;
		var view = _camera.Components.Create<CameraComponent>();
		view.IsMainCamera = true;
		view.Priority = 100;
		_camera.Components.Create<SkateCamera>().Target = Rider;
	}

	private void StopRiding()
	{
		_riding = false;
		if ( _camera.IsValid() )
			_camera.Destroy();
		_camera = null;
		// The board this client simulates rolls on unridden until it comes to rest.
		if ( _mine && !Board.IsProxy )
		{
			Board.SetInput( default );
			Board.SetRiderAttached( false );
		}
		// The host has already let go of the player by now, so the one put away here is the one given back.
		if ( _mine && _stowedPlayer.IsValid() )
		{
			var side = Rotation.FromYaw( WorldRotation.Yaw() ).Right * GetOffSide;
			_stowedPlayer.WorldPosition = (_fellAt ?? WorldPosition + side) + Vector3.Up * GetOffLift;
		}
		_fellAt = null;
		Unstow();
		_mine = false;
	}

	// The player's own object stays on (the game's menus and stats read it); what would show or collide is switched off
	// on every client, and on its own client what would walk or shoot as well.
	private void Stow( bool mine )
	{
		_stowedComponents.Clear();
		_stowedChildren.Clear();
		_stowedPlayer = Player;
		if ( !Player.IsValid() )
			return;
		foreach ( var component in Player.Components.GetAll() )
		{
			if ( mine && component.Enabled && System.Array.IndexOf( StowedComponentTypes, component.GetType().Name ) >= 0 )
				_stowedComponents.Add( component );
		}
		foreach ( var child in Player.Children )
		{
			if ( child.Enabled && child.Name != KeptChild )
				_stowedChildren.Add( child );
		}
		foreach ( var component in _stowedComponents )
			component.Enabled = false;
		foreach ( var child in _stowedChildren )
			child.Enabled = false;
	}

	private void Unstow()
	{
		foreach ( var child in _stowedChildren )
		{
			if ( child.IsValid() )
				child.Enabled = true;
		}
		foreach ( var component in _stowedComponents )
		{
			if ( component.IsValid() )
				component.Enabled = true;
		}
		_stowedComponents.Clear();
		_stowedChildren.Clear();
		_stowedPlayer = null;
	}

	// Every client dresses its own rider model in the player's clothing, once. The clothing is read while the player's
	// body is put away, so disabled renderers are found too; with no body to read, the rider stays as it is.
	private void Dress()
	{
		if ( !Rider.IsValid() || _dressed == Rider || !Player.IsValid() )
			return;
		var model = Rider.Components.Get<SkateRider>()?.VisualObject;
		var body = model?.Components.Get<SkinnedModelRenderer>( FindMode.EverythingInSelf );
		if ( !body.IsValid() )
			return;
		var own = PlayerBody( Player );
		if ( !own.IsValid() )
			return;
		// The outfit hides the body under its clothes (body groups) and sets the skin: without them the bare body shows
		// through the clothing.
		body.BodyGroups = own.BodyGroups;
		body.MaterialGroup = own.MaterialGroup;
		body.MaterialOverride = own.MaterialOverride;
		body.Tint = own.Tint;
		// Clothing is what the player's body wears, merged onto its bones; held tools are not.
		foreach ( var clothing in own.GameObject.Components.GetAll<SkinnedModelRenderer>( FindMode.EverythingInDescendants ) )
		{
			if ( clothing == own || clothing.Model is null || clothing.BoneMergeTarget != own )
				continue;
			var worn = new GameObject( model, true, clothing.GameObject.Name );
			worn.NetworkMode = NetworkMode.Never;
			var renderer = worn.Components.Create<SkinnedModelRenderer>();
			renderer.Model = clothing.Model;
			renderer.MaterialGroup = clothing.MaterialGroup;
			renderer.MaterialOverride = clothing.MaterialOverride;
			renderer.BodyGroups = clothing.BodyGroups;
			renderer.Tint = clothing.Tint;
			renderer.BoneMergeTarget = body;
		}
		_dressed = Rider;
	}

	private Model BodyOf( GameObject player ) => PlayerBody( player )?.Model ?? FallbackBody;

	// The player's body: the player controller's own renderer, or the first skinned model on the player.
	private static SkinnedModelRenderer PlayerBody( GameObject player ) =>
		player.Components.Get<PlayerController>( FindMode.EverythingInSelfAndDescendants )?.Renderer
		?? player.Components.Get<SkinnedModelRenderer>( FindMode.EverythingInSelfAndDescendants );

	protected override void OnDestroy()
	{
		// A board removed under its rider (undo, cleanup) gives the player back.
		if ( _riding )
			StopRiding();
		if ( (!Networking.IsActive || Networking.IsHost) && Rider.IsValid() )
			Rider.Destroy();
	}
}
