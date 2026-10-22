using System.Collections.Generic;
using System.Linq;
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
	// What the rider was dressed as (RiderOutfit.Signature of the player's body), the pieces put on it, and how long since
	// the player's look was last compared with it: a look that changes (an outfit still loading as the ride began, or
	// changed during it) is copied again.
	private const float DressCompareSeconds = 0.5f;
	private int _dressedAs;
	private List<GameObject> _worn;
	private TimeSince _dressCompared;
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

	// Getting on from where the player stood (no farther than StepOnReach inches from the board), the rider stands there,
	// walks up to the board if it is further than a step and steps onto it, and whether it has been put there yet, and
	// whether it is walking up (for no longer than WalkOnLongest seconds): put on the board at once, the body jumped from
	// where the player stood across onto it.
	private const float StepOnReach = 160f;
	private const float WalkOnLongest = 3f;
	// How hard the stick is pushed for the walk up (a walk, not a run).
	private const float WalkOnPace = 0.45f;
	private bool _stepOn;
	private bool _stepBegun;
	private bool _walkingOn;
	private TimeSince _walkOnSince;

	// The player's body, left showing as the player is put away until the rider's own body shows (at most
	// ShownBodyLongest seconds): put away at once, neither body showed for a fifth of a second while the rider got ready.
	private GameObject _shownBody;
	private TimeSince _shownSince;
	private const float ShownBodyLongest = 1f;

	// Whether the rider has been put where the player was (picking the board up, or stepping onto it), or rides from the
	// start: until then the player and the camera stay where they are.
	private bool Placed => (!CarryStart || _carryBegun) && (!_stepOn || _stepBegun);

	// The rider's body as it was last shown (each bone's place in the world, on every client), for the player's own body to
	// ease across from as it is given back, over TakeOverBlend seconds: shown at once in its own pose, standing where it was
	// put, it jumped 4-12 in in a frame as a throw, a drop or a step off handed back.
	private Transform[] _riderShown;
	private bool _riderShownValid;
	private Transform[] _givenBackFrom;
	private SkinnedModelRenderer _givenBackBody;
	private TimeSince _givenBackSince;
	// Where the body was first drawn given back, and whether it has been yet: the pose eased from goes on across the ground
	// as the body does from there. Left where it was, a body given back walking or running left it behind and was dragged
	// back to it a step a frame; taken along up and down as well, the body's settling from where it is put (GetOffLift)
	// dropped it 3 in.
	private Vector3 _givenBackStart;
	private bool _givenBackInBody;
	// How the rider was walking (inches per second, across the ground) as it was last shown, for the player given back on
	// foot to go on at.
	private Vector3 _riderMoving;

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
		// The rider's pose as last drawn is kept before the host may take the rider off below.
		if ( _riding )
			KeepRiderShown();
		// The board's owner is whoever rode it last, so the host's own duties go by being the host.
		if ( !Networking.IsActive || Networking.IsHost )
			HostUpdate();
		Dress();
		var riding = RiderConnection != Guid.Empty && Rider.IsValid() && Player.IsValid();
		if ( _riding )
			ShowBodyUntilRider();
		if ( riding && !_riding )
			StartRiding( Connection.Local is { } local && local.Id == RiderConnection );
		else if ( !riding && _riding )
			StopRiding();
		if ( _riding )
			EaseFromPlayer();
		EaseBodyBack();
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

	// Every client: the player's body goes on showing, as it was, until the rider's own body is shown, and is put away the
	// frame it is: the two bodies are never both shown, and never neither.
	private void ShowBodyUntilRider()
	{
		if ( !_shownBody.IsValid() )
			return;
		var rider = Rider.IsValid() ? Rider.Components.Get<SkateRider>() : null;
		var shown = rider.IsValid() && rider.VisualObject.IsValid() && rider.VisualObject.Active;
		if ( !shown && _shownSince < ShownBodyLongest )
		{
			// Until then the rider's first poses are set (again each frame) to ease from the body as it is now shown: given
			// once, the ease had mostly run by the time the rider showed, and the body jumped 4 in.
			_takenPose = BodyPose( Player );
			_takenPoseGiven = false;
			return;
		}
		_shownBody.Enabled = false;
		_shownBody = null;
	}

	// Every client, each frame as it is drawn: the bodies swap the frame the rider's shows, and the rider's pose is kept for
	// the player's to ease back from.
	protected override void OnPreRender()
	{
		if ( !_riding )
			return;
		ShowBodyUntilRider();
		KeepRiderShown();
	}

	private void KeepRiderShown()
	{
		// A rider already taken off leaves the pose it was last shown in.
		var rider = Rider.IsValid() ? Rider.Components.Get<SkateRider>() : null;
		if ( !rider.IsValid() )
			return;
		var visual = rider.VisualObject;
		var body = visual.IsValid() && visual.Active ? visual.Components.Get<SkinnedModelRenderer>() : null;
		var scene = body.IsValid() ? body.SceneModel : null;
		if ( scene is null || body.Model is null )
		{
			_riderShownValid = false;
			return;
		}
		if ( _riderShown?.Length != body.Model.BoneCount )
			_riderShown = new Transform[ body.Model.BoneCount ];
		for ( var bone = 0; bone < _riderShown.Length; bone++ )
			_riderShown[ bone ] = scene.GetBoneWorldTransform( bone );
		_riderShownValid = true;
		_riderMoving = rider.Mode is SkateRiderMode.Walking or SkateRiderMode.Carrying ? rider.Walker.Velocity.WithZ( 0f ) : Vector3.Zero;
	}

	// Every client, as the player is given back: its body starts in the rider's last pose, where the rider was, and eases
	// into its own.
	private void GiveBodyBack( GameObject player )
	{
		var body = player.IsValid() ? PlayerBody( player ) : null;
		_givenBackFrom = null;
		if ( _riderShownValid && body.IsValid() && body.Model?.BoneCount == _riderShown.Length )
		{
			_givenBackFrom = new Transform[ _riderShown.Length ];
			System.Array.Copy( _riderShown, _givenBackFrom, _riderShown.Length );
			_givenBackBody = body;
			_givenBackSince = 0f;
			_givenBackInBody = false;
		}
		_riderShownValid = false;
	}

	private void EaseBodyBack()
	{
		if ( _givenBackFrom is null )
			return;
		var body = _givenBackBody;
		var scene = body.IsValid() ? body.SceneModel : null;
		// The body switched back on has its model again from the next frame: the ease starts once it does.
		if ( scene is null && body.IsValid() && !_riding )
		{
			_givenBackSince = 0f;
			return;
		}
		var progress = Math.Clamp( _givenBackSince / TakeOverBlend, 0f, 1f );
		var fade = progress * progress * (3f - 2f * progress);
		if ( scene is null || fade >= 1f || _riding )
		{
			if ( body.IsValid() )
				body.ClearPhysicsBones();
			_givenBackFrom = null;
			_givenBackBody = null;
			return;
		}
		var bones = body.Model.Bones.AllBones;
		// The rider's pose is taken into the frame the model is drawn in this frame, its own pose into the one its animation
		// was last worked out in: just switched back on and moved, the model's own frame is a frame behind, and the body
		// was shown 6-8 in off for a frame and dipped 3 in.
		var drawn = body.WorldTransform;
		var animated = scene.Transform;
		if ( !_givenBackInBody )
		{
			_givenBackStart = drawn.Position;
			_givenBackInBody = true;
		}
		var along = (drawn.Position - _givenBackStart).WithZ( 0f );
		for ( var bone = 0; bone < _givenBackFrom.Length; bone++ )
		{
			var from = drawn.ToLocal( _givenBackFrom[ bone ].WithPosition( _givenBackFrom[ bone ].Position + along ) );
			// Its own pose as its animation last gave it (the first frame, before it has given one, the rider's).
			var to = fade > 0f && body.TryGetBoneTransformAnimation( bones[ bone ], out var own ) ? animated.ToLocal( own ) : from;
			scene.SetBoneOverride( bone, new Transform( Vector3.Lerp( from.Position, to.Position, fade ),
				Rotation.Slerp( from.Rotation, to.Rotation, fade ) ) );
		}
	}

	// Every client: once the rider is up, its first poses ease across from the player's body as it was.
	private void EaseFromPlayer()
	{
		if ( _takenPoseGiven || _takenPose is null )
			return;
		var rider = Rider.Components.Get<SkateRider>();
		if ( !rider.IsValid() || !rider.VisualObject.IsValid() )
			return;
		rider.BlendFromWorld( _takenPose, TakeOverBlend );
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
			if ( CarryStart || _stepOn )
			{
				if ( _stepOn )
					Board.TurnUpright();
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
		// Getting on from beside the board: the rider stands where the player stood, facing as it faced, and steps onto it
		// (or, if it cannot step, gets straight on).
		if ( _stepOn && !_stepBegun && _boardTaken && rider.VisualObject.IsValid() )
		{
			_stepBegun = true;
			if ( !rider.StandAt( _carryFrom ) )
			{
				RequestGetOff();
				return;
			}
			_walkingOn = true;
			_walkOnSince = 0f;
		}
		if ( _walkingOn && !WalkOn( rider ) )
		{
			RequestGetOff();
			return;
		}
		// The skate camera takes the screen once the rider is shown (its model appears with its first pose), and picking the
		// board up once the rider stands where the player stood: sooner, it showed neither the player's body, put away, nor
		// the rider's, and started on the board it swept across from there through the body.
		if ( !_camera.IsValid() && Placed && rider.VisualObject.IsValid() && rider.VisualObject.Active )
			FollowRider();
		// The player object stays with the rider, so it is where the rider is when it comes back (once its own body, shown
		// until the rider's is, is put away: moved sooner, the body shown went with it).
		if ( Player.IsValid() && Placed && !_shownBody.IsValid() )
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
		if ( rider.Mode == SkateRiderMode.Carrying && !rider.IsInteracting && SkateBindings.Toggled( "throw" ) )
			rider.RequestThrow();
	}

	// Walking up to the board to get on: steps on as soon as it can, walking straight at it till then; if it has not got
	// there in WalkOnLongest seconds it gets straight on. False if it cannot get on at all.
	private bool WalkOn( SkateRider rider )
	{
		var mounted = rider.Mode != SkateRiderMode.Walking || rider.RequestMount();
		if ( !mounted && _walkOnSince < WalkOnLongest )
		{
			// It walks itself up (the keys are not read meanwhile), setting off once its body is shown, standing where the
			// player's was.
			var shown = rider.VisualObject.Active;
			if ( !shown )
				_walkOnSince = 0f;
			rider.ReadLocalInput = false;
			rider.SetWalkInput( shown ? (Board.WorldPosition - rider.WorldPosition).WithZ( 0f ).Normal * WalkOnPace : Vector3.Zero );
			return true;
		}
		_walkingOn = false;
		rider.SetWalkInput( Vector3.Zero );
		rider.ReadLocalInput = true;
		return mounted || rider.Mount();
	}

	// The use control (SkateBindings), riding: the rider steps off (the player is handed back once it has, as after any
	// step off), or gets off at once if it cannot. Carrying: a tap throws the board down to ride, a hold drops it.
	private void ReadUse( SkateRider rider )
	{
		if ( !SkateBindings.Held( "use" ) )
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
			if ( !rider.RequestDismount() )
				RequestGetOff();
		}
		else if ( rider.Mode == SkateRiderMode.Carrying && _useHeld >= HoldToCarry && !rider.IsInteracting )
		{
			_useSpent = true;
			rider.RequestDrop();
		}
	}

	// Off the board and not carrying it (a bail, a runout or a step off, the board dropped or thrown), the player gets up
	// on foot where the rider is once what it was doing has played out: a ragdoll come to rest and the get-up from it, a
	// run out, a drop or throw finished to the end of its motion (given back as the board left the hand, the player's body
	// took over mid-throw); or once it has been at it too long.
	private void HandBackWhenDone( SkateRider rider )
	{
		if ( rider.Mode is SkateRiderMode.Riding or SkateRiderMode.Carrying || !Placed || _walkingOn )
		{
			_offBoard = 0f;
			return;
		}
		if ( _fellAt is not null )
			return;
		var busy = rider.Mode == SkateRiderMode.Bailed || rider.IsInteracting || rider.GettingUp || rider.AnimationOverlay is not null;
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
			_stepOn = !CarryStart && Board.WorldPosition.Distance( _carryFrom.Position ) <= StepOnReach
				&& MathF.Abs( Vector3.Dot( Board.WorldRotation.Up, Vector3.Up ) ) >= 0.6f;
			_stepBegun = false;
			_walkingOn = false;
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
		// Q and the number keys are the board's trick keys for the ride, not the spawn menu and the inventory's.
		Systems.RideKeys.Ride();
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
		_camera = new GameObject( true, "Skate camera" );
		_camera.NetworkMode = NetworkMode.Never;
		// It shows its first frame from where the game's view was, until it has placed itself behind the rider.
		if ( _gameCamera.IsValid() )
			_camera.WorldTransform = _gameCamera.WorldTransform;
		// The follow view is shown through the game's own camera, so it keeps the game's look and eases in and out of
		// it (see SkateCameraHandOver); the skate camera's own only holds the follow pose, at the game camera's field of
		// view. With no game camera it shows the view itself.
		var view = _camera.Components.Create<CameraComponent>( !_gameCamera.IsValid() );
		view.IsMainCamera = true;
		view.Priority = 100;
		if ( _gameCamera.IsValid() )
		{
			view.FovAxis = _gameCamera.FovAxis;
			view.FieldOfView = _gameCamera.FieldOfView;
		}
		_camera.Components.Create<SkateCamera>().Target = Rider;
		if ( _gameCamera.IsValid() )
			_camera.Components.Create<SkateCameraHandOver>().TakeOver( _gameCamera, view );
	}

	private void StopRiding()
	{
		_riding = false;
		_takenPose = null;
		ShowHud();
		// The view eases back to the game's camera before the skate camera goes (one made before it did cuts there).
		if ( _camera.IsValid() )
		{
			if ( _camera.Components.Get<SkateCameraHandOver>() is { } handOver )
				handOver.HandBack();
			else
				_camera.Destroy();
		}
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
		var givenBack = _mine && _stowedPlayer.IsValid() ? _stowedPlayer.Components.Get<PlayerController>( FindMode.EverythingInSelfAndDescendants ) : null;
		if ( _mine && _stowedPlayer.IsValid() )
		{
			var side = Rotation.FromYaw( WorldRotation.Yaw() ).Right * GetOffSide;
			_stowedPlayer.WorldPosition = (_fellAt ?? WorldPosition + side) + Vector3.Up * GetOffLift;
			// Switched back on, the controller takes its eyes' and its body's facing from the object's own turn (and sets
			// that back to none): set only on its eyes, the player came back facing the world's north, turning round.
			var facing = _fellAt is not null ? _fellYaw : WorldRotation.Yaw();
			_stowedPlayer.WorldRotation = Rotation.FromYaw( facing );
			if ( givenBack is not null )
				givenBack.EyeAngles = givenBack.EyeAngles.WithYaw( facing );
		}
		_fellAt = null;
		_shownBody = null;
		var player = _stowedPlayer;
		Unstow();
		GiveBodyBack( player );
		// It comes back walking as the rider walked: switched off it kept the speed it had as it got on, and walked on with
		// that, and stopped dead it showed a walking rider halting in a frame (the body and the view both stopping, then
		// setting off again as the keys still held walked it on).
		if ( givenBack is not null )
		{
			if ( givenBack.Body.IsValid() )
				givenBack.Body.Velocity = _riderMoving;
			givenBack.WishVelocity = _riderMoving;
		}
		_riderMoving = Vector3.Zero;
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
		// The body itself (with its clothes) stays shown until the rider's is (see ShowBodyUntilRider).
		var body = PlayerBody( Player );
		_shownBody = body.IsValid() ? _stowedChildren.FirstOrDefault( child => body.GameObject.IsAncestor( child ) ) : null;
		_shownSince = 0f;
		foreach ( var child in _stowedChildren )
		{
			if ( child != _shownBody )
				child.Enabled = false;
		}
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

	// Every client dresses its own rider model as it sees the player's body dressed (RiderOutfit.CopyFrom: clothing, skin,
	// eyes and shape), and again whenever that look changes. The look is read while the player's body is put away, so
	// disabled renderers are found too; with no body to read, the rider stays as it is.
	private void Dress()
	{
		if ( !Rider.IsValid() || !Player.IsValid() || (_dressed == Rider && _dressCompared < DressCompareSeconds) )
			return;
		_dressCompared = 0f;
		var model = Rider.Components.Get<SkateRider>()?.VisualObject;
		var body = model?.Components.Get<SkinnedModelRenderer>( FindMode.EverythingInSelf );
		var own = PlayerBody( Player );
		if ( !body.IsValid() || !own.IsValid() )
			return;
		var look = RiderOutfit.Signature( own );
		if ( _dressed == Rider && look == _dressedAs )
			return;
		_worn ??= new List<GameObject>();
		RiderOutfit.CopyFrom( own, body, _worn );
		_dressed = Rider;
		_dressedAs = look;
	}

	// The player's body pose as it is shown now, each bone's place in the world (for a rider of the same model to ease
	// from); null with no body to read.
	private static Transform[] BodyPose( GameObject player )
	{
		var body = player.IsValid() ? PlayerBody( player ) : null;
		var scene = body?.SceneModel;
		if ( scene is null || body.Model is null )
			return null;
		var pose = new Transform[ body.Model.BoneCount ];
		for ( var bone = 0; bone < pose.Length; bone++ )
			pose[ bone ] = scene.GetBoneWorldTransform( bone );
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
