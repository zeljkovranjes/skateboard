using System.Collections.Generic;
using Sandbox;
using Skateboarding.Engine;
using Skateboarding.Systems;

namespace Skateboarding.Components;

/// <summary>
/// Lets a player get on this board with Use, or pick it up by holding Use: their own body, movement and tools are put
/// away, and a skate rider in their clothes takes their place, driven by their input and followed by the skate camera.
/// Riding, a bail ragdolls the rider and a runout plays out; once either is over the player gets up there on foot, and the
/// board stays where it rolled. Use again gets off, with the player standing beside the board. Carrying, the rider walks
/// with the board in hand: tap Use to throw it down and ride, hold Use to drop it, or attack to throw it ahead; then the
/// player is back on foot. The host decides who rides; the rider's own client owns and simulates the board it rides
/// (keeping it until the board comes to rest and sleeps), so each player pays for their own board only and steers it with
/// no round trip.
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

	/// <summary>The connection riding (or carrying) this board, or empty.</summary>
	[Sync( SyncFlags.FromHost )] public Guid RiderConnection { get; set; }

	/// <summary>The skate rider standing in for the player while it rides or carries the board.</summary>
	[Sync( SyncFlags.FromHost )] public GameObject Rider { get; set; }

	/// <summary>The player object that got on, switched off while it rides.</summary>
	[Sync( SyncFlags.FromHost )] public GameObject Player { get; set; }

	/// <summary>Whether this time the player picked the board up rather than getting on it.</summary>
	[Sync( SyncFlags.FromHost )] public bool CarryStart { get; set; }

	/// <summary>
	/// The connection whose board this is, which can call it back from anywhere by holding Use: whoever spawned it, then
	/// whoever last rode or carried it. Empty while no one has.
	/// </summary>
	[Sync( SyncFlags.FromHost )] public Guid OwnerId { get; set; }

	/// <summary>When (host time) its owner last took it: of several boards of theirs, the latest is the one called back.</summary>
	[Sync( SyncFlags.FromHost )] public float OwnedAt { get; set; }

	/// <summary>Farthest (metres) the owner calls the board back to the hand from; it flies through whatever is between.</summary>
	[Property] public float RecallRange { get; set; } = 2000f;

	/// <summary>Seconds a board called from afar shakes where it lies, rattling, before it flies.</summary>
	[Property] public float RecallShake { get; set; } = 0.6f;

	[RequireComponent] public Skateboard Board { get; set; }

	// Inches beside the board a player is put when they get off, and above the ground wherever they are put.
	private const float GetOffSide = 32f;
	private const float GetOffLift = 4f;

	// Seconds after leaving the board (a bail, a runout, a drop) the player gets up even if that has not played out.
	private const float GetUpAtLatest = 6f;

	// Seconds Use is held to pick the board up (or, carrying it, to drop it) rather than ride.
	private const float HoldToCarry = 0.35f;

	// Seconds the rider's first poses take to ease across from the player's own body as it takes its place.
	private const float TakeOverBlend = 0.3f;

	// The parts of the player switched off while it rides, to switch back on as it gets off: its movement (which also
	// reads its inventory's keys) and physics body, and every child that was on (its body and clothes, colliders, held
	// tool, flashlight) but its voice. The inventory itself stays on: the spawn menu reads the tool gun through it.
	private readonly List<Component> _stowedComponents = new();
	private readonly List<GameObject> _stowedChildren = new();
	private GameObject _stowedPlayer;
	private static readonly string[] StowedComponentTypes = { "PlayerController", "Rigidbody" };
	private const string KeptChild = "Voice";

	private GameObject _camera;
	// The game's own view, switched off while the skate camera has the screen: left where the player's eyes were, it is
	// inside the rider standing there.
	private CameraComponent _gameCamera;

	// The game's HUD parts put away while this client's player rides the board (not while it carries it), by their
	// component's name: its hotbar and health; the rest of the HUD stays as it is. The parts found, and those that were
	// showing as the ride began, which are given back as it ends.
	private static readonly string[] HiddenHud = { "Inventory", "Vitals" };
	private readonly List<GameObject> _hudParts = new();

	// The board's controls card, on a screen panel of its own scaled as the game's HUD is, just above it.
	private GameObject _cardScreen;
	private SkateControlsCard _card;
	private readonly List<GameObject> _hiddenHud = new();
	private bool _hudHidden;
	private GameObject _dressed;
	// Whether this client has put the riding player away, and whether that player is this client's own.
	private bool _riding;
	private bool _mine;
	private TimeSince _offBoard;
	private bool _wasBailed;
	// Where (and facing which way) the player gets up once the rider is done off the board.
	private Vector3? _fellAt;
	private float _fellYaw;

	// The player's body as it was when the rider took its place, for the rider's first poses to ease from.
	private Transform[] _takenPose;

	// The rider whose calls of the board this client sounds for everyone.
	private SkateRider _sounding;
	private bool _takenPoseGiven;

	// A press of Use on the board, tapped to ride or held to pick up.
	private GameObject _pressPlayer;
	private TimeSince _pressed;
	private bool _pressCarried;

	// Use gets on and off: the press that got the rider on is still down as it starts riding, so getting off waits for
	// Use to be let go first. Carrying, how long it has been held tells a tap from a hold.
	private bool _useLetGo;
	private float _useHeld;
	private bool _useSpent;

	// Whether this rider's client has got the board it now owns ready, and (picking it up) where the player stood and
	// whether the rider has gone for the board from there.
	private bool _boardTaken;
	private Transform _carryFrom;
	private bool _carryBegun;

	protected override void OnStart()
	{
		// Spawned, the board lies free with no one on it (so it can fall asleep), until someone gets on.
		if ( !IsProxy && RiderConnection == Guid.Empty )
			Board.SetRiderAttached( false );
		// It is the board of whoever spawned it, when the network says who that was.
		if ( (!Networking.IsActive || Networking.IsHost) && OwnerId == Guid.Empty && GameObject.Network.Owner is { } spawner )
			Claim( spawner );
		// The rider's clips are read once per scene, a moment's work: done as the board appears, not as someone gets on.
		Scene.GetSystem<RiderAnimationLibrary>()?.Get( AnimationClips, AnimationGraph );
	}

	bool IPressable.CanPress( IPressable.Event e ) => RiderConnection == Guid.Empty && PlayerOf( e ).IsValid();

	bool IPressable.Press( IPressable.Event e )
	{
		var player = PlayerOf( e );
		if ( RiderConnection != Guid.Empty || !player.IsValid() )
			return false;
		_pressPlayer = player;
		_pressed = 0f;
		_pressCarried = false;
		return true;
	}

	bool IPressable.Pressing( IPressable.Event e )
	{
		if ( _pressCarried || !_pressPlayer.IsValid() )
			return false;
		if ( _pressed < HoldToCarry )
			return true;
		_pressCarried = true;
		StartCarry( _pressPlayer );
		return false;
	}

	void IPressable.Release( IPressable.Event e )
	{
		if ( !_pressCarried && _pressPlayer.IsValid() )
			StartRide( _pressPlayer );
		_pressPlayer = null;
	}

	IPressable.Tooltip? IPressable.GetTooltip( IPressable.Event e ) =>
		RiderConnection == Guid.Empty ? new IPressable.Tooltip( "Ride", "skateboarding", "Tap to ride, hold to pick up" ) : null;

	// The player object behind whoever pressed: the top of the presser's hierarchy.
	private static GameObject PlayerOf( IPressable.Event e ) => e.Source.IsValid() ? e.Source.GameObject.Root : null;

	/// <summary>Puts the player on the board (asked of the host).</summary>
	internal void StartRide( GameObject player ) => RequestRide( player, false );

	/// <summary>Has the player pick the board up (asked of the host).</summary>
	internal void StartCarry( GameObject player ) => RequestRide( player, true );

	/// <summary>Calls the player's own board back to their hand from wherever it is (asked of the host).</summary>
	internal void Recall( GameObject player ) => RequestRecall( player );

	/// <summary>Host: puts the asking player on the board, or has them pick it up, when no one else has it.</summary>
	[Rpc.Host]
	private void RequestRide( GameObject player, bool carry ) => Take( player, carry, Rpc.Caller ?? Connection.Local );

	/// <summary>Host: has the asking player call the board back to the hand, when it is theirs and no one has it.</summary>
	[Rpc.Host]
	private void RequestRecall( GameObject player )
	{
		var caller = Rpc.Caller ?? Connection.Local;
		if ( caller is null || OwnerId != Guid.Empty && OwnerId != caller.Id )
			return;
		Take( player, true, caller );
	}

	// Host: the player rides the board, or picks it up (from wherever it is, the board flying to the hand); it is theirs
	// from now.
	private void Take( GameObject player, bool carry, Connection caller )
	{
		if ( RiderConnection != Guid.Empty || !player.IsValid() || caller is null )
			return;
		Claim( caller );
		var rider = new GameObject( true, "Skate rider" );
		rider.WorldTransform = new Transform( WorldPosition, Rotation.FromYaw( WorldRotation.Yaw() ) );
		var skate = rider.Components.Create<SkateRider>();
		skate.Board = Board;
		skate.CharacterModel = BodyOf( player );
		skate.AnimationClips = AnimationClips;
		skate.AnimationGraph = AnimationGraph;
		// On foot with the board it handles as the player does on their own: quick to start, stop and turn.
		skate.ResponsiveWalking = true;
		// Picked up from afar the board flies to the hand through whatever is between, like a hammer called home.
		skate.RecallRange = RecallRange;
		skate.RecallNeedsSight = false;
		skate.RecallShake = RecallShake;
		rider.NetworkSpawn( caller );
		// The rider's client simulates the board from here (getting it ready once it owns it); if that player leaves, the
		// host takes the board back rather than it going with them.
		Board.GameObject.Network.SetOrphanedMode( NetworkOrphaned.Host );
		Board.GameObject.Network.AssignOwnership( caller );
		Board.SetDriver( caller );
		CarryStart = carry;
		Rider = rider;
		Player = player;
		RiderConnection = caller.Id;
	}

	// The board called to its rider's hand: every client makes the sound itself (no sound file), following the board.
	[Rpc.Broadcast]
	private void SoundCall( float shake, float flight, int seed ) => RecallSound.Play( Board.GameObject, shake, flight, seed );

	private void Claim( Connection owner )
	{
		OwnerId = owner.Id;
		OwnedAt = Time.Now;
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
		CarryStart = false;
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
		if ( _riding )
			EaseFromPlayer();
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

	// Every client: once the rider is up, its first poses ease across from the player's body as it was.
	private void EaseFromPlayer()
	{
		if ( _takenPoseGiven || _takenPose is null )
			return;
		var rider = Rider.Components.Get<SkateRider>();
		if ( !rider.IsValid() || !rider.VisualObject.IsValid() )
			return;
		rider.BlendFrom( _takenPose, TakeOverBlend );
		_takenPoseGiven = true;
	}

	// The riding player's own client.
	private void RideUpdate()
	{
		var rider = Rider.Components.Get<SkateRider>();
		if ( !rider.IsValid() )
			return;
		// Once this client owns the board it gets it ready: to ride (upright, loaded with the rider, from rest), or to be
		// picked up where it lies.
		if ( !_boardTaken && !Board.IsProxy )
		{
			if ( CarryStart )
			{
				Board.SetRiderAttached( false );
			}
			else
			{
				Board.TurnUpright();
				Board.SetRiderAttached( true );
				Board.ResetSimulation();
			}
			_boardTaken = true;
		}
		// Picking the board up: the rider stands where the player stood, facing as it faced, and reaches for the board.
		if ( CarryStart && !_carryBegun && _boardTaken && rider.VisualObject.IsValid() )
		{
			_carryBegun = true;
			if ( !rider.StandAt( _carryFrom ) )
			{
				RequestGetOff();
				return;
			}
			if ( !rider.RequestPickUp() )
			{
				_fellAt = _carryFrom.Position;
				_fellYaw = _carryFrom.Rotation.Yaw();
				RequestGetOff();
				return;
			}
		}
		// The skate camera takes the screen once the rider is shown (its model appears with its first pose), and picking the
		// board up once the rider stands where the player stood: sooner, it showed neither the player's body, put away, nor
		// the rider's, and started on the board it swept across from there through the body.
		if ( !_camera.IsValid() && (!CarryStart || _carryBegun) && rider.VisualObject.IsValid() && rider.VisualObject.Active )
			FollowRider();
		// The player object stays with the rider, so it is where the rider is when it comes back.
		if ( Player.IsValid() && (!CarryStart || _carryBegun) )
			Player.WorldPosition = Rider.WorldPosition;
		if ( _sounding != rider )
		{
			_sounding = rider;
			rider.BoardCalled += ( shake, flight ) => SoundCall( shake, flight, Game.Random.Int( 0, int.MaxValue - 1 ) );
		}
		UpdateHud( rider.Mode == SkateRiderMode.Riding );
		UpdateCard( rider );
		HandBackWhenDone( rider );
		ReadUse( rider );
		if ( rider.Mode == SkateRiderMode.Carrying && !rider.IsInteracting && Input.Pressed( SandboxActions.Catch ) )
			rider.RequestThrow();
	}

	// Use, riding: gets off at once. Carrying: a tap throws the board down to ride, a hold drops it.
	private void ReadUse( SkateRider rider )
	{
		if ( !Input.Down( SandboxActions.GetOff ) )
		{
			if ( _useLetGo && _useHeld > 0f && !_useSpent && rider.Mode == SkateRiderMode.Carrying && !rider.IsInteracting )
				rider.RequestMount();
			_useLetGo = true;
			_useHeld = 0f;
			_useSpent = false;
			return;
		}
		if ( !_useLetGo || _useSpent )
			return;
		_useHeld += Time.Delta;
		if ( rider.Mode == SkateRiderMode.Riding && !rider.IsInteracting )
		{
			_useSpent = true;
			RequestGetOff();
		}
		else if ( rider.Mode == SkateRiderMode.Carrying && _useHeld >= HoldToCarry && !rider.IsInteracting )
		{
			_useSpent = true;
			rider.RequestDrop();
		}
	}

	// Off the board and not carrying it (a bail, a runout or a step off, the board dropped or thrown), the player gets up
	// on foot where the rider is once what it was doing has played out: a ragdoll come to rest, a run out, a drop or throw
	// finished; or once it has been at it too long.
	private void HandBackWhenDone( SkateRider rider )
	{
		if ( rider.Mode is SkateRiderMode.Riding or SkateRiderMode.Carrying || CarryStart && !_carryBegun )
		{
			_offBoard = 0f;
			return;
		}
		if ( _fellAt is not null )
			return;
		var busy = rider.Mode == SkateRiderMode.Bailed || rider.IsInteracting;
		if ( busy && _offBoard < GetUpAtLatest )
			return;
		_fellAt = Rider.WorldPosition;
		_fellYaw = Rider.WorldRotation.Yaw();
		RequestGetOff();
	}

	// Every client hides the riding player's own body (having taken its pose for the rider to ease from); its own client
	// also takes its controls and follows the rider.
	private void StartRiding( bool mine )
	{
		_riding = true;
		_mine = mine;
		_takenPose = BodyPose( Player );
		_takenPoseGiven = false;
		if ( mine )
		{
			var body = PlayerBody( Player );
			_carryFrom = new Transform( Player.WorldPosition, Rotation.FromYaw( (body.IsValid() ? body.WorldRotation : Player.WorldRotation).Yaw() ) );
		}
		Stow( mine );
		if ( !mine )
			return;
		_fellAt = null;
		_useLetGo = false;
		_useHeld = 0f;
		_useSpent = false;
		_boardTaken = false;
		_carryBegun = false;
		_offBoard = 0f;
	}

	// Riding, the hotbar and health are kept put away: the game can switch them back on at any time (its menus do), so
	// they are put away again every frame of the ride. Off the board (carrying it, stepping off, a bail) they come back.
	private void UpdateHud( bool riding )
	{
		if ( !riding )
		{
			ShowHud();
			return;
		}
		if ( !_hudHidden || _hudParts.Exists( part => !part.IsValid() ) )
		{
			FindHud();
			_hiddenHud.Clear();
			_hiddenHud.AddRange( _hudParts.FindAll( part => part.Enabled ) );
			_hudHidden = true;
		}
		foreach ( var part in _hudParts )
		{
			if ( part.IsValid() && part.Enabled )
				part.Enabled = false;
		}
	}

	private void UpdateCard( SkateRider rider )
	{
		if ( !_cardScreen.IsValid() )
		{
			_cardScreen = new GameObject( true, "Skate controls" );
			_cardScreen.NetworkMode = NetworkMode.Never;
			var screen = _cardScreen.Components.Create<ScreenPanel>();
			screen.ZIndex = 101;
			screen.ScaleStrategy = ScreenPanel.AutoScale.FollowDesktopScaling;
		}
		if ( _card is null || !_card.IsValid )
		{
			var root = _cardScreen.Components.Get<ScreenPanel>()?.GetPanel();
			if ( root is null )
				return;
			_card = new SkateControlsCard { Parent = root };
		}
		var speed = rider.Mode == SkateRiderMode.Riding
			? Board.State.Speed
			: rider.Walker.Velocity.WithZ( 0f ).Length / SkateUnits.InchesPerMetre;
		_card.Tick( rider.Mode, speed );
	}

	private void FindHud()
	{
		_hudParts.Clear();
		foreach ( var panel in Scene.GetAllComponents<ScreenPanel>() )
		{
			foreach ( var part in panel.GameObject.Children )
			{
				foreach ( var component in part.Components.GetAll<Component>( FindMode.EverythingInSelf ) )
				{
					if ( System.Array.IndexOf( HiddenHud, component.GetType().Name ) < 0 )
						continue;
					_hudParts.Add( part );
					break;
				}
			}
		}
	}

	private void ShowHud()
	{
		if ( !_hudHidden )
			return;
		foreach ( var part in _hiddenHud )
		{
			if ( part.IsValid() )
				part.Enabled = true;
		}
		_hiddenHud.Clear();
		_hudHidden = false;
	}

	private void FollowRider()
	{
		if ( _camera.IsValid() )
			return;
		_gameCamera = Scene.Camera;
		if ( _gameCamera.IsValid() )
			_gameCamera.Enabled = false;
		_camera = new GameObject( true, "Skate camera" );
		_camera.NetworkMode = NetworkMode.Never;
		// It shows its first frame from where the game's view was, until it has placed itself behind the rider.
		if ( _gameCamera.IsValid() )
			_camera.WorldTransform = _gameCamera.WorldTransform;
		var view = _camera.Components.Create<CameraComponent>();
		view.IsMainCamera = true;
		view.Priority = 100;
		_camera.Components.Create<SkateCamera>().Target = Rider;
	}

	private void StopRiding()
	{
		_riding = false;
		_takenPose = null;
		ShowHud();
		if ( _camera.IsValid() )
			_camera.Destroy();
		_camera = null;
		if ( _cardScreen.IsValid() )
			_cardScreen.Destroy();
		_cardScreen = null;
		_card = null;
		if ( _gameCamera.IsValid() )
			_gameCamera.Enabled = true;
		_gameCamera = null;
		// The board this client simulates rolls on unridden until it comes to rest.
		if ( _mine && !Board.IsProxy )
		{
			Board.SetInput( default );
			Board.SetRiderAttached( false );
		}
		// The host has already let go of the player by now, so the one put away here is the one given back: on foot where
		// the rider was, facing its way, or beside the board having stepped off it.
		if ( _mine && _stowedPlayer.IsValid() )
		{
			var side = Rotation.FromYaw( WorldRotation.Yaw() ).Right * GetOffSide;
			_stowedPlayer.WorldPosition = (_fellAt ?? WorldPosition + side) + Vector3.Up * GetOffLift;
			if ( _fellAt is not null && _stowedPlayer.Components.Get<PlayerController>( FindMode.EverythingInSelfAndDescendants ) is { } controller )
				controller.EyeAngles = controller.EyeAngles.WithYaw( _fellYaw );
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
		// Put away, the controller no longer clears what it was looking at: the board's "tap to ride" would stay on screen.
		if ( Player.Components.Get<PlayerController>( FindMode.EverythingInSelf ) is { } controller )
		{
			controller.Tooltips.Clear();
			controller.Hovered = null;
		}
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

	// The player's body pose as it is shown now, each bone in the body model's own space (what a rider of the same model
	// overrides its bones with); null with no body to read.
	private static Transform[] BodyPose( GameObject player )
	{
		var body = player.IsValid() ? PlayerBody( player ) : null;
		var scene = body?.SceneModel;
		if ( scene is null || body.Model is null )
			return null;
		var pose = new Transform[ body.Model.BoneCount ];
		for ( var bone = 0; bone < pose.Length; bone++ )
			pose[ bone ] = scene.Transform.ToLocal( scene.GetBoneWorldTransform( bone ) );
		return pose;
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
